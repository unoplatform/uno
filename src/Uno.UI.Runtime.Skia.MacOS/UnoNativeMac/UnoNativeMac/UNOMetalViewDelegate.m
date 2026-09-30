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
    // GRContext from the main thread concurrently with the render thread. An external-present view is
    // paused too, but has no render thread: this callback is what drives it.
    if (view.isPaused && !self.externalPresent)
    {
        return;
    }

#if DEBUG
    NSLog (@"drawInMTKView: %f %f", view.drawableSize.width, view.drawableSize.height);
#endif
    // The negotiated context owns this view's CAMetalLayer, so we must NOT acquire currentDrawable
    // (that would contend with the context's own nextDrawable). Just tick managed code with texture = NULL; the managed
    // MacOSWindowHost.MetalDraw routes to the context's present path (AcquireRenderTarget + Present) on the layer.
    if (self.externalPresent)
    {
        CGSize wsize = view.drawableSize;
        uno_get_metal_draw_callback()((__bridge void*) view.window, wsize.width, wsize.height, NULL);
        return;
    }

    id<CAMetalDrawable> drawable = view.currentDrawable;
    if (drawable == nil)
    {
        // Returning without calling managed code would stop this window rendering for good: AppKit has
        // already cleared needsDisplay to make this call, and the managed side latches its frame request
        // (CompositionTarget.RenderRequested stays set), so every later RequestNewFrame coalesces into the
        // invalidation that produced this dropped frame. Re-arm on a later turn -- setting it here would be
        // cleared by the display pass we are inside of.
        __weak MTKView *weakView = view;
        dispatch_after(dispatch_time(DISPATCH_TIME_NOW, (int64_t)(NSEC_PER_SEC / 120)), dispatch_get_main_queue(), ^{
            weakView.needsDisplay = YES;
        });
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
    // A paused view never runs the draw cycle that would apply the new size to its CAMetalLayer, so
    // nextDrawable would keep vending textures at the previous size. Applying it here (main thread,
    // before the managed resize) keeps the drawable in step with the window. CAMetalLayer allows
    // drawableSize to be set from any thread; if the render thread acquires a drawable between this
    // write and the managed resize it simply presents one more frame at the old size, which stays
    // self-consistent because uno_window_present_texture blits only the overlapping region.
    CAMetalLayer* metalLayer = (CAMetalLayer*)view.layer;
    if (view.isPaused && [metalLayer isKindOfClass:[CAMetalLayer class]] && !CGSizeEqualToSize(metalLayer.drawableSize, size))
    {
        metalLayer.drawableSize = size;
    }

    uno_get_resize_callback()((__bridge void*) view.window, size.width / scale, size.height / scale);
}

@end

// --- Render thread drawable lifecycle functions ---
// Called from the managed render thread (a background thread). These use
// CAMetalLayer.nextDrawable directly instead of MTKView.currentDrawable, which is only
// valid during drawInMTKView: callbacks on the main thread. nextDrawable is thread-safe.
// See: https://developer.apple.com/documentation/quartzcore/cametallayer

static MTKView* _Nullable uno_window_metal_view(NSWindow* window)
{
    if (window == nil) return nil;

    // The rendering view is a subview of the container returned by contentViewController.view,
    // so it must be reached through renderingView rather than the content view itself.
    NSView* renderingView = ((UNOWindow*)window).renderingView;
    return [renderingView isKindOfClass:[MTKView class]] ? (MTKView*)renderingView : nil;
}

bool uno_window_get_drawable_size(NSWindow* window, double* width, double* height)
{
    @autoreleasepool {
        MTKView* view = uno_window_metal_view(window);
        if (view == nil) return false;

        CGSize size = view.drawableSize;
        *width = size.width;
        *height = size.height;
        return size.width > 0 && size.height > 0;
    }
}

void* _Nullable uno_window_create_render_texture(NSWindow* window, int width, int height)
{
    @autoreleasepool {
        MTKView* view = uno_window_metal_view(window);
        if (view == nil || width <= 0 || height <= 0) return NULL;

        MTLTextureDescriptor* descriptor = [MTLTextureDescriptor texture2DDescriptorWithPixelFormat:view.colorPixelFormat
                                                                                              width:(NSUInteger)width
                                                                                             height:(NSUInteger)height
                                                                                          mipmapped:NO];
        // RenderTarget so Skia can draw into it, ShaderRead so the present blit (and acrylic sampling) can read it.
        descriptor.usage = MTLTextureUsageRenderTarget | MTLTextureUsageShaderRead;
        descriptor.storageMode = MTLStorageModePrivate;

        id<MTLTexture> texture = [view.device newTextureWithDescriptor:descriptor];
        return texture == nil ? NULL : (void*)CFBridgingRetain(texture);
    }
}

void uno_window_release_texture(void* _Nullable texture)
{
    if (texture != NULL)
    {
        CFBridgingRelease(texture);
    }
}

bool uno_window_present_texture(NSWindow* window, void* texture)
{
    @autoreleasepool {
        MTKView* view = uno_window_metal_view(window);
        if (view == nil || texture == NULL) return false;

        UNOMetalViewDelegate* delegate = ((UNOWindow*)window).metalViewDelegate;
        if (delegate == nil) return false;

        CAMetalLayer* metalLayer = (CAMetalLayer*)view.layer;
        if (![metalLayer isKindOfClass:[CAMetalLayer class]]) return false;

        // Acquired only now, with the frame already composed: nextDrawable blocks for ~1s and then returns nil
        // once the layer's pool is exhausted, so the drawable must be held for the blit alone and not across
        // the render.
        // See: https://developer.apple.com/library/archive/documentation/3DDrawing/Conceptual/MTLBestPracticesGuide/Drawables.html
        id<CAMetalDrawable> drawable = [metalLayer nextDrawable];
        if (drawable == nil) return false;

        id<MTLTexture> source = (__bridge id<MTLTexture>)texture;
        id<MTLTexture> destination = drawable.texture;
        // A mismatch means the layer resized under us; that frame is blitted short and the next acquire resizes.
        NSUInteger width = MIN(source.width, destination.width);
        NSUInteger height = MIN(source.height, destination.height);

        id<MTLCommandBuffer> commandBuffer = [delegate.queue commandBuffer];
        id<MTLBlitCommandEncoder> blit = [commandBuffer blitCommandEncoder];
        [blit copyFromTexture:source
                  sourceSlice:0
                  sourceLevel:0
                 sourceOrigin:MTLOriginMake(0, 0, 0)
                   sourceSize:MTLSizeMake(width, height, 1)
                    toTexture:destination
             destinationSlice:0
             destinationLevel:0
            destinationOrigin:MTLOriginMake(0, 0, 0)];
        [blit endEncoding];

        [commandBuffer presentDrawable:drawable];
        [commandBuffer commit];
        return true;
    }
}
