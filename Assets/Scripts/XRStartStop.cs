using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

public class XRStartStop : MonoBehaviour
{
    [Header("References")]
    public AudioRecorder recorder;      // set to the AudioRecorder on AudioManager
    public WavUploader uploader;        // NEW: drag the WavUploader component here
    public Text hud;                    // optional HUD text

    InputDevice rightHand;
    bool prevTrig, prevA;

    void Start()
    {
        SetHud("Looking for right controller…");
        FindRightHand();
    }

    void FindRightHand()
    {
        var list = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Right |
            InputDeviceCharacteristics.Controller |
            InputDeviceCharacteristics.HeldInHand, list);

        if (list.Count > 0)
        {
            rightHand = list[0];
            SetHud("Ready. Press Right Trigger or A to START recording.");
            Debug.Log("[XRStartStop] Right controller found: " + rightHand.name);
        }
        else
        {
            SetHud("No right controller detected. Move it or press any button.");
        }
    }

    void Update()
    {
        if (recorder == null) { SetHud("Recorder missing on AudioManager."); return; }

        if (!rightHand.isValid)
        {
            FindRightHand();
            return;
        }

        bool trigNow = rightHand.TryGetFeatureValue(CommonUsages.triggerButton, out var t) && t;
        bool aNow    = rightHand.TryGetFeatureValue(CommonUsages.primaryButton, out var a) && a;

        bool pressedEdge = (trigNow && !prevTrig) || (aNow && !prevA);
        if (pressedEdge)
        {
            if (!recorder.IsRecording)
            {
                Debug.Log("[XRStartStop] StartRecording()");
                recorder.StartRecording();
                SetHud("🎙️ Recording… Press Right Trigger or A to STOP.");
            }
            else
            {
                Debug.Log("[XRStartStop] StopRecordingAndSave()");
                var path = recorder.StopRecordingAndSave();

                if (!string.IsNullOrEmpty(path))
                {
                    SetHud($"✅ Saved:\n{path}\nUploading…");
                    if (uploader != null) uploader.Upload(path);
                    else Debug.LogWarning("[XRStartStop] Uploader not assigned; skipping upload.");
                }
                else
                {
                    SetHud("⚠️ Save failed. Try again.");
                }
            }
        }

        prevTrig = trigNow; prevA = aNow;
    }

    void SetHud(string s)
    {
        if (hud != null) hud.text = s;
        Debug.Log("[HUD] " + s);
    }
}
