//
//  UNOClipboard.h
//

#pragma once

#import <AppKit/AppKit.h>
#import <Foundation/Foundation.h>

NS_ASSUME_NONNULL_BEGIN

void uno_clipboard_clear(void);

void* _Nullable uno_clipboard_get_content(void);
void uno_clipboard_set_content(void* content);

void uno_clipboard_start_content_changed(void);
void uno_clipboard_stop_content_changed(void);

NS_ASSUME_NONNULL_END
