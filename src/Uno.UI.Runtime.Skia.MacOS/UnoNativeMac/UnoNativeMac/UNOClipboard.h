//
//  UNOClipboard.h
//

#pragma once

#import "UnoNativeMac.h"

NS_ASSUME_NONNULL_BEGIN

void uno_clipboard_clear(void);

void uno_clipboard_get_content(const char* _Nonnull * _Nullable htmlContent, const char* _Nonnull * _Nullable rtfContent, const char* _Nonnull * _Nullable  textContent, const char* _Nonnull * _Nullable uri, const char* _Nonnull * _Nullable fileUrl);
bool uno_clipboard_set_content(char* htmlContent, char* rtfContent, char* textContent, char* uri);

void uno_clipboard_start_content_changed(void);
void uno_clipboard_stop_content_changed(void);

typedef void (*clipboard_changed_fn_ptr)(void);
clipboard_changed_fn_ptr uno_clipboard_get_content_changed_callback(void);
void uno_clipboard_set_content_changed_callback(clipboard_changed_fn_ptr p);

NS_ASSUME_NONNULL_END
