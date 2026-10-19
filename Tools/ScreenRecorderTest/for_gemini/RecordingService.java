package com.cardwarskingdom.recordertest;

import android.app.Notification;
import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.app.PendingIntent;
import android.app.Service;
import android.content.Context;
import android.content.Intent;
import android.os.Build;
import android.os.Environment;
import android.os.IBinder;
import android.util.Log;

import androidx.annotation.Nullable;
import androidx.core.app.NotificationCompat;

import java.io.File;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.PrintWriter;
import java.io.StringWriter;
import java.nio.charset.StandardCharsets;
import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.Locale;

public class RecordingService extends Service {
    private static final String TAG = "RecordingService";
    public static final String ACTION_STOP = "com.cardwarskingdom.recordertest.action.STOP";
    private static final String CHANNEL_ID = "recorder_channel";
    private static final int NOTIF_ID = 0x1001;

    private ScreenRecorderEngine recorder;
    private File serviceLogFile;

    @Override
    public void onCreate() {
        super.onCreate();
        createNotificationChannel();
        try {
            File docs = getExternalFilesDir(android.os.Environment.DIRECTORY_DOCUMENTS);
            if (docs == null) docs = getFilesDir();
            String ts = new SimpleDateFormat("yyyyMMdd_HHmmss", Locale.US).format(new Date());
            serviceLogFile = new File(docs, "service_log_" + ts + ".txt");
            if (!serviceLogFile.exists()) {
                serviceLogFile.getParentFile().mkdirs();
                serviceLogFile.createNewFile();
            }
            writeToServiceLog("Service created: " + ts);
        } catch (IOException e) {
            Log.e(TAG, "Unable to create service log file", e);
        }
    }

    @Override
    public int onStartCommand(Intent intent, int flags, int startId) {
        if (intent == null) {
            return START_NOT_STICKY;
        }

        String action = intent.getAction();
        if (ACTION_STOP.equals(action)) {
            stopRecordingInternal();
            stopSelf();
            return START_NOT_STICKY;
        }

        int resultCode = intent.getIntExtra("resultCode", -1);
        Intent data = intent.getParcelableExtra("data");

        if (resultCode == -1 || data == null) {
            Log.e(TAG, "Missing resultCode or data; stopping service");
            stopSelf();
            return START_NOT_STICKY;
        }

        Notification notif = buildNotification("Grabando pantalla...");
        startForeground(NOTIF_ID, notif);

        File outputDir = getExternalFilesDir(Environment.DIRECTORY_MOVIES);
        String timestamp = new SimpleDateFormat("yyyyMMdd_HHmmss", Locale.US).format(new Date());
        String outputPath = new File(outputDir, "service_recording_" + timestamp + ".mp4").getAbsolutePath();

        try {
            writeToServiceLog("Attempting to start recorder, output=" + outputPath);
            recorder = new ScreenRecorderEngine(this, resultCode, data, outputPath);
            recorder.setCallback(new ScreenRecorderEngine.RecorderCallback() {
                @Override
                public void onRecordingStarted() {
                    Log.d(TAG, "Recording started (service)");
                    writeToServiceLog("Recording started");
                }

                @Override
                public void onRecordingStopped(String outputPath) {
                    Log.d(TAG, "Recording stopped: " + outputPath);
                    writeToServiceLog("Recording stopped: " + outputPath);
                    stopSelf();
                }

                @Override
                public void onError(String error) {
                    Log.e(TAG, "Recording error: " + error);
                    writeToServiceLog("Recording error: " + error);
                    stopSelf();
                }
            });

            recorder.start();
        } catch (Throwable e) {
            Log.e(TAG, "Failed to start recorder", e);
            writeExceptionToServiceLog(e);
            stopSelf();
        }

        return START_STICKY;
    }

    private void stopRecordingInternal() {
        if (recorder != null) {
            try {
                recorder.stop();
            } catch (Exception e) {
                Log.e(TAG, "Error stopping recorder", e);
                writeExceptionToServiceLog(e);
            }
            recorder = null;
        }
    }

    @Override
    public void onDestroy() {
        stopRecordingInternal();
        super.onDestroy();
    }

    @Nullable
    @Override
    public IBinder onBind(Intent intent) {
        return null;
    }

    private Notification buildNotification(String text) {
        Intent stopIntent = new Intent(this, RecordingService.class);
        stopIntent.setAction(ACTION_STOP);
        PendingIntent pStop = PendingIntent.getService(this, 0, stopIntent, PendingIntent.FLAG_IMMUTABLE | PendingIntent.FLAG_UPDATE_CURRENT);

        return new NotificationCompat.Builder(this, CHANNEL_ID)
            .setContentTitle("Screen Recorder Test")
            .setContentText(text)
            .setSmallIcon(android.R.drawable.ic_menu_camera)
                .setOngoing(true)
                .addAction(new NotificationCompat.Action(0, "Detener", pStop))
                .build();
    }

    private void createNotificationChannel() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            NotificationChannel channel = new NotificationChannel(CHANNEL_ID, "Grabación de pantalla", NotificationManager.IMPORTANCE_LOW);
            NotificationManager nm = (NotificationManager) getSystemService(Context.NOTIFICATION_SERVICE);
            if (nm != null) {
                nm.createNotificationChannel(channel);
            }
        }
    }

    private void writeToServiceLog(String text) {
        if (serviceLogFile == null) return;
        String timestamp = new SimpleDateFormat("yyyy-MM-dd HH:mm:ss", Locale.US).format(new Date());
        String line = "[" + timestamp + "] " + text + "\n";
        try (FileOutputStream fos = new FileOutputStream(serviceLogFile, true)) {
            fos.write(line.getBytes(StandardCharsets.UTF_8));
        } catch (IOException e) {
            Log.e(TAG, "Error writing service log", e);
        }
    }

    private void writeExceptionToServiceLog(Throwable t) {
        if (serviceLogFile == null) return;
        StringWriter sw = new StringWriter();
        t.printStackTrace(new PrintWriter(sw));
        writeToServiceLog(sw.toString());
    }
}
