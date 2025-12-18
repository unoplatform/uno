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
