# Script para obtener el ultimo crash log

Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "  Obteniendo ultimo crash log" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host ""

# Obtener ultimas lineas del logcat buscando errores
$crashLog = adb logcat -d -v time | Select-String -Pattern "AndroidRuntime|FATAL|Exception" -Context 5,10

if ($crashLog) {
    Write-Host "[!] Crash detectado:" -ForegroundColor Red
    Write-Host ""
    $crashLog | ForEach-Object { Write-Host $_.Line }
} else {
    Write-Host "[OK] No se detectaron crashes recientes" -ForegroundColor Green
}

Write-Host ""
Write-Host "Para ver logs en tiempo real ejecuta:" -ForegroundColor Yellow
Write-Host "  .\view_logs.ps1 -Clear" -ForegroundColor White
