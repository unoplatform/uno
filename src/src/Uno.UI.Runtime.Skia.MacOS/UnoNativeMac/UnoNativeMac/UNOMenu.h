#import <Foundation/Foundation.h>
#import <AppKit/AppKit.h>

#ifdef __cplusplus
extern "C" {
#endif

typedef void (*menu_item_click_fn_ptr)(const char* _Nonnull id);

void uno_menu_set_click_callback(menu_item_click_fn_ptr _Nullable p);

void uno_menu_begin(void);
void uno_menu_begin_top(const char* _Nonnull id, const char* _Nonnull title);
void uno_menu_add_item(const char* _Nonnull id, const char* _Nonnull title, const char* _Nullable keyEquivalent);
void uno_menu_add_separator(void);
void uno_menu_begin_submenu(const char* _Nonnull id, const char* _Nonnull title);
void uno_menu_end_submenu(void);
void uno_menu_end_top(void);
void uno_menu_commit(void);
void uno_menu_set_enabled(const char* _Nonnull id, bool enabled);

#ifdef __cplusplus
}
#endif
