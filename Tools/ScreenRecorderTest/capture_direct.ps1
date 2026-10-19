# Script para capturar logs DIRECTAMENTE a archivo
# ADB escribe directo al archivo, no pasa por PowerShell

$timestamp = Get-Date -Format 'yyyyMMdd_HHmmss'
$logFile = "crash_log_$timestamp.txt"

Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "  Captura Directa de Logs" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "[*] Limpiando logs..." -ForegroundColor Yellow
adb logcat -c
Start-Sleep -Seconds 1
Write-Host "[OK] Logs limpiados" -ForegroundColor Green
Write-Host ""
Write-Host "[*] Logs se guardaran en: $logFile" -ForegroundColor Yellow
Write-Host ""
Write-Host "AHORA:" -ForegroundColor Green
Write-Host "  1. Abre la app en tu telefono" -ForegroundColor White
Write-Host "  2. Presiona el boton de GRABAR" -ForegroundColor White  
Write-Host "  3. Espera a que crashee" -ForegroundColor White
Write-Host "  4. Vuelve aqui y presiona Ctrl+C" -ForegroundColor White
Write-Host ""
Write-Host "Capturando... (Ctrl+C cuando termine)" -ForegroundColor Yellow
Write-Host ""

# Guardar DIRECTAMENTE sin pasar por PowerShell
# Esto es mas confiable
adb logcat -v time > $logFile

Write-Host ""
Write-Host "[OK] Captura detenida" -ForegroundColor Green
Write-Host ""

# Analizar el archivo
if (Test-Path $logFile) {
    $fileSize = (Get-Item $logFile).Length
    Write-Host "[OK] Archivo guardado: $logFile ($fileSize bytes)" -ForegroundColor Green
    Write-Host ""
    
    # Buscar errores
    Write-Host "[*] Buscando errores..." -ForegroundColor Yellow
    
    $content = Get-Content $logFile
    $errors = $content | Select-String -Pattern "FATAL|AndroidRuntime|Exception" | Select-Object -First 20
    $appLogs = $content | Select-String -Pattern "RecorderTest|RecorderEngine|MainActivity" | Select-Object -Last 30
    
    if ($errors) {
        Write-Host ""
        Write-Host "====== ERRORES FATALES ======" -ForegroundColor Red
        $errors | ForEach-Object { Write-Host $_ -ForegroundColor Red }
    }
    
    if ($appLogs) {
        Write-Host ""
        Write-Host "====== ULTIMOS LOGS DE LA APP ======" -ForegroundColor Yellow
        $appLogs | ForEach-Object { Write-Host $_ -ForegroundColor Yellow }
    }
    
    Write-Host ""
    Write-Host "Ver archivo completo: notepad $logFile" -ForegroundColor Cyan
} else {
    Write-Host "[X] No se creo el archivo de log" -ForegroundColor Red
}
