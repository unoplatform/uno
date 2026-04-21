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

/// Holds the drawable acquired by uno_window_acquire_next_frame until
/// uno_window_present_frame is called. Only accessed from the render thread.
@property (nullable) id<CAMetalDrawable> currentFrameDrawable;

@end

typedef void (*metal_draw_fn_ptr)(void* /* window */, double /* width */, double /* height */, void* _Nullable /* texture */);
metal_draw_fn_ptr uno_get_metal_draw_callback(void);
void uno_set_draw_callback(metal_draw_fn_ptr p);

/// Acquires the next drawable from the MTKView and returns its texture handle and size.
/// The drawable is held by the delegate until uno_window_present_frame is called.
/// Returns false if no drawable is available.
bool uno_window_acquire_next_frame(NSWindow* window, void* _Nullable * _Nonnull texture, double* width, double* height);

/// Presents the previously acquired drawable via a Metal command buffer.
/// Must be called after uno_window_acquire_next_frame returned true.
void uno_window_present_frame(NSWindow* window);

NS_ASSUME_NONNULL_END
