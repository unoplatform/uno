//
//  UNOMetalViewDelegate.h
//

#pragma once

#import "UnoNativeMac.h"

NS_ASSUME_NONNULL_BEGIN

@import MetalKit;

@interface UNOMetalViewDelegate : NSObject<MTKViewDelegate>

- (nonnull instancetype)initWithMetalKitView:(nonnull MTKView *)mtkView;

@property (nonatomic, strong, nullable) id<MTLCommandQueue> queue;

// When YES, the negotiated context owns the view's CAMetalLayer (drawable acquire + present). drawInMTKView then
// skips its own currentDrawable acquire / presentDrawable and just ticks managed code (texture = NULL), which drives
// the context's own swapchain. See uno_window_set_external_present / uno_window_get_metal_layer.
@property (assign) BOOL externalPresent;

/// Stops tracking vsyncs for good. Called when the window closes.
- (void)invalidateVsync;

@end

typedef void (*metal_draw_fn_ptr)(void* /* window */, double /* width */, double /* height */, void* _Nullable /* texture */);
metal_draw_fn_ptr uno_get_metal_draw_callback(void);
void uno_set_draw_callback(metal_draw_fn_ptr p);

/// Reports the window's CAMetalLayer drawable size, in pixels. Returns false when it has no Metal view
/// or the layer has no size yet. Called from the managed render thread.
bool uno_window_get_drawable_size(NSWindow* window, double* width, double* height);

/// Creates a render texture in the Metal view's own pixel format, retained for the caller to keep across
/// frames, and released with uno_window_release_texture. Returns NULL when the window has no Metal view.
void* _Nullable uno_window_create_render_texture(NSWindow* window, int width, int height);

/// Releases a texture returned by uno_window_create_render_texture.
void uno_window_release_texture(void* _Nullable texture);

/// Acquires the layer's next drawable, blits the already-composed texture onto it, presents and releases it.
/// Returns false when the layer vended no drawable. Called from the managed render thread.
bool uno_window_present_texture(NSWindow* window, void* texture);

/// Gets the time of the latest vsync of the window's display that was seen (CACurrentMediaTime seconds, so the same
/// vsync always gives the same value) and the vsync period. False when unknown: before macOS 14, before the first
/// vsync, or when the display stopped delivering them. Keeps vsync tracking running while it is being asked. Called
/// from the managed render thread.
bool uno_window_get_vsync(NSWindow* window, double* lastVsync, double* period);

NS_ASSUME_NONNULL_END
