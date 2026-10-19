# Script para buscar y analizar logs existentes

Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "  Buscando archivos de log existentes" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host ""

# Buscar archivos de log
$logFiles = Get-ChildItem -Path . -Filter "*crash_log*.txt" | Sort-Object LastWriteTime -Descending

if ($logFiles) {
    Write-Host "[OK] Encontrados $($logFiles.Count) archivo(s) de log:" -ForegroundColor Green
    Write-Host ""
    
    foreach ($file in $logFiles) {
        Write-Host "  - $($file.Name) ($([math]::Round($file.Length/1KB, 2)) KB) - $($file.LastWriteTime)" -ForegroundColor White
    }
    
    Write-Host ""
    $latestLog = $logFiles[0]
    Write-Host "[*] Analizando el mas reciente: $($latestLog.Name)" -ForegroundColor Yellow
    Write-Host ""
    
    # Leer contenido
    $content = Get-Content $latestLog.FullName
    
    # Buscar errores fatales
    $fatalErrors = $content | Select-String -Pattern "FATAL|AndroidRuntime" -Context 0,5
    $exceptions = $content | Select-String -Pattern "Exception" -Context 0,3
    $appLogs = $content | Select-String -Pattern "RecorderTest|RecorderEngine|MainActivity"
    
    if ($fatalErrors) {
        Write-Host "====== ERRORES FATALES ======" -ForegroundColor Red
        Write-Host ""
        $fatalErrors | ForEach-Object {
            Write-Host $_.Line -ForegroundColor Red
            $_.Context.PostContext | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
        }
    }
    
    if ($exceptions) {
        Write-Host ""
        Write-Host "====== EXCEPCIONES ======" -ForegroundColor Red
        Write-Host ""
        $exceptions | Select-Object -First 10 | ForEach-Object {
            Write-Host $_.Line -ForegroundColor Yellow
        }
    }
    
    if ($appLogs) {
        Write-Host ""
        Write-Host "====== LOGS DE LA APP (ultimos 20) ======" -ForegroundColor Cyan
        Write-Host ""
        $appLogs | Select-Object -Last 20 | ForEach-Object {
            Write-Host $_.Line -ForegroundColor White
        }
    }
    
    Write-Host ""
    Write-Host "Ver archivo completo: notepad $($latestLog.Name)" -ForegroundColor Cyan
    
} else {
    Write-Host "[!] No se encontraron archivos de log" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Ejecuta primero: .\capture_direct.ps1" -ForegroundColor White
}
