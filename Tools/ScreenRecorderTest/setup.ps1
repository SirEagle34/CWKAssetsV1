# Script para inicializar el proyecto por primera vez
# Descarga Gradle wrapper si no existe

param(
    [switch]$SkipGradle
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "  Screen Recorder Test - Setup Inicial" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan

$projectPath = $PSScriptRoot

# Verificar Java
Write-Host ""
Write-Host "[*] Verificando Java..." -ForegroundColor Yellow

# Refrescar PATH en esta sesión
$env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")

$javaFound = $false
$javaCmd = $null

# Intentar encontrar java en PATH
if (Get-Command java -ErrorAction SilentlyContinue) {
    $javaCmd = "java"
    $javaFound = $true
}

# Si no está en PATH, buscar en ubicaciones comunes
if (!$javaFound) {
    $commonJavaPaths = @(
        "$env:ProgramFiles\Eclipse Adoptium\jdk-*\bin\java.exe",
        "$env:ProgramFiles\Java\jdk-*\bin\java.exe",
        "$env:ProgramFiles\OpenJDK\jdk-*\bin\java.exe",
        "${env:ProgramFiles(x86)}\Eclipse Adoptium\jdk-*\bin\java.exe",
        "${env:ProgramFiles(x86)}\Java\jdk-*\bin\java.exe"
    )
    
    foreach ($pattern in $commonJavaPaths) {
        $found = Get-ChildItem -Path $pattern -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($found) {
            $javaCmd = $found.FullName
            $javaFound = $true
            Write-Host "[!] Java encontrado en: $javaCmd" -ForegroundColor Yellow
            Write-Host "[!] Agregando al PATH de esta sesion..." -ForegroundColor Yellow
            $env:Path = "$(Split-Path $javaCmd);$env:Path"
            break
        }
    }
}

if (!$javaFound) {
    Write-Host "[X] Java no encontrado" -ForegroundColor Red
    Write-Host ""
    Write-Host "Si acabas de instalar Java:" -ForegroundColor Yellow
    Write-Host "  1. Cierra esta ventana de PowerShell" -ForegroundColor White
    Write-Host "  2. Abre una NUEVA ventana de PowerShell" -ForegroundColor White
    Write-Host "  3. Ejecuta setup.ps1 nuevamente" -ForegroundColor White
    Write-Host ""
    Write-Host "O instala JDK si no lo has hecho:" -ForegroundColor Yellow
    Write-Host "  - winget install EclipseAdoptium.Temurin.17.JDK" -ForegroundColor White
    Write-Host "  - Descarga: https://adoptium.net/" -ForegroundColor White
    exit 1
}

# Verificar versión (java -version escribe a stderr, no stdout)
try {
    $javaVersionOutput = & $javaCmd -version 2>&1
    $javaVersion = ($javaVersionOutput | Select-String "version" | Select-Object -First 1).ToString().Trim()
    Write-Host "[OK] Java instalado: $javaVersion" -ForegroundColor Green
} catch {
    Write-Host "[OK] Java encontrado y funcional" -ForegroundColor Green
}

# Verificar Android SDK
Write-Host ""
Write-Host "[*] Verificando Android SDK..." -ForegroundColor Yellow
$androidHome = $env:ANDROID_HOME
if (!$androidHome) {
    $androidHome = "$env:LOCALAPPDATA\Android\Sdk"
}

if (!(Test-Path $androidHome)) {
    Write-Host "[!] Android SDK no encontrado en: $androidHome" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Instala Android Studio o descarga SDK Command Line Tools" -ForegroundColor White
    Write-Host "El proyecto puede compilar sin SDK pero necesitaras Android Studio para full features" -ForegroundColor Gray
} else {
    Write-Host "[OK] Android SDK: $androidHome" -ForegroundColor Green
    $env:ANDROID_HOME = $androidHome
}

# Verificar ADB
Write-Host ""
Write-Host "[*] Verificando ADB..." -ForegroundColor Yellow
if (!(Get-Command adb -ErrorAction SilentlyContinue)) {
    Write-Host "[!] ADB no encontrado en PATH" -ForegroundColor Yellow
    Write-Host "Instala Android SDK Platform Tools para usar quick_test.ps1" -ForegroundColor White
} else {
    $adbVersion = adb version 2>&1 | Select-String "Android Debug Bridge" | Select-Object -First 1
    Write-Host "[OK] $adbVersion" -ForegroundColor Green
}

# Descargar Gradle Wrapper si no existe
if (!$SkipGradle) {
    Write-Host ""
    Write-Host "[*] Verificando Gradle Wrapper..." -ForegroundColor Yellow
    
    $wrapperJar = "$projectPath\gradle\wrapper\gradle-wrapper.jar"
    
    if (!(Test-Path $wrapperJar)) {
        Write-Host "[+] Descargando gradle-wrapper.jar..." -ForegroundColor Cyan
        
        $wrapperDir = Split-Path $wrapperJar
        if (!(Test-Path $wrapperDir)) {
            New-Item -ItemType Directory -Path $wrapperDir -Force | Out-Null
        }
        
        $url = "https://raw.githubusercontent.com/gradle/gradle/master/gradle/wrapper/gradle-wrapper.jar"
        
        try {
            Invoke-WebRequest -Uri $url -OutFile $wrapperJar -UseBasicParsing
            Write-Host "[OK] Gradle wrapper descargado" -ForegroundColor Green
        } catch {
            Write-Host "[X] Error descargando gradle-wrapper.jar" -ForegroundColor Red
            Write-Host "Descarga manualmente desde: https://gradle.org/install/" -ForegroundColor Yellow
            exit 1
        }
    } else {
        Write-Host "[OK] Gradle wrapper ya existe" -ForegroundColor Green
    }
}

# Verificar estructura de proyecto
Write-Host ""
Write-Host "[*] Verificando estructura del proyecto..." -ForegroundColor Yellow

$requiredFiles = @(
    "app\build.gradle",
    "app\src\main\AndroidManifest.xml",
    "app\src\main\java\com\cardwarskingdom\recordertest\MainActivity.java",
    "app\src\main\java\com\cardwarskingdom\recordertest\ScreenRecorderEngine.java"
)

$allOK = $true
foreach ($file in $requiredFiles) {
    $fullPath = Join-Path $projectPath $file
    if (Test-Path $fullPath) {
        Write-Host "  [+] $file" -ForegroundColor Green
    } else {
        Write-Host "  [X] $file (no encontrado)" -ForegroundColor Red
        $allOK = $false
    }
}

if (!$allOK) {
    Write-Host ""
    Write-Host "[X] Faltan archivos del proyecto" -ForegroundColor Red
    exit 1
}

# Resumen
Write-Host ""
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "  Setup Completado" -ForegroundColor Green
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Siguiente paso:" -ForegroundColor Yellow
Write-Host ""
Write-Host "  1. Conecta tu dispositivo Android con USB debugging" -ForegroundColor White
Write-Host "  2. Ejecuta: .\quick_test.ps1" -ForegroundColor White
Write-Host ""
Write-Host "Documentacion:" -ForegroundColor Yellow
Write-Host "  - README.md - Guia completa" -ForegroundColor White
Write-Host "  - QUICKSTART.md - Inicio rapido" -ForegroundColor White
Write-Host "  - INTEGRATION_TO_UNITY.md - Integracion a Unity" -ForegroundColor White
Write-Host ""
Write-Host "Comandos utiles:" -ForegroundColor Yellow
Write-Host "  - .\quick_test.ps1          - Compilar + Instalar + Ejecutar" -ForegroundColor White
Write-Host "  - .\quick_test.ps1 -Clean   - Limpiar y compilar" -ForegroundColor White
Write-Host "  - adb devices               - Ver dispositivos conectados" -ForegroundColor White
Write-Host ""
Write-Host "Listo para empezar!" -ForegroundColor Green
Write-Host ""
