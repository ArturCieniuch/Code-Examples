using System;
using SimpleJSON;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BackOfficeCommunication.Types;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;

public class SpriteLoader : MonoBehaviour {
    public static SpriteLoader instance;

    //Sprite displayed when unable to load image
    public Sprite defaultSprite;

    //Sprite displayed while loading image
    public Sprite loadSprite;

    private static Dictionary<Image, ImageData> imageData;

    private static Dictionary<string, List<Image>> imageList;
    private static Dictionary<string, Sprite> spriteCache;
    private List<CoroutineData> spriteQueue;
    private Coroutine spriteQueueHandler;

    private class ImageData {
        public Coroutine coroutine;
        public UnityWebRequest request;
    }

    private class CoroutineData {
        public string path;
        public Image image;
        public UnityAction<Sprite> callback = null;
        public Vector2 pivot;

        public CoroutineData(string path, Image image, UnityAction<Sprite> callback, Vector2 pivot) {
            this.path = path;
            this.image = image;
            this.callback = callback;
            this.pivot = pivot;
        }
    }

    private static int cleaningCycleCounter = 0;
    private static int maxLoadsBeforeCleaning = 5;

    private void Awake() {
        instance = this;
        imageData = new Dictionary<Image, ImageData>();
        imageList = new Dictionary<string, List<Image>>();
        spriteCache = new Dictionary<string, Sprite>();
        spriteQueue = new List<CoroutineData>();
    }

    public void clear() {
        if (spriteQueueHandler != null) {
            StopCoroutine(spriteQueueHandler);
            spriteQueueHandler = null;
        }

        List<string> keys = spriteCache.Keys.ToList();

        foreach (string path in keys) {
            Destroy(spriteCache[path].texture);
            Destroy(spriteCache[path]);
        }

        imageData.Clear();
        imageList.Clear();
        spriteCache.Clear();
        spriteQueue.Clear();
    }

    /// <summary>
    /// Loads and caches sprite from disk
    /// </summary>
    /// <param name="path">path to texture</param>
    /// <param name="image">Image that will be using sprite</param>
    /// <param name="callback">Callback on sprite loaded</param>
    /// <param name="pivot">Pivot for the loaded sprite</param>
    /// <param name="isLoadingThumbnail">Is it loading thumbnail of a sprite first</param>
    public void loadSprite(string path, Image image, UnityAction<Sprite> callback = null, Vector2 pivot = default(Vector2), bool isLoadingThumbnail = true) {
        if (image == null || string.IsNullOrEmpty(path)) {
            Debug.LogWarning("ERROR LOADING SPRITE: EMPTY PATH OR IMAGE");
            return;
        }

        if (imageData.ContainsKey(image)) {
            if (imageData[image].coroutine != null) {
                StopCoroutine(imageData[image].coroutine);
                imageData[image].coroutine = null;

                imageData[image].request.Dispose();
                imageData[image].request = null;
            }
        } else {
            imageData.Add(image, new ImageData());
        }

        // it crashed when we didn't check for existing of the key inside imageList
        if (spriteCache.ContainsKey(path) && imageList.ContainsKey(path) && spriteCache[path].pivot == pivot) {
            imageList[path].Add(image);
            image.sprite = spriteCache[path];
            image.overrideSprite = null;

            AspectRatioFitter aspectRatioFitter = image.GetComponent<AspectRatioFitter>();

            if (aspectRatioFitter != null) {
                float aspectRation = (float) image.sprite.texture.width / (float) image.sprite.texture.height;
                aspectRatioFitter.aspectRatio = aspectRation;
            }

            callback?.Invoke(spriteCache[path]);
            return;
        }

        if (isLoadingThumbnail) {
            imageData[image].coroutine = StartCoroutine(loadSpriteCoroutine(path, image, callback, pivot));
        } else {
            imageData[image].coroutine = StartCoroutine(loadFullSpriteCoroutine(path, image, callback, pivot));
        }
    }

    /// <summary>
    /// Check if thera are any sprites that are unused and removed them
    /// </summary>
    public void checkAndClearCache() {
        List<string> keys = imageList.Keys.ToList();

        foreach (string path in keys) {
            bool isSpriteInUse = false;

            foreach (Image image in imageList[path]) {
                if (image != null && image.sprite == spriteCache[path]) {
                    isSpriteInUse = true;
                    break;
                }
            }

            if (!isSpriteInUse) {
                Destroy(spriteCache[path].texture);
                Destroy(spriteCache[path]);

                imageList.Remove(path);
                spriteCache.Remove(path);
            }
        }
    }

