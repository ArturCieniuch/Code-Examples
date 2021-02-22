using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class WallScreenShotManager : MonoBehaviour {
    public static WallScreenShotManager instance;

    public bool wallScreenShot, tileScreenShot;

    public RectTransform wallArea;
    public RectTransform background;

    public UnityAction<byte[]> wallScreenShotCallback;
    public UnityAction<Texture2D> tileScreenShotCallback;

    private Tile tile;
    private RawImage tilePreview;

    private void Awake() {
        instance = this;
    }

    /// <summary>
    /// sets flag for taking wall screenshot which is taken in OnPostRender function
    /// </summary>
    public void takeWallScreenShot(UnityAction<byte[]> _wallScreenShotCallback) {
        wallScreenShotCallback = _wallScreenShotCallback;
        wallScreenShot = true;
    }

    /// <summary>
    /// sets flag for taking tile screenshot which is taken in OnPostRender function
    /// </summary>
    public void takeTileScreenShot(Tile tile, UnityAction<Texture2D> _tileScreenshotCallback) {
        this.tile = tile;
        tileScreenShotCallback = _tileScreenshotCallback;
        tileScreenShot = true;
    }

    /// <summary>
    /// function which checks takeWallScreenshot flag and take if takeWallScreenshot is true
    /// </summary>
    private IEnumerator OnPostRender() {
        if (wallScreenShot) {
            SystemPopupController.instance.setPopupCanvasState(false);
            WallManager.setTilesAlpha(1f);
            PanelManager.instance.controlPopup.gameObject.SetActive(false);
            wallScreenShot = false;

            Camera.current.cullingMask &= ~(1 << LayerMask.NameToLayer("UI Wall Creator"));

            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            Vector2 size = getRectSize(wallArea);

            var tex = new Texture2D((int)size.x, (int)size.y, TextureFormat.RGB24, false);

            Vector2 rectPos = getScreenPoint(background.position);
            float startX = rectPos.x - size.x / 2f;
            float startY = rectPos.y - size.y / 2f;

            startX = (float)Math.Round(startX, 2);
            startY = (float)Math.Round(startY, 2);

            Rect screenShotAreaRect = new Rect(Mathf.CeilToInt(startX), Mathf.CeilToInt(startY), Mathf.FloorToInt(size.x), Mathf.FloorToInt(size.y));

            tex.ReadPixels(screenShotAreaRect, 0, 0);
            tex.Apply();

            Camera.current.cullingMask |= 1 << LayerMask.NameToLayer("UI Wall Creator");

            // Encode texture into PNG
            byte[] bytes = ImageConversion.EncodeToJPG(tex, 60);
            Destroy(tex);

            PanelManager.instance.controlPopup.gameObject.SetActive(true);
            SystemPopupController.instance.setPopupCanvasState(true);

            wallScreenShotCallback?.Invoke(bytes);
        }

        if (tileScreenShot) {
            SystemPopupController.instance.setPopupCanvasState(false);
            PanelManager.instance.controlPopup.gameObject.SetActive(false);
            tileScreenShot = false;

            Camera.current.cullingMask &= ~(1 << LayerMask.NameToLayer("UI Wall Creator"));

            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            RectTransform tileRectTransform = tile.GetComponent<RectTransform>();
            Vector2 size = getRectSize(tileRectTransform);

            var tex = new Texture2D((int)size.x, (int)size.y, TextureFormat.RGB24, false);

            Vector2 rectPos = getScreenPoint(tileRectTransform.position);
            float startX = rectPos.x - size.x / 2;
            float startY = rectPos.y - size.y / 2;

            Rect screenShotAreaRect = new Rect(startX, startY, size.x, size.y);

            tex.ReadPixels(screenShotAreaRect, 0, 0);
            tex.Apply();

            Camera.current.cullingMask |= 1 << LayerMask.NameToLayer("UI Wall Creator");

            PanelManager.instance.controlPopup.gameObject.SetActive(true);
            SystemPopupController.instance.setPopupCanvasState(true);

            tileScreenShotCallback?.Invoke(tex);
        }
    }

    private Vector2 getRectSize(RectTransform rect) {
        return new Vector2(rect.sizeDelta.x * rect.lossyScale.x, rect.sizeDelta.y * rect.lossyScale.y);
    }

    private Vector2 getScreenPoint(Vector3 position) {
        return RectTransformUtility.WorldToScreenPoint(Camera.main, position);
    }
}