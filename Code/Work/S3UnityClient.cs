using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Boo.Lang;
using UnityEngine;

public class S3UnityClient : MonoBehaviour {


    [SerializeField]
    private bool debug;

    [SerializeField]
    private string _awsKey = "";

    [SerializeField]
    private string _awsSecret = "";

    [SerializeField]
    private string _awsBucket = "";

    [SerializeField]
    private string _awsRegion = "";

    [SerializeField]
    private string _awsMywallUpdate = "";

    private RegionEndpoint awsRegion = RegionEndpoint.EUWest1;

    private AmazonS3Client client;
    private bool isInitDone;
    private bool isDownloadingStopped;
    private int garbageCollectorDownloadCount;
    private int garbageCollectorDownloadLimit = 5;
    private int slowModePackSize = 102400;
    private int normalModePackSize = 5242880;

    public string getBucket() {
        return _awsBucket;
    }

    public string getRegion() {
        return _awsRegion;
    }

    public void initialize(string awsKey, string awsSecret, string aswBucket, string awsCustomRegion, string awsMywallUpdate) {
        _awsKey = awsKey;
        _awsSecret = awsSecret;
        _awsBucket = aswBucket;
        _awsMywallUpdate = awsMywallUpdate;
        _awsRegion = awsCustomRegion;

        setRegion();

        AmazonS3Config config = new AmazonS3Config();

        config.Timeout = TimeSpan.FromSeconds(10);
        config.ReadWriteTimeout = TimeSpan.FromSeconds(30);

        config.RegionEndpoint = awsRegion;

        client = new AmazonS3Client(_awsKey, _awsSecret, config);

        isInitDone = true;

        if (debug) {
            Debug.Log("Init");
        }
    }

    public void clearInitData() {
        isInitDone = false;
        client = null;
        _awsKey = null;
        _awsSecret = null;
        _awsBucket = null;
        _awsRegion = null;
        _awsMywallUpdate = null;
    }

    public void getObjectMetadata(string fileToDownload, Action<GetObjectMetadataResponse> callback) {
        client.GetObjectMetadataAsync(_awsBucket, fileToDownload, (metaResponseObj) => {
            if (metaResponseObj.Exception != null) {
                Debug.LogWarning(metaResponseObj.Exception);

                AmazonS3Exception exception;
                try {
                    exception = (AmazonS3Exception)metaResponseObj.Exception;
                } catch {
                    callback(null);
                    return;
                }

                callback(null);
                return;
            }

            GetObjectMetadataResponse response = metaResponseObj.Response;

            callback(response);
        });
    }

    public void getTextBlob(Action<RestResponse> callback, string fileToDownload = "") {
        string responseText = string.Empty;

        fileToDownload = $"{fileToDownload}".Replace("//", "/");

        if (debug) {
            Debug.LogFormat("Get Text Blob. Url: {0}/{1}", _awsBucket, fileToDownload);
        }

        if (!isInitDone) {
            Debug.LogWarning("S3 is not initialized");
            callback(new RestResponse("S3 is not initialized", HttpStatusCode.InternalServerError, fileToDownload, ""));
            return;
        }

        client.GetObjectMetadataAsync(_awsBucket, fileToDownload, (metaResponseObj) => {
            if (metaResponseObj.Exception != null) {
                Debug.LogWarning(metaResponseObj.Exception);

                AmazonS3Exception exception;
                try {
                    exception = (AmazonS3Exception)metaResponseObj.Exception;
                } catch {
                    callback(new RestResponse(metaResponseObj.Exception.Message, 0, fileToDownload, responseText));
                    return;
                }

                callback(new RestResponse(metaResponseObj.Exception.Message, exception.StatusCode, fileToDownload, responseText));
                return;
            }

            client.GetObjectAsync(_awsBucket, fileToDownload, (responseObj) => {
                if (responseObj.Exception != null) {
                    Debug.LogWarning(responseObj.Exception);
                    AmazonS3Exception exception;
                    try {
                        exception = (AmazonS3Exception)responseObj.Exception;
                    } catch {
                        callback(new RestResponse(responseObj.Exception.Message, 0, fileToDownload, responseText));

                        responseObj = null;
                        if (++garbageCollectorDownloadCount >= garbageCollectorDownloadLimit) {
                            GC.Collect();
                            garbageCollectorDownloadCount = 0;
                        }
                        return;
                    }

                    callback(new RestResponse(responseObj.Exception.Message, exception.StatusCode, fileToDownload, responseText));

                    responseObj = null;
                    if (++garbageCollectorDownloadCount >= garbageCollectorDownloadLimit) {
                        GC.Collect();
                        garbageCollectorDownloadCount = 0;
                    }
                    return;
                }

                using (StreamReader reader = new StreamReader(responseObj.Response.ResponseStream)) {
                    responseText = reader.ReadToEnd();
                }

                callback(new RestResponse(responseObj.Response.HttpStatusCode, fileToDownload, responseText));

                responseObj = null;
                if (++garbageCollectorDownloadCount >= garbageCollectorDownloadLimit) {
                    GC.Collect();
                    garbageCollectorDownloadCount = 0;
                }
            });
        });
    }

