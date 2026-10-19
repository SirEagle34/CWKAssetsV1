package com.cardwarskingdom.recordertest;

import android.content.Context;
import android.hardware.display.DisplayManager;
import android.hardware.display.VirtualDisplay;
import android.media.AudioAttributes;
import android.media.AudioFormat;
import android.media.AudioPlaybackCaptureConfiguration;
import android.media.AudioRecord;
import android.media.MediaCodec;
import android.media.MediaCodecInfo;
import android.media.MediaFormat;
import android.media.MediaMuxer;
import android.media.MediaScannerConnection;
import android.media.projection.MediaProjection;
import android.os.Build;
import android.os.Handler;
import android.os.HandlerThread;
import android.os.Looper;
import android.util.DisplayMetrics;
import android.util.Log;
import android.view.Surface;
import android.view.WindowManager;

import java.io.File;
import java.io.IOException;
import java.nio.ByteBuffer;

/**
 * Motor de grabacion para Android 14/15.
 *
 * CORRECCION CLAVE: recibe un MediaProjection ya obtenido en el main thread
 * (desde RecordingService.onStartCommand). En Android 14+ getMediaProjection()
 * debe llamarse en el hilo principal; hacerlo en un hilo secundario provoca
 * que la VirtualDisplay no reciba frames y no se genere ningun archivo.
 */
public class ScreenRecorderEngine {
    private static final String TAG = "ScreenRecorderEngine";

    private static final int TARGET_LONG_EDGE = 2560;
    private static final int VIDEO_FPS    = 30;
    private static final int VIDEO_BITRATE = 6_000_000;
    private static final int I_FRAME_INTERVAL = 1;
    private static final String VIDEO_MIME_TYPE = MediaFormat.MIMETYPE_VIDEO_AVC;
    private static final String AUDIO_MIME_TYPE = MediaFormat.MIMETYPE_AUDIO_AAC;
    private static final int AUDIO_SAMPLE_RATE = 44100;
    private static final int AUDIO_CHANNEL_COUNT = 1;
    private static final int AUDIO_BITRATE = 96_000;
    private static final long DRAIN_TIMEOUT_US = 10_000L;

    private final Context context;
    private final MediaProjection mediaProjection;
    private final String outputPath;

    private MediaCodec     videoEncoder;
    private MediaCodec     audioEncoder;
    private Surface        inputSurface;
    private MediaMuxer     muxer;
    private VirtualDisplay virtualDisplay;
    private AudioRecord    audioRecord;
    private Thread         audioFeedThread;

    private HandlerThread encoderThread;
    private Handler       encoderHandler;

    private volatile boolean isRecording  = false;
    private boolean muxerStarted          = false;
    private int     videoTrackIndex       = -1;
    private int     audioTrackIndex       = -1;
    private boolean audioEnabled          = false;
    private boolean audioInputEosQueued   = false;
    private long    videoFirstTimestampUs = -1;
    private long    audioFirstTimestampUs = -1;
    private long    audioSamplesSubmitted = 0;
    private MediaFormat pendingVideoFormat;
    private MediaFormat pendingAudioFormat;
    private int videoWidth;
    private int videoHeight;

    private RecorderCallback callback;

    public interface RecorderCallback {
        void onRecordingStarted();
        void onRecordingStopped(String outputPath);
        void onError(String error);
    }

    /**
     * @param mediaProjection objeto obtenido en el main thread con
     *                        projectionManager.getMediaProjection(resultCode, data)
     */
    public ScreenRecorderEngine(Context context,
                                MediaProjection mediaProjection,
                                String outputPath) {
        this.context         = context;
        this.mediaProjection = mediaProjection;
        this.outputPath      = outputPath;
    }

    public void setCallback(RecorderCallback cb) {
        this.callback = cb;
    }

    public void start() {
        if (isRecording) return;
        isRecording = true;

        encoderThread = new HandlerThread("ScreenRecorderThread");
        encoderThread.start();
        encoderHandler = new Handler(encoderThread.getLooper());

        // Callback de proyeccion en main thread para no bloquear el encoder
        mediaProjection.registerCallback(new MediaProjection.Callback() {
            @Override
            public void onStop() {
                Log.w(TAG, "MediaProjection stopped by system");
                stop();
            }
        }, new Handler(Looper.getMainLooper()));

        encoderHandler.post(this::startRecordingInternal);
    }

