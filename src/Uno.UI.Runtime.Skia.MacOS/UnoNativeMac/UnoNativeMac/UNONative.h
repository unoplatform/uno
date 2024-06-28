//
//  UNONative.h
//

#pragma once

#import "UnoNativeMac.h"

NS_ASSUME_NONNULL_BEGIN

NSView* uno_native_create_sample(NSWindow *window, const char* _Nullable text);

void uno_native_measure(NSView* element, double childWidth, double childHeight, double availableWidth, double availableHeight, double* width, double* height);

void uno_native_set_opacity(NSView* element, double opacity);

void uno_native_set_visibility(NSView* element, bool visible);

NS_ASSUME_NONNULL_END
