//
//  UNONative.h
//

#pragma once

#import "UnoNativeMac.h"
#import "UNOWindow.h"

NS_ASSUME_NONNULL_BEGIN

@protocol UNONativeElement

@property (nonatomic) bool visible;

-(void) detach;

@end

@interface UNORedView : NSView<UNONativeElement>

@end

NSView* uno_native_create_sample(NSWindow *window, const char* _Nullable text);

void uno_native_arrange(NSView *element, double arrangeLeft, double arrangeTop, double arrangeWidth, double arrangeHeight);

void uno_native_attach(NSView* element);

void uno_native_detach(NSView* element);

bool uno_native_is_attached(NSView* element);

void uno_native_measure(NSView* element, double childWidth, double childHeight, double availableWidth, double availableHeight, double* width, double* height);

void uno_native_set_opacity(NSView* element, double opacity);

void uno_native_set_visibility(NSView* element, bool visible);

NS_ASSUME_NONNULL_END
