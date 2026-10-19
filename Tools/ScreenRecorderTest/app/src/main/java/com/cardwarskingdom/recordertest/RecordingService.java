package com.cardwarskingdom.recordertest;

import android.app.Notification;
import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.app.PendingIntent;
import android.app.Service;
import android.content.Context;
import android.content.Intent;
import android.media.projection.MediaProjection;
import android.media.projection.MediaProjectionManager;
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

/**
 * Servicio en primer plano que ejecuta la grabación de pantalla.
 * Actualizado para Android 15: Manejo seguro de Parcelables y Foreground Service.
 */
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
            if (recorder != null) {
                writeToServiceLog("Deteniendo motor de grabación...");
                try {
                    recorder.stop();
                } catch (Exception e) {
                    writeExceptionToServiceLog(e);
                }
                // Delay breve para permitir que el muxer cierre el archivo
                new android.os.Handler(getMainLooper()).postDelayed(this::stopSelf, 1000);
            } else {
                stopSelf();
            }
            return START_NOT_STICKY;
        }

        // 1. Iniciar Foreground Service INMEDIATAMENTE (Requisito Android 14/15)
        Notification notif = buildNotification("Grabando pantalla...");
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q) {
            startForeground(NOTIF_ID, notif,
                android.content.pm.ServiceInfo.FOREGROUND_SERVICE_TYPE_MEDIA_PROJECTION);
        } else {
            startForeground(NOTIF_ID, notif);
        }

        // Obtener extras de permiso desde la Activity
        int resultCode = intent.getIntExtra("resultCode", -1);
        Intent data;
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
            data = intent.getParcelableExtra("data", Intent.class);
        } else {
            data = intent.getParcelableExtra("data");
        }

        if (resultCode != android.app.Activity.RESULT_OK || data == null) {
            Log.e(TAG, "Missing/invalid resultCode or data for MediaProjection");
            writeToServiceLog("ERROR: resultCode/data inválidos para MediaProjection");
            stopSelf();
            return START_NOT_STICKY;
        }

        // IMPORTANTE Android 14/15: llamar getMediaProjection() después de startForeground
        MediaProjectionManager projMgr =
                (MediaProjectionManager) getSystemService(Context.MEDIA_PROJECTION_SERVICE);
        MediaProjection mediaProjection = projMgr.getMediaProjection(resultCode, data);
        if (mediaProjection == null) {
            Log.e(TAG, "getMediaProjection() returned null");
            writeToServiceLog("ERROR: getMediaProjection devolvió null");
            stopSelf();
            return START_NOT_STICKY;
        }
        Log.d(TAG, "MediaProjection obtenido en servicio OK");
        writeToServiceLog("MediaProjection obtenido en servicio");

        // 3. Configurar archivo de salida
        File outputDir = getExternalFilesDir("ScreenRecordings");
        if (outputDir == null) {
            outputDir = new File(getFilesDir(), "ScreenRecordings");
        }
        if (!outputDir.exists()) {
            boolean created = outputDir.mkdirs();
            Log.d(TAG, "Output directory created: " + created + " at " + outputDir.getAbsolutePath());
            writeToServiceLog("Directorio creado: " + outputDir.getAbsolutePath());
        }
        
        String timestamp = new SimpleDateFormat("yyyyMMdd_HHmmss", Locale.US).format(new Date());
        String outputPath = new File(outputDir, "test_recording_" + timestamp + ".mp4").getAbsolutePath();

        // 4. Iniciar motor de grabación
        try {
            writeToServiceLog("Iniciando grabador en: " + outputPath);
            recorder = new ScreenRecorderEngine(this, mediaProjection, outputPath);
            recorder.setCallback(new ScreenRecorderEngine.RecorderCallback() {
                @Override
                public void onRecordingStarted() {
                    writeToServiceLog("✅ Grabación iniciada correctamente");
                }

                @Override
                public void onRecordingStopped(String path) {
                    File outFile = new File(path);
                    long fileSize = outFile.exists() ? outFile.length() : 0;
                    writeToServiceLog("✅ Grabación finalizada: " + path + " (tamaño: " + fileSize + " bytes)");
                    if (fileSize == 0) {
                        writeToServiceLog("❌ ERROR: Archivo de salida vacío - encoding probablemente falló");
                    }
                    stopSelf();
                }

                @Override
                public void onError(String error) {
                    writeToServiceLog("❌ Error en grabación: " + error);
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
        PendingIntent pStop = PendingIntent.getService(this, 0, stopIntent, 
                PendingIntent.FLAG_IMMUTABLE | PendingIntent.FLAG_UPDATE_CURRENT);

        return new NotificationCompat.Builder(this, CHANNEL_ID)
            .setContentTitle("Grabador de Pantalla")
            .setContentText(text)
            .setSmallIcon(android.R.drawable.ic_menu_camera)
            .setOngoing(true)
            .setPriority(NotificationCompat.PRIORITY_LOW)
            .addAction(new NotificationCompat.Action(0, "Detener", pStop))
            .build();
    }

    private void createNotificationChannel() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            NotificationChannel channel = new NotificationChannel(CHANNEL_ID, 
                "Grabación de pantalla", NotificationManager.IMPORTANCE_LOW);
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
