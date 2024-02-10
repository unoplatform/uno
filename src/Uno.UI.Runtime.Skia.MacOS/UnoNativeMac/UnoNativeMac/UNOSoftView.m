//
//  UNOSoftView.m
//

#import "UNOSoftView.h"
#import "UNOWindow.h"
#import "UnoNativeMac.h"

static soft_draw_fn_ptr soft_draw;

inline soft_draw_fn_ptr uno_get_soft_draw_callback(void)
{
    return soft_draw;
}

void uno_set_soft_draw_callback(soft_draw_fn_ptr p)
{
    soft_draw = p;
}

@implementation UNOSoftView

- (void)drawRect:(NSRect)dirtyRect
{
    double width = dirtyRect.origin.x + dirtyRect.size.width;
    double height = dirtyRect.origin.y + dirtyRect.size.height;
#if DEBUG
    NSLog (@"drawRect: window %p width %f height %f", self.window, width, height);
#endif

    int size = 0;
    int rowBytes = 0;
    void *data = nil;
    // call managed code
    uno_get_soft_draw_callback()((__bridge void*) self.window, width, height, &data, &rowBytes, &size);
    if (size == 0) {
        return;
    }

    CGDataProviderRef provider = CGDataProviderCreateWithData(NULL, data, size, NULL);
    CGColorSpaceRef colorspace = CGColorSpaceCreateDeviceRGB();
    CGImageRef image = CGImageCreate(width, height,
                                     /* bitsPerComponent */ 8,
                                     /* bitsPerPixel */ 32,
                                     /* bytesPerRow */ rowBytes,
                                     colorspace,
                                     /* CGBitmapInfo */ kCGBitmapByteOrder32Big | kCGImageAlphaPremultipliedLast,
                                     provider,
                                     /* const CGFloat *decode */ NULL,
                                     /* shouldInterpolate */ FALSE,
                                     kCGRenderingIntentDefault);
#if DEBUG_SCREENSHOT
    CFURLRef url = (__bridge CFURLRef)[NSURL fileURLWithPath:@"/Users/poupou/native.jpeg"];
    CGImageDestinationRef destination = CGImageDestinationCreateWithURL(url, kUTTypePNG, 1, NULL);
    CGImageDestinationAddImage(destination, cgimage, nil);
    if (!CGImageDestinationFinalize(destination)) {
        NSLog(@"Failed to write image %p to %@", cgimage, url);
    }
    CFRelease(destination);
#endif

    CGRect rect = CGRectMake(0, 0, width, height);
    CGContextDrawImage(NSGraphicsContext.currentContext.CGContext, rect, image);

    CGImageRelease(image);
    CGColorSpaceRelease(colorspace);
    CGDataProviderRelease(provider);
}

- (void)setFrameSize:(NSSize)newSize
{
#if DEBUG
    NSLog(@"setFrameSize: window %p width %f height %f", self.window, newSize.width, newSize.height);
#endif
    [super setFrameSize:newSize];
    uno_get_resize_callback()((__bridge void*) self.window, newSize.width, newSize.height);
}

@end