    private void startRecordingInternal() {
        try {
            Log.d(TAG, "startRecordingInternal begin");

            resolveVideoDimensions();

            configureVideoEncoder();
            configureAudioEncoderAndRecorder();

            File outFile = new File(outputPath);
            File outDir  = outFile.getParentFile();
            if (outDir != null && !outDir.exists()) {
                boolean dirCreated = outDir.mkdirs();
                Log.d(TAG, "mkdirs(" + outDir + ") = " + dirCreated);
                if (!dirCreated && !outDir.exists()) {
                    throw new IOException("Failed to create output directory: " + outDir);
                }
            }
            try {
                muxer = new MediaMuxer(outFile.getAbsolutePath(),
                        MediaMuxer.OutputFormat.MUXER_OUTPUT_MPEG_4);
                Log.d(TAG, "✅ Muxer created: " + outFile.getAbsolutePath());
            } catch (IOException e) {
                Log.e(TAG, "❌ MUXER CREATION FAILED", e);
                throw new IOException("Failed to create MediaMuxer: " + e.getMessage(), e);
            }

            createVirtualDisplay();

            if (callback != null) callback.onRecordingStarted();

            while (isRecording) {
                drainVideoEncoder(false);
                drainAudioEncoder(false);
                try { Thread.sleep(10); } catch (InterruptedException ignored) {}
            }

            Log.d(TAG, "Loop ended — sending EOS");
            if (videoEncoder != null) {
                try {
                    videoEncoder.signalEndOfInputStream();
                    Log.d(TAG, "video signalEndOfInputStream OK");
                } catch (Exception e) {
                    Log.e(TAG, "video signalEndOfInputStream failed: " + e.getMessage());
                }
            }

            if (audioEnabled) {
                stopAudioFeedThread();
                queueAudioEos();
            }

            drainVideoEncoder(true);
            drainAudioEncoder(true);

            cleanup();

            MediaScannerConnection.scanFile(context, new String[]{outputPath}, null,
                    (path, uri) -> Log.d(TAG, "MediaScanner done: " + path));

            if (callback != null) callback.onRecordingStopped(outputPath);

        } catch (IOException e) {
            Log.e(TAG, "IOException in startRecordingInternal", e);
            notifyError(e.getMessage());
            cleanup();
        } catch (Exception e) {
            Log.e(TAG, "Unexpected error in startRecordingInternal", e);
            notifyError(e.getMessage());
            cleanup();
        }
    }

    private void configureVideoEncoder() throws IOException {
        try {
            MediaFormat format = MediaFormat.createVideoFormat(VIDEO_MIME_TYPE, videoWidth, videoHeight);
            format.setInteger(MediaFormat.KEY_COLOR_FORMAT,
                        MediaCodecInfo.CodecCapabilities.COLOR_FormatSurface);
            format.setInteger(MediaFormat.KEY_BIT_RATE, VIDEO_BITRATE);
            format.setInteger(MediaFormat.KEY_FRAME_RATE, VIDEO_FPS);
            format.setInteger(MediaFormat.KEY_I_FRAME_INTERVAL, I_FRAME_INTERVAL);
            format.setInteger(MediaFormat.KEY_REPEAT_PREVIOUS_FRAME_AFTER, 100_000);

            videoEncoder = MediaCodec.createEncoderByType(VIDEO_MIME_TYPE);
            if (videoEncoder == null) {
                throw new IOException("createEncoderByType returned null for " + VIDEO_MIME_TYPE);
            }
            videoEncoder.configure(format, null, null, MediaCodec.CONFIGURE_FLAG_ENCODE);
            inputSurface = videoEncoder.createInputSurface();
            if (inputSurface == null) {
                throw new IOException("createInputSurface returned null");
            }
            videoEncoder.start();
            Log.d(TAG, "✅ Video encoder configured: " + videoWidth + "x" + videoHeight + " @ " + VIDEO_FPS + "fps, bitrate=" + VIDEO_BITRATE);
        } catch (Exception e) {
            Log.e(TAG, "❌ VIDEO ENCODER CONFIGURATION FAILED", e);
            throw new IOException("Failed to configure video encoder: " + e.getMessage(), e);
        }
    }

