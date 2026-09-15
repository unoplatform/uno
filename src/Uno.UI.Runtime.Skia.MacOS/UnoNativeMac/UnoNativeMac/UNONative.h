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

/// Takes the native-side strong reference on a freshly created element.
/// Creators hand back an autoreleased object, so without this the peer dies at the next pool drain and
/// every later `uno_native_*` call retains freed memory.
void uno_native_track(NSView<UNONativeElement>* element);

NSView* uno_native_create_sample(NSWindow *window, const char* _Nullable text);

void uno_native_arrange(NSView<UNONativeElement>* element, double arrangeLeft, double arrangeTop, double arrangeWidth, double arrangeHeight);

void uno_native_attach(NSView<UNONativeElement>* element);

void uno_native_detach(NSView<UNONativeElement>* element);

bool uno_native_is_attached(NSView<UNONativeElement>* element);

void uno_native_measure(NSView<UNONativeElement>* element, double childWidth, double childHeight, double availableWidth, double availableHeight, double* width, double* height);

void uno_native_set_opacity(NSView<UNONativeElement>* element, double opacity);

void uno_native_dispose(NSView<UNONativeElement> *element);

int32_t uno_password_vault_read(const char* scope, uint8_t* _Nullable * _Nonnull data, int32_t* length);

int32_t uno_password_vault_write(const char* scope, const uint8_t* data, int32_t length);

void uno_password_vault_free(uint8_t* data, int32_t length);

NS_ASSUME_NONNULL_END
