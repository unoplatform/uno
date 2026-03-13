//
//  UNONative.m
//

#import "UNONative.h"

static NSMutableSet<NSView*> *elements;
static NSMutableSet<NSView*> *transients;

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
    sample.originalSuperView = window.contentViewController.view;
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

// Camera capture dialog using AVFoundation.
// Presents a modal window with a live camera preview and Capture/Cancel buttons.
// This function must be called from the main thread (same pattern as file pickers).

// Receives the async photo capture result and dismisses the modal.
// The delegate callback fires on the session's internal queue (not main thread),
// so it dispatches stopModal back to the main queue where the modal run loop
// will process it.
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
    // Dismiss the modal from the main thread (the modal run loop drains the main queue)
    dispatch_async(dispatch_get_main_queue(), ^{
        [NSApp stopModal];
    });
}

@end

// Handles button clicks for the camera modal dialog.
@interface UNOCameraModalHelper : NSObject
@property (nonatomic, strong) AVCapturePhotoOutput *photoOutput;
@property (nonatomic, strong) UNOCameraCaptureDelegate *captureDelegate;
@end

@implementation UNOCameraModalHelper

- (void)capturePhoto:(NSButton *)sender {
    // Disable button to prevent double-tap
    sender.enabled = NO;

    // Initiate async photo capture — the delegate callback will dismiss the modal
    self.captureDelegate = [[UNOCameraCaptureDelegate alloc] init];
    AVCapturePhotoSettings *settings = [AVCapturePhotoSettings photoSettings];
    [self.photoOutput capturePhotoWithSettings:settings delegate:self.captureDelegate];
}

- (void)cancel:(NSButton *)sender {
    [NSApp abortModal];
}

@end

const char* _Nullable uno_capture_photo(bool useJpeg)
{
    @autoreleasepool {
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
        UNOCameraModalHelper *helper = [[UNOCameraModalHelper alloc] init];
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
        NSModalResponse response = [NSApp runModalForWindow:window];
        [window orderOut:nil];
        [session stopRunning];

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
            return strdup([filePath UTF8String]);
        }

        return NULL;
    }
}
