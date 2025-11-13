//
//  UNOSoftView.m
//

#import "UNOSoftView.h"
#import "UNOWindow.h"
#import "UnoNativeMac.h"

@implementation UNOSoftView

- (void)drawRect:(NSRect)dirtyRect
{
    double width = dirtyRect.origin.x + dirtyRect.size.width;
    double height = dirtyRect.origin.y + dirtyRect.size.height;
#if DEBUG
    NSLog (@"drawRect: window %p width %f height %f", self.window, width, height);
#endif
    CGFloat scale = self.window.backingScaleFactor;

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
    CGImageRef image = CGImageCreate(width * scale,
                                     height * scale,
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
    CGImageDestinationAddImage(destination, image, nil);
    if (!CGImageDestinationFinalize(destination)) {
        NSLog(@"Failed to write image %p to %@", image, url);
    }
    CFRelease(destination);
#endif

    CGRect rect = CGRectMake(0, 0, width, height);
    CGSize content = [self.window contentRectForFrameRect: rect].size;
    rect = CGRectMake(0, content.height - rect.size.height, rect.size.width, rect.size.height);
    CGContextDrawImage(NSGraphicsContext.currentContext.CGContext, rect, image);

    CGImageRelease(image);
    CGColorSpaceRelease(colorspace);
    CGDataProviderRelease(provider);
}

- (void)setFrameSize:(NSSize)newSize
{
#if DEBUG
    NSLog(@"setFrameSize: %p %f x %f", self.window, newSize.width, newSize.height);
#endif
    [super setFrameSize:newSize];
    uno_get_resize_callback()((__bridge void*) self.window, newSize.width, newSize.height);
}

@end
