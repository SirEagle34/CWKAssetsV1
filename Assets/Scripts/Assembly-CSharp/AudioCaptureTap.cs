using UnityEngine;

/// <summary>
/// Lightweight tap component that must live on the active AudioListener GameObject.
/// Forwards audio samples to an AudioCaptureSession (which can be on a persistent object).
/// </summary>
public class AudioCaptureTap : MonoBehaviour
{
    public AudioCaptureSession Session;

    void OnAudioFilterRead(float[] data, int channels)
    {
        if (Session == null)
            return;

        Session.AddSamples(data, channels);
        // Do not modify 'data' so gameplay audio remains audible.
    }
}