    private IEnumerator loadSpriteCoroutine(string path, Image image, UnityAction<Sprite> callback = null, Vector2 pivot = default(Vector2)) {
        string protocol = string.Empty;

        if (BackOfficeDataCache.instance.isWallApp) {
            protocol = "file://";
        }

        Sprite sprite = defaultSprite;
        image.preserveAspect = true;
        image.overrideSprite = loadSprite;
        string originalPath = path;
        bool isThumbnail = true;

        //Forced loading sprite thumbnail first
        if (!path.Contains("-300x300.") && !string.IsNullOrEmpty(Path.GetExtension(path))) {
            string fileExtension = Path.GetExtension(path);
            path = path.Replace(fileExtension, $"-300x300.jpg");
            isThumbnail = false;
        }

        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(protocol + path, true)) {
            imageData[image].request = www;

            www.timeout = 30;
#if !UNITY_WEBGL || UNITY_EDITOR
            www.SetRequestHeader("Cookie", $"ci_session={UserDetailsManager.sessionId}");
#endif

            yield return www.SendWebRequest();

            if (!string.IsNullOrEmpty(www.error)) {
                Debug.LogWarning(www.error);
                Debug.LogWarningFormat("File does not exist: {0}", path);
                if (image != null) {
                    image.sprite = sprite;
                    image.overrideSprite = null;
                    callback?.Invoke(sprite);

                    if (!isThumbnail) {
                        StartCoroutine(loadFullSpriteCoroutine(originalPath, image, callback, pivot));
                    }
                }

                www.Dispose();
                yield break;
            }

            Texture2D tex = DownloadHandlerTexture.GetContent(www);

            if (tex == null || (tex.width <= 8 && tex.height <= 8)) {
                Debug.Log($"NO TEXTURE: {protocol + path}");
                if (image != null) {
                    image.sprite = sprite;
                    image.overrideSprite = null;
                    callback?.Invoke(sprite);
                }

                Destroy(tex);

                www.Dispose();
                yield break;
            }

            if (spriteCache.ContainsKey(path)) {
                Destroy(tex);
                sprite = spriteCache[path];
            } else {
                //Add sprite to cache
                sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, 100, 0, SpriteMeshType.FullRect);
                spriteCache.Add(path, sprite);
            }

            if (image != null) {
                if (!imageList.ContainsKey(path)) {
                    imageList.Add(path, new List<Image>());
                }

                imageList[path].Add(image);

                image.sprite = sprite;
                image.overrideSprite = null;

                AspectRatioFitter aspectRatioFitter = image.GetComponent<AspectRatioFitter>();

                if (aspectRatioFitter != null) {
                    float aspectRation = (float) image.sprite.texture.width / (float) image.sprite.texture.height;
                    aspectRatioFitter.aspectRatio = aspectRation;
                }

                //If is not thumbnail add full size sprite to load queue
                if (!isThumbnail) {
                    spriteQueue.Add(new CoroutineData(originalPath, image, callback, pivot));

                    if (spriteQueueHandler == null) {
                        spriteQueueHandler = StartCoroutine(handleSpriteQueue());
                    }
                }

                callback?.Invoke(sprite);
            }

            www.Dispose();

