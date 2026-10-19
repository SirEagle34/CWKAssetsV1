# Script to analyze video encoding failures from logs
# Usage: .\analyze_encoding_failure.ps1 [log_file_path]

param(
    [string]$LogFile
)

function Get-LatestLog {
    $serviceLogs = Get-ChildItem "service_log_*.txt" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    $crashLogs = Get-ChildItem "crash_log_*.txt" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    
    if ($serviceLogs) { return $serviceLogs.FullName }
    if ($crashLogs) { return $crashLogs.FullName }
    return $null
}

if (-not $LogFile) {
    $LogFile = Get-LatestLog
    if (-not $LogFile) {
        Write-Host "❌ No log files found. Run a recording test first." -ForegroundColor Red
        exit 1
    }
    Write-Host "📄 Using latest log: $LogFile" -ForegroundColor Cyan
}

Write-Host "`n===== VIDEO ENCODING FAILURE DIAGNOSIS =====" -ForegroundColor Yellow
Write-Host "📋 Log File: $LogFile`n" -ForegroundColor Cyan

# Check file existence
if (-not (Test-Path $LogFile)) {
    Write-Host "❌ Log file not found: $LogFile" -ForegroundColor Red
    exit 1
}

$content = Get-Content $LogFile -Raw

# Diagnosis checks
$issues = @()
$success = $false

# 1. Check if recording started
if ($content -match "Grabación iniciada correctamente") {
    Write-Host "✅ Recording started successfully" -ForegroundColor Green
} else {
    Write-Host "❌ Recording never started" -ForegroundColor Red
    $issues += "Recording initialization failed"
}

# 2. Check for encoder configuration errors
if ($content -match "❌ VIDEO ENCODER CONFIGURATION FAILED|❌ MUXER CREATION FAILED") {
    Write-Host "❌ Encoder/Muxer configuration failed" -ForegroundColor Red
    $issues += "Hardware encoder or muxer initialization error"
    $matches = [regex]::Matches($content, "❌ (.*?)(?=\n|$)")
    foreach ($match in $matches) {
        Write-Host "   └─ $($match.Groups[1].Value)" -ForegroundColor Yellow
    }
}

# 3. Check if muxer was started
if ($content -match "✅ MUXER STARTED") {
    Write-Host "✅ Muxer started successfully" -ForegroundColor Green
} elseif ($content -match "❌ MUXER NEVER STARTED") {
    Write-Host "❌ CRITICAL: Muxer was never started - no video file generated" -ForegroundColor Red
    $issues += "Codec format change event not received - likely encoder hardware issue"
} else {
    Write-Host "⚠️  Muxer status unclear" -ForegroundColor Yellow
    $issues += "Unknown muxer initialization state"
}

# 4. Check for timeout errors
if ($content -match "EOS DRAIN TIMEOUT|EOS drain timeout") {
    Write-Host "❌ EOS (End of Stream) timeout detected" -ForegroundColor Red
    $issues += "Encoder did not finish outputting frames in time"
}

# 5. Check for write failures
if ($content -match "MUXER WRITE FAILED|writeSampleData failed") {
    Write-Host "❌ Muxer write failures detected" -ForegroundColor Red
    $issues += "Failed to write encoded frames to output file"
}

# 6. Check final file size
if ($content -match "final size=(\d+)") {
    $match = [regex]::Match($content, "final size=(\d+)")
    $size = [int]$match.Groups[1].Value
    if ($size -eq 0) {
        Write-Host "❌ Output file is EMPTY (0 bytes)" -ForegroundColor Red
        $issues += "Video encoding produced no output file"
    } elseif ($size -lt 10000) {
        Write-Host "⚠️  Output file is very small: $size bytes" -ForegroundColor Yellow
        $issues += "Video file size suspiciously small - likely corrupted"
    } else {
        Write-Host "✅ Output file generated: $size bytes" -ForegroundColor Green
        $success = $true
    }
}

# 7. Check for format change events
$formatMatches = [regex]::Matches($content, "OUTPUT_FORMAT_CHANGED")
if ($formatMatches.Count -gt 0) {
    Write-Host "✅ Format change events received: $($formatMatches.Count)" -ForegroundColor Green
} else {
    Write-Host "⚠️  No format change events detected" -ForegroundColor Yellow
    $issues += "Encoder may not be outputting data properly"
}

# Summary
Write-Host "`n===== DIAGNOSIS SUMMARY =====" -ForegroundColor Yellow

if ($success) {
    Write-Host "✅ VIDEO ENCODING LIKELY SUCCESSFUL - Check output file" -ForegroundColor Green
} else {
    Write-Host "❌ VIDEO ENCODING FAILED WITH ISSUES:" -ForegroundColor Red
    $issues | ForEach-Object {
        Write-Host "   • $_" -ForegroundColor Red
    }
    
    Write-Host "`n📋 RECOMMENDED ACTIONS:" -ForegroundColor Cyan
    Write-Host "   1. Clear cache: adb shell pm clear com.cardwarskingdom.recordertest" -ForegroundColor White
    Write-Host "   2. Rebuild APK: .\quick_test.ps1" -ForegroundColor White
    Write-Host "   3. Check device storage: adb shell df /sdcard" -ForegroundColor White
    Write-Host "   4. Try lower resolution: Modify TARGET_LONG_EDGE in ScreenRecorderEngine.java" -ForegroundColor White
    Write-Host "   5. Check device compatibility: Some devices have hardware encoder issues" -ForegroundColor White
}

Write-Host "`n❓ For detailed logs, check:" -ForegroundColor Cyan
Write-Host "   Service Log: adb pull /sdcard/Android/data/com.cardwarskingdom.recordertest/files/Documents/" -ForegroundColor White
Write-Host "   System Log:  adb logcat -d | grep -i 'ScreenRecorder'" -ForegroundColor White
