# Script para ver logs de la app en tiempo real y diagnosticar crashes

param(
    [switch]$Clear,
    [switch]$SaveToFile
)

$packageName = "com.cardwarskingdom.recordertest"
$logFile = "crash_log_$(Get-Date -Format 'yyyyMMdd_HHmmss').txt"

Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "  Logs de Screen Recorder Test" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host ""

if ($Clear) {
    Write-Host "[*] Limpiando logs anteriores..." -ForegroundColor Yellow
    adb logcat -c
    Write-Host "[OK] Logs limpiados" -ForegroundColor Green
    Write-Host ""
}

if ($SaveToFile) {
    Write-Host "[*] Los logs se guardaran en: $logFile" -ForegroundColor Yellow
}

Write-Host "Monitoreando logs..." -ForegroundColor Yellow
Write-Host "Presiona Ctrl+C para detener" -ForegroundColor Gray
Write-Host ""
Write-Host "Ahora abre la app y presiona el boton de grabar" -ForegroundColor Green
Write-Host ""

# Ver logs filtrados y guardar en archivo si se solicita
try {
    if ($SaveToFile) {
        # Guardar en archivo Y mostrar en pantalla
        adb logcat -v time | Tee-Object -FilePath $logFile | Select-String -Pattern "RecorderTest|RecorderEngine|AndroidRuntime|FATAL|MainActivity|MediaProjection|MediaCodec|Exception" --LineBuffered
    } else {
        # Solo mostrar en pantalla
        adb logcat -v time | Select-String -Pattern "RecorderTest|RecorderEngine|AndroidRuntime|FATAL|MainActivity|MediaProjection|MediaCodec|Exception" --LineBuffered
    }
} catch {
    # Usuario presiono Ctrl+C
    if ($SaveToFile) {
        Write-Host ""
        Write-Host "[OK] Logs guardados en: $logFile" -ForegroundColor Green
    }
}