    public void getBlob(Action<RestResponse> callback, string fileToDownload, string destination, bool forceDownload = false, string checksumValue = "", bool slowMode = false) {
        fileToDownload = $"{fileToDownload}";
        isDownloadingStopped = false;

        int partSize = slowMode ? slowModePackSize : normalModePackSize;

        if (debug) {
            Debug.LogFormat("Get Blob. Url: {0}/{1}", _awsBucket, fileToDownload);
        }

        if (!isInitDone) {
            Debug.LogWarning("S3 is not initialized");
            callback?.Invoke(new RestResponse("S3 is not initialized", HttpStatusCode.InternalServerError, fileToDownload, ""));
            return;
        }

        if (!forceDownload && checksum(destination, checksumValue, true)) {
            if (debug) {
                Debug.Log($"Loading local file {destination}");
            }

            callback(new RestResponse(HttpStatusCode.OK, fileToDownload, $"Loading local file {destination}"));
            return;
        }

        client.GetObjectMetadataAsync(_awsBucket, fileToDownload, (metaResponseObj) => {
            if (metaResponseObj.Exception != null) {
                Debug.LogWarning($"{fileToDownload} - {metaResponseObj.Exception}");

                AmazonS3Exception exception;
                try {
                    exception = (AmazonS3Exception)metaResponseObj.Exception;
                } catch {
                    callback(new RestResponse(metaResponseObj.Exception.Message, 0, fileToDownload, string.Empty));
                    return;
                }

                callback(new RestResponse(metaResponseObj.Exception.Message, exception.StatusCode, fileToDownload, string.Empty));
                return;
            }

            GetObjectRequest request = new GetObjectRequest { BucketName = _awsBucket, Key = fileToDownload };

            if (File.Exists(destination)) {
                FileInfo info = new FileInfo(destination);
                long existingFileLength = info.Length;

                if (existingFileLength == metaResponseObj.Response.Headers.ContentLength) {
                    callback(new RestResponse(HttpStatusCode.OK, fileToDownload, string.Empty));
                    return;
                }

                if (existingFileLength > metaResponseObj.Response.Headers.ContentLength) {
                    // incorrect file, this could happen after a fix made in BO which resulted in a new image generated.
                    // A file needs to be re-downloaded
                    File.Delete(destination);
                    existingFileLength = 0;
                }
                
                if (existingFileLength + partSize > metaResponseObj.Response.Headers.ContentLength) {
                    partSize = (int) (metaResponseObj.Response.Headers.ContentLength - existingFileLength);
                }

                request.ByteRange = new ByteRange(existingFileLength, existingFileLength + partSize);
                
            } else {
                if (partSize > metaResponseObj.Response.Headers.ContentLength) {
                    partSize = (int) metaResponseObj.Response.Headers.ContentLength;
                }

                request.ByteRange = new ByteRange(0, partSize);
            }

            client.GetObjectAsync(request, (responseObj) => {
                if (responseObj.Exception != null) {
                    Debug.LogWarning(responseObj.Exception);
                    AmazonS3Exception exception;
                    try {
                        exception = (AmazonS3Exception)responseObj.Exception;
                    } catch {
                        callback(new RestResponse(responseObj.Exception.Message, 0, fileToDownload, string.Empty));
                        responseObj = null;

                        if (++garbageCollectorDownloadCount >= garbageCollectorDownloadLimit) {
                            GC.Collect();
                            garbageCollectorDownloadCount = 0;
                        }
                        return;
                    }

                    callback(new RestResponse(responseObj.Exception.Message, exception.StatusCode, fileToDownload, string.Empty));

                    responseObj = null;

                    if (++garbageCollectorDownloadCount >= garbageCollectorDownloadLimit) {
                        GC.Collect();
                        garbageCollectorDownloadCount = 0;
                    }
                    return;
                }

                string folderPath = destination.Replace(Path.GetFileName(destination), "");

                if (!Directory.Exists(folderPath)) {
                    Directory.CreateDirectory(folderPath);
                }

                if (File.Exists(destination)) {
                    FileInfo info = new FileInfo(destination);

                    long existingFileLength = info.Length;

                    if (existingFileLength == metaResponseObj.Response.Headers.ContentLength) {
                        callback(new RestResponse(responseObj.Response.HttpStatusCode, fileToDownload, string.Empty));
                        return;
                    }
                }

                using (var fileStream = File.OpenWrite(destination)) {
                    fileStream.Seek(0, SeekOrigin.End);
                    copyStream(responseObj.Response.ResponseStream, fileStream);
                }

                if (responseObj.Response.ResponseStream.Length < partSize) {
                    callback(new RestResponse(responseObj.Response.HttpStatusCode, fileToDownload, string.Empty));
                } else {
                    if (isDownloadingStopped) {
                        return;
                    }
                    getBlob(callback, fileToDownload, destination, forceDownload, checksumValue, slowMode);
                }

                responseObj = null;

                if (++garbageCollectorDownloadCount >= garbageCollectorDownloadLimit) {
                    GC.Collect();
                    garbageCollectorDownloadCount = 0;
                }
            });
        });
    }

