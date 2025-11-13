//
//  UNONative.h
//

#pragma once

#import "UnoNativeMac.h"
#import "UNOWindow.h"

NS_ASSUME_NONNULL_BEGIN

@protocol UNONativeElement

@property (nonatomic) NSView* originalSuperView;

-(void) dispose;

@end

@interface UNORedView : NSView<UNONativeElement>

@end

NSView* uno_native_create_sample(NSWindow *window, const char* _Nullable text);

void uno_native_arrange(NSView<UNONativeElement>* element, double arrangeLeft, double arrangeTop, double arrangeWidth, double arrangeHeight);

void uno_native_attach(NSView<UNONativeElement>* element);

void uno_native_detach(NSView<UNONativeElement>* element);

bool uno_native_is_attached(NSView<UNONativeElement>* element);

void uno_native_measure(NSView<UNONativeElement>* element, double childWidth, double childHeight, double availableWidth, double availableHeight, double* width, double* height);

void uno_native_set_opacity(NSView<UNONativeElement>* element, double opacity);

void uno_native_dispose(NSView<UNONativeElement> *element);

NS_ASSUME_NONNULL_END
