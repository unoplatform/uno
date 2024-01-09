//
//  UnoNativeMac.m
//

#import "UnoNativeMac.h"
#import "UNOApplication.h"
#import "UNOMetalViewDelegate.h"
#import "UNOWindow.h"

static UNOMetalViewDelegate *d;
static NSWindow *main_window;

static draw_fn_ptr draw;
static resize_fn_ptr resize;
static window_did_change_screen_fn_ptr window_did_change_screen;
static window_did_change_screen_parameters_fn_ptr window_did_change_screen_parameters;

static id<NSObject> sn;
static id windowDidChangeScreen;

static UNOApplicationDelegate *ad;

// libSkiaSharp
extern void* gr_direct_context_make_metal(id device, id queue);


@interface windowDidChangeScreenNoteClass : NSObject
{
    struct SharedScreenData *sharedScreenData;
}
+ (windowDidChangeScreenNoteClass*) initWith:(void*) screenData;
- (void) windowDidChangeScreenNotification:(NSNotification*) note;
- (void) applicationDidChangeScreenParametersNotification:(NSNotification*) note;
@end

@implementation windowDidChangeScreenNoteClass

+ (windowDidChangeScreenNoteClass*) initWith:(void*) screenData
{
    windowDidChangeScreenNoteClass *windowDidChangeScreen = [windowDidChangeScreenNoteClass new];
    windowDidChangeScreen->sharedScreenData = screenData;
    return windowDidChangeScreen;
}

- (void) windowDidChangeScreenNotification:(NSNotification*) note
{
    NSScreen *screen = [note.object screen];
    CGSize s = [screen convertRectToBacking:screen.frame].size;
    // store basic, non-calculated values inside shared memory
    sharedScreenData->ScreenHeightInRawPixels = (uint)s.height;
    sharedScreenData->ScreenWidthInRawPixels = (uint)s.width;
    sharedScreenData->RawPixelsPerViewPixel = screen.backingScaleFactor;
#if DEBUG
    NSLog(@"ScreenHeightInRawPixels %d ScreenWidthInRawPixels %d RawPixelsPerViewPixel %d", sharedScreenData->ScreenHeightInRawPixels, sharedScreenData->ScreenWidthInRawPixels, sharedScreenData->RawPixelsPerViewPixel);
#endif
    uno_get_window_did_change_screen_callback()();
}

- (void) applicationDidChangeScreenParametersNotification:(NSNotification*) note
{
#if DEBUG
    NSLog(@"NSApplicationDidChangeScreenParametersNotification");
#endif
    uno_get_window_did_change_screen_parameters_callback()();
}

@end

NSWindow* uno_app_get_main_window(void)
{
    return main_window;
}

// TODO
// - add initial window size (GCSize)
// - add initial background color
void* uno_app_initialize(char *title)
{
    CGRect size = NSMakeRect(0, 0, 800, 600);
    NSApplication *app = [NSApplication sharedApplication];
    app.delegate = ad = [[UNOApplicationDelegate alloc] init];
    [app setActivationPolicy:NSApplicationActivationPolicyRegular];
    
    main_window = [[UNOWindow alloc] initWithContentRect:size
        styleMask:NSWindowStyleMaskTitled|NSWindowStyleMaskClosable|NSWindowStyleMaskMiniaturizable|NSWindowStyleMaskResizable
        backing:NSBackingStoreBuffered defer:NO];
    
    NSViewController *vc = [[NSViewController alloc] init];

    id<MTLDevice> device = MTLCreateSystemDefaultDevice();
    
    MTKView *v = [[MTKView alloc] initWithFrame:size device:device];
    v.enableSetNeedsDisplay = YES;
//    v.clearColor = MTLClearColorMake(0.0, 0.5, 1.0, 1.0); // FIXME: remove or set to default background color
    v.delegate = d = [[UNOMetalViewDelegate alloc] initWithMetalKitView:v];
    vc.view = v;

    main_window.contentViewController = vc;
    
    // Notifications
    NSNotificationCenter *center = [NSNotificationCenter defaultCenter];
    NSNotification *nw = [[NSNotification alloc] initWithName:NSWindowDidChangeScreenNotification object:main_window userInfo:nil];

    assert(windowDidChangeScreen);
    [center addObserver:windowDidChangeScreen selector:@selector(windowDidChangeScreenNotification:) name:NSWindowDidChangeScreenNotification object:main_window];
    // we need values for the current screen (before it change, because it might never)
    [windowDidChangeScreen windowDidChangeScreenNotification:nw];

    [center addObserver:windowDidChangeScreen selector:@selector(applicationDidChangeScreenParametersNotification:) name:NSApplicationDidChangeScreenParametersNotification object:nil];
    // we need values for the current screen (before it change, because it might never)
    [windowDidChangeScreen applicationDidChangeScreenParametersNotification:nw];
    
    // KVO observation for dark/light theme
    [app addObserver:ad forKeyPath:NSStringFromSelector(@selector(effectiveAppearance)) options:NSKeyValueObservingOptionNew context:nil];

    main_window.title = [NSString stringWithUTF8String:title];

    return gr_direct_context_make_metal(device, d.queue);
}

inline window_did_change_screen_fn_ptr uno_get_window_did_change_screen_callback(void)
{
    return window_did_change_screen;
}

void uno_set_window_did_change_screen_callback(struct SharedScreenData *screenData, window_did_change_screen_fn_ptr p)
{
    windowDidChangeScreen = [windowDidChangeScreenNoteClass initWith:screenData];
    window_did_change_screen = p;
}

inline window_did_change_screen_parameters_fn_ptr uno_get_window_did_change_screen_parameters_callback(void)
{
    return window_did_change_screen_parameters;
}

void uno_set_window_did_change_screen_parameters_callback(window_did_change_screen_parameters_fn_ptr p)
{
    window_did_change_screen_parameters = p;
}

inline draw_fn_ptr uno_get_draw_callback(void)
{
    return draw;
}

void uno_set_draw_callback(draw_fn_ptr p)
{
    draw = p;
}

inline resize_fn_ptr uno_get_resize_callback(void)
{
    return resize;
}

void uno_set_resize_callback(resize_fn_ptr p)
{
    resize = p;
}