    private void resolveVideoDimensions() {
        WindowManager wm = (WindowManager) context.getSystemService(Context.WINDOW_SERVICE);
        DisplayMetrics dm = new DisplayMetrics();
        wm.getDefaultDisplay().getRealMetrics(dm);

        int screenWidth = dm.widthPixels;
        int screenHeight = dm.heightPixels;
        if (screenWidth <= 0 || screenHeight <= 0) {
            screenWidth = 1080;
            screenHeight = 1920;
        }

        boolean portrait = screenHeight >= screenWidth;
        float aspect = portrait
                ? (screenWidth / (float) screenHeight)
                : (screenHeight / (float) screenWidth);

        if (portrait) {
            videoHeight = TARGET_LONG_EDGE;
            videoWidth = makeEven(Math.round(videoHeight * aspect));
        } else {
            videoWidth = TARGET_LONG_EDGE;
            videoHeight = makeEven(Math.round(videoWidth * aspect));
        }

        videoWidth = Math.max(videoWidth, 320);
        videoHeight = Math.max(videoHeight, 320);

        Log.d(TAG, "Resolved video size from screen " + screenWidth + "x" + screenHeight +
                " -> " + videoWidth + "x" + videoHeight + " (portrait=" + portrait + ")");
    }

    private int makeEven(int value) {
        return (value / 2) * 2;
    }

    private void configureAudioEncoderAndRecorder() {
        try {
            if (Build.VERSION.SDK_INT < Build.VERSION_CODES.Q) {
                Log.w(TAG, "Internal audio capture requires Android 10+; audio disabled");
                audioEnabled = false;
                return;
            }

            int minBuffer = AudioRecord.getMinBufferSize(
                    AUDIO_SAMPLE_RATE,
                    AudioFormat.CHANNEL_IN_MONO,
                    AudioFormat.ENCODING_PCM_16BIT
            );
            if (minBuffer <= 0) {
                Log.w(TAG, "AudioRecord minBuffer invalid, audio disabled");
                audioEnabled = false;
                return;
            }

            MediaFormat audioFormat = MediaFormat.createAudioFormat(
                    AUDIO_MIME_TYPE,
                    AUDIO_SAMPLE_RATE,
                    AUDIO_CHANNEL_COUNT
            );
            audioFormat.setInteger(MediaFormat.KEY_AAC_PROFILE, MediaCodecInfo.CodecProfileLevel.AACObjectLC);
            audioFormat.setInteger(MediaFormat.KEY_BIT_RATE, AUDIO_BITRATE);
            audioFormat.setInteger(MediaFormat.KEY_MAX_INPUT_SIZE, minBuffer * 2);

            audioEncoder = MediaCodec.createEncoderByType(AUDIO_MIME_TYPE);
            audioEncoder.configure(audioFormat, null, null, MediaCodec.CONFIGURE_FLAG_ENCODE);
            audioEncoder.start();

                AudioPlaybackCaptureConfiguration playbackConfig =
                    new AudioPlaybackCaptureConfiguration.Builder(mediaProjection)
                        .addMatchingUsage(AudioAttributes.USAGE_MEDIA)
                        .addMatchingUsage(AudioAttributes.USAGE_GAME)
                        .addMatchingUsage(AudioAttributes.USAGE_UNKNOWN)
                        .build();

                AudioFormat captureFormat = new AudioFormat.Builder()
                    .setEncoding(AudioFormat.ENCODING_PCM_16BIT)
                    .setSampleRate(AUDIO_SAMPLE_RATE)
                    .setChannelMask(AudioFormat.CHANNEL_IN_MONO)
                    .build();

                audioRecord = new AudioRecord.Builder()
                    .setAudioFormat(captureFormat)
                    .setBufferSizeInBytes(minBuffer * 2)
                    .setAudioPlaybackCaptureConfig(playbackConfig)
                    .build();

            if (audioRecord.getState() != AudioRecord.STATE_INITIALIZED) {
                Log.w(TAG, "AudioRecord not initialized, audio disabled");
                releaseAudioResources();
                audioEnabled = false;
                return;
            }

            audioRecord.startRecording();
            audioEnabled = true;
            startAudioFeedThread(minBuffer * 2);
            Log.d(TAG, "Audio capture enabled (INTERNAL/PlaybackCapture)");

        } catch (SecurityException se) {
            Log.w(TAG, "Internal audio denied by policy/permission, recording video only");
            releaseAudioResources();
            audioEnabled = false;
        } catch (Exception e) {
            Log.w(TAG, "Internal audio init failed, recording video only: " + e.getMessage());
            releaseAudioResources();
            audioEnabled = false;
        }
    }

