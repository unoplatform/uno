//
//  UNOApplication.h
//

#pragma once

#import "UnoNativeMac.h"

NS_ASSUME_NONNULL_BEGIN

typedef void (*system_theme_change_fn_ptr)(void);
system_theme_change_fn_ptr uno_get_system_theme_change_callback(void);
void uno_set_system_theme_change_callback(system_theme_change_fn_ptr p);
uint32 uno_get_system_theme(void);

typedef void (*high_contrast_change_fn_ptr)(void);
high_contrast_change_fn_ptr uno_get_high_contrast_change_callback(void);
void uno_set_high_contrast_change_callback(high_contrast_change_fn_ptr p);
bool uno_get_high_contrast(void);

// High Contrast system colors (ARGB packed as uint32)
typedef struct {
    uint32 buttonFaceColor;
    uint32 buttonTextColor;
    uint32 grayTextColor;
    uint32 highlightColor;
    uint32 highlightTextColor;
    uint32 hotlightColor;
    uint32 windowColor;
    uint32 windowTextColor;
    uint32 activeCaptionColor;
    uint32 backgroundColor;
    uint32 captionTextColor;
    uint32 inactiveCaptionColor;
    uint32 inactiveCaptionTextColor;
    uint32 disabledTextColor;
} UnoHighContrastColors;

void uno_get_high_contrast_colors(UnoHighContrastColors *colors);

typedef void (*text_scale_factor_change_fn_ptr)(void);
text_scale_factor_change_fn_ptr uno_get_text_scale_factor_change_callback(void);
void uno_set_text_scale_factor_change_callback(text_scale_factor_change_fn_ptr p);
double uno_get_text_scale_factor(void);

bool uno_app_initialize(bool *supportsMetal);
NSWindow* uno_app_get_main_window(void);

id<MTLDevice> uno_application_get_metal_device(void);
void uno_application_set_badge(const char *badge);
void uno_application_set_icon(const char *path);
bool uno_application_open_url(const char *url);
bool uno_application_query_url_support(const char *url);
bool uno_application_is_bundled(void);

typedef void (*application_start_fn_ptr)(void);
application_start_fn_ptr uno_get_application_start_callback(void);

typedef bool (*application_can_exit_fn_ptr)(void);
application_can_exit_fn_ptr uno_get_application_can_exit_callback(void);
void uno_set_application_can_exit_callback(application_can_exit_fn_ptr p);
void uno_application_quit(void);

@interface UNOApplicationDelegate : NSObject <NSApplicationDelegate>

- (void)applicationDidFinishLaunching:(NSNotification *)notification;
- (NSApplicationTerminateReply)applicationShouldTerminate:(NSApplication *)sender;
- (BOOL)applicationShouldTerminateAfterLastWindowClosed:(NSApplication *)sender;
- (void)observeValueForKeyPath:(nullable NSString *)keyPath ofObject:(nullable id)object change:(nullable NSDictionary<NSKeyValueChangeKey, id> *)change context:(nullable void *)context;
- (void)accessibilityDisplayOptionsChanged:(NSNotification *)notification;
- (void)textScalePreferenceChanged:(NSNotification *)notification;

@end

NS_ASSUME_NONNULL_END
