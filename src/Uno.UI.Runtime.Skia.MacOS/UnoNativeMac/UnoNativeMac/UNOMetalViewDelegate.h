//
//  UNOMetalViewDelegate.h
//

#pragma once

#import "UnoNativeMac.h"

NS_ASSUME_NONNULL_BEGIN

@import MetalKit;

@interface UNOMetalViewDelegate : NSObject<MTKViewDelegate>

- (nonnull instancetype)initWithMetalKitView:(nonnull MTKView *)mtkView;

@property (nullable) id<MTLCommandQueue> queue;

// When YES, the negotiated context owns the view's CAMetalLayer (drawable acquire + present). drawInMTKView then
// skips its own currentDrawable acquire / presentDrawable and just ticks managed code (texture = NULL), which drives
// the context's own swapchain. See uno_window_set_external_present / uno_window_get_metal_layer.
@property (assign) BOOL externalPresent;

@end

typedef void (*metal_draw_fn_ptr)(void* /* window */, double /* width */, double /* height */, void* _Nullable /* texture */);
metal_draw_fn_ptr uno_get_metal_draw_callback(void);
void uno_set_draw_callback(metal_draw_fn_ptr p);

NS_ASSUME_NONNULL_END
