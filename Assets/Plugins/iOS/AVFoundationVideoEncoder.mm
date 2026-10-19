#import <Foundation/Foundation.h>
#import <AVFoundation/AVFoundation.h>
#import <CoreGraphics/CoreGraphics.h>
#import <UIKit/UIKit.h>

/**
 * Codificador de video usando AVFoundation nativo de iOS
 * Combina frames JPEG + audio WAV en MP4 usando hardware del iPhone/iPad
 */

extern "C" {
    
    // Función exportada para Unity
    bool AVFoundationEncodeVideo(const char* framesDirPath, 
                                  const char* audioPath, 
                                  const char* outputPath,
                                  int frameRate,
                                  int width,
                                  int height);
    
    bool AVFoundationIsAvailable();
}

@interface VideoEncoder : NSObject
+ (BOOL)encodeVideoWithFramesDir:(NSString*)framesDir
                        audioPath:(NSString*)audioPath
                       outputPath:(NSString*)outputPath
                        frameRate:(int)frameRate
                            width:(int)width
                           height:(int)height;
@end

@implementation VideoEncoder

+ (BOOL)encodeVideoWithFramesDir:(NSString*)framesDir
                        audioPath:(NSString*)audioPath
                       outputPath:(NSString*)outputPath
                        frameRate:(int)frameRate
                            width:(int)width
                           height:(int)height {
    
    NSLog(@"[AVFoundationEncoder] Starting video encoding...");
    NSLog(@"[AVFoundationEncoder] Frames: %@", framesDir);
    NSLog(@"[AVFoundationEncoder] Audio: %@", audioPath);
    NSLog(@"[AVFoundationEncoder] Output: %@", outputPath);
    NSLog(@"[AVFoundationEncoder] Resolution: %dx%d @ %d fps", width, height, frameRate);
    
    @autoreleasepool {
        NSError* error = nil;
        
        // Verificar directorio de frames
        NSFileManager* fileManager = [NSFileManager defaultManager];
        BOOL isDirectory;
        if (![fileManager fileExistsAtPath:framesDir isDirectory:&isDirectory] || !isDirectory) {
            NSLog(@"[AVFoundationEncoder] Frames directory not found: %@", framesDir);
            return NO;
        }
        
        // Obtener lista de frames ordenados
        NSArray* allFiles = [fileManager contentsOfDirectoryAtPath:framesDir error:&error];
        if (error) {
            NSLog(@"[AVFoundationEncoder] Error listing frames: %@", error.localizedDescription);
            return NO;
        }
        
        NSPredicate* jpgPredicate = [NSPredicate predicateWithFormat:@"SELF MATCHES %@", @"frame_\\d+\\.jpg"];
        NSArray* frameFiles = [[allFiles filteredArrayUsingPredicate:jpgPredicate] sortedArrayUsingSelector:@selector(compare:)];
        
        if (frameFiles.count == 0) {
            NSLog(@"[AVFoundationEncoder] No frame files found in: %@", framesDir);
            return NO;
        }
        
        NSLog(@"[AVFoundationEncoder] Found %lu frames", (unsigned long)frameFiles.count);
        
        // Asegurar dimensiones pares (requisito de H.264)
        if (width % 2 != 0) width = (width / 2) * 2;
        if (height % 2 != 0) height = (height / 2) * 2;
        
        // Eliminar archivo de salida si ya existe
        if ([fileManager fileExistsAtPath:outputPath]) {
            [fileManager removeItemAtPath:outputPath error:nil];
        }
        
        // Crear AVAssetWriter
        NSURL* outputURL = [NSURL fileURLWithPath:outputPath];
        AVAssetWriter* writer = [[AVAssetWriter alloc] initWithURL:outputURL
                                                           fileType:AVFileTypeMPEG4
                                                              error:&error];
        if (error) {
            NSLog(@"[AVFoundationEncoder] Error creating writer: %@", error.localizedDescription);
            return NO;
        }
        
        // Configurar video input
        NSDictionary* videoSettings = @{
            AVVideoCodecKey: AVVideoCodecTypeH264,
            AVVideoWidthKey: @(width),
            AVVideoHeightKey: @(height),
            AVVideoCompressionPropertiesKey: @{
                AVVideoAverageBitRateKey: @(6000000), // 6 Mbps
                AVVideoMaxKeyFrameIntervalKey: @(frameRate), // I-frame cada segundo
                AVVideoProfileLevelKey: AVVideoProfileLevelH264HighAutoLevel
            }
        };
        
        AVAssetWriterInput* videoInput = [AVAssetWriterInput assetWriterInputWithMediaType:AVMediaTypeVideo
                                                                             outputSettings:videoSettings];
        videoInput.expectsMediaDataInRealTime = NO;
        
        // Crear pixel buffer adaptor
        NSDictionary* pixelBufferAttributes = @{
            (NSString*)kCVPixelBufferPixelFormatTypeKey: @(kCVPixelFormatType_32ARGB),
            (NSString*)kCVPixelBufferWidthKey: @(width),
            (NSString*)kCVPixelBufferHeightKey: @(height)
        };
        
        AVAssetWriterInputPixelBufferAdaptor* adaptor = [AVAssetWriterInputPixelBufferAdaptor
            assetWriterInputPixelBufferAdaptorWithAssetWriterInput:videoInput
            sourcePixelBufferAttributes:pixelBufferAttributes];
        
        if (![writer canAddInput:videoInput]) {
            NSLog(@"[AVFoundationEncoder] Cannot add video input");
            return NO;
        }
        
        [writer addInput:videoInput];
        
        // Iniciar escritura
        if (![writer startWriting]) {
            NSLog(@"[AVFoundationEncoder] Failed to start writing: %@", writer.error.localizedDescription);
            return NO;
        }
        
        [writer startSessionAtSourceTime:kCMTimeZero];
        
        // Escribir frames
        __block BOOL success = YES;
        __block int frameIndex = 0;
        CMTime presentationTime = kCMTimeZero;
        CMTime frameDuration = CMTimeMake(1, frameRate);
        
        for (NSString* frameFile in frameFiles) {
            @autoreleasepool {
                NSString* framePath = [framesDir stringByAppendingPathComponent:frameFile];
                
                // Cargar imagen
                UIImage* image = [UIImage imageWithContentsOfFile:framePath];
                if (!image) {
                    NSLog(@"[AVFoundationEncoder] Failed to load frame: %@", frameFile);
                    continue;
                }
                
                // Redimensionar si es necesario
                if (image.size.width != width || image.size.height != height) {
                    UIGraphicsBeginImageContextWithOptions(CGSizeMake(width, height), YES, 1.0);
                    [image drawInRect:CGRectMake(0, 0, width, height)];
                    image = UIGraphicsGetImageFromCurrentImageContext();
                    UIGraphicsEndImageContext();
                }
                
                // Esperar a que input esté listo
                while (!videoInput.readyForMoreMediaData) {
                    [NSThread sleepForTimeInterval:0.01];
                }
                
                // Crear pixel buffer desde imagen
                CVPixelBufferRef pixelBuffer = NULL;
                CVPixelBufferPoolCreatePixelBuffer(NULL, adaptor.pixelBufferPool, &pixelBuffer);
                
                if (pixelBuffer) {
                    CVPixelBufferLockBaseAddress(pixelBuffer, 0);
                    
                    void* pixelData = CVPixelBufferGetBaseAddress(pixelBuffer);
                    CGColorSpaceRef colorSpace = CGColorSpaceCreateDeviceRGB();
                    
                    CGContextRef context = CGBitmapContextCreate(pixelData,
                                                                width,
                                                                height,
                                                                8,
                                                                CVPixelBufferGetBytesPerRow(pixelBuffer),
                                                                colorSpace,
                                                                kCGImageAlphaNoneSkipFirst);
                    
                    CGContextDrawImage(context, CGRectMake(0, 0, width, height), image.CGImage);
                    
                    CGContextRelease(context);
                    CGColorSpaceRelease(colorSpace);
                    CVPixelBufferUnlockBaseAddress(pixelBuffer, 0);
                    
                    // Añadir frame
                    if (![adaptor appendPixelBuffer:pixelBuffer withPresentationTime:presentationTime]) {
                        NSLog(@"[AVFoundationEncoder] Failed to append frame %d", frameIndex);
                        success = NO;
                    }
                    
                    CVPixelBufferRelease(pixelBuffer);
                    
                    presentationTime = CMTimeAdd(presentationTime, frameDuration);
                    frameIndex++;
                    
                    if (frameIndex % 30 == 0) {
                        NSLog(@"[AVFoundationEncoder] Encoded %d/%lu frames", frameIndex, (unsigned long)frameFiles.count);
                    }
                }
            }
        }
        
        // Finalizar video
        [videoInput markAsFinished];
        
        // Esperar a que termine
        dispatch_semaphore_t semaphore = dispatch_semaphore_create(0);
        [writer finishWritingWithCompletionHandler:^{
            dispatch_semaphore_signal(semaphore);
        }];
        dispatch_semaphore_wait(semaphore, DISPATCH_TIME_FOREVER);
        
        if (writer.status != AVAssetWriterStatusCompleted) {
            NSLog(@"[AVFoundationEncoder] Writing failed: %@", writer.error.localizedDescription);
            return NO;
        }
        
        NSLog(@"[AVFoundationEncoder] Video encoding complete: %d frames", frameIndex);
        NSLog(@"[AVFoundationEncoder] Output file: %@", outputPath);
        
        // TODO: Agregar audio track desde WAV
        // Por ahora solo video, el audio puede agregarse después si es necesario
        // (requiere AVAssetReader para WAV + AVAssetWriterInput para audio)
        
        return YES;
    }
}

@end

// Implementación de funciones C para Unity
bool AVFoundationEncodeVideo(const char* framesDirPath, 
                              const char* audioPath, 
                              const char* outputPath,
                              int frameRate,
                              int width,
                              int height) {
    
    @autoreleasepool {
        NSString* framesDir = [NSString stringWithUTF8String:framesDirPath];
        NSString* audio = audioPath ? [NSString stringWithUTF8String:audioPath] : nil;
        NSString* output = [NSString stringWithUTF8String:outputPath];
        
        return [VideoEncoder encodeVideoWithFramesDir:framesDir
                                            audioPath:audio
                                           outputPath:output
                                            frameRate:frameRate
                                                width:width
                                               height:height];
    }
}

bool AVFoundationIsAvailable() {
    return YES; // AVFoundation siempre disponible en iOS
}
