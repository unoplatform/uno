//
//  UNOSoftView.h
//

#pragma once

#import "UnoNativeMac.h"

NS_ASSUME_NONNULL_BEGIN

@import AppKit;

@interface UNOSoftView : NSView <NSTextInputClient, NSDraggingDestination, NSDraggingSource>

- (void)drawRect:(NSRect)dirtyRect;
- (void)setFrameSize:(NSSize)newSize;

@property (nonatomic) BOOL imeActive;
@property (nonatomic, readonly) BOOL keyEventHandledByIME;

- (NSDragOperation)draggingEntered:(id<NSDraggingInfo>)sender;
- (NSDragOperation)draggingUpdated:(id<NSDraggingInfo>)sender;
- (void)draggingExited:(nullable id<NSDraggingInfo>)sender;
- (BOOL)performDragOperation:(id<NSDraggingInfo>)sender;

- (NSDragOperation)draggingSession:(NSDraggingSession *)session sourceOperationMaskForDraggingContext:(NSDraggingContext)context;
- (void)draggingSession:(NSDraggingSession *)session endedAtPoint:(NSPoint)screenPoint operation:(NSDragOperation)operation;

@end

typedef void (*soft_draw_fn_ptr)(void* /* window */, double /* width */, double /* height */, void* /* data */, int* /* rowBytes */, int* /* size */);
soft_draw_fn_ptr uno_get_soft_draw_callback(void);
void uno_set_soft_draw_callback(soft_draw_fn_ptr p);

NS_ASSUME_NONNULL_END