            //After "maxLoadsBeforeCleaning" loaded sprites run clenning function
            if (++cleaningCycleCounter >= maxLoadsBeforeCleaning) {
                yield return null;

                cleaningCycleCounter = 0;
                checkAndClearCache();
            }
        }
    }

    /// <summary>
    /// Loads queue of full size sprites
    /// </summary>
    /// <returns></returns>
    private IEnumerator handleSpriteQueue() {
        while (spriteQueue.Count > 0) {
            yield return loadFullSpriteCoroutine(spriteQueue[0].path, spriteQueue[0].image, spriteQueue[0].callback, spriteQueue[0].pivot);
            spriteQueue.RemoveAt(0);
        }

        spriteQueueHandler = null;
    }

    private IEnumerator loadFullSpriteCoroutine(string path, Image image, UnityAction<Sprite> callback = null, Vector2 pivot = default(Vector2)) {
        if (spriteCache.ContainsKey(path) && imageList.ContainsKey(path) && spriteCache[path].pivot == pivot) {
            imageList[path].Add(image);
            image.sprite = spriteCache[path];
            image.overrideSprite = null;

            AspectRatioFitter aspectRatioFitter = image.GetComponent<AspectRatioFitter>();

            if (aspectRatioFitter != null) {
                float aspectRation = (float) image.sprite.texture.width / (float) image.sprite.texture.height;
                aspectRatioFitter.aspectRatio = aspectRation;
            }

            callback?.Invoke(spriteCache[path]);
            yield break;
        }

        string protocol = string.Empty;

        if (BackOfficeDataCache.instance.isWallApp) {
            protocol = "file://";
        }

        Sprite sprite = defaultSprite;
        image.preserveAspect = true;
        //image.overrideSprite = loadSprite;

        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(protocol + path, true)) {
            imageData[image].request = www;

            www.timeout = 30;
#if !UNITY_WEBGL || UNITY_EDITOR
            www.SetRequestHeader("Cookie", $"ci_session={UserDetailsManager.sessionId}");
#endif

            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError) {
                //Send error report
                JSONObject errorParams = new JSONObject();
                errorParams["file_name"] = Path.GetFileName(path);
                WallErrorReporter.instance.wallAppErrorReport(ErrorReportCode.imageLoadError, (int) www.responseCode, www.error, errorParams);

                Debug.LogWarning(www.error);
                Debug.LogWarningFormat("File does not exist: {0}", path);

                if (image != null) {
                    image.sprite = sprite;
                    image.overrideSprite = null;
                    callback?.Invoke(sprite);
                }

                www.Dispose();
                yield break;
            }

            Texture2D tex = DownloadHandlerTexture.GetContent(www);

            if (tex == null || (tex.width <= 8 && tex.height <= 8)) {
                //Send error report
                JSONObject errorParams = new JSONObject();
                errorParams["file_name"] = Path.GetFileName(path);
                WallErrorReporter.instance.wallAppErrorReport(ErrorReportCode.imageLoadError, -1, "No texture", errorParams);

                Debug.Log($"NO TEXTURE: {protocol + path}");
                if (image != null) {
                    image.sprite = sprite;
                    image.overrideSprite = null;
                    callback?.Invoke(sprite);
                }

                Destroy(tex);

                www.Dispose();
                yield break;
            }

            if (spriteCache.ContainsKey(path)) {
                Destroy(tex);
                sprite = spriteCache[path];
            } else {
                //Add sprite to cache
                sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, 100, 0, SpriteMeshType.FullRect);
                spriteCache.Add(path, sprite);
            }

            if (image != null) {
                if (!imageList.ContainsKey(path)) {
                    imageList.Add(path, new List<Image>());
                }

                imageList[path].Add(image);

                image.sprite = sprite;
                image.overrideSprite = null;

                AspectRatioFitter aspectRatioFitter = image.GetComponent<AspectRatioFitter>();

                if (aspectRatioFitter != null) {
                    float aspectRation = (float) image.sprite.texture.width / (float) image.sprite.texture.height;
                    aspectRatioFitter.aspectRatio = aspectRation;
                }

                callback?.Invoke(sprite);
            }

            www.Dispose();

            //After "maxLoadsBeforeCleaning" loaded sprites run clenning function
            if (++cleaningCycleCounter >= maxLoadsBeforeCleaning) {
                yield return null;

                cleaningCycleCounter = 0;
                checkAndClearCache();
            }
        }
    }

    public void setDefaultSprite(Image image) {
        image.sprite = defaultSprite;
        AspectRatioFitter aspectRatioFitter = image.GetComponent<AspectRatioFitter>();

        if (aspectRatioFitter != null) {
            float aspectRation = (float) image.sprite.texture.width / (float) image.sprite.texture.height;
            aspectRatioFitter.aspectRatio = aspectRation;
        }
    }

    /// <summary>
    /// gets sprite and sets appropriate mask and crop node to the sprite but uses the media information mostly from interface
    /// </summary>
    /// <param name="path"></param>
    /// <param name="targetMask"></param>
    /// <param name="targetImage"></param>
    /// <param name="media"></param>
    /// <param name="onComplete"></param>
    public void loadSpriteAndSetCrop(string path, RectTransform targetMask, Image targetImage, BOPropertyTypeMedia media, UnityAction onComplete = null) {
        loadSprite(path, targetImage, (sprite) => {
            RectTransform imageRect = targetImage.transform as RectTransform;

            imageRect.sizeDelta = new Vector2(media.width, media.height);
            imageRect.anchoredPosition = new Vector2(media.positionX, media.positionY);
            imageRect.localScale = new Vector3(media.scaleX, media.scaleY, 1);
            imageRect.localRotation = Quaternion.Euler(0, 0, media.rotation);
            if (onComplete != null) {
                onComplete();
            }
        });
    }

    /// <summary>
    /// gets sprite and sets appropriate mask and crop node to the sprite
    /// </summary>
    /// <param name="path"></param>
    /// <param name="targetMask"></param>
    /// <param name="targetImage"></param>
    /// <param name="mediaNode"></param>
    /// <param name="cropNode"></param>
    /// <param name="onComplete"></param>
    public void loadSpriteAndSetCrop(string path, RectTransform targetMask, Image targetImage, JSONNode mediaNode, JSONNode cropNode, UnityAction onComplete = null) {
        loadSprite(path, targetImage, (sprite) => {
            RectTransform imageRect = targetImage.transform as RectTransform;

            imageRect.sizeDelta = new Vector2(mediaNode["width"].AsInt, mediaNode["height"].AsInt);
            imageRect.anchoredPosition = new Vector2(cropNode["position"]["x"].AsFloat, cropNode["position"]["y"].AsFloat);
            if (cropNode.Exists("scale")) {
                imageRect.localScale = new Vector3(cropNode["scale"]["x"].AsFloat, cropNode["scale"]["y"].AsFloat, 1);
            } else {
                imageRect.localScale = Vector3.one;
            }

            imageRect.localRotation = Quaternion.Euler(0, 0, cropNode["rotation"]["z"].AsFloat);
            if (onComplete != null) {
                onComplete();
            }
        });
    }

    /// <summary>
    /// gets media path for given media json
    /// </summary>
    /// <param name="media"></param>
    /// <returns></returns>
    public static string getMediaPath(JSONNode media) {
        if (!BackOfficeDataCache.instance.isWallApp) {
            return getMediaThumbnailPath(media);
        } else {
            return $"{WallScript.instance.persistentDataPath}{media["directory"].Value}/{media["s3_filename"].Value}";
        }
    }

    /// <summary>
    /// gets media path for given media json
    /// </summary>
    /// <param name="media"></param>
    /// <returns></returns>
    public static string getMediaThumbnailPath(JSONNode media) {
        if (BackOfficeDataCache.instance.isWallApp) {
            string fileExtension = Path.GetExtension(media["s3_filename"].Value);
            string fileName = Path.GetFileName(media["s3_filename"].Value).Replace(fileExtension, $"-300x300{fileExtension}");
            return $"{WallScript.instance.persistentDataPath}{media["directory"].Value}/{fileName}";
        } else {
            return media["thumbnail_url"];
        }
    }

    /// <summary>
    /// gets video media preview (jpg file)
    /// </summary>
    /// <param name="videoNode"></param>
    /// <returns></returns>
    public static string getVideoPreviewPath(JSONNode videoNode) {
        if (!BackOfficeDataCache.instance.isWallApp) {
            return videoNode["thumbnail_url"];
        } else {
            return $"{WallScript.instance.persistentDataPath}{videoNode["directory"].Value}/{videoNode["s3_filename"].Value}" + ".jpg";
        }
    }

    public static bool isMedia360(JSONNode media) {
        return media["360_photo"] != null && media["360_photo"].AsBool;
    }

    public static int getMediaIdFromPath(string mediaPath) {
        if (string.IsNullOrEmpty(mediaPath)) {
            return -1;
        }

        int mediaIdStartIndex = mediaPath.LastIndexOf("/");
        int mediaIdSEndIndex = mediaPath.LastIndexOf(".");
        int mediaIdLength = mediaIdSEndIndex - mediaIdStartIndex;

        int mediaId = -1;

        if (mediaIdStartIndex == -1 || mediaIdSEndIndex == -1 || mediaIdLength == 0) {
            Debug.LogError($"Could not extract media id from media path: {mediaPath}");
            return mediaId;
        }

        mediaIdStartIndex++;
        mediaIdLength--;

        try {
            mediaId = int.Parse(mediaPath.Substring(mediaIdStartIndex, mediaIdLength));
        } catch (Exception e) {
            Debug.LogError($"Could not extract media id from media path: {mediaPath}. Exception: {e.Message}");
        }

        return mediaId;
    }
}