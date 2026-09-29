//
//  UNOClipboard.h
//

#pragma once

#import "UnoNativeMac.h"

NS_ASSUME_NONNULL_BEGIN

struct ClipboardData {
    char* htmlContent;
    char* rtfContent;
    char* textContent;
    char* uri;
    char* fileUrl;

    char* bitmapFormat;
    char* bitmapPath;
    void* bitmapData;
    uint64 bitmapSize;
};

void uno_clipboard_clear(void);

void uno_clipboard_get_content(struct ClipboardData* data);
bool uno_clipboard_set_content(struct ClipboardData* data);

void uno_clipboard_start_content_changed(void);
void uno_clipboard_stop_content_changed(void);

typedef void (*clipboard_changed_fn_ptr)(void);
clipboard_changed_fn_ptr uno_clipboard_get_content_changed_callback(void);
void uno_clipboard_set_content_changed_callback(clipboard_changed_fn_ptr p);

NS_ASSUME_NONNULL_END