    public void stop() {
        Log.d(TAG, "stop() called");
        isRecording = false;
    }

    private void createVirtualDisplay() {
        WindowManager wm = (WindowManager) context.getSystemService(Context.WINDOW_SERVICE);
        DisplayMetrics dm = new DisplayMetrics();
        wm.getDefaultDisplay().getRealMetrics(dm);

        virtualDisplay = mediaProjection.createVirtualDisplay(
                "ScreenRecorder",
                videoWidth, videoHeight, dm.densityDpi,
                DisplayManager.VIRTUAL_DISPLAY_FLAG_AUTO_MIRROR,
                inputSurface,
                null,
                encoderHandler
        );
        Log.d(TAG, "VirtualDisplay created — size=" + videoWidth + "x" + videoHeight + " density=" + dm.densityDpi);
    }

    private void startAudioFeedThread(int bufferSize) {
        audioFeedThread = new Thread(() -> {
            byte[] pcm = new byte[bufferSize];
            while (isRecording && audioEnabled && audioRecord != null) {
                int read = audioRecord.read(pcm, 0, pcm.length, AudioRecord.READ_NON_BLOCKING);
                if (read > 0) {
                    feedAudioEncoder(pcm, read);
                } else {
                    try { Thread.sleep(5); } catch (InterruptedException ignored) {}
                }
            }
        }, "AudioFeedThread");
        audioFeedThread.start();
    }

    private void feedAudioEncoder(byte[] data, int size) {
        if (audioEncoder == null || size <= 0) return;
        int idx = audioEncoder.dequeueInputBuffer(DRAIN_TIMEOUT_US);
        if (idx < 0) return;

        ByteBuffer inputBuffer = audioEncoder.getInputBuffer(idx);
        if (inputBuffer == null) return;
        inputBuffer.clear();
        inputBuffer.put(data, 0, size);

        long ptsUs = (audioSamplesSubmitted * 1_000_000L) / AUDIO_SAMPLE_RATE;
        int samples = size / 2;
        audioSamplesSubmitted += samples;

        audioEncoder.queueInputBuffer(idx, 0, size, ptsUs, 0);
    }

    private void queueAudioEos() {
        if (!audioEnabled || audioEncoder == null || audioInputEosQueued) return;
        for (int i = 0; i < 100; i++) {
            int idx = audioEncoder.dequeueInputBuffer(DRAIN_TIMEOUT_US);
            if (idx >= 0) {
                long ptsUs = (audioSamplesSubmitted * 1_000_000L) / AUDIO_SAMPLE_RATE;
                audioEncoder.queueInputBuffer(idx, 0, 0, ptsUs, MediaCodec.BUFFER_FLAG_END_OF_STREAM);
                audioInputEosQueued = true;
                Log.d(TAG, "audio EOS queued");
                return;
            }
            try { Thread.sleep(5); } catch (InterruptedException ignored) {}
        }
        Log.w(TAG, "audio EOS could not be queued in time");
    }

    private void stopAudioFeedThread() {
        if (audioFeedThread != null) {
            try { audioFeedThread.join(500); } catch (InterruptedException ignored) {}
            audioFeedThread = null;
        }
        if (audioRecord != null) {
            try { audioRecord.stop(); } catch (Exception ignored) {}
        }
    }

    private void startMuxerIfReady() {
        if (muxerStarted || muxer == null || pendingVideoFormat == null) {
            if (muxer != null && !muxerStarted) {
                Log.d(TAG, "⏳ Waiting for muxer conditions: videoFormat=" + (pendingVideoFormat != null) + 
                      " audioFormat=" + (pendingAudioFormat != null) + " audioEnabled=" + audioEnabled);
            }
            return;
        }
        if (audioEnabled && pendingAudioFormat == null) {
            return;
        }

        try {
            videoTrackIndex = muxer.addTrack(pendingVideoFormat);
            if (audioEnabled) {
                audioTrackIndex = muxer.addTrack(pendingAudioFormat);
            }
            muxer.start();
            muxerStarted = true;
            Log.d(TAG, "✅ MUXER STARTED videoTrack=" + videoTrackIndex + " audioTrack=" + audioTrackIndex);
        } catch (Exception e) {
            Log.e(TAG, "❌ MUXER START FAILED", e);
            notifyError("Muxer initialization failed: " + e.getMessage());
        }
    }

