//
//  UNOApplication.m
//

#import "UNOApplication.h"

static system_theme_change_fn_ptr system_theme_change;

inline system_theme_change_fn_ptr uno_get_system_theme_change_callback(void)
{
    return system_theme_change;
}

void uno_set_system_theme_change_callback(system_theme_change_fn_ptr p)
{
    system_theme_change = p;
}

uint32 /* Uno.Helpers.Theming.SystemTheme */ uno_get_system_theme(void)
{
    NSApplication *app = [NSApplication sharedApplication];
    NSAppearance *appearance = app.effectiveAppearance;
    NSAppearanceName appearanceName = [appearance bestMatchFromAppearancesWithNames:@[NSAppearanceNameAqua,
                                                                                      NSAppearanceNameDarkAqua]];
    return [appearanceName isEqualToString:NSAppearanceNameAqua] ? 0 : 1;
}

void uno_application_set_icon(const char *path)
{
    NSApplication *app = [NSApplication sharedApplication];
    app.applicationIconImage = [[NSImage alloc] initWithContentsOfFile:[NSString stringWithUTF8String:path]];
}

bool uno_application_open_url(const char *url)
{
    NSURL *u = [NSURL URLWithString:[NSString stringWithUTF8String:url]];
    return [[NSWorkspace sharedWorkspace] openURL:u];
}

bool uno_application_query_url_support(const char *url)
{
    NSURL *u = [NSURL URLWithString:[NSString stringWithUTF8String:url]];
    return [[NSWorkspace sharedWorkspace] URLForApplicationToOpenURL:u] != nil;
}

bool uno_application_is_full_screen(void)
{
    NSWindow *win = [[NSApplication sharedApplication] keyWindow];
    // keyWindow might not be set, yet - so we return false
    bool result = win;
    if (result) {
        result = (win.styleMask & NSWindowStyleMaskFullScreen) == NSWindowStyleMaskFullScreen;
    }
#if DEBUG
    NSLog(@"uno_application_is_fullscreen %@ %s", win, result ? "true" : "false");
#endif
    return result;
}

bool uno_application_enter_full_screen(void)
{
    NSWindow *win = [[NSApplication sharedApplication] keyWindow];
    bool result = win;
    if (result && (win.styleMask & NSWindowStyleMaskFullScreen) != NSWindowStyleMaskFullScreen) {
        [win toggleFullScreen:nil];
        result = true;
    }
#if DEBUG
    NSLog(@"uno_application_enter_fullscreen %@ %s", win, result ? "true" : "false");
#endif
    return result;
}

void uno_application_exit_full_screen(void)
{
    NSWindow *win = [[NSApplication sharedApplication] keyWindow];
    if (win && (win.styleMask & NSWindowStyleMaskFullScreen) == NSWindowStyleMaskFullScreen) {
        [win toggleFullScreen:nil];
    }
#if DEBUG
    NSLog(@"uno_application_exit_fullscreen %@", win);
#endif
}

static application_can_exit_fn_ptr application_can_exit;

inline application_can_exit_fn_ptr uno_get_application_can_exit_callback(void)
{
    return application_can_exit;
}

void uno_set_application_can_exit_callback(application_can_exit_fn_ptr p)
{
    application_can_exit = p;
}

void uno_application_quit(void)
{
#if DEBUG
    NSLog(@"uno_application_quit");
#endif
    NSApplication *app = [NSApplication sharedApplication];
    [app terminate:app];
}

@implementation UNOApplicationDelegate

- (void)applicationDidFinishLaunching:(NSNotification *)notification
{
#if DEBUG
    NSLog(@"UNOApplicationDelegate.applicationDidFinishLaunching %@", notification);
#endif
    NSWindow *win = uno_app_get_main_window();
    [win makeKeyWindow];
    [win orderFrontRegardless];
}

- (BOOL)applicationShouldTerminateAfterLastWindowClosed:(NSApplication *)theApplication
{
#if DEBUG
    NSLog(@"UNOApplicationDelegate.applicationShouldTerminateAfterLastWindowClosed %@", theApplication);
#endif
    // as long as `ICoreApplicationExtension.CanExit` returns `true` there's no need for any additional check here
    return uno_get_application_can_exit_callback()() ? YES : NO;
}

- (void)observeValueForKeyPath:(NSString *)keyPath ofObject:(id)object change:(NSDictionary<NSString *,id> *)change context:(void *)context
{
#if DEBUG
    NSLog(@"UNOApplicationDelegate.observeValueForKeyPath keyPath:%@", keyPath);
#endif
    if ([keyPath isEqualToString:NSStringFromSelector(@selector(effectiveAppearance))]) {
        dispatch_async(dispatch_get_main_queue(), ^{
            uno_get_system_theme_change_callback()();
        });
    }
}

@end
