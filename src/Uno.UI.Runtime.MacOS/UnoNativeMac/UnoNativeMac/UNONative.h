//
//  UNONative.h
//

#pragma once

#import "UnoNativeMac.h"
#import "UNOWindow.h"

NS_ASSUME_NONNULL_BEGIN

@protocol UNONativeElement

/// Weak, so an element the app keeps around does not keep a closed window's rendering view alive.
@property (nonatomic, weak, nullable) NSView* originalSuperView;

-(void) dispose;

@optional

/// Size reported when the element is measured without a finite constraint.
-(NSSize) naturalSize;

@end

@interface UNORedView : NSView

@end

/// Container for an application-provided NSView, which it keeps alive until disposed.
@interface UNONativeViewHost : NSView<UNONativeElement>

@property (nonatomic, nullable) NSView* hostedView;

@end

/// Takes the native-side strong reference on a freshly created element.
/// Creators hand back an autoreleased object, so without this the peer dies at the next pool drain and
/// every later `uno_native_*` call retains freed memory.
void uno_native_track(NSView<UNONativeElement>* element);

/// Creates a plain view for the samples, to be wrapped by `uno_native_create_host` before the pool drains.
NSView* uno_native_create_sample(const char* _Nullable text);

/// Returns nil when `view` is not an NSView.
NSView* _Nullable uno_native_create_host(id view);

void uno_native_arrange(NSView<UNONativeElement>* element, double arrangeLeft, double arrangeTop, double arrangeWidth, double arrangeHeight);

void uno_native_attach(NSView<UNONativeElement>* element, NSWindow* window);

void uno_native_detach(NSView<UNONativeElement>* element);

bool uno_native_is_attached(NSView<UNONativeElement>* element);

void uno_native_measure(NSView<UNONativeElement>* element, double childWidth, double childHeight, double availableWidth, double availableHeight, double* width, double* height);

void uno_native_set_opacity(NSView<UNONativeElement>* element, double opacity);

void uno_native_dispose(NSView<UNONativeElement> *element);

NS_ASSUME_NONNULL_END
