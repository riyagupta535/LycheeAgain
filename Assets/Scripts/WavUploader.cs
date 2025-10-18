using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class WavUploader : MonoBehaviour
{
    [Header("Server URL")]
    [Tooltip("Example: http://172.22.43.29:8000/analyze/")]
    public string analyzeUrl = "http://172.22.43.29:8000/analyze/";

    [Header("Status Display (optional)")]
    public UnityEngine.UI.Text hud;

    public void Upload(string wavPath)
    {
        if (string.IsNullOrEmpty(wavPath) || !File.Exists(wavPath))
        {
            Log($"❌ File not found: {wavPath}");
            return;
        }
        StartCoroutine(UploadCoroutine(wavPath));
    }

    IEnumerator UploadCoroutine(string wavPath)
    {
        Log($"⬆️ Uploading: {wavPath}");

        byte[] data = File.ReadAllBytes(wavPath);
        var fileName = Path.GetFileName(wavPath);

        var form = new WWWForm();
        form.AddBinaryData("file", data, fileName, "audio/wav");

        using (var req = UnityWebRequest.Post(analyzeUrl, form))
        {
            req.timeout = 60;
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
                Log("✅ Upload success\n" + req.downloadHandler.text);
            else
                Log("❌ Upload failed: " + req.error + "\n" + req.downloadHandler.text);
        }
    }

    void Log(string s)
    {
        Debug.Log("[WavUploader] " + s);
        if (hud) hud.text = s;
    }
}
