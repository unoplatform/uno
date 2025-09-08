//
//  UNOApplication.m
//

#import "UNOApplication.h"

static UNOApplicationDelegate *ad;
static system_theme_change_fn_ptr system_theme_change;
static id<MTLDevice> device;

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

void uno_application_set_badge(const char *badge)
{
    NSApplication *app = [NSApplication sharedApplication];
    NSDockTile *dockTile = [app dockTile];
    [dockTile setBadgeLabel:[NSString stringWithUTF8String:badge]];
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

static application_can_exit_fn_ptr application_can_exit;

inline application_can_exit_fn_ptr uno_get_application_can_exit_callback(void)
{
    return application_can_exit;
}

void uno_set_application_can_exit_callback(application_can_exit_fn_ptr p)
{
    application_can_exit = p;
}

static application_start_fn_ptr application_start;

inline application_start_fn_ptr uno_get_application_start_callback(void)
{
    return application_start;
}

void uno_set_application_start_callback(application_start_fn_ptr p)
{
    application_start = p;
}

typedef void (*application_start_fn_ptr)(void);
application_start_fn_ptr uno_get_application_start_callback(void);

bool uno_application_is_bundled(void)
{
    return NSRunningApplication.currentApplication.bundleIdentifier != nil;
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
    NSWindow *win = uno_app_get_main_window();
    NSLog(@"UNOApplicationDelegate.applicationDidFinishLaunching notification %@ win %@", notification, win);
#endif
    // creating the window will call `makeKeyWindow` and `orderFrontRegardless`

    uno_get_application_start_callback()();
}

- (NSApplicationTerminateReply)applicationShouldTerminate:(NSApplication *)sender {
#if DEBUG
    NSLog(@"UNOApplicationDelegate.applicationShouldTerminate %@", sender);
#endif
    // as long as `ICoreApplicationExtension.CanExit` returns `true` there's no need for any additional check here
    return uno_get_application_can_exit_callback()() ? NSTerminateNow : NSTerminateCancel;
}

- (BOOL)applicationShouldTerminateAfterLastWindowClosed:(NSApplication *)sender {
#if DEBUG
    NSLog(@"UNOApplicationDelegate.applicationShouldTerminateAfterLastWindowClosed %@", sender);
#endif
    return YES;
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
