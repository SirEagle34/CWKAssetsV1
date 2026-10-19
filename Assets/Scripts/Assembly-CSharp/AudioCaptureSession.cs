using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Persistent audio capture session that accumulates samples and writes a WAV on Stop.
/// Audio samples are pushed in via AudioCaptureTap.OnAudioFilterRead attached to the active AudioListener.
/// </summary>
public class AudioCaptureSession : MonoBehaviour
{
    private bool isRecording;
    private string outputFilePath;
    private List<float> audioData;

    private int sampleRate = 44100;
    private int channels = 2;

    public bool IsCapturing
    {
        get { return isRecording; }
    }

    public void StartCapture(string outputPath)
    {
        if (isRecording)
        {
            Debug.LogWarning("[AudioCaptureSession] Already recording audio");
            return;
        }

        outputFilePath = outputPath;

        // Pre-allocate ~10 minutes stereo at 44100Hz to reduce reallocations.
        int estimatedSamples = sampleRate * channels * 600;
        audioData = new List<float>(estimatedSamples);

        isRecording = true;

        Debug.Log("[AudioCaptureSession] Started capturing audio to: " + outputFilePath);
    }

    public void StopCapture()
    {
        if (!isRecording)
        {
            Debug.LogWarning("[AudioCaptureSession] Not currently recording");
            return;
        }

        isRecording = false;

        try
        {
            if (audioData == null || audioData.Count == 0)
            {
                Debug.LogWarning("[AudioCaptureSession] No audio data captured");
                return;
            }

            float[] samples = audioData.ToArray();
            SaveWAV(outputFilePath, samples, sampleRate, channels);

            Debug.Log(string.Format(
                "[AudioCaptureSession] Audio saved: {0} ({1} samples, {2:F2}s)",
                outputFilePath,
                samples.Length,
                (float)samples.Length / sampleRate / channels
            ));
        }
        catch (Exception ex)
        {
            Debug.LogError("[AudioCaptureSession] Error saving audio: " + ex.Message);
        }
        finally
        {
            if (audioData != null)
            {
                audioData.Clear();
            }
        }
    }

    public void AddSamples(float[] data, int dataChannels)
    {
        if (!isRecording || audioData == null || data == null || data.Length == 0)
            return;

        // We assume Unity output is stereo in this project; if channels mismatch, we still store raw samples.
        // OnAudioFilterRead provides interleaved channels.
        audioData.AddRange(data);

        // Track channels from the callback (optional, but helps if project changes)
        channels = Mathf.Max(1, dataChannels);
    }

    public float GetCurrentDuration()
    {
        if (audioData == null || audioData.Count == 0)
            return 0f;

        return (float)audioData.Count / sampleRate / Mathf.Max(1, channels);
    }

    private void SaveWAV(string filename, float[] samples, int sampleRate, int channels)
    {
        string directory = Path.GetDirectoryName(filename);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using (FileStream fs = new FileStream(filename, FileMode.Create))
        using (BinaryWriter writer = new BinaryWriter(fs))
        {
            int byteRate = sampleRate * channels * 2;
            int dataSize = samples.Length * 2;

            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(dataSize + 36);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

            writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)channels);
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write((short)(channels * 2));
            writer.Write((short)16);

            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            writer.Write(dataSize);

            for (int i = 0; i < samples.Length; i++)
            {
                float clampedSample = Mathf.Clamp(samples[i], -1.0f, 1.0f);
                short intSample = (short)(clampedSample * 32767f);
                writer.Write(intSample);
            }
        }
    }
}