    private void drainVideoEncoder(boolean endOfStream) {
        if (videoEncoder == null || muxer == null) return;
        MediaCodec.BufferInfo info = new MediaCodec.BufferInfo();
        int retries = 0;
        final int MAX_RETRIES = 60; // 60 x 50 ms = 3 s max espera EOS

        while (true) {
            try {
                int idx = videoEncoder.dequeueOutputBuffer(info, DRAIN_TIMEOUT_US);

                if (idx == MediaCodec.INFO_TRY_AGAIN_LATER) {
                    if (!endOfStream) break;
                    if (++retries > MAX_RETRIES) {
                        Log.e(TAG, "❌ VIDEO EOS DRAIN TIMEOUT - encoding may have failed");
                        notifyError("Video encoding failed: EOS timeout after " + (MAX_RETRIES * 50) + "ms");
                        break;
                    }
                    try { Thread.sleep(50); } catch (InterruptedException ignored) {}

                } else if (idx == MediaCodec.INFO_OUTPUT_FORMAT_CHANGED) {
                    pendingVideoFormat = videoEncoder.getOutputFormat();
                    Log.d(TAG, "✅ VIDEO OUTPUT_FORMAT_CHANGED: " + pendingVideoFormat);
                    startMuxerIfReady();

                } else if (idx >= 0) {
                    ByteBuffer buf = videoEncoder.getOutputBuffer(idx);

                    if ((info.flags & MediaCodec.BUFFER_FLAG_CODEC_CONFIG) != 0) {
                        info.size = 0;
                    }

                    if (info.size > 0 && muxerStarted && buf != null) {
                        try {
                            if (videoFirstTimestampUs < 0) videoFirstTimestampUs = info.presentationTimeUs;
                            info.presentationTimeUs -= videoFirstTimestampUs;
                            buf.position(info.offset);
                            buf.limit(info.offset + info.size);
                            muxer.writeSampleData(videoTrackIndex, buf, info);
                            Log.v(TAG, "video writeSampleData size=" + info.size + " pts=" + info.presentationTimeUs);
                        } catch (Exception e) {
                            Log.e(TAG, "❌ VIDEO MUXER WRITE FAILED", e);
                            notifyError("Video muxer write failed: " + e.getMessage());
                        }
                    } else if (info.size > 0 && !muxerStarted) {
                        Log.w(TAG, "⚠️ VIDEO FRAME BEFORE MUXER START (muxer may not initialize) — size=" + info.size);
                    }

                    videoEncoder.releaseOutputBuffer(idx, false);
                    if ((info.flags & MediaCodec.BUFFER_FLAG_END_OF_STREAM) != 0) {
                        Log.d(TAG, "✅ VIDEO EOS FLAG — DRAIN COMPLETE");
                        break;
                    }
                }
            } catch (Exception e) {
                Log.e(TAG, "❌ UNEXPECTED ERROR IN VIDEO DRAIN", e);
                notifyError("Unexpected video encoding error: " + e.getMessage());
                break;
            }
        }
    }