    public void stopDownload() {
        isDownloadingStopped = true;
    }

    public bool checksum(string filePath, string checksumValue, bool slowMode = false) {
        if (!File.Exists(filePath)) {
            return false;
        }

        checksumValue = checksumValue.Replace("\"", "");

        // if all files exist, check checksum
        using (var md5 = MD5.Create()) {
            using (var stream = File.OpenRead(filePath)) {
                string fileChecksum = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", string.Empty).ToLower();

                if (checksumValue != fileChecksum) {
                    // remove corrupted file
                    stream.Close();
                    if (!slowMode) {
                        File.Delete(filePath);
                    }

                    return false;
                }

                return true;
            }
        }
    }

    public void putBlob(Action<RestResponse> callback, byte[] content, string filename) {
        if (debug) {
            Debug.LogFormat("Put Blob. Url: {0}/{1}", _awsBucket, filename);
        }

        if (!isInitDone) {
            Debug.LogWarning("S3 is not initialized");
            callback(new RestResponse("S3 is not initialized", HttpStatusCode.InternalServerError, filename, ""));
            return;
        }

        MemoryStream stream = new MemoryStream(content);

        var request = new PutObjectRequest() {
            BucketName = _awsBucket,
            Key = filename,
            InputStream = stream,
            CannedACL = S3CannedACL.Private
        };

        client.PutObjectAsync(request, (responseObj) => {
            if (responseObj.Exception != null) {
                Debug.LogWarning(responseObj.Exception);
                AmazonS3Exception exception;

                try {
                    exception = (AmazonS3Exception)responseObj.Exception;
                } catch {
                    callback(new RestResponse(responseObj.Exception.Message, 0, filename, string.Empty));
                    return;
                }

                callback(new RestResponse(responseObj.Exception.Message, exception.StatusCode, filename, string.Empty));
                return;
            }

            callback(new RestResponse(responseObj.Response.HttpStatusCode, filename, string.Empty));
        });
    }

    public void putTextBlob(Action<RestResponse> callback, string content, string filename) {
        if (debug) {
            Debug.LogFormat("Put Text Blob. Url: {0}/{1}", _awsBucket, filename);
        }

        if (!isInitDone) {
            Debug.LogWarning("S3 is not initialized");
            callback(new RestResponse("S3 is not initialized", HttpStatusCode.InternalServerError, filename, ""));
            return;
        }

        byte[] byteArray = Encoding.UTF8.GetBytes(content);
        MemoryStream stream = new MemoryStream(byteArray);

        var request = new PutObjectRequest() {
            BucketName = _awsBucket,
            Key = filename,
            InputStream = stream,
            CannedACL = S3CannedACL.Private
        };

        client.PutObjectAsync(request, (responseObj) => {
            if (responseObj.Exception != null) {
                Debug.LogWarning(responseObj.Exception);
                AmazonS3Exception exception;

                try {
                    exception = (AmazonS3Exception)responseObj.Exception;
                } catch {
                    callback(new RestResponse(responseObj.Exception.Message, 0, filename, string.Empty));
                    return;
                }

                callback(new RestResponse(responseObj.Exception.Message, exception.StatusCode, filename, string.Empty));
                return;
            }

            callback(new RestResponse(responseObj.Response.HttpStatusCode, filename, string.Empty));
        });
    }

