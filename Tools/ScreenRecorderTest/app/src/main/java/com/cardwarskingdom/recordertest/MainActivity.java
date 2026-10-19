package com.cardwarskingdom.recordertest;

import android.Manifest;
import android.app.Activity;
import android.content.Context;
import android.content.Intent;
import android.content.pm.PackageManager;
import android.media.projection.MediaProjectionManager;
import android.os.Build;
import android.os.Bundle;
import android.os.Environment;
import java.io.FileOutputStream;
import java.io.IOException;
import java.nio.charset.StandardCharsets;
import android.util.Log;
import android.view.View;
import android.view.Gravity;
import android.content.Intent;
import android.widget.Button;
import android.widget.ScrollView;
import android.widget.TextView;
import android.widget.Toast;

import androidx.core.app.ActivityCompat;
import androidx.core.content.ContextCompat;

import java.io.File;
import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.Locale;

/**
 * Activity de prueba para MediaProjection + MediaCodec
 * Testing rápido sin Unity
 */
public class MainActivity extends Activity {
    private static final String TAG = "RecorderTest";
    private static final int REQUEST_CODE_SCREEN_CAPTURE = 1000;
    private static final int REQUEST_CODE_AUDIO_PERMISSION = 2000;
    
    private Button btnStartRecording;
    private Button btnStopRecording;
    private Button btnClearLog;
    private TextView tvLog;
    private ScrollView scrollView;
    private java.io.File logFile;
    
    private MediaProjectionManager projectionManager;
    private ScreenRecorderEngine recorder;
    private boolean isRecording = false;
    
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        
        // Crear UI simple
        createSimpleUI();
        
        // Inicializar
        projectionManager = (MediaProjectionManager) getSystemService(Context.MEDIA_PROJECTION_SERVICE);
        
        log("✅ App iniciada correctamente");
        log("📱 Dispositivo: " + Build.MANUFACTURER + " " + Build.MODEL);
        log("🤖 Android: " + Build.VERSION.RELEASE + " (API " + Build.VERSION.SDK_INT + ")");
        
        // Verificar permisos
        checkPermissions();

        // Preparar archivo de log en el almacenamiento privado de la app
        try {
            java.io.File docs = getExternalFilesDir(Environment.DIRECTORY_DOCUMENTS);
            if (docs == null) docs = getFilesDir();
            String timestamp = new SimpleDateFormat("yyyyMMdd_HHmmss", Locale.US).format(new Date());
            logFile = new java.io.File(docs, "crash_log_" + timestamp + ".txt");
            if (!logFile.exists()) {
                logFile.getParentFile().mkdirs();
                logFile.createNewFile();
            }
            // Header inicial
            try (FileOutputStream fos = new FileOutputStream(logFile, true)) {
                String header = "Log creado: " + new SimpleDateFormat("yyyy-MM-dd HH:mm:ss", Locale.US).format(new Date()) + "\n";
                fos.write(header.getBytes(StandardCharsets.UTF_8));
            }
            log("📄 Log guardado en: " + logFile.getAbsolutePath());
        } catch (IOException e) {
            Log.e(TAG, "No se pudo crear archivo de log", e);
            logFile = null;
        }
    }
    
    private void createSimpleUI() {
        // Layout programático simple
        android.widget.LinearLayout layout = new android.widget.LinearLayout(this);
        layout.setOrientation(android.widget.LinearLayout.VERTICAL);
        // Aumentar padding superior para Android 15 (Edge-to-Edge)
        layout.setPadding(32, 150, 32, 32);
        layout.setGravity(Gravity.TOP | Gravity.CENTER_HORIZONTAL);
        
        // Título
        TextView title = new TextView(this);
        title.setText("🎬 Screen Recorder Test");
        title.setTextSize(24);
        android.widget.LinearLayout.LayoutParams titleParams = new android.widget.LinearLayout.LayoutParams(
            android.widget.LinearLayout.LayoutParams.MATCH_PARENT,
            android.widget.LinearLayout.LayoutParams.WRAP_CONTENT);
        // Más espacio debajo del título para separar de los botones
        titleParams.setMargins(0, 0, 0, 60);
        title.setLayoutParams(titleParams);
        title.setGravity(Gravity.CENTER_HORIZONTAL);
        layout.addView(title);
        
        // Botón Start
        btnStartRecording = new Button(this);
        btnStartRecording.setText("▶️ Iniciar Grabación (10 seg)");
        btnStartRecording.setOnClickListener(v -> startRecording());
        android.widget.LinearLayout.LayoutParams btnParams = new android.widget.LinearLayout.LayoutParams(
            android.widget.LinearLayout.LayoutParams.MATCH_PARENT,
            android.widget.LinearLayout.LayoutParams.WRAP_CONTENT);
        // Separación clara entre botones
        btnParams.setMargins(0, 20, 0, 0);
        btnStartRecording.setLayoutParams(btnParams);
        layout.addView(btnStartRecording);
        
        // Botón Stop
        btnStopRecording = new Button(this);
        btnStopRecording.setText("⏹️ Detener Grabación");
        btnStopRecording.setEnabled(false);
        btnStopRecording.setOnClickListener(v -> stopRecording());
        btnStopRecording.setLayoutParams(btnParams);
        layout.addView(btnStopRecording);
        
        // Botón Clear
        btnClearLog = new Button(this);
        btnClearLog.setText("🗑️ Limpiar Log");
        btnClearLog.setOnClickListener(v -> {
            tvLog.setText("");
            log("Log limpiado");
        });
        btnClearLog.setLayoutParams(btnParams);
        layout.addView(btnClearLog);
        
        // Log area
        TextView logLabel = new TextView(this);
        logLabel.setText("\n📋 Logs:");
        logLabel.setTextSize(16);
        layout.addView(logLabel);
        
        scrollView = new ScrollView(this);
        android.widget.LinearLayout.LayoutParams scrollParams = 
            new android.widget.LinearLayout.LayoutParams(
                android.widget.LinearLayout.LayoutParams.MATCH_PARENT,
                0, 1.0f);
        scrollView.setLayoutParams(scrollParams);
        
        tvLog = new TextView(this);
        tvLog.setTextSize(12);
        tvLog.setPadding(16, 16, 16, 16);
        tvLog.setBackgroundColor(0xFF111111);
        tvLog.setTextColor(0xFFCCCCCC);
        scrollView.addView(tvLog);
        layout.addView(scrollView);
        
        setContentView(layout);
    }
    
    private void checkPermissions() {
        // Verificar permiso de audio
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M) {
            if (ContextCompat.checkSelfPermission(this, Manifest.permission.RECORD_AUDIO) 
                != PackageManager.PERMISSION_GRANTED) {
                log("⚠️ Solicitando permiso de audio...");
                ActivityCompat.requestPermissions(this, 
                    new String[]{Manifest.permission.RECORD_AUDIO}, 
                    REQUEST_CODE_AUDIO_PERMISSION);
            } else {
                log("✅ Permiso de audio concedido");
            }
        }
    }
    
    private void startRecording() {
        log("\n🎬 Iniciando proceso de grabación...");
        
        // Solicitar permiso de captura de pantalla
        Intent captureIntent = projectionManager.createScreenCaptureIntent();
        startActivityForResult(captureIntent, REQUEST_CODE_SCREEN_CAPTURE);
    }
    
    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);
        
        if (requestCode == REQUEST_CODE_SCREEN_CAPTURE) {
            if (resultCode == RESULT_OK) {
                log("✅ Permiso de grabación concedido");

                // Validar Intent de permiso
                if (data == null) {
                    log("❌ ERROR: Intent data es null — no se puede iniciar servicio");
                    Toast.makeText(this, "Error: datos de permiso inválidos", Toast.LENGTH_SHORT).show();
                    return;
                }
                log("✅ Intent data OK (action=" + data.getAction() + ")");
                
                // Crear directorio de salida
                File outputDir = getExternalFilesDir("ScreenRecordings");
                if (outputDir == null) {
                    outputDir = new File(getFilesDir(), "ScreenRecordings");
                }
                if (!outputDir.exists()) {
                    outputDir.mkdirs();
                }
                String timestamp = new SimpleDateFormat("yyyyMMdd_HHmmss", Locale.US).format(new Date());
                File outputFile = new File(outputDir, "test_recording_" + timestamp + ".mp4");
                
                log("📁 Archivo: " + outputFile.getAbsolutePath());
                
                // Iniciar el servicio y pasar resultCode/data para que el servicio
                // haga getMediaProjection() DESPUÉS de startForeground.
                try {
                    Intent serviceIntent = new Intent(this, RecordingService.class);
                    serviceIntent.putExtra("resultCode", resultCode);
                    serviceIntent.putExtra("data", data);
                    if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
                        ContextCompat.startForegroundService(this, serviceIntent);
                    } else {
                        startService(serviceIntent);
                    }

                    log("🔴 Grabación iniciada (servicio)");
                    isRecording = true;
                    btnStartRecording.setEnabled(false);
                    btnStopRecording.setEnabled(true);

                    // Auto-detener después de 10 segundos (para testing)
                    tvLog.postDelayed(() -> {
                        if (isRecording) {
                            log("⏰ 10 segundos alcanzados, deteniendo...");
                            stopRecording();
                        }
                    }, 10000);
                } catch (Exception e) {
                    log("❌ Error al iniciar servicio de grabación: " + e.getMessage());
                    Log.e(TAG, "Error starting recording service", e);
                }
            } else {
                log("❌ Permiso de grabación denegado");
                Toast.makeText(this, "Permiso denegado", Toast.LENGTH_SHORT).show();
            }
        }
    }
    
    private void stopRecording() {
        // Enviar intención para detener el servicio
        if (isRecording) {
            log("⏹️ Deteniendo grabación (servicio)...");
            Intent stopIntent = new Intent(this, RecordingService.class);
            stopIntent.setAction(RecordingService.ACTION_STOP);
            startService(stopIntent);

            isRecording = false;
            btnStartRecording.setEnabled(true);
            btnStopRecording.setEnabled(false);
        }
    }
    
    @Override
    protected void onDestroy() {
        super.onDestroy();
        if (recorder != null) {
            recorder.stop();
        }
    }
    
    private void log(String message) {
        String timestamp = new SimpleDateFormat("HH:mm:ss", Locale.US).format(new Date());
        String logMessage = "[" + timestamp + "] " + message + "\n";
        
        runOnUiThread(() -> {
            tvLog.append(logMessage);
            scrollView.post(() -> scrollView.fullScroll(View.FOCUS_DOWN));
        });
        
        Log.d(TAG, message);

        // También guardar en archivo para facilitar copia/pegado
        if (logFile != null) {
            try (FileOutputStream fos = new FileOutputStream(logFile, true)) {
                fos.write(logMessage.getBytes(StandardCharsets.UTF_8));
            } catch (IOException e) {
                Log.e(TAG, "Error escribiendo log a archivo", e);
            }
        }
    }
}
