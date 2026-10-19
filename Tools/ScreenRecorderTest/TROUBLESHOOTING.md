# Quick Troubleshooting Guide - Video Encoding Failures

## 🚨 Video Encoding Failed?

### Step 1: Analyze the Logs (30 seconds)
```powershell
cd Tools\ScreenRecorderTest
.\analyze_encoding_failure.ps1
```

### Step 2: Identify the Issue

| Error Message | Root Cause | Fix |
|---|---|---|
| ❌ `MUXER NEVER STARTED` | Encoder not outputting format | Lower resolution: Change `TARGET_LONG_EDGE = 1280` |
| ❌ `VIDEO ENCODER CONFIGURATION FAILED` | Device doesn't support H.264 | Try different device or check codec support |
| ❌ `MUXER WRITE FAILED` | Output buffer corruption | Increase bitrate or reduce resolution |
| ❌ `EOS DRAIN TIMEOUT` | Encoder stuck, not finishing | Clear app data & try again |
| ⚠️ `FINAL OUTPUT SIZE IS ZERO` | No frames captured | Check permissions & storage |

---

## Common Solutions by Symptom

### **Issue: "No Output Video Generated"**
```powershell
# Clear app data (removes corrupted state)
adb shell pm clear com.cardwarskingdom.recordertest

# Rebuild and test
cd Tools\ScreenRecorderTest
.\quick_test.ps1
```

### **Issue: "Output File is Too Small" (<100KB for 10sec video)**
Edit `ScreenRecorderEngine.java`:
```java
// Line 39-40: Reduce resolution
private static final int TARGET_LONG_EDGE = 1280;  // Was 2560

// Line 41: Reduce bitrate
private static final int VIDEO_BITRATE = 3_000_000;  // Was 6_000_000
```

Then rebuild:
```powershell
.\gradlew clean assembleDebug
adb install -r app/build/outputs/apk/debug/app-debug.apk
```

### **Issue: "Hardware Encoder Not Found"**
Check if device supports H.264:
```powershell
adb logcat -s "codec" | findstr /i "h264"
# Look for "MediaCodec" or "HW-based" messages
```

If no H.264 encoder:
- Try a different device
- Enable software fallback (advanced fix)

### **Issue: "Storage Space Low"**
```powershell
# Check available space
adb shell df /sdcard

# Clear cache if needed
adb shell rm -rf /sdcard/Android/data/com.cardwarskingdom.recordertest/cache/*
```

---

## Advanced: Enable Verbose Logging

To get maximum detail in logs:

1. **Edit ScreenRecorderEngine.java** (line 39):
```java
private static final String TAG = "ScreenRecorderEngine";  // Already good
```

2. **Run with system logcat capture**:
```powershell
# Terminal 1: Capture logs
adb shell "logcat -s ScreenRecorderEngine:D -v threadtime" > recording_debug.log &

# Terminal 2: Run test
.\quick_test.ps1

# Terminal 1: Ctrl+C after test completes, examine recording_debug.log
```

---

## Success Checklist

A successful recording should show these in order:

- [ ] ✅ Video encoder configured: WIDTHxHEIGHT @ 30fps
- [ ] ✅ Muxer created: /sdcard/Android/data/.../Movies/test_recording_*.mp4
- [ ] ✅ MUXER STARTED videoTrack=0
- [ ] ✅ writeSampleData size=XXXX (repeated many times)
- [ ] ✅ MUXER STOPPED — size=XXXXX bytes  
- [ ] 📁 File exists at: `/sdcard/Android/data/com.cardwarskingdom.recordertest/files/Movies/`
- [ ] 📊 File size > 100KB (for a 10-second video)

If any ❌ or size == 0: Encoding **FAILED**

---

## Emergency: Force Minimal Configuration

If nothing else works, use minimal settings:

**ScreenRecorderEngine.java** modifications:
```java
// Line 39: Ultra-low resolution
private static final int TARGET_LONG_EDGE = 640;        // Down from 2560

// Line 41: Very low bitrate  
private static final int VIDEO_BITRATE = 1_000_000;     // Down from 6M

// Line 42: Lower FPS if needed
private static final int VIDEO_FPS    = 15;             // From 30

// Disable audio for testing
// Line 316: Set to false
private boolean audioEnabled = false;  // Temporarily
```

Rebuild and test:
```powershell
.\gradlew clean assembleDebug
adb install -r app/build/outputs/apk/debug/app-debug.apk
.\quick_test.ps1
```

---

## File Locations

| Item | Path |
|---|---|
| Service Logs | `/sdcard/Android/data/com.cardwarskingdom.recordertest/files/Documents/service_log_*.txt` |
| Output Videos | `/sdcard/Android/data/com.cardwarskingdom.recordertest/files/Movies/*.mp4` |
| App Cache | `/sdcard/Android/data/com.cardwarskingdom.recordertest/cache/` |
| Source Code | `Tools/ScreenRecorderTest/app/src/main/java/com/cardwarskingdom/recordertest/` |

---

## Contact / Reports

If you still see encoding failures after these steps:

1. Collect logs:
```powershell
mkdir debug_logs
adb pull /sdcard/Android/data/com.cardwarskingdom.recordertest/files/ debug_logs/
adb logcat -d > debug_logs/system_logcat.txt
echo "Device Info:" > debug_logs/device_info.txt
adb shell getprop ro.product.model >> debug_logs/device_info.txt
adb shell getprop ro.build.version.release >> debug_logs/device_info.txt
```

2. Check `debug_logs/service_log_*.txt` for ❌ ERROR lines
3. Search logs for: "MUXER", "ENCODER", "FAILED", "ERROR"
4. Verify `device_info.txt` lists your device model and Android version
