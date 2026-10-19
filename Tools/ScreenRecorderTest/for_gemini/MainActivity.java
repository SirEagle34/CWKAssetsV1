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
        layout.setPadding(32, 32, 32, 32);
        layout.setGravity(Gravity.TOP | Gravity.CENTER_HORIZONTAL);
        
        // Título
        TextView title = new TextView(this);
        title.setText("🎬 Screen Recorder Test");
        title.setTextSize(24);
        android.widget.LinearLayout.LayoutParams titleParams = new android.widget.LinearLayout.LayoutParams(
            android.widget.LinearLayout.LayoutParams.MATCH_PARENT,
            android.widget.LinearLayout.LayoutParams.WRAP_CONTENT);
        titleParams.setMargins(0, 0, 0, 24);
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
        btnParams.setMargins(0, 8, 0, 0);
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
                
                // Crear directorio de salida
                File outputDir = getExternalFilesDir(Environment.DIRECTORY_MOVIES);
                String timestamp = new SimpleDateFormat("yyyyMMdd_HHmmss", Locale.US).format(new Date());
                File outputFile = new File(outputDir, "test_recording_" + timestamp + ".mp4");
                
                log("📁 Archivo: " + outputFile.getAbsolutePath());
                
                // Iniciar grabación a través de un Foreground Service (recomendado en Android 15+)
                try {
                    Intent serviceIntent = new Intent(this, RecordingService.class);
                    serviceIntent.putExtra("resultCode", resultCode);
                    serviceIntent.putExtra("data", data);
                    // Usar startForegroundService en Android O+
                    if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
                        ContextCompat.startForegroundService(this, serviceIntent);
                    } else {
                        startService(serviceIntent);
                    }

I will now create the other files in for_gemini. Continue...