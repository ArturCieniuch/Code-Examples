using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Events;

public class Encryption {
    private static byte[] key;
    private static byte[] bytes;

    private static string defaulKey = "Some key";

    public static string fromBase64String(string s) {
        s = Encoding.UTF8.GetString(Convert.FromBase64String(s));
        return s;
    }

    public static string toBase64String(string s) {
        s = Convert.ToBase64String(Encoding.UTF8.GetBytes(s));
        return s;
    }

    private static void setKey() {
        key = new byte[32];
        Encoding.UTF8.GetBytes(defaulKey).CopyTo(key, 0);
    }

    public static void setKey(string newKey, int keyLength) {
        key = new byte[keyLength / 8];
        Encoding.UTF8.GetBytes(newKey).CopyTo(key, 0);
    }

    public static byte[] encrypt(string toEncrypt) {
        if (key == null) {
            setKey();
        }
        bytes = encryptStringToBytes_Aes(toEncrypt, key);
        return bytes;
    }

    public static void encryptToPlayerPrefs(string playerPrefsKey, string value) {
        if (string.IsNullOrEmpty(value)) {
            PlayerPrefs.DeleteKey(playerPrefsKey);
            return;
        }

        byte[] tempKey = key;
        setKey(AdminManager.instance.getLicenseAesKey(), 256);

        PlayerPrefs.SetString(playerPrefsKey, Encryption.encryptToBase64(value));
        PlayerPrefs.Save();

        key = tempKey;
    }

    public static string decryptFromPlayerPrefs(string playerPrefsKey) {
        byte[] tempKey = key;
        setKey(AdminManager.instance.getLicenseAesKey(), 256);

        if (string.IsNullOrEmpty(PlayerPrefs.GetString(playerPrefsKey))) {
            return string.Empty;
        }

        string value = decryptFromBase64(PlayerPrefs.GetString(playerPrefsKey));

        key = tempKey;

        if (string.IsNullOrEmpty(value)) {
            PlayerPrefs.DeleteKey(playerPrefsKey);
        }

        return value;
    }

    public static JSONNode decryptJsonFromFile(string path, UnityAction<Exception> onErrorCallback = null, string overrideKey = null) {
        if (!string.IsNullOrEmpty(overrideKey)) {
            setKey(overrideKey, 256);
        } else {
            setKey(AdminManager.instance.getWallListKey(), 256);
        }
        JSONNode node = new JSONObject();

        string json = File.ReadAllText(path);

        node = JSON.Parse(json);

        if (node.Exists("data")) {
            try {
                node = JSON.Parse(decryptFromBase64(node["data"]));
            } catch(Exception e) {
                node = null;
                onErrorCallback?.Invoke(e);
            }
        }

        return node;
    }

    public static void encryptJsonToFile(string path, string content) {
        setKey(AdminManager.instance.getWallListKey(), 256);
        JSONNode node = new JSONObject();
        node["data"] = encryptToBase64(content);
        File.WriteAllText(path, node.ToString());
    }

    public static JSONNode decryptJsonFromString(string json) {
        setKey(AdminManager.instance.getWallListKey(), 256);
        JSONNode node = new JSONObject();

        try {
            node = JSON.Parse(json);
            node = JSON.Parse(decryptFromBase64(node["data"]));
        } catch {
            node = JSON.Parse(json);
        }

        return node;
    }

    public static string encryptToBase64(string toEncrypt) {
        return Convert.ToBase64String(encrypt(toEncrypt));
    }

    public static string decrypt(byte[] toDecrypt) {
        if (key == null) {
            setKey();
        }

        try {
            return decryptStringFromBytes_Aes(toDecrypt, key);
        } catch (CryptographicException e){
            Debug.LogWarning(e);
            return string.Empty;
        }
    }

    public static string decryptFromBase64(string toDecrypt) {
        byte[] newBytes = Convert.FromBase64String(toDecrypt);
        return decrypt(newBytes);
    }

    public static string hash(string input) {
        using (SHA1Managed sha1 = new SHA1Managed()) {
            var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sb = new StringBuilder(hash.Length * 2);

            foreach (byte b in hash) {
                // can be "x2" if you want lowercase
                sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }
    }

    private static byte[] encryptStringToBytes_Aes(string plainText, byte[] Key) {
        if (plainText == null || plainText.Length <= 0)
            throw new ArgumentNullException("plainText");
        if (Key == null || Key.Length <= 0)
            throw new ArgumentNullException("Key");
        byte[] encrypted;
        using (Aes aesAlg = Aes.Create()) {
            aesAlg.Mode = CipherMode.ECB;
            aesAlg.Key = Key;
            aesAlg.Padding = PaddingMode.PKCS7;

            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, null);

            using (MemoryStream msEncrypt = new MemoryStream()) {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt
                    , encryptor, CryptoStreamMode.Write)) {
                    using (StreamWriter swEncrypt = new StreamWriter(
                        csEncrypt)) {
                        swEncrypt.Write(plainText);
                    }
                    encrypted = msEncrypt.ToArray();
                }
            }
        }
        return encrypted;
    }

    private static string decryptStringFromBytes_Aes(byte[] cipherText, byte[] Key) {
        if (cipherText == null || cipherText.Length <= 0)
            throw new ArgumentNullException("cipherText");
        if (Key == null || Key.Length <= 0)
            throw new ArgumentNullException("Key");

        string plaintext = null;

        using (Aes aesAlg = Aes.Create()) {
            aesAlg.Mode = CipherMode.ECB;
            aesAlg.Key = Key;
            aesAlg.Padding = PaddingMode.PKCS7;

            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, null);
            using (MemoryStream msDecrypt = new MemoryStream(cipherText)) {
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt
                    , decryptor, CryptoStreamMode.Read)) {
                    //	csDecrypt.FlushFinalBlock ();
                    using (StreamReader srDecrypt = new StreamReader(
                        csDecrypt)) {
                        plaintext = srDecrypt.ReadToEnd();
                    }
                }
            }
        }
        return plaintext;
    }
}