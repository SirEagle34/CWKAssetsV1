# Add project specific ProGuard rules here.
# By default, the flags in this file are appended to flags specified
# in ${sdk.dir}/tools/proguard/proguard-android.txt
# You can edit the include path and order by changing the proguardFiles
# directive in build.gradle.
#
# For more details, see
#   http://developer.android.com/guide/developing/tools/proguard.html

# Keep MediaCodec, MediaProjection, and MediaMuxer
-keep class android.media.** { *; }
-keep class android.media.projection.** { *; }

# Keep our recorder classes
-keep class com.cardwarskingdom.recordertest.** { *; }
