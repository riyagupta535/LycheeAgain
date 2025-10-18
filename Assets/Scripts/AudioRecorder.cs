using System;
using System.IO;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

public class AudioRecorder : MonoBehaviour
{
    [Header("Playback")]
    public AudioSource playbackSource;

    [Header("Recording")]
    public int sampleRate = 48000;   // Quest mic is 48k
    public int maxRecordSeconds = 60;

    string micDevice;
    AudioClip clip;
    bool isRecording;
    const int Channels = 1;

    void Start()
    {
        Debug.Log("[AudioRecorder] Start()");
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            Permission.RequestUserPermission(Permission.Microphone);
#endif
    }

    public bool IsRecording => isRecording;

    public void StartRecording()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Debug.LogWarning("[AudioRecorder] Mic permission not granted yet.");
            Permission.RequestUserPermission(Permission.Microphone);
            return;
        }
#endif
        if (isRecording) return;

        var devices = Microphone.devices;
        micDevice = (devices != null && devices.Length > 0) ? devices[0] : null;
        if (string.IsNullOrEmpty(micDevice))
        {
            Debug.LogError("[AudioRecorder] No microphone found.");
            return;
        }

        clip = Microphone.Start(micDevice, false, maxRecordSeconds, sampleRate);
        isRecording = true;
        Debug.Log($"[AudioRecorder] Recording… device='{micDevice}', {sampleRate} Hz");
    }

    public string StopRecordingAndSave()
    {
        if (!isRecording)
        {
            Debug.LogWarning("[AudioRecorder] Stop called but not recording.");
            return null;
        }

        int pos = Microphone.GetPosition(micDevice);
        Microphone.End(micDevice);
        isRecording = false;

        if (pos <= 0 || clip == null)
        {
            Debug.LogError($"[AudioRecorder] No samples captured (pos={pos}).");
            return null;
        }

        // Trim to actual length
        float[] data = new float[pos * Channels];
        clip.GetData(data, 0);
        var trimmed = AudioClip.Create("Recording", pos, Channels, sampleRate, false);
        trimmed.SetData(data, 0);
        clip = trimmed;

        string dir = Application.persistentDataPath;
        string path = Path.Combine(dir, $"recording_{DateTime.Now:yyyyMMdd_HHmmss}.wav");
        try
        {
            SaveWav16(path, clip);
            Debug.Log("[AudioRecorder] Saved WAV: " + path + $" (samples={clip.samples})");
        }
        catch (Exception ex)
        {
            Debug.LogError("[AudioRecorder] Save failed: " + ex);
            return null;
        }

        if (playbackSource != null) { playbackSource.clip = clip; playbackSource.Play(); }
        return path;
    }

    // 16-bit PCM WAV writer (self-contained)
    static void SaveWav16(string filePath, AudioClip c)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        using var fs = new FileStream(filePath, FileMode.Create);
        using var bw = new BinaryWriter(fs);

        int samples = c.samples * c.channels;
        var floatData = new float[samples];
        c.GetData(floatData, 0);
        var intData = new short[samples];
        for (int i = 0; i < samples; i++)
        {
            float f = Mathf.Clamp(floatData[i], -1f, 1f);
            intData[i] = (short)(f * short.MaxValue);
        }

        int byteRate = c.frequency * c.channels * 2;
        int sub2 = intData.Length * 2;
        int chunk = 36 + sub2;

        void W(string s) => bw.Write(System.Text.Encoding.ASCII.GetBytes(s));
        W("RIFF"); bw.Write(chunk); W("WAVE");
        W("fmt "); bw.Write(16); bw.Write((short)1);
        bw.Write((short)c.channels); bw.Write(c.frequency);
        bw.Write(byteRate); bw.Write((short)(c.channels * 2)); bw.Write((short)16);
        W("data"); bw.Write(sub2);
        foreach (var s in intData) bw.Write(s);
    }
}
