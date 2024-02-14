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

@end

typedef void (*draw_fn_ptr)(void* /* window */, double /* width */, double /* height */, void* _Nullable /* texture */);
draw_fn_ptr uno_get_draw_callback(void);
void uno_set_draw_callback(draw_fn_ptr p);

NS_ASSUME_NONNULL_END
