using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Empty
{
    // no additional data in the package
}

public abstract class JSONLoader<T> : Loader
{
    [System.Serializable]
    public class Field
    {
        public string key, value;

        public Field(string k, string v)
        {
            key = k;
            value = v;
        }
    }

    [System.Serializable]
    public class File
    {
        public string name, fileName, contentType;
        public byte[] data;

        public File(string n, byte[] d, string f, string c)
        {
            name = n;
            data = d;
            fileName = f;
            contentType = c;
        }
    }

    [Header("Web source")]
    public string encoding = "utf-8";
    public bool postRequest = false;
    public Field[] formData; // used only in post requests
    public File[] formFiles; // used only in post requests
    [Header("Authentication")]
    public bool authentication = true;
    public string nameOfKeyPostVariable = "key";
    public string keyPlayerPreference = "AuthenticationLoader_key";
    [Header("Messages")]
    public string jsonErrorMessage = "Invalid server answer.";

    protected class Package<U>
    {
        public bool authentication = false;
        public string error = string.Empty;
        public U data;
    }

    protected Package<T> content;

    override protected void LoadCache()
    {
        if (System.IO.File.Exists(path))
        {
            content = JsonUtility.FromJson<Package<T>>(System.IO.File.ReadAllText(path));
            // print("Cache loaded!");
            OnContentUpdate();
        }
    }

    override protected void SaveCache()
    {
        System.IO.File.WriteAllText(path, JsonUtility.ToJson(content));
        // print("Cache saved!");
    }

    override protected IEnumerator GetContentRoutine()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            OnNetworkUnreachable();
            yield break;
        }
        UnityWebRequest www;
        if (postRequest)
        {
            List<IMultipartFormSection> form = new List<IMultipartFormSection>();
            foreach (Field f in formData)
                form.Add(new MultipartFormDataSection(f.key, f.value));
            if (authentication && PlayerPrefs.HasKey(keyPlayerPreference) && PlayerPrefs.GetString(keyPlayerPreference) != string.Empty)
                form.Add(new MultipartFormDataSection(nameOfKeyPostVariable, PlayerPrefs.GetString(keyPlayerPreference)));
            foreach (File f in formFiles)
                form.Add(new MultipartFormFileSection(f.name, f.data, f.fileName, f.contentType));
            www = UnityWebRequest.Post(url, form);
        }
        else
            www = UnityWebRequest.Get(url);
        OnLoading();

        yield return www.SendWebRequest();

        if (www.isNetworkError)
            OnNetworkError();
        else if (www.isHttpError)
            OnHttpError();
        else
        {
            content = new Package<T>();
            try
            {
                JsonUtility.FromJsonOverwrite(System.Text.Encoding.
                    GetEncoding(encoding).GetString(www.downloadHandler.data),
                    content);
            }
            catch (ArgumentException)
            {
                PrintOnLoaderLog(content.error = jsonErrorMessage);
                yield break;
            }
            SaveCache();
            OnContentUpdate();
        };
    }
    protected virtual void OnContentUpdate()
    {
        PrintOnLoaderLog(content.error);
        UseContent();
    }
}
