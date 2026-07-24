//
//  UNOMetalViewDelegate.m
//

#import "UNOMetalViewDelegate.h"
#import "UNOWindow.h"
#import "UnoNativeMac.h"

@implementation UNOMetalViewDelegate
{
    id<MTLDevice> _device;
}

- (nonnull instancetype)initWithMetalKitView:(nonnull MTKView *)mtkView
{
    self = [super init];
    if (self)
    {
        _device = mtkView.device;
        self.queue = [_device newCommandQueue];
        
        mtkView.colorPixelFormat = MTLPixelFormatRGBA8Unorm;
        mtkView.depthStencilPixelFormat = MTLPixelFormatDepth32Float_Stencil8;
        mtkView.sampleCount = 1;
        // this property has no effect on x86_64, only on arm64, and is required for sampling (which acrylicbrush does)
        mtkView.framebufferOnly = false;
#if DEBUG
        NSLog(@"initWithMetalKitView: paused %s enableSetNeedsDisplay %s", mtkView.paused ? "true" : "false", mtkView.enableSetNeedsDisplay ? "true" : "false");
#endif
    }
    
    return self;
}

- (void)drawInMTKView:(nonnull MTKView *)view
{
    // A paused view is driven by the managed render thread, which owns the GRContext. AppKit can
    // still call this (occlusion changes, backing-store redraws), so bail out rather than touch the
    // GRContext from the main thread concurrently with the render thread.
    if (view.isPaused)
    {
        return;
    }

#if DEBUG
    NSLog (@"drawInMTKView: %f %f", view.drawableSize.width, view.drawableSize.height);
#endif
    id<CAMetalDrawable> drawable = view.currentDrawable;
    if (drawable == nil)
    {
        return;
    }

    CGSize size = view.drawableSize;
    // call managed code
    uno_get_metal_draw_callback()((__bridge void*) view.window, size.width, size.height, (__bridge void*) drawable.texture);

#if DEBUG
    id<MTLCommandBuffer> commandBuffer = nil;
    if (@available(macOS 11.0, *)) {
        MTLCommandBufferDescriptor* desc = [[MTLCommandBufferDescriptor alloc] init];
        desc.errorOptions = MTLCommandBufferErrorOptionEncoderExecutionStatus; // this has a performance impact
        commandBuffer = [self.queue commandBufferWithDescriptor:desc];
        [commandBuffer addCompletedHandler:^(id <MTLCommandBuffer> commandbuf) {
            [self trace:commandbuf withPrefix:@"addCompletedHandler"]; // status should be 4 (Completed)
        }];
    } else {
        commandBuffer = [self.queue commandBuffer];
    }
    NSLog (@"drawInMTKView MTLCommandBuffer: %@", commandBuffer);
#else
    id<MTLCommandBuffer> commandBuffer = [self.queue commandBuffer];
#endif

#if DEBUG_METAL
    [self trace:commandBuffer withPrefix:@"prePresentDrawable"]; // status should be 0 (NotEnqueued)
#endif
    [commandBuffer presentDrawable:drawable];

#if DEBUG_METAL
    [self trace:commandBuffer withPrefix:@"preCommit"]; // status should be 0 (NotEnqueued)
#endif
    [commandBuffer commit];
#if DEBUG_METAL
    [self trace:commandBuffer withPrefix:@"postCommit"]; // status should be 2 (Committed)
#endif
}

#if DEBUG
- (void)trace:(id <MTLCommandBuffer>) commandBuffer withPrefix:(NSString*) prefix
{
    id logs = nil;
    if (@available(macOS 11.0, *)) {
        logs = commandBuffer.logs;
    }
    NSLog (@"drawInMTKView %@ status %lu LOG %@ ERROR %@", prefix, commandBuffer.status, logs, commandBuffer.error);
}
#endif

- (void)mtkView:(nonnull MTKView *)view drawableSizeWillChange:(CGSize)size
{
    CGFloat scale = view.window.backingScaleFactor;
#if DEBUG
    NSLog (@"drawableSizeWillChange: %p %f x %f @ %gx", view.window, size.width, size.height, scale);
#endif
    uno_get_resize_callback()((__bridge void*) view.window, size.width / scale, size.height / scale);
}

@end

// --- Render thread drawable lifecycle functions ---
// Called from the managed render thread (a background thread). These use
// CAMetalLayer.nextDrawable directly instead of MTKView.currentDrawable, which is only
// valid during drawInMTKView: callbacks on the main thread. nextDrawable is thread-safe.
// See: https://developer.apple.com/documentation/quartzcore/cametallayer

bool uno_window_acquire_next_frame(NSWindow* window, void** texture, double* width, double* height)
{
    @autoreleasepool {
        if (window == nil) return false;

        MTKView* view = (MTKView*)window.contentViewController.view;
        if (view == nil || ![view isKindOfClass:[MTKView class]]) return false;

        UNOWindow* unoWindow = (UNOWindow*)window;
        UNOMetalViewDelegate* delegate = unoWindow.metalViewDelegate;
        if (delegate == nil) return false;

        // nextDrawable is thread-safe (unlike MTKView.currentDrawable). It blocks up to ~1s
        // if all drawables are in use (occluded/minimized window); with maximumDrawableCount=2
        // set in uno_window_create that is rare, and a nil return is handled by the caller.
        CAMetalLayer* metalLayer = (CAMetalLayer*)view.layer;

        id<CAMetalDrawable> drawable = [metalLayer nextDrawable];
        if (drawable == nil) return false;

        // Hold the drawable until present (strong property retains via ARC).
        delegate.currentFrameDrawable = drawable;

        *texture = (__bridge void*)drawable.texture;
        CGSize size = metalLayer.drawableSize;
        *width = size.width;
        *height = size.height;
        return true;
    }
}

void uno_window_present_frame(NSWindow* window)
{
    @autoreleasepool {
        if (window == nil) return;

        UNOWindow* unoWindow = (UNOWindow*)window;
        UNOMetalViewDelegate* delegate = unoWindow.metalViewDelegate;
        if (delegate == nil) return;

        id<CAMetalDrawable> drawable = delegate.currentFrameDrawable;
        if (drawable == nil) return;

        id<MTLCommandBuffer> commandBuffer = [delegate.queue commandBuffer];
        [commandBuffer presentDrawable:drawable];
        [commandBuffer commit];

        // Release the drawable as soon as possible after committing; the command buffer
        // retains it internally for presentation.
        // See: https://developer.apple.com/library/archive/documentation/3DDrawing/Conceptual/MTLBestPracticesGuide/Drawables.html
        delegate.currentFrameDrawable = nil;
    }
}