    public void deleteBlob(Action<RestResponse> callback, string fileToDelete) {
        if (debug) {
            Debug.LogFormat("Delete Blob. Url: {0}/{1}", _awsBucket, fileToDelete);
        }

        if (!isInitDone) {
            Debug.LogWarning("S3 is not initialized");
            callback(new RestResponse("S3 is not initialized", HttpStatusCode.InternalServerError, fileToDelete, ""));
            return;
        }

        client.GetObjectMetadataAsync(_awsBucket, fileToDelete, (metaResponseObj) => {
            if (metaResponseObj.Exception != null) {
                Debug.LogWarning(metaResponseObj.Exception);
                AmazonS3Exception exception;

                try {
                    exception = (AmazonS3Exception)metaResponseObj.Exception;
                } catch {
                    callback(new RestResponse(metaResponseObj.Exception.Message, 0, fileToDelete, string.Empty));
                    return;
                }

                callback(new RestResponse(metaResponseObj.Exception.Message, exception.StatusCode, fileToDelete, string.Empty));
                return;
            }

            client.DeleteObjectAsync(_awsBucket, fileToDelete, (responseObj) => {
                if (responseObj.Exception != null) {
                    Debug.LogWarning(responseObj.Exception);
                    AmazonS3Exception exception;

                    try {
                        exception = (AmazonS3Exception)responseObj.Exception;
                    } catch {
                        callback(new RestResponse(responseObj.Exception.Message, 0, fileToDelete, string.Empty));
                        return;
                    }

                    callback(new RestResponse(responseObj.Exception.Message, exception.StatusCode, fileToDelete, string.Empty));
                    return;
                }

                callback(new RestResponse(responseObj.Response.HttpStatusCode, fileToDelete, string.Empty));
            });
        });
    }

    private static void copyStream(Stream input, Stream output) {
        byte[] buffer = new byte[32768];
        int read;
        while ((read = input.Read(buffer, 0, buffer.Length)) > 0) {
            output.Write(buffer, 0, read);
        }
    }

    private void setRegion() {
        awsRegion = RegionEndpoint.GetBySystemName(_awsRegion);

    }

    /// <summary>
    /// Retrieves S3 location of the update APK update file of mywall
    /// </summary>
    /// <returns>s3 path</returns>
    public string getMywallUpdateLocation() {
        return _awsMywallUpdate;
    }
}

public abstract class Response {
    public bool IsError { get; set; }

    public string ErrorMessage { get; set; }

    public string Url { get; set; }

    public HttpStatusCode StatusCode { get; set; }

    public string Content { get; set; }

    protected Response(HttpStatusCode statusCode) {
        this.StatusCode = statusCode;
        this.IsError = !((int)statusCode >= 200 && (int)statusCode < 300);
    }

    // success
    protected Response(HttpStatusCode statusCode, string url, string text) {
        this.IsError = false;
        this.Url = url;
        this.ErrorMessage = null;
        this.StatusCode = statusCode;
        this.Content = text;
    }

    // failure
    protected Response(string error, HttpStatusCode statusCode, string url, string text) {
        this.IsError = true;
        this.Url = url;
        this.ErrorMessage = error;
        this.StatusCode = statusCode;
        this.Content = text;
    }
}

public sealed class RestResponse : Response {

    // success
    public RestResponse(HttpStatusCode statusCode, string url, string text) : base(statusCode, url, text) {
    }

    // failure
    public RestResponse(string error, HttpStatusCode statusCode, string url, string text) : base(error, statusCode, url, text) {
    }
}

public sealed class RestResponse<T> : Response, IRestResponse<T> {
    public T Data { get; set; }

    // success
    public RestResponse(HttpStatusCode statusCode, string url, string text, T data) : base(statusCode, url, text) {
        this.Data = data;
    }

    // failure
    public RestResponse(string error, HttpStatusCode statusCode, string url, string text) : base(error, statusCode, url, text) {
    }
}

public interface IRestResponse<T> {
    bool IsError { get; }

    string ErrorMessage { get; }

    string Url { get; }

    HttpStatusCode StatusCode { get; }

    string Content { get; }

    T Data { get; }
}