//
//  UNONative.m
//

#import "UNONative.h"
#import "UNOApplication.h"
#import <AVFoundation/AVFoundation.h>
#import <dispatch/dispatch.h>

static NSMutableSet<NSView*> *elements;
static NSMutableSet<NSView*> *transients;

static BOOL EnsureCaptureAuthorization(AVMediaType mediaType)
{
    // Ensure required Info.plist usage description keys are present before requesting access.
    // Skip this check when the app is not running as a bundled .app (e.g., during development
    // via `dotnet run`), since there is no Info.plist to read from in that case.
    if (uno_application_is_bundled()) {
        NSString *usageDescriptionKey = nil;
        if ([mediaType isEqualToString:AVMediaTypeVideo]) {
            usageDescriptionKey = @"NSCameraUsageDescription";
        } else if ([mediaType isEqualToString:AVMediaTypeAudio]) {
            usageDescriptionKey = @"NSMicrophoneUsageDescription";
        }

        if (usageDescriptionKey != nil) {
            id usageDescription = [[NSBundle mainBundle] objectForInfoDictionaryKey:usageDescriptionKey];
            if (usageDescription == nil) {
#if DEBUG
                NSLog(@"Missing %@ in Info.plist. Capture authorization denied.", usageDescriptionKey);
#endif
                return NO;
            }
        }
    }

    AVAuthorizationStatus status = [AVCaptureDevice authorizationStatusForMediaType:mediaType];
    if (status == AVAuthorizationStatusAuthorized) {
        return YES;
    }

    if (status == AVAuthorizationStatusNotDetermined) {
        __block BOOL authorized = NO;
        __block BOOL completed = NO;

        [AVCaptureDevice requestAccessForMediaType:mediaType
                                 completionHandler:^(BOOL granted) {
                                     authorized = granted;
                                     completed = YES;
                                 }];

        if ([NSThread isMainThread]) {
            // Pump the main run loop while waiting to keep the UI responsive
            // and allow the system permission dialog to be displayed/handled.
            NSRunLoop *runLoop = [NSRunLoop currentRunLoop];
            while (!completed) {
                @autoreleasepool {
                    [runLoop runMode:NSDefaultRunLoopMode
                            beforeDate:[NSDate dateWithTimeIntervalSinceNow:0.1]];
                }
            }
        } else {
            // On a background thread, it is safe to block while waiting for completion.
            dispatch_semaphore_t semaphore = dispatch_semaphore_create(0);

            // Wrap the completion to signal the semaphore when finished.
            __block BOOL completionInstalled = NO;
            // Ensure we only install the signaling wrapper once.
            if (!completionInstalled) {
                completionInstalled = YES;
                [AVCaptureDevice requestAccessForMediaType:mediaType
                                         completionHandler:^(BOOL granted) {
                                             authorized = granted;
                                             completed = YES;
                                             dispatch_semaphore_signal(semaphore);
                                         }];
            }

            // Wait indefinitely for the user/system to complete the authorization flow.
            dispatch_semaphore_wait(semaphore, DISPATCH_TIME_FOREVER);
        }

        return authorized;
    }

    return NO;
}

@implementation UNORedView : NSView

@synthesize originalSuperView;

// make the background red for easier tracking
- (BOOL)wantsUpdateLayer
{
    return !self.hidden;
}

- (void)updateLayer
{
    self.layer.backgroundColor = NSColor.redColor.CGColor;
}

- (void)detach {
    // nothing needed
}

- (void)dispose {
#if DEBUG
    NSLog(@"UNORedView %p disposing with superview %p", self, self.superview);
#endif
    if (self.superview) {
        [self removeFromSuperview];
    }
}

@end

NSView* uno_native_create_sample(NSWindow *window, const char* _Nullable text)
{
    // no NSLabel on macOS
    NSTextField* label = [[NSTextField alloc] initWithFrame:NSMakeRect(0, 0, 100, 100)];
    label.bezeled = NO;
    label.drawsBackground = NO;
    label.editable = NO;
    label.selectable = NO;
    label.stringValue = [NSString stringWithUTF8String:text];
    label.frame = NSMakeRect(0, 0, label.fittingSize.width, label.fittingSize.height);

    UNORedView* sample = [[UNORedView alloc] initWithFrame:label.frame];
    [sample addSubview:label];
#if DEBUG
    NSLog(@"uno_native_create_sample #%p label: %@", sample, label.stringValue);
#endif
    sample.originalSuperView = ((UNOWindow*)window).renderingView;
    return sample;
}

void uno_native_arrange(NSView<UNONativeElement> *element, double arrangeLeft, double arrangeTop, double arrangeWidth, double arrangeHeight)
{
    NSRect arrange = NSMakeRect(arrangeLeft, arrangeTop, arrangeWidth, arrangeHeight);
    element.frame = arrange;
#if DEBUG
    NSLog(@"uno_native_arrange %p arrange(%g,%g,%g,%g)", element,
          arrangeLeft, arrangeTop, arrangeWidth, arrangeHeight);
#endif
}

void uno_native_attach(NSView<UNONativeElement>* element)
{
#if DEBUG
    NSLog(@"!!uno_native_attach %p", element);
#endif
    bool already_attached = NO;
    if (!elements) {
        elements = [[NSMutableSet alloc] initWithCapacity:10];
    } else {
        already_attached = [elements containsObject:element];
    }
#if DEBUG
    NSLog(@"uno_native_attach %p -> %s attached", element, already_attached ? "already" : "not previously");
#endif
    if (!already_attached) {
        // note: it's too early to add a mask since the layer has not been set yet
        [elements addObject:element];
    }
    [element.originalSuperView addSubview:element];
}

void uno_native_detach(NSView<UNONativeElement>* element)
{
#if DEBUG
    NSLog(@"uno_native_detach %p", element);
#endif
    element.layer.mask = nil;

    if (!transients) {
        transients = [[NSMutableSet alloc] initWithCapacity:10];
    }
    // once removed from superview the instance can be freed by the runtime unless we keep another reference to it
    [transients addObject:element];
    [elements removeObject:element];
    [element removeFromSuperview];
}

bool uno_native_is_attached(NSView<UNONativeElement>* element)
{
    bool attached = elements ? [elements containsObject:element] : NO;
#if DEBUG
    NSLog(@"uno_native_is_attached %s", attached ? "YES" : "NO");
#endif
    return attached;
}

void uno_native_measure(NSView<UNONativeElement>* element, double childWidth, double childHeight, double availableWidth, double availableHeight, double* width, double* height)
{
    CGSize size = element.subviews.firstObject.frame.size;
    
    double resolvedWidth = isfinite(availableWidth) ? availableWidth : (isfinite(childWidth) ? childWidth : -1.0);
    double resolvedHeight = isfinite(availableHeight) ? availableHeight : (isfinite(childHeight) ? childHeight : -1.0);
    if (resolvedWidth < 0)
    {
        resolvedWidth = size.width;
    }
    if (resolvedHeight < 0)
    {
        resolvedHeight = size.height;
    }

    *width = resolvedWidth;
    *height = resolvedHeight;
#if DEBUG
    NSLog(@"uno_native_measure %p : child %g x %g / available %g x %g -> %g x %g", element, childWidth, childHeight, availableWidth, availableHeight, *width, *height);
#endif
}

void uno_native_set_opacity(NSView<UNONativeElement>* element, double opacity)
{
#if DEBUG
    NSLog(@"uno_native_set_opacity #%p : %g -> %g", element, element.alphaValue, opacity);
#endif
    element.alphaValue = opacity;
}

void uno_native_dispose(NSView<UNONativeElement>* element)
{
#if DEBUG
    NSLog(@"uno_native_dispose #%p", element);
#endif
    [element dispose];
    [transients removeObject:element];
}

// Camera capture using AVFoundation.
// Both photo and video present a modal window with a live camera preview.
// These functions must be called from the main thread (same pattern as file pickers).

// --- Photo capture delegate ---

@interface UNOCameraCaptureDelegate : NSObject <AVCapturePhotoCaptureDelegate>
@property (nonatomic, strong) NSData *capturedImageData;
@end

@implementation UNOCameraCaptureDelegate

- (void)captureOutput:(AVCapturePhotoOutput *)output
    didFinishProcessingPhoto:(AVCapturePhoto *)photo
                       error:(NSError *)error
{
    if (!error) {
        self.capturedImageData = [photo fileDataRepresentation];
    }
    dispatch_async(dispatch_get_main_queue(), ^{
        [NSApp stopModal];
    });
}

@end

// --- Window delegate that aborts the modal when the close button (or Cmd+W) is used ---

@interface UNOCameraWindowDelegate : NSObject <NSWindowDelegate>
@property (nonatomic, weak) AVCaptureMovieFileOutput *movieOutput;
@end

@implementation UNOCameraWindowDelegate

- (void)windowWillClose:(NSNotification *)notification {
    if (self.movieOutput && self.movieOutput.isRecording) {
        [self.movieOutput stopRecording];
    }
    [NSApp abortModal];
}

@end

// --- Photo modal helper ---

@interface UNOPhotoModalHelper : NSObject
@property (nonatomic, strong) AVCapturePhotoOutput *photoOutput;
@property (nonatomic, strong) UNOCameraCaptureDelegate *captureDelegate;
@end

@implementation UNOPhotoModalHelper

- (void)capturePhoto:(NSButton *)sender {
    sender.enabled = NO;
    self.captureDelegate = [[UNOCameraCaptureDelegate alloc] init];
    AVCapturePhotoSettings *settings = [AVCapturePhotoSettings photoSettings];
    [self.photoOutput capturePhotoWithSettings:settings delegate:self.captureDelegate];
}

- (void)cancel:(NSButton *)sender {
    [NSApp abortModal];
}

@end

// --- Video recording delegate ---

@interface UNOVideoRecordingDelegate : NSObject <AVCaptureFileOutputRecordingDelegate>
@property (nonatomic, assign) BOOL succeeded;
@end

@implementation UNOVideoRecordingDelegate

- (void)captureOutput:(AVCaptureFileOutput *)output
    didFinishRecordingToOutputFileAtURL:(NSURL *)outputFileURL
                        fromConnections:(NSArray<AVCaptureConnection *> *)connections
                                  error:(NSError *)error
{
    self.succeeded = (error == nil);
    dispatch_async(dispatch_get_main_queue(), ^{
        [NSApp stopModal];
    });
}

@end

// --- Video modal helper ---

@interface UNOVideoModalHelper : NSObject
@property (nonatomic, strong) AVCaptureMovieFileOutput *movieOutput;
@property (nonatomic, strong) UNOVideoRecordingDelegate *recordingDelegate;
@property (nonatomic, strong) NSURL *outputFileURL;
@property (nonatomic, weak) NSButton *cancelButton;
@property (nonatomic, assign) BOOL isRecording;
@end

@implementation UNOVideoModalHelper

- (void)toggleRecording:(NSButton *)sender {
    if (!self.isRecording) {
        // Start recording
        self.isRecording = YES;
        sender.title = @"Stop";
        self.cancelButton.enabled = NO;

        self.recordingDelegate = [[UNOVideoRecordingDelegate alloc] init];
        [self.movieOutput startRecordingToOutputFileURL:self.outputFileURL recordingDelegate:self.recordingDelegate];
    } else {
        // Stop recording — delegate callback will dismiss the modal
        sender.enabled = NO;
        [self.movieOutput stopRecording];
    }
}

- (void)cancel:(NSButton *)sender {
    if (self.isRecording) {
        [self.movieOutput stopRecording];
    }
    [NSApp abortModal];
}

@end

// Tracks the currently active capture modal window so that
// uno_capture_cancel() can safely abort it from any thread.
static NSWindow * _Nullable s_activeCaptureWindow = nil;

void uno_capture_cancel(void)
{
    dispatch_async(dispatch_get_main_queue(), ^{
        if (s_activeCaptureWindow != nil) {
            [NSApp abortModal];
        }
    });
}

char* _Nullable uno_capture_photo(bool useJpeg)
{
    @autoreleasepool {
        // Ensure we have authorization to use the camera before accessing the device.
        // This also validates that the appropriate Info.plist usage description key
        // (e.g., NSCameraUsageDescription) is present when running as a bundled app.
        if (!EnsureCaptureAuthorization(AVMediaTypeVideo))
        {
            return NULL;
        }

        // Find camera device
        AVCaptureDevice *camera = [AVCaptureDevice defaultDeviceWithMediaType:AVMediaTypeVideo];
        if (!camera) {
            return NULL;
        }

        NSError *error = nil;
        AVCaptureDeviceInput *input = [AVCaptureDeviceInput deviceInputWithDevice:camera error:&error];
        if (!input) {
            return NULL;
        }

        // Set up capture session
        AVCaptureSession *session = [[AVCaptureSession alloc] init];
        session.sessionPreset = AVCaptureSessionPresetPhoto;

        if (![session canAddInput:input]) {
            return NULL;
        }
        [session addInput:input];

        AVCapturePhotoOutput *photoOutput = [[AVCapturePhotoOutput alloc] init];
        if (![session canAddOutput:photoOutput]) {
            return NULL;
        }
        [session addOutput:photoOutput];

        // Build the modal window with camera preview
        NSRect windowRect = NSMakeRect(0, 0, 640, 520);
        NSWindow *window = [[NSWindow alloc]
            initWithContentRect:windowRect
                      styleMask:NSWindowStyleMaskTitled | NSWindowStyleMaskClosable
                        backing:NSBackingStoreBuffered
                          defer:NO];
        // Wire window delegate so the close button / Cmd+W aborts the modal
        UNOCameraWindowDelegate *windowDelegate = [[UNOCameraWindowDelegate alloc] init];
        window.delegate = windowDelegate;

        window.title = @"Camera Capture";
        [window center];

        NSView *contentView = window.contentView;

        // Camera preview layer
        AVCaptureVideoPreviewLayer *previewLayer = [AVCaptureVideoPreviewLayer layerWithSession:session];
        previewLayer.videoGravity = AVLayerVideoGravityResizeAspectFill;
        previewLayer.frame = NSMakeRect(0, 60, 640, 460);

        NSView *previewView = [[NSView alloc] initWithFrame:NSMakeRect(0, 60, 640, 460)];
        previewView.wantsLayer = YES;
        [previewView.layer addSublayer:previewLayer];
        [contentView addSubview:previewView];

        // Helper that handles button actions
        UNOPhotoModalHelper *helper = [[UNOPhotoModalHelper alloc] init];
        helper.photoOutput = photoOutput;

        // Capture button — initiates async capture, delegate dismisses modal when done
        NSButton *captureButton = [[NSButton alloc] initWithFrame:NSMakeRect(270, 15, 100, 32)];
        captureButton.title = @"Capture";
        captureButton.bezelStyle = NSBezelStyleRounded;
        captureButton.keyEquivalent = @"\r";
        captureButton.target = helper;
        captureButton.action = @selector(capturePhoto:);
        [contentView addSubview:captureButton];

        // Cancel button
        NSButton *cancelButton = [[NSButton alloc] initWithFrame:NSMakeRect(20, 15, 100, 32)];
        cancelButton.title = @"Cancel";
        cancelButton.bezelStyle = NSBezelStyleRounded;
        cancelButton.keyEquivalent = @"\033"; // Escape
        cancelButton.target = helper;
        cancelButton.action = @selector(cancel:);
        [contentView addSubview:cancelButton];

        // Start camera and run modal
        [session startRunning];
        s_activeCaptureWindow = window;
        NSModalResponse response = [NSApp runModalForWindow:window];
        s_activeCaptureWindow = nil;
        [window orderOut:nil];
        [session stopRunning];

        // Keep the window delegate alive for the duration of the modal
        // (NSWindow.delegate is weak, so ARC could release it prematurely)
        (void)windowDelegate;

        // NSModalResponseAbort means user cancelled
        if (response == NSModalResponseAbort) {
            return NULL;
        }

        NSData *imageData = helper.captureDelegate.capturedImageData;
        if (!imageData) {
            return NULL;
        }

        // Convert to requested format
        NSBitmapImageRep *imageRep = [NSBitmapImageRep imageRepWithData:imageData];
        if (!imageRep) {
            return NULL;
        }

        NSData *outputData;
        if (useJpeg) {
            outputData = [imageRep representationUsingType:NSBitmapImageFileTypeJPEG properties:@{NSImageCompressionFactor: @0.9}];
        } else {
            outputData = [imageRep representationUsingType:NSBitmapImageFileTypePNG properties:@{}];
        }

        if (!outputData) {
            return NULL;
        }

        // Write to temp file
        NSString *tempDir = NSTemporaryDirectory();
        NSString *fileName = [NSString stringWithFormat:@"%@.%@", [[NSUUID UUID] UUIDString], useJpeg ? @"jpg" : @"png"];
        NSString *filePath = [tempDir stringByAppendingPathComponent:fileName];

        if ([outputData writeToFile:filePath atomically:YES]) {
            NSLog(@"Camera capture saved to: %@", filePath);
            return strdup([filePath UTF8String]);
        }

        return NULL;
    }
}

char* _Nullable uno_capture_video(void)
{
    @autoreleasepool {
        // Ensure camera authorization
        if (!EnsureCaptureAuthorization(AVMediaTypeVideo)) {
            return NULL;
        }

        // Determine microphone authorization (audio is optional)
        BOOL audioAuthorized = EnsureCaptureAuthorization(AVMediaTypeAudio);

        // Find camera and (optionally) microphone
        AVCaptureDevice *camera = [AVCaptureDevice defaultDeviceWithMediaType:AVMediaTypeVideo];
        if (!camera) {
            return NULL;
        }

        NSError *error = nil;
        AVCaptureDeviceInput *videoInput = [AVCaptureDeviceInput deviceInputWithDevice:camera error:&error];
        if (!videoInput) {
            return NULL;
        }

        // Set up capture session
        AVCaptureSession *session = [[AVCaptureSession alloc] init];
        session.sessionPreset = AVCaptureSessionPresetHigh;

        if (![session canAddInput:videoInput]) {
            return NULL;
        }
        [session addInput:videoInput];

        // Add audio input if available and authorized
        if (audioAuthorized) {
            AVCaptureDevice *mic = [AVCaptureDevice defaultDeviceWithMediaType:AVMediaTypeAudio];
            if (mic) {
                AVCaptureDeviceInput *audioInput = [AVCaptureDeviceInput deviceInputWithDevice:mic error:&error];
                if (audioInput && [session canAddInput:audioInput]) {
                    [session addInput:audioInput];
                }
            }
        }

        AVCaptureMovieFileOutput *movieOutput = [[AVCaptureMovieFileOutput alloc] init];
        if (![session canAddOutput:movieOutput]) {
            return NULL;
        }
        [session addOutput:movieOutput];

        // Prepare output file path (.mov — AVCaptureMovieFileOutput writes QuickTime containers)
        NSString *tempDir = NSTemporaryDirectory();
        NSString *movFileName = [NSString stringWithFormat:@"%@.mov", [[NSUUID UUID] UUIDString]];
        NSString *filePath = [tempDir stringByAppendingPathComponent:movFileName];
        NSURL *fileURL = [NSURL fileURLWithPath:filePath];

        // Build the modal window with camera preview
        NSRect windowRect = NSMakeRect(0, 0, 640, 520);
        NSWindow *window = [[NSWindow alloc]
            initWithContentRect:windowRect
                      styleMask:NSWindowStyleMaskTitled | NSWindowStyleMaskClosable
                        backing:NSBackingStoreBuffered
                          defer:NO];
        window.title = @"Video Capture";
        [window center];

        // Wire window delegate so the close button / Cmd+W aborts the modal
        UNOCameraWindowDelegate *windowDelegate = [[UNOCameraWindowDelegate alloc] init];
        windowDelegate.movieOutput = movieOutput;
        window.delegate = windowDelegate;

        NSView *contentView = window.contentView;

        // Camera preview layer
        AVCaptureVideoPreviewLayer *previewLayer = [AVCaptureVideoPreviewLayer layerWithSession:session];
        previewLayer.videoGravity = AVLayerVideoGravityResizeAspectFill;
        previewLayer.frame = NSMakeRect(0, 60, 640, 460);

        NSView *previewView = [[NSView alloc] initWithFrame:NSMakeRect(0, 60, 640, 460)];
        previewView.wantsLayer = YES;
        [previewView.layer addSublayer:previewLayer];
        [contentView addSubview:previewView];

        // Helper that handles button actions
        UNOVideoModalHelper *helper = [[UNOVideoModalHelper alloc] init];
        helper.movieOutput = movieOutput;
        helper.outputFileURL = fileURL;

        // Record button — toggles between Start/Stop
        NSButton *recordButton = [[NSButton alloc] initWithFrame:NSMakeRect(270, 15, 100, 32)];
        recordButton.title = @"Record";
        recordButton.bezelStyle = NSBezelStyleRounded;
        recordButton.keyEquivalent = @"\r";
        recordButton.target = helper;
        recordButton.action = @selector(toggleRecording:);
        [contentView addSubview:recordButton];

        // Cancel button
        NSButton *cancelButton = [[NSButton alloc] initWithFrame:NSMakeRect(20, 15, 100, 32)];
        cancelButton.title = @"Cancel";
        cancelButton.bezelStyle = NSBezelStyleRounded;
        cancelButton.keyEquivalent = @"\033"; // Escape
        cancelButton.target = helper;
        cancelButton.action = @selector(cancel:);
        [contentView addSubview:cancelButton];
        helper.cancelButton = cancelButton;

        // Start camera and run modal
        [session startRunning];
        s_activeCaptureWindow = window;
        NSModalResponse response = [NSApp runModalForWindow:window];
        s_activeCaptureWindow = nil;
        [window orderOut:nil];
        [session stopRunning];

        // Keep the window delegate alive for the duration of the modal
        // (NSWindow.delegate is weak, so ARC could release it prematurely)
        (void)windowDelegate;

        // NSModalResponseAbort means user cancelled
        if (response == NSModalResponseAbort) {
            [[NSFileManager defaultManager] removeItemAtURL:fileURL error:nil];
            return NULL;
        }

        if (!helper.recordingDelegate.succeeded) {
            [[NSFileManager defaultManager] removeItemAtURL:fileURL error:nil];
            return NULL;
        }

        // Verify the file was written
        if (![[NSFileManager defaultManager] fileExistsAtPath:filePath]) {
            return NULL;
        }

        // Convert from MOV to MP4 using AVAssetExportSession (passthrough, no re-encoding)
        NSString *mp4FileName = [NSString stringWithFormat:@"%@.mp4", [[NSUUID UUID] UUIDString]];
        NSString *mp4Path = [tempDir stringByAppendingPathComponent:mp4FileName];
        NSURL *mp4URL = [NSURL fileURLWithPath:mp4Path];

        AVAsset *asset = [AVAsset assetWithURL:fileURL];
        AVAssetExportSession *exportSession = [[AVAssetExportSession alloc] initWithAsset:asset presetName:AVAssetExportPresetPassthrough];
        exportSession.outputURL = mp4URL;
        exportSession.outputFileType = AVFileTypeMPEG4;
        exportSession.shouldOptimizeForNetworkUse = YES;

        dispatch_semaphore_t exportSema = dispatch_semaphore_create(0);
        [exportSession exportAsynchronouslyWithCompletionHandler:^{
            dispatch_semaphore_signal(exportSema);
        }];

        // Wait with a bounded timeout to avoid hanging indefinitely
        dispatch_time_t exportTimeout = dispatch_time(DISPATCH_TIME_NOW, (int64_t)(120 * NSEC_PER_SEC));
        long exportWaitResult = dispatch_semaphore_wait(exportSema, exportTimeout);

        // Clean up the intermediate MOV file
        [[NSFileManager defaultManager] removeItemAtURL:fileURL error:nil];

        if (exportWaitResult != 0) {
            // Timed out — cancel the export and clean up
            [exportSession cancelExport];
            [[NSFileManager defaultManager] removeItemAtURL:mp4URL error:nil];
            return NULL;
        }

        if (exportSession.status != AVAssetExportSessionStatusCompleted) {
            [[NSFileManager defaultManager] removeItemAtURL:mp4URL error:nil];
            return NULL;
        }

        NSLog(@"Video capture exported to: %@", mp4Path);
        return strdup([mp4Path UTF8String]);
    }
}
