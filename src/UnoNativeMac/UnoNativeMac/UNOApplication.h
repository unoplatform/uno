//
//  UNOApplication.h
//

#import <AppKit/AppKit.h>
#import <Foundation/Foundation.h>
#import "UnoNativeMac.h"

#pragma once

NS_ASSUME_NONNULL_BEGIN

typedef void (*system_theme_change_fn_ptr)(void);
system_theme_change_fn_ptr uno_get_system_theme_change_callback(void);
void uno_set_system_theme_change_callback(system_theme_change_fn_ptr p);
uint32 uno_get_system_theme(void);

void uno_application_set_icon(const char *path);
bool uno_application_open_url(const char *url);
bool uno_application_query_url_support(const char *url);

bool uno_application_enter_fullscreen(void);
void uno_application_exit_fullscreen(void);

bool uno_application_is_fullscreen(void);
void uno_application_toggle_fullscreen(void);

typedef bool (*application_can_exit_fn_ptr)(void);
application_can_exit_fn_ptr uno_get_application_can_exit_callback(void);
void uno_set_application_can_exit_callback(application_can_exit_fn_ptr p);
void uno_application_quit(void);

@interface UNOApplicationDelegate : NSObject <NSApplicationDelegate>

- (void)applicationDidFinishLaunching:(NSNotification *)notification;
- (BOOL)applicationShouldTerminateAfterLastWindowClosed:(NSApplication *)theApplication;
- (void)observeValueForKeyPath:(nullable NSString *)keyPath ofObject:(nullable id)object change:(nullable NSDictionary<NSKeyValueChangeKey, id> *)change context:(nullable void *)context;

@end

NS_ASSUME_NONNULL_END
