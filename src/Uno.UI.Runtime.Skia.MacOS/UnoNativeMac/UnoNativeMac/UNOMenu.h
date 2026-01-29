//
//  UNOMenu.h
//

#pragma once

#import "UnoNativeMac.h"

NS_ASSUME_NONNULL_BEGIN

typedef void (*menu_item_callback_fn_ptr)(int32 callbackId);

void uno_menu_set_item_callback(menu_item_callback_fn_ptr callback);

void uno_menu_bar_clear(void);
void uno_menu_bar_add_menu(NSMenu *menu, const char *title);

NSMenu* uno_menu_create(const char *title);
void uno_menu_add_separator(NSMenu *menu);
void uno_menu_add_item(NSMenu *menu, const char *title, bool enabled, int32 callbackId);
void uno_menu_add_toggle_item(NSMenu *menu, const char *title, bool isChecked, bool enabled, int32 callbackId);
void uno_menu_add_submenu(NSMenu *parentMenu, NSMenu *submenu, const char *title);

NS_ASSUME_NONNULL_END