    private void drainAudioEncoder(boolean endOfStream) {
        if (!audioEnabled || audioEncoder == null || muxer == null) return;

        MediaCodec.BufferInfo info = new MediaCodec.BufferInfo();
        int retries = 0;
        final int MAX_RETRIES = 60;

        while (true) {
            try {
                int idx = audioEncoder.dequeueOutputBuffer(info, DRAIN_TIMEOUT_US);

                if (idx == MediaCodec.INFO_TRY_AGAIN_LATER) {
                    if (!endOfStream) break;
                    if (++retries > MAX_RETRIES) {
                        Log.e(TAG, "❌ AUDIO EOS DRAIN TIMEOUT - encoding may have failed");
                        notifyError("Audio encoding failed: EOS timeout");
                        break;
                    }
                    try { Thread.sleep(50); } catch (InterruptedException ignored) {}

                } else if (idx == MediaCodec.INFO_OUTPUT_FORMAT_CHANGED) {
                    pendingAudioFormat = audioEncoder.getOutputFormat();
                    Log.d(TAG, "✅ AUDIO OUTPUT_FORMAT_CHANGED: " + pendingAudioFormat);
                    startMuxerIfReady();

                } else if (idx >= 0) {
                    ByteBuffer buf = audioEncoder.getOutputBuffer(idx);

                    if ((info.flags & MediaCodec.BUFFER_FLAG_CODEC_CONFIG) != 0) {
                        info.size = 0;
                    }

                    if (info.size > 0 && muxerStarted && buf != null && audioTrackIndex >= 0) {
                        try {
                            if (audioFirstTimestampUs < 0) audioFirstTimestampUs = info.presentationTimeUs;
                            info.presentationTimeUs -= audioFirstTimestampUs;
                            buf.position(info.offset);
                            buf.limit(info.offset + info.size);
                            muxer.writeSampleData(audioTrackIndex, buf, info);
                            Log.v(TAG, "audio writeSampleData size=" + info.size + " pts=" + info.presentationTimeUs);
                        } catch (Exception e) {
                            Log.e(TAG, "❌ AUDIO MUXER WRITE FAILED", e);
                            notifyError("Audio muxer write failed: " + e.getMessage());
                        }
                    }

                    audioEncoder.releaseOutputBuffer(idx, false);
                    if ((info.flags & MediaCodec.BUFFER_FLAG_END_OF_STREAM) != 0) {
                        Log.d(TAG, "✅ AUDIO EOS FLAG — DRAIN COMPLETE");
                        break;
                    }
                }
            } catch (Exception e) {
                Log.e(TAG, "❌ UNEXPECTED ERROR IN AUDIO DRAIN", e);
                notifyError("Unexpected audio encoding error: " + e.getMessage());
                break;
            }
        }
    }

    private void cleanup() {
        Log.d(TAG, "cleanup — muxerStarted=" + muxerStarted + " videoTrack=" + videoTrackIndex + " audioTrack=" + audioTrackIndex);
        try {
            if (virtualDisplay != null) { virtualDisplay.release(); virtualDisplay = null; }
            if (inputSurface   != null) { inputSurface.release();   inputSurface   = null; }
            stopAudioFeedThread();

            if (videoEncoder != null) {
                try { videoEncoder.stop(); } catch (Exception ignored) {}
                videoEncoder.release();
                videoEncoder = null;
            }
            if (audioEncoder != null) {
                try { audioEncoder.stop(); } catch (Exception ignored) {}
                audioEncoder.release();
                audioEncoder = null;
            }
            if (audioRecord != null) {
                try { audioRecord.release(); } catch (Exception ignored) {}
                audioRecord = null;
            }
            if (muxer != null) {
                if (muxerStarted) {
                    try {
                        muxer.stop();
                        File outFile = new File(outputPath);
                        long fileSize = outFile.exists() ? outFile.length() : 0;
                        Log.d(TAG, "✅ MUXER STOPPED — ENCODING COMPLETE — size=" + fileSize + " bytes");
                    } catch (Exception e) { 
                        Log.e(TAG, "❌ MUXER.STOP() FAILED - VIDEO ENCODING FAILED", e);
                        notifyError("Muxer failed to stop: " + e.getMessage());
                    }
                } else {
                    Log.e(TAG, "❌ MUXER NEVER STARTED - NO OUTPUT VIDEO GENERATED! Possible causes: Format change timeout, encoder initialization failures");
                    notifyError("Video encoding failed: Muxer never started (codec config not received)");
                }
                muxer.release(); muxer = null;
            }
            if (mediaProjection != null) mediaProjection.stop();
        } catch (Exception e) {
            Log.e(TAG, "❌ CLEANUP ERROR", e);
        } finally {
            if (encoderThread != null) { encoderThread.quitSafely(); encoderThread = null; }
        }
        File outFile = new File(outputPath);
        long finalSize = outFile.exists() ? outFile.length() : 0;
        if (finalSize == 0) {
            Log.e(TAG, "❌ FINAL OUTPUT SIZE IS ZERO — VIDEO ENCODING FAILED");
            notifyError("Video encoding produced no output");
        } else {
            Log.d(TAG, "✅ cleanup done — final size=" + finalSize + " bytes");
        }
    }

    private void releaseAudioResources() {
        try {
            if (audioRecord != null) {
                try { audioRecord.stop(); } catch (Exception ignored) {}
                audioRecord.release();
                audioRecord = null;
            }
            if (audioEncoder != null) {
                try { audioEncoder.stop(); } catch (Exception ignored) {}
                audioEncoder.release();
                audioEncoder = null;
            }
        } catch (Exception ignored) {
        }
    }

    private void notifyError(String msg) {
        if (callback != null) callback.onError(msg);
    }
}
