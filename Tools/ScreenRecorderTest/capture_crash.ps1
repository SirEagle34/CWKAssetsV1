# Script para capturar TODOS los logs mientras reproduces el crash
# Guarda TODO en un archivo para analisis posterior

$logFile = "full_crash_log_$(Get-Date -Format 'yyyyMMdd_HHmmss').txt"

Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "  Capturando TODOS los logs" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "[*] Limpiando logs anteriores..." -ForegroundColor Yellow
adb logcat -c
Write-Host "[OK] Logs limpiados" -ForegroundColor Green
Write-Host ""
Write-Host "[*] Guardando logs en: $logFile" -ForegroundColor Yellow
Write-Host ""
Write-Host "INSTRUCCIONES:" -ForegroundColor Green
Write-Host "  1. Abre la app en el telefono" -ForegroundColor White
Write-Host "  2. Presiona el boton de grabar" -ForegroundColor White
Write-Host "  3. Cuando la app se cierre, vuelve aqui" -ForegroundColor White
Write-Host "  4. Presiona Ctrl+C para detener" -ForegroundColor White
Write-Host ""
Write-Host "Esperando... (Ctrl+C para detener)" -ForegroundColor Yellow
Write-Host ""

try {
    # Capturar TODOS los logs sin filtros
    adb logcat -v time | Tee-Object -FilePath $logFile
} catch {
    # Usuario presiono Ctrl+C
}

Write-Host ""
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "[OK] Logs capturados: $logFile" -ForegroundColor Green
Write-Host ""

# Buscar errores en el archivo
Write-Host "[*] Buscando errores..." -ForegroundColor Yellow

$errors = Select-String -Path $logFile -Pattern "AndroidRuntime|FATAL|Exception|RecorderTest|RecorderEngine" | Select-Object -First 50

if ($errors) {
    Write-Host ""
    Write-Host "[!] Errores encontrados:" -ForegroundColor Red
    Write-Host ""
    $errors | ForEach-Object { 
        $line = $_.Line
        if ($line -match "FATAL|Exception") {
            Write-Host $line -ForegroundColor Red
        } elseif ($line -match "RecorderTest|RecorderEngine") {
            Write-Host $line -ForegroundColor Yellow
        } else {
            Write-Host $line -ForegroundColor Gray
        }
    }
} else {
    Write-Host "[!] No se encontraron errores evidentes" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Archivo completo: $logFile" -ForegroundColor Cyan
Write-Host "Puedes abrirlo con: notepad $logFile" -ForegroundColor Gray
