using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public static class SDGeneratorEditor
{
    public static IEnumerator Generate(string prompt, System.Action<Texture2D> onDone)
    {
        string url = "http://127.0.0.1:7860/sdapi/v1/txt2img";

        string json = "{\"prompt\":\"" + prompt + "\",\"steps\":20}";

        UnityWebRequest req = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            Texture2D tex = Decode(req.downloadHandler.text);
            onDone?.Invoke(tex);
        }
        else
        {
            Debug.LogError(req.error);
        }
    }

    static Texture2D Decode(string json)
    {
        SDResponse data = JsonUtility.FromJson<SDResponse>(json);
        byte[] bytes = System.Convert.FromBase64String(data.images[0]);

        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(bytes);
        tex.Apply();

        return tex;
    }

    [System.Serializable]
    class SDResponse
    {
        public string[] images;
    }
}