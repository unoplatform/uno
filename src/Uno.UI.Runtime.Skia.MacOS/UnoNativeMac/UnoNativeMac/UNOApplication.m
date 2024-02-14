//
//  UNOApplication.m
//

#import "UNOApplication.h"

static UNOApplicationDelegate *ad;
static system_theme_change_fn_ptr system_theme_change;
static id<MTLDevice> device;
static NSTimeInterval uptime = 0;

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

NSTimeInterval uno_get_system_uptime(void)
{
    if (uptime == 0) {
        uptime = NSProcessInfo.processInfo.systemUptime;
    }
    return uptime;
}

bool uno_app_initialize(bool *metal)
{
    NSApplication *app = [NSApplication sharedApplication];
    if (app) {
        app.delegate = ad = [[UNOApplicationDelegate alloc] init];
        [app setActivationPolicy:NSApplicationActivationPolicyRegular];

        // KVO observation for dark/light theme
        [app addObserver:ad forKeyPath:NSStringFromSelector(@selector(effectiveAppearance)) options:NSKeyValueObservingOptionNew context:nil];
    }
    device = MTLCreateSystemDefaultDevice();
#if DEBUG
    NSLog(@"uno_app_initialize Metal requested %s available %s", *metal ? "true" : "false", device != nil ? "true" : "false");
#endif
    if (*metal == NO) {
        // even if Metal was not requested we still return if it was available
        *metal = device != nil;
        device = nil;
    } else {
        *metal = device != nil;
    }
    return app != nil;
}

id<MTLDevice> uno_application_get_metal_device(void)
{
    return device;
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
    NSWindow *win = uno_app_get_main_window();
#if DEBUG
    NSLog(@"UNOApplicationDelegate.applicationDidFinishLaunching notification %@ win %@", notification, win);
#endif
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
