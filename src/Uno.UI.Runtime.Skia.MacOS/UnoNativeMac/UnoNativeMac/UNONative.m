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

// Camera capture implementation using AVFoundation
const char* _Nullable uno_capture_photo(bool useJpeg)
{
    @autoreleasepool {
        // Import AVFoundation framework for camera access
        NSString *tempDir = NSTemporaryDirectory();
        NSString *fileName = [NSString stringWithFormat:@"%@.%@", [[NSUUID UUID] UUIDString], useJpeg ? @"jpg" : @"png"];
        NSString *filePath = [tempDir stringByAppendingPathComponent:fileName];
        
        // For now, we'll use IKPictureTaker which is deprecated but simpler
        // A full AVFoundation implementation would be more complex
        // Note: IKPictureTaker is deprecated in macOS 10.15+, but still works
        #pragma clang diagnostic push
        #pragma clang diagnostic ignored "-Wdeprecated-declarations"
        
        __block NSString *resultPath = nil;
        __block BOOL completed = NO;
        
        dispatch_async(dispatch_get_main_queue(), ^{
            // Create an IKPictureTaker instance
            IKPictureTaker *pictureTaker = [IKPictureTaker pictureTaker];
            
            // Run the picture taker modal dialog
            NSInteger result = [pictureTaker runModal];
            
            if (result == NSModalResponseOK) {
                // Get the captured image
                NSImage *image = [pictureTaker outputImage];
                
                if (image) {
                    // Convert NSImage to the requested format
                    CGImageRef cgImage = [image CGImageForProposedRect:NULL context:nil hints:nil];
                    NSBitmapImageRep *imageRep = [[NSBitmapImageRep alloc] initWithCGImage:cgImage];
                    
                    NSData *imageData;
                    if (useJpeg) {
                        imageData = [imageRep representationUsingType:NSBitmapImageFileTypeJPEG properties:@{NSImageCompressionFactor: @0.9}];
                    } else {
                        imageData = [imageRep representationUsingType:NSBitmapImageFileTypePNG properties:@{}];
                    }
                    
                    // Write to file
                    if ([imageData writeToFile:filePath atomically:YES]) {
                        resultPath = [filePath copy];
                    }
                }
            }
            
            completed = YES;
        });
        
        // Wait for completion (this is blocking, but acceptable for modal camera capture)
        while (!completed) {
            [[NSRunLoop currentRunLoop] runMode:NSDefaultRunLoopMode beforeDate:[NSDate dateWithTimeIntervalSinceNow:0.1]];
        }
        
        #pragma clang diagnostic pop
        
        if (resultPath) {
            return strdup([resultPath UTF8String]);
        }
        
        return NULL;
    }
}
