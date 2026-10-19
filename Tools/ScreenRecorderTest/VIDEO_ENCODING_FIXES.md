# Video Encoding Failure - Root Causes & Fixes Applied

## Issues Identified

### 1. **Insufficient Error Handling** (FIXED ✅)
The original `drainVideoEncoder()` and `drainAudioEncoder()` methods had minimal error reporting:
- Silent failures when muxer write operations failed
- No distinction between timeout vs actual encoding errors
- Errors cascaded without proper diagnostics

**Fix**: Added try-catch blocks with detailed error logging:
```java
try {
    muxer.writeSampleData(videoTrackIndex, buf, info);
} catch (Exception e) {
    Log.e(TAG, "❌ VIDEO MUXER WRITE FAILED", e);
    notifyError("Video muxer write failed: " + e.getMessage());
}
```

### 2. **Missing Initialization Checks** (FIXED ✅)
Video encoder, muxer, and input surface creation had no validation:
- `createEncoderByType()` could return null (device incompatibility)
- `createInputSurface()` could fail silently
- Muxer directory creation failures were not checked

**Fix**: Added null checks and directory validation:
```java
if (videoEncoder == null) {
    throw new IOException("createEncoderByType returned null");
}
if (!dirCreated && !outDir.exists()) {
    throw new IOException("Failed to create output directory");
}
```

### 3. **Silent Muxer Startup Failure** (FIXED ✅)
The most critical issue: If format change events don't arrive, muxer never starts, producing zero-byte files.

**Original behavior**:
```java
// No error if muxer fails to start
muxer.start();
```

**Fixed behavior**:
```java
try {
    muxer.start();
    muxerStarted = true;
    Log.d(TAG, "✅ MUXER STARTED...");
} catch (Exception e) {
    Log.e(TAG, "❌ MUXER START FAILED", e);
    notifyError("Muxer initialization failed: " + e.getMessage());
}
```

### 4. **Poor Cleanup Diagnostics** (FIXED ✅)
The cleanup method didn't report why video encoding failed:
- No distinction between successful and failed encodes
- No file size validation in final output
- Swallowed critical muxer errors

**Fix**: Added comprehensive cleanup diagnostics:
```java
if (muxerStarted) {
    try {
        muxer.stop();
        long fileSize = outFile.exists() ? outFile.length() : 0;
        Log.d(TAG, "✅ MUXER STOPPED — size=" + fileSize + " bytes");
    } catch (Exception e) {
        Log.e(TAG, "❌ MUXER.STOP() FAILED", e);
        notifyError("Muxer failed to stop: " + e.getMessage());
    }
} else {
    Log.e(TAG, "❌ MUXER NEVER STARTED - NO OUTPUT VIDEO GENERATED!");
}

if (finalSize == 0) {
    Log.e(TAG, "❌ FINAL OUTPUT SIZE IS ZERO — VIDEO ENCODING FAILED");
}
```

---

## Common Causes of Video Encoding Failures

### 1. **Hardware Encoder Not Available**
- Device doesn't support H.264 encoding
- Encoder already in use by another app
- **Solution**: Check device logs with `adb logcat | grep -i "encoder"`

### 2. **Codec Format Change Never Received**
- VirtualDisplay not properly configured
- Frame rate mismatch between display and encoder
- Dead encoder thread
- **Solution**: Look for ❌ "MUXER NEVER STARTED" in logs

### 3. **Storage/Permission Issues**
- Output directory not writable
- Insufficient free space
- File descriptor limits reached
- **Solution**: Check `adb shell df /sdcard` and permissions

### 4. **Thread Synchronization Problems**
- Encoder thread deadlock
- Output buffer pool exhaustion
- **Solution**: Check for "EOS drain timeout" errors

### 5. **Resolution Too High**
- Device can't handle requested bitrate (6 Mbps)
- Memory exhaustion
- Frame drop cascade
- **Solution**: Reduce TARGET_LONG_EDGE (currently 2560)

---

## How to Use the New Diagnostics

### Method 1: Run Analysis Script
```powershell
cd Tools\ScreenRecorderTest
./analyze_encoding_failure.ps1
```

This will:
- ✅ Check if message "MUXER STARTED" appears
- ✅ Detect encoder configuration errors
- ✅ Report final file size
- ✅ Suggest remediation steps

### Method 2: Read Service Logs Directly
```powershell
adb pull /sdcard/Android/data/com.cardwarskingdom.recordertest/files/Documents/
# Open service_log_*.txt files
```

Look for:
- 🟢 Green ✅ messages = successful operation
- 🔴 Red ❌ messages = encoding failure
- 🟡 Yellow ⚠️ messages = warnings

### Method 3: Monitor Real-Time
```powershell
adb logcat -s "ScreenRecorderEngine" -f service_log_realtime.txt &
# Run recording test
# Ctrl+C to stop
```

---

## Files Modified

1. **ScreenRecorderEngine.java**
   - Enhanced `drainVideoEncoder()` with error handling
   - Enhanced `drainAudioEncoder()` with error handling
   - Improved `cleanup()` with diagnostics
   - Added validation to `configureVideoEncoder()`
   - Fixed `startMuxerIfReady()` error handling
   - Added muxer creation error checks

2. **RecordingService.java**
   - Enhanced error callbacks with detailed logging
   - Added final file size reporting

3. **analyze_encoding_failure.ps1** (NEW)
   - Automated log analysis script
   - Diagnoses 6 common failure patterns
   - Provides remediation suggestions

---

## Next Steps for Debugging

If you still see "video encoding failed":

1. **Run the analysis script**:
   ```powershell
   .\analyze_encoding_failure.ps1
   ```

2. **Check for muxer startup failure**:
   - If you see "❌ MUXER NEVER STARTED", the issue is encoder output not working
   - Try reducing bitrate or resolution in ScreenRecorderEngine.java

3. **Check storage space**:
   ```powershell
   adb shell df /sdcard
   ```

4. **Test with logcat monitoring**:
   ```powershell
   adb logcat -c
   # Run test
   adb logcat -d -s "ScreenRecorderEngine" > debug.log
   # Examine debug.log for ❌ ERROR lines
   ```

5. **Lower resolution test**:
   In ScreenRecorderEngine.java, change:
   ```java
   private static final int TARGET_LONG_EDGE = 1280; // Down from 2560
   ```

---

## Success Indicators

When encoding works, you should see:
- ✅ Recording started successfully
- ✅ Video encoder configured
- ✅ Muxer created
- ✅ MUXER STARTED videoTrack=0
- ✅ writeSampleData (video and audio)
- ✅ MUXER STOPPED — size=XXXXX bytes

If final size > 10KB, the video file is likely valid.
