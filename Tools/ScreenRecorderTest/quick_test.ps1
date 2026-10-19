# Script de compilacion e instalacion ultra-rapida
# Compila -> Instala -> Ejecuta -> Muestra logs en 15-30 segundos

param(
    [switch]$Clean,
    [switch]$NoInstall,
    [switch]$NoRun,
    [switch]$Watch
)

$ErrorActionPreference = "Stop"
$projectPath = "$PSScriptRoot"
$packageName = "com.cardwarskingdom.recordertest"
$mainActivity = "$packageName/.MainActivity"

function Write-Step {
    param([string]$Message, [string]$Color = "Cyan")
    Write-Host ""
    Write-Host $Message -ForegroundColor $Color
}

function Write-Success {
    param([string]$Message)
    Write-Host "[OK] $Message" -ForegroundColor Green
}

function Write-Error-Custom {
    param([string]$Message)
    Write-Host "[X] $Message" -ForegroundColor Red
}

# Verificar ADB
if (!(Get-Command adb -ErrorAction SilentlyContinue)) {
    Write-Error-Custom "ADB no encontrado. Instala Android SDK Platform Tools"
    exit 1
}

# Verificar dispositivo conectado
$devices = adb devices | Select-String -Pattern "device$"
if (!$devices) {
    Write-Error-Custom "No hay dispositivos conectados"
    Write-Host "Conecta tu dispositivo con USB debugging habilitado" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "  Screen Recorder Test - Build & Deploy Rapido" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan

# Clean build si se solicita
if ($Clean) {
    Write-Step "[*] Limpiando proyecto..."
    Push-Location $projectPath
    if (Test-Path "app\build") {
        Remove-Item -Recurse -Force "app\build" -ErrorAction SilentlyContinue
    }
    .\gradlew.bat clean
    Pop-Location
    Write-Success "Proyecto limpiado"
}

# Compilar APK
Write-Step "[*] Compilando APK..."
Write-Host "[!] Primera compilacion puede tardar 2-5 minutos descargando dependencias..." -ForegroundColor Yellow
Write-Host ""
$startTime = Get-Date

Push-Location $projectPath

# Usar gradlew para compilar - mostrar salida en tiempo real
Write-Host "Ejecutando: gradlew.bat assembleDebug" -ForegroundColor Gray
Write-Host ""

try {
    # Ejecutar Gradle mostrando salida en tiempo real
    & .\gradlew.bat assembleDebug
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error-Custom "Compilacion fallida (codigo de salida: $LASTEXITCODE)"
        Pop-Location
        exit 1
    }
} catch {
    Write-Error-Custom "Error ejecutando Gradle: $_"
    Pop-Location
    exit 1
}

Pop-Location

$buildTime = (Get-Date) - $startTime
Write-Host ""
Write-Success "APK compilado en $([math]::Round($buildTime.TotalSeconds, 1)) segundos"

# Encontrar APK
$apkPath = Get-ChildItem -Path "$projectPath\app\build\outputs\apk\debug" -Filter "*.apk" | 
    Sort-Object LastWriteTime -Descending | 
    Select-Object -First 1

if (!$apkPath) {
    Write-Error-Custom "APK no encontrado"
    exit 1
}

Write-Host "[+] APK: $($apkPath.Name) ($([math]::Round($apkPath.Length / 1MB, 2)) MB)" -ForegroundColor White

# Instalar si no se especificó -NoInstall
if (!$NoInstall) {
    Write-Step "[*] Instalando en dispositivo..."
    
    # Desinstalar versión anterior si existe
    adb uninstall $packageName 2>&1 | Out-Null
    
    # Instalar nueva versión
    $installOutput = adb install -r $apkPath.FullName 2>&1
    
    if ($installOutput -match "Success") {
        Write-Success "APK instalado correctamente"
    } else {
        Write-Error-Custom "Error al instalar"
        Write-Host $installOutput -ForegroundColor Red
        exit 1
    }
}

# Ejecutar app si no se especificó -NoRun
if (!$NoRun -and !$NoInstall) {
    Write-Step "[*] Ejecutando app..."
    
    # Limpiar logs previos
    adb logcat -c
    
    # Iniciar activity
    adb shell am start -n $mainActivity 2>&1 | Out-Null
    Start-Sleep -Seconds 1
    
    Write-Success "App iniciada"
    
    # Mostrar logs
    Write-Step "[*] Logs en tiempo real (Ctrl+C para detener):"
    Write-Host ""
    
    try {
        adb logcat -v time | Select-String -Pattern "RecorderTest|RecorderEngine|AndroidRuntime|FATAL" --LineBuffered
    } catch {
        # Usuario presiono Ctrl+C
    }
}

Write-Host ""
Write-Host "[OK] Proceso completado" -ForegroundColor Green
