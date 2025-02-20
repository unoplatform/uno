//
//  UNOSoftView.h
//

#pragma once

#import "UnoNativeMac.h"

NS_ASSUME_NONNULL_BEGIN

@import AppKit;

@interface UNOSoftView : NSView

- (void)drawRect:(NSRect)dirtyRect;
- (void)setFrameSize:(NSSize)newSize;

@end

typedef void (*soft_draw_fn_ptr)(void* /* window */, double /* width */, double /* height */, void* /* data */, int* /* rowBytes */, int* /* size */);
soft_draw_fn_ptr uno_get_soft_draw_callback(void);
void uno_set_soft_draw_callback(soft_draw_fn_ptr p);

NS_ASSUME_NONNULL_END
