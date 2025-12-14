//
//  UNOWindow.m
//

#import "UNOWindow.h"
#import "MouseButtons.h"
#import "UNOApplication.h"
#import "UNOSoftView.h"

static NSWindow *main_window;
static NSMutableSet<NSWindow*> *windows;
static id windowDidChangeScreen;

static window_did_change_screen_fn_ptr window_did_change_screen;
static window_did_change_screen_parameters_fn_ptr window_did_change_screen_parameters;

// libSkiaSharp
extern void* gr_direct_context_make_metal(id device, id queue);

static uno_drawable_resize_fn_ptr window_resize;
static metal_draw_fn_ptr metal_draw;
static soft_draw_fn_ptr soft_draw;

inline uno_drawable_resize_fn_ptr uno_get_resize_callback(void)
{
    return window_resize;
}

inline metal_draw_fn_ptr uno_get_metal_draw_callback(void)
{
    return metal_draw;
}

inline soft_draw_fn_ptr uno_get_soft_draw_callback(void)
{
    return soft_draw;
}

void uno_set_drawing_callbacks(metal_draw_fn_ptr metal, soft_draw_fn_ptr soft, uno_drawable_resize_fn_ptr resize)
{
    metal_draw = metal;
    soft_draw = soft;
    window_resize = resize;
}

@implementation windowDidChangeScreenNoteClass

+ (windowDidChangeScreenNoteClass*) init
{
    windowDidChangeScreenNoteClass *windowDidChangeScreen = [windowDidChangeScreenNoteClass new];
    return windowDidChangeScreen;
}

- (void) windowDidChangeScreenNotification:(NSNotification*) note
{
#if DEBUG
    NSLog(@"windowDidChangeScreenNotification %@", note);
#endif
    NSWindow *window = note.object;
    NSScreen *screen = window.screen;

    CGSize s = [screen convertRectToBacking:screen.frame].size;
#if DEBUG
    NSLog(@"    ScreenHeightInRawPixels %g ScreenWidthInRawPixels %g RawPixelsPerViewPixel %g", s.height, s.width, screen.backingScaleFactor);
#endif
    uno_get_window_did_change_screen_callback()(window, (uint)s.height, (uint)s.width, screen.backingScaleFactor);
}

- (void) applicationDidChangeScreenParametersNotification:(NSNotification*) note
{
    NSWindow *window = note.object;
#if DEBUG
    NSLog(@"NSApplicationDidChangeScreenParametersNotification %@", window);
#endif
    uno_get_window_did_change_screen_parameters_callback()(window);
}

@end

NSWindow* uno_app_get_main_window(void)
{
    if (!main_window) {
        main_window = uno_window_create(800, 600);
    }
#if DEBUG
    NSLog(@"uno_app_get_main_window %p", main_window);
#endif
    return main_window;
}

@implementation UNOMetalFlippedView : MTKView

// behave like UIView/UWP/WinUI, where the origin is top/left, instead of bottom/left
-(BOOL) isFlipped {
    return YES;
}

-(instancetype) initWithFrame:(CGRect)frameRect device:(id<MTLDevice>)device {
    self = [super initWithFrame:frameRect device:device];
    if (self) {
        // TODO
    }
    return self;
}

@end

NSWindow* uno_window_create(double width, double height)
{
    CGRect size = NSMakeRect(0, 0, width, height);
    UNOWindow *window = [[UNOWindow alloc] initWithContentRect:size
                                                     styleMask:NSWindowStyleMaskTitled|NSWindowStyleMaskClosable|NSWindowStyleMaskMiniaturizable|NSWindowStyleMaskResizable
                                                       backing:NSBackingStoreBuffered defer:NO];
    
    NSViewController *vc = [[NSViewController alloc] init];
    
    id device = uno_application_get_metal_device();
    if (device) {
        UNOMetalFlippedView *v = [[UNOMetalFlippedView alloc] initWithFrame:size device:device];
        v.enableSetNeedsDisplay = YES;
        window.metalViewDelegate = [[UNOMetalViewDelegate alloc] initWithMetalKitView:v];
        v.delegate = window.metalViewDelegate;
        vc.view = v;
    } else {
        UNOSoftView *v = [[UNOSoftView alloc] initWithFrame:size];
        vc.view = v;
    }
    
    window.contentViewController = vc;
    
    // Notifications
    NSNotificationCenter *center = [NSNotificationCenter defaultCenter];

    assert(windowDidChangeScreen);
    [center addObserver:windowDidChangeScreen selector:@selector(windowDidChangeScreenNotification:) name:NSWindowDidChangeScreenNotification object:window];

    [center addObserver:windowDidChangeScreen selector:@selector(applicationDidChangeScreenParametersNotification:) name:NSApplicationDidChangeScreenParametersNotification object:window];
    
    [windows addObject:window];

    // do not show the window until activate has been called
    [window makeKeyWindow];
    [window orderOut:nil];

    return window;
}

void uno_window_activate(UNOWindow *window)
{
#if DEBUG
    NSLog(@"uno_window_activate %@ state %d", window, window.overlappedPresenterState);
#endif
    switch (window.overlappedPresenterState) {
        case OverlappedPresenterStateRestored: {
            // ordering to front can move the window (but not resize it) from what was set before activating it, so we move it back
            CGPoint current = window.frame.origin;
            // don't call `uno_window_restore` since we want the pre-activation state (not the current one) to be used
            [window orderFrontRegardless];
            [window setFrameOrigin:current];
            break;
        }
        case OverlappedPresenterStateMinimized:
            uno_window_minimize(window, false);
            break;
        case OverlappedPresenterStateMaximized:
            uno_window_maximize(window);
            break;
    }
}

void uno_window_notify_screen_change(NSWindow *window)
{
    assert(windowDidChangeScreen);
    NSNotification *nw = [[NSNotification alloc] initWithName:NSWindowDidChangeScreenNotification object:window userInfo:nil];
    [windowDidChangeScreen windowDidChangeScreenNotification:nw];
    [windowDidChangeScreen applicationDidChangeScreenParametersNotification:nw];
}

void uno_window_invalidate(NSWindow *window)
{
#if DEBUG
    NSLog(@"uno_window_invalidate %@ view: %p", window, window.contentViewController.view);
#endif
    window.contentViewController.view.needsDisplay = true;
}

void uno_window_close(NSWindow *window)
{
#if DEBUG
    NSLog(@"uno_window_close %@", window);
#endif
    [window performClose:nil];
}

void uno_window_move(NSWindow *window, double x, double y)
{
#if DEBUG
    NSLog(@"uno_window_move %@ x: %g y: %g", window, x, y);
#endif
    [window setFrameOrigin:NSMakePoint(x, y)];
}

bool uno_window_resize(NSWindow *window, double width, double height)
{
#if DEBUG
    NSLog (@"uno_window_resize %@ %f %f", window, width, height);
#endif
    bool result = false;
    if (window) {
        NSRect frame = window.frame;
        // consider the titlebar height when re-sizing the window
        CGSize content = [window contentRectForFrameRect: frame].size;
        frame.size = CGSizeMake(width, height + (frame.size.height - content.height));
        [window setFrame:frame display:true animate:false];
        result = true;
    }
    return result;
}

void uno_window_set_min_size(NSWindow *window, double width, double height)
{
#if DEBUG
    NSLog (@"uno_window_set_min_size %@ %f %f", window, width, height);
#endif
    window.minSize = CGSizeMake(width, height);
}

void uno_window_set_max_size(NSWindow *window, double width, double height)
{
#if DEBUG
    NSLog (@"uno_window_set_max_size %@ %f %f", window, width, height);
#endif
    window.maxSize = CGSizeMake(width, height);
}

void uno_window_get_position(NSWindow *window, double *x, double *y)
{
    CGPoint origin = window.frame.origin;
#if DEBUG
    NSLog (@"uno_window_get_position %@ %f %f", window, origin.x, origin.y);
#endif
    *x = origin.x;
    *y = origin.y;
}

char* uno_window_get_title(NSWindow *window)
{
    return strdup(window.title.UTF8String);
}

void uno_window_set_title(NSWindow *window, const char* title)
{
    window.title = [NSString stringWithUTF8String:title];
}

bool uno_window_is_full_screen(NSWindow *window)
{
    bool result = window != nil;
    if (result) {
        result = (window.styleMask & NSWindowStyleMaskFullScreen) == NSWindowStyleMaskFullScreen;
    }
#if DEBUG
    NSLog(@"uno_window_is_full_screen %@ %s", window, result ? "true" : "false");
#endif
    return result;
}

bool uno_window_enter_full_screen(NSWindow *window)
{
    bool result = window != nil;
    if (result && (window.styleMask & NSWindowStyleMaskFullScreen) != NSWindowStyleMaskFullScreen) {
        [window toggleFullScreen:nil];
        result = true;
    }
#if DEBUG
    NSLog(@"uno_window_enter_full_screen %@ %s", window, result ? "true" : "false");
#endif
    return result;
}

void uno_window_exit_full_screen(NSWindow *window)
{
    if (window && (window.styleMask & NSWindowStyleMaskFullScreen) == NSWindowStyleMaskFullScreen) {
        [window toggleFullScreen:nil];
    }
#if DEBUG
    NSLog(@"uno_window_exit_full_screen %@", window);
#endif
}

// on macOS double-clicking on the titlebar maximize the window (not the green icon)
void uno_window_maximize(NSWindow *window)
{
#if DEBUG
    NSLog(@"uno_window_maximize %@", window);
#endif
    [window performZoom:nil];
}

void uno_window_minimize(NSWindow *window, bool activateWindow)
{
#if DEBUG
    NSLog(@"uno_window_minimize %@ %s", window, activateWindow ? "true" : "false");
#endif
    [window performMiniaturize:nil];
    if (activateWindow && window.canBecomeMainWindow) {
        [window makeMainWindow];
    }
}

void uno_window_restore(UNOWindow *window, bool activateWindow)
{
#if DEBUG
    NSLog(@"uno_window_restore %@ %s", window, activateWindow ? "true" : "false");
#endif
    switch(uno_window_get_overlapped_presenter_state(window)) {
        case OverlappedPresenterStateMaximized:
            [window zoom:nil];
            break;
        case OverlappedPresenterStateMinimized:
            [window deminiaturize:nil];
            break;
        default:
            break;
    }
    if (activateWindow && window.canBecomeMainWindow) {
        [window makeMainWindow];
    }
}

OverlappedPresenterState uno_window_get_overlapped_presenter_state(UNOWindow *window)
{
#if DEBUG
    NSLog(@"uno_window_get_overlapped_presenter_state %@ state %d", window, window.overlappedPresenterState);
#endif
    return window.overlappedPresenterState;
}

void uno_window_set_always_on_top(NSWindow* window, bool isAlwaysOnTop)
{
    NSWindowLevel level = window.level;
    if (isAlwaysOnTop) {
        level = NSStatusWindowLevel;
    } else {
        level = NSNormalWindowLevel;
    }
#if DEBUG
    NSLog(@"uno_window_set_always_on_top %@ 0x%x %s 0x%x", window, (uint)level, isAlwaysOnTop ? "true" : "false", (uint)level);
#endif
    window.level = level;
}

void uno_window_set_border_and_title_bar(NSWindow *window, bool hasBorder, bool hasTitleBar)
{
    NSWindowStyleMask style = window.styleMask;
    if (!hasBorder)
        style |= NSWindowStyleMaskBorderless;
    else
        style ^= NSWindowStyleMaskBorderless;
    if (hasTitleBar)
        style |= NSWindowStyleMaskTitled;
    else
        style ^= NSWindowStyleMaskTitled;
#if DEBUG
    NSLog(@"uno_window_set_border_and_title_bar %@ 0x%x hasBorder %s hasTitleBar %s 0x%x", window, (uint)window.styleMask,
          hasBorder ? "true" : "false", hasTitleBar ? "true" : "false", (uint)style);
#endif
    window.styleMask = style;
}

void uno_window_set_maximizable(NSWindow* window, bool isMaximizable)
{
#if DEBUG
    NSWindowCollectionBehavior cb = window.collectionBehavior;
#endif
    // unlike Windows on macOS the (green) maximizable button is for full screen, not zoomed
    // `windowShouldZoom:toFrame:` will check the collectionBehavior to [dis]allow zooming
    if (isMaximizable) {
        [window setCollectionBehavior:NSWindowCollectionBehaviorDefault];
        [[window standardWindowButton:NSWindowZoomButton] setEnabled:YES];
    } else {
        [window setCollectionBehavior:NSWindowCollectionBehaviorFullScreenAuxiliary|NSWindowCollectionBehaviorFullScreenNone|NSWindowCollectionBehaviorFullScreenDisallowsTiling];
        [[window standardWindowButton:NSWindowZoomButton] setEnabled:NO];
    }
#if DEBUG
    NSLog(@"uno_window_set_maximizable %@ 0x%x %s 0x%x", window, (uint)cb, isMaximizable ? "true" : "false", (uint)window.collectionBehavior);
#endif
}

void uno_window_set_minimizable(NSWindow* window, bool isMinimizable)
{
    NSWindowStyleMask style = window.styleMask;
    if (isMinimizable)
        style |= NSWindowStyleMaskMiniaturizable;
    else
        style ^= NSWindowStyleMaskMiniaturizable;
#if DEBUG
    NSLog(@"uno_window_set_minimizable %@ 0x%x %s 0x%x", window, (uint)window.styleMask, isMinimizable ? "true" : "false", (uint)style);
#endif
    window.styleMask = style;
}

bool uno_window_set_modal(NSWindow *window, bool isModal)
{
    // this is a read-only property so we simply log if we can't change it to the requested value
    return isModal == window.isModalPanel;
}

void uno_window_set_resizable(NSWindow *window, bool isResizable)
{
    NSWindowStyleMask style = window.styleMask;
    if (isResizable)
        style |= NSWindowStyleMaskResizable;
    else
        style ^= NSWindowStyleMaskResizable;
#if DEBUG
    NSLog(@"uno_window_set_resizable %@ 0x%x %s 0x%x", window, (uint)window.styleMask, isResizable ? "true" : "false", (uint)style);
#endif
    window.styleMask = style;
}

inline window_did_change_screen_fn_ptr uno_get_window_did_change_screen_callback(void)
{
    return window_did_change_screen;
}

inline window_did_change_screen_parameters_fn_ptr uno_get_window_did_change_screen_parameters_callback(void)
{
    return window_did_change_screen_parameters;
}

void uno_set_window_screen_change_callbacks(window_did_change_screen_fn_ptr screen, window_did_change_screen_parameters_fn_ptr parameters)
{
    windowDidChangeScreen = [windowDidChangeScreenNoteClass init];
    window_did_change_screen = screen;
    window_did_change_screen_parameters = parameters;
}

static window_key_callback_fn_ptr window_key_down;
static window_key_callback_fn_ptr window_key_up;

inline static window_key_callback_fn_ptr uno_get_window_key_down_callback(void)
{
    return window_key_down;
}

inline static window_key_callback_fn_ptr uno_get_window_key_up_callback(void)
{
    return window_key_up;
}

VirtualKey get_virtual_key(unsigned short keyCode)
{
    switch(keyCode) {
        case 29: return VirtualKeyNumber0;
        case 18: return VirtualKeyNumber1;
        case 19: return VirtualKeyNumber2;
        case 20: return VirtualKeyNumber3;
        case 21: return VirtualKeyNumber4;
        case 23: return VirtualKeyNumber5;
        case 22: return VirtualKeyNumber6;
        case 26: return VirtualKeyNumber7;
        case 28: return VirtualKeyNumber8;
        case 25: return VirtualKeyNumber9;

        case 0: return VirtualKeyA;
        case 11: return VirtualKeyB;
        case 8: return VirtualKeyC;
        case 2: return VirtualKeyD;
        case 14: return VirtualKeyE;
        case 3: return VirtualKeyF;
        case 5: return VirtualKeyG;
        case 4: return VirtualKeyH;
        case 34: return VirtualKeyI;
        case 38: return VirtualKeyJ;
        case 40: return VirtualKeyK;
        case 37: return VirtualKeyL;
        case 46: return VirtualKeyM;
        case 45: return VirtualKeyN;
        case 31: return VirtualKeyO;
        case 35: return VirtualKeyP;
        case 12: return VirtualKeyQ;
        case 15: return VirtualKeyR;
        case 1: return VirtualKeyS;
        case 17: return VirtualKeyT;
        case 32: return VirtualKeyU;
        case 9: return VirtualKeyV;
        case 13: return VirtualKeyW;
        case 7: return VirtualKeyX;
        case 16: return VirtualKeyY;
        case 6: return VirtualKeyZ;

        // Those keys are not mapped in the VirtualKey enum by windows, however the event is still raised
        // WARNING: Those keys are only "LOCATION on keyboard" codes.
        //            This means that for instance the 187 is a '=' on a querty keyboard, while it's a '+' on an azerty.
        case 10: return (VirtualKey)191; // § (Value observed on UWP 19041, fr-FR AZERTY US-int keyboard)
        case 50: return (VirtualKey)192; // ` (Value observed on UWP 19041, fr-FR qwerty US-int keyboard)
        case 27: return (VirtualKey)189; // - (Value observed on UWP 19041, fr-FR qwerty US-int keyboard)
        case 24: return (VirtualKey)187; // = (Value observed on UWP 19041, fr-FR qwerty US-int keyboard)
        case 33: return (VirtualKey)219; // [ (Value observed on UWP 19041, fr-FR qwerty US-int keyboard)
        case 30: return (VirtualKey)221; // ] (Value observed on UWP 19041, fr-FR qwerty US-int keyboard)
        case 41: return (VirtualKey)186; // ; (Value observed on UWP 19041, fr-FR qwerty US-int keyboard)
        case 39: return (VirtualKey)222; // ' (Value observed on UWP 19041, fr-FR qwerty US-int keyboard)
        case 43: return (VirtualKey)188; // , (Value observed on UWP 19041, fr-FR qwerty US-int keyboard)
        case 47: return (VirtualKey)190; // . (Value observed on UWP 19041, fr-FR qwerty US-int keyboard)
        case 44: return (VirtualKey)191; // / (Value observed on UWP 19041, fr-FR qwerty US-int keyboard)
        case 42: return (VirtualKey)220; // \ (Value observed on UWP 19041, fr-FR qwerty US-int keyboard)

        // [Key|Number] Pad
        case 82: return VirtualKeyNumberPad0;
        case 83: return VirtualKeyNumberPad1;
        case 84: return VirtualKeyNumberPad2;
        case 85: return VirtualKeyNumberPad3;
        case 86: return VirtualKeyNumberPad4;
        case 87: return VirtualKeyNumberPad5;
        case 88: return VirtualKeyNumberPad6;
        case 89: return VirtualKeyNumberPad7;
        case 91: return VirtualKeyNumberPad8;
        case 92: return VirtualKeyNumberPad9;
        case 65: return VirtualKeyDecimal;
        case 67: return VirtualKeyMultiply;
        case 69: return VirtualKeyAdd;
        case 75: return VirtualKeyDivide;
        case 78: return VirtualKeySubtract;
        case 81: return (VirtualKey)187;
        case 71: return VirtualKeyClear;
        case 76: return VirtualKeyEnter;

        case 49: return VirtualKeySpace;
        case 36: return VirtualKeyEnter;
        case 48: return VirtualKeyTab;
        case 51: return VirtualKeyBack;
        case 117: return VirtualKeyDelete;
        // 52   : return VirtualKey.␊

        case 53: return VirtualKeyEscape;
        case 55: return VirtualKeyLeftWindows;
        case 56: return VirtualKeyShift;
        case 57: return VirtualKeyCapitalLock;
        case 58: return VirtualKeyMenu;
        case 59: return VirtualKeyControl;
        case 60: return VirtualKeyRightShift;
        case 61: return VirtualKeyRightMenu;
        case 62: return VirtualKeyRightControl;

        // Functions
        // 63   : return VirtualKey.fn
        
        case 122: return VirtualKeyF1;
        case 120: return VirtualKeyF2;
        case 99: return VirtualKeyF3;
        case 118: return VirtualKeyF4;
        case 96: return VirtualKeyF5;
        case 97: return VirtualKeyF6;
        case 98: return VirtualKeyF7;
        case 100: return VirtualKeyF8;
        case 101: return VirtualKeyF9;
        case 109: return VirtualKeyF10;
        case 103: return VirtualKeyF11;
        case 111: return VirtualKeyF12;
        case 105: return VirtualKeyF13;
        case 107: return VirtualKeyF14;
        case 113: return VirtualKeyF15;
        case 106: return VirtualKeyF16;
        case 64: return VirtualKeyF17;
        case 79: return VirtualKeyF18;
        case 80: return VirtualKeyF19;
        case 90: return VirtualKeyF20;

        // Volume (Those keys does not fire any event on UWP)
        // 72   : return VirtualKey. // Volume down
        // 73   : return VirtualKey. // Volume up
        // 74   : return VirtualKey. // Mute

        // Navigation
        case 114: return VirtualKeyInsert;
        case 115: return VirtualKeyHome;
        case 119: return VirtualKeyEnd;
        case 116: return VirtualKeyPageUp;
        case 121: return VirtualKeyPageDown;
        case 123: return VirtualKeyLeft;
        case 124: return VirtualKeyRight;
        case 125: return VirtualKeyDown;
        case 126: return VirtualKeyUp;

        default: return VirtualKeyNone;
    }
}

// FIXME: based on uno/src/Uno.UWP/System/VirtualKeyHelper.macOS.cs where only Shift and Control are considered ?!?
// https://learn.microsoft.com/en-us/uwp/api/windows.system.virtualkeymodifiers?view=winrt-22621
VirtualKeyModifiers get_modifiers(NSEventModifierFlags mods)
{
    VirtualKeyModifiers vkm = VirtualKeyModifiersNone;
    if (mods & NSEventModifierFlagControl) {
        vkm |= VirtualKeyModifiersControl;
    }
    if (mods & NSEventModifierFlagShift) {
        vkm |= VirtualKeyModifiersShift;
    }
    if (mods & NSEventModifierFlagOption) {
        vkm |= VirtualKeyModifiersMenu; // documented as Alt
    }
    if (mods & NSEventModifierFlagCommand) {
        vkm |= VirtualKeyModifiersWindows;
    }
    return vkm;
}

UniChar get_unicode(NSEvent *event)
{
    UniCharCount count = 1;
    UniChar unicode[count];
    CGEventKeyboardGetUnicodeString(event.CGEvent, count, &count, unicode);
#if DEBUG
    if (count > 1) {
        NSLog(@"get_unicode - more than one unicode character returned");
    }
#endif
    return unicode[0];
}

static window_mouse_callback_fn_ptr window_mouse_event;

inline static window_mouse_callback_fn_ptr uno_get_window_mouse_event_callback(void)
{
    return window_mouse_event;
}

static window_move_or_resize_fn_ptr window_move_event;

inline static window_move_or_resize_fn_ptr uno_get_window_move_event_callback(void)
{
    return window_move_event;
}

static window_move_or_resize_fn_ptr window_resize_event;

inline static window_move_or_resize_fn_ptr uno_get_window_resize_event_callback(void)
{
    return window_resize_event;
}

void uno_set_window_events_callbacks(window_key_callback_fn_ptr keyDown,
    window_key_callback_fn_ptr keyUp,
    window_mouse_callback_fn_ptr pointer,
    window_move_or_resize_fn_ptr move,
    window_move_or_resize_fn_ptr resize)
{
    window_key_down = keyDown;
    window_key_up = keyUp;
    window_mouse_event = pointer;
    window_move_event = move;
    window_resize_event = resize;
}

static window_should_close_fn_ptr window_should_close;

inline window_should_close_fn_ptr uno_get_window_should_close_callback(void)
{
    return window_should_close;
}

static window_close_fn_ptr window_close;

inline window_close_fn_ptr uno_get_window_close_callback(void)
{
    return window_close;
}

void uno_set_window_close_callbacks(window_should_close_fn_ptr shouldClose, window_close_fn_ptr close)
{
    window_should_close = shouldClose;
    window_close = close;
}

void uno_window_get_metal_handles(UNOWindow* window, void** device, void** queue)
{
    *device = (__bridge void *)(uno_application_get_metal_device());
    *queue = (__bridge void *)(window.metalViewDelegate.queue);
#if DEBUG
    NSLog(@"uno_window_get_metal device %p queue %p", device, queue);
#endif
}

CGFloat readNextCoord(const char *svg, int *position, long length)
{
    CGFloat result = NAN;
    if (*position >= length) {
#if DEBUG
        NSLog(@"uno_window_clip_svg readNextCoord position:%d >= length:%ld", *position, length);
#endif
        return result;
    }
    const char* start = svg + *position;
    char* end;
    result = strtod(start, &end);
    *position += (int)(end - start);
    return result;
}

bool uno_window_clip_svg(UNOWindow* window, const char* svg)
{
    if (svg) {
        CGFloat scale = window.screen.backingScaleFactor;
#if DEBUG
        NSLog(@"uno_window_clip_svg %@ %@ %s scale: %g", window, window.contentView.layer.description, svg, scale);
#endif
        NSArray<__kindof NSView *> *subviews = window.contentViewController.view.subviews;
        for (int i = 0; i < subviews.count; i++) {
            NSView* view = subviews[i];
            NSRect frame = view.frame;
            // if called too early the element might not have been arranged and it's values cannot be used yet
            if (NSIsEmptyRect(frame))
                return false;

            CGFloat vx = frame.origin.x;
            CGFloat vy = frame.origin.y;
#if DEBUG
            NSLog(@"uno_window_clip_svg subview %d %@ layer %@ mask %@", i, view, view.layer, view.layer.mask);
#endif
            CGMutablePathRef path = CGPathCreateMutable();
            // small subset of an SVG path parser handling trusted input of integer-based points
            long length = strlen(svg);
            for (int i=0; i < length;) {
                CGFloat x, y, x2, y2;
                char op = svg[i];
                switch (op) {
                    case 'M':
                        i++; // skip M
                        x = readNextCoord(svg, &i, length);
                        i++; // skip separator
                        y = readNextCoord(svg, &i, length);
                        // there might not be a separator (not required before the next op)
#if DEBUG_PARSER
                        NSLog(@"uno_window_clip_svg parsing CGPathMoveToPoint %g %g - position %d", x, y, i);
#endif
                        x = (x / scale - vx);
                        y = (y / scale - vy);
                        CGPathMoveToPoint(path, nil, x, y);
                        break;
                    case 'L':
                        i++; // skip L
                        x = readNextCoord(svg, &i, length);
                        i++; // skip separator
                        y = readNextCoord(svg, &i, length);
                        // there might not be a separator (not required before the next op)
#if DEBUG_PARSER
                        NSLog(@"uno_window_clip_svg parsing CGPathAddLineToPoint %g %g - position %d", x, y, i);
#endif
                        x = (x / scale - vx);
                        y = (y / scale - vy);
                        CGPathAddLineToPoint(path, nil, x, y);
                        break;
                    case 'Q':
                        i++; // skip Z
                        x = readNextCoord(svg, &i, length);
                        i++; // skip separator
                        y = readNextCoord(svg, &i, length);
                        i++; // skip separator
                        x2 = readNextCoord(svg, &i, length);
                        i++; // skip separator
                        y2 = readNextCoord(svg, &i, length);
                        // there might not be a separator (not required before the next op)
#if DEBUG_PARSER
                        NSLog(@"uno_window_clip_svg parsing CGPathAddQuadCurveToPoint %g %g %g %g - position %d", x, y, x2, y2, i);
#endif
                        x = (x / scale - vx);
                        y = (y / scale - vy);
                        x2 = (x2 / scale - vx);
                        y2 = (y2 / scale - vy);
                        CGPathAddQuadCurveToPoint(path, nil, x, y, x2, y2);
                        break;
                    case 'Z':
                        i++; // skip Z
#if DEBUG_PARSER
                        NSLog(@"uno_window_clip_svg parsing CGPathCloseSubpath - position %d", i);
#endif
                        CGPathCloseSubpath(path);
                        break;
#if DEBUG
                    default:
                        if (op != ' ') {
                            NSLog(@"uno_window_clip_svg parsing unknown op %c at position %d", op, i);
                        }
                        i++; // skip unknown op
                        break;
#endif
                }
            }
            CAShapeLayer* mask = view.layer.mask;
            if (mask == nil) {
                view.layer.mask = mask = [[CAShapeLayer alloc] init];
            }
            mask.fillColor = NSColor.blueColor.CGColor; // anything but clearColor
            mask.fillRule = kCAFillRuleEvenOdd;
            mask.path = path;
        }
    } else {
#if DEBUG
        NSLog(@"uno_window_clip_svg %@ %@ reset", window, window.contentView.layer.description);
#endif
        NSArray<__kindof NSView *> *subviews = window.contentViewController.view.subviews;
        for (int i = 0; i < subviews.count; i++) {
            NSView* view = subviews[i];
#if DEBUG
            NSLog(@"uno_window_clip_svg reset subview %d %@ layer %@ mask %@", i, view, view.layer, view.layer.mask);
#endif
            CAShapeLayer* mask = view.layer.mask;
            if (mask != nil) {
                mask.fillColor = NSColor.clearColor.CGColor;
                mask.fillRule = kCAFillRuleEvenOdd;
                mask.path = nil;
            }
        }
    }
    return true;
}

@implementation UNOWindow : NSWindow

NSEventModifierFlags _previousFlags;
NSOperatingSystemVersion _osVersion;

+ (void)initialize {
    windows = [[NSMutableSet alloc] initWithCapacity:10];
    _osVersion = [[NSProcessInfo processInfo] operatingSystemVersion];
}

- (instancetype)initWithContentRect:(NSRect)contentRect styleMask:(NSWindowStyleMask)style backing:(NSBackingStoreType)backingStoreType defer:(BOOL)flag {
    self = [super initWithContentRect:contentRect styleMask:style backing:backingStoreType defer:flag];
    if (self) {
        self.delegate = self;
        self.overlappedPresenterState = OverlappedPresenterStateRestored;
    }
    return self;
}

- (void)dealloc {
    [windows removeObject:self];
}

@synthesize overlappedPresenterState;

- (BOOL) getPositionFrom:(NSEvent*)event x:(CGFloat*)px y:(CGFloat *)py
{
    *py = self.contentView.frame.size.height - event.locationInWindow.y;
    // if we are in the titlebar, let send the event to `super` ... so close button will continue to work
    if (*py < 0) {
        return NO;
    }
    *px = event.locationInWindow.x;
    return YES;
}

- (void)sendEvent:(NSEvent *)event {
    if (![[NSApplication sharedApplication] isActive]) {
        [super sendEvent:event];
        return;
    }

    bool handled = false;
    MouseEvents mouse = MouseEventsNone;
    PointerDeviceType pdt = PointerDeviceTypeMouse;
    bool inContact = NO;

    switch ([event type]) {
        case NSEventTypeLeftMouseDown:
        case NSEventTypeOtherMouseDown:
        case NSEventTypeRightMouseDown: {
            mouse = MouseEventsDown;
            inContact = YES;
            break;
        }
        case NSEventTypeLeftMouseUp:
        case NSEventTypeOtherMouseUp:
        case NSEventTypeRightMouseUp: {
            mouse = MouseEventsUp;
            break;
        }
        case NSEventTypeLeftMouseDragged:
        case NSEventTypeRightMouseDragged:
        case NSEventTypeOtherMouseDragged: /* usually middle mouse dragged */
            inContact = YES;
        case NSEventTypeMouseMoved: {
            mouse = MouseEventsMoved;
            break;
        }
        case NSEventTypeTabletPoint: 
        case NSEventTypeTabletProximity: {
            mouse = MouseEventsMoved;
            pdt = PointerDeviceTypePen;
            break;
        }
        case NSEventTypeDirectTouch: {
            mouse = MouseEventsMoved;
            pdt = PointerDeviceTypeTouch;
            break;
        }
        case NSEventTypeScrollWheel: {
            mouse = MouseEventsScrollWheel;
            break;
        }
        case NSEventTypeMouseEntered: {
            mouse = MouseEventsEntered;
            break;
        }
        case NSEventTypeMouseExited: {
            mouse = MouseEventsExited;
            break;
        }
        case NSEventTypeKeyDown: {
            unsigned short scanCode = event.keyCode;
            UniChar unicode = get_unicode(event);
            handled = uno_get_window_key_down_callback()(self, get_virtual_key(scanCode), get_modifiers(event.modifierFlags), scanCode, unicode);
#if DEBUG
            NSLog(@"NSEventTypeKeyDown: %@ window %p unicode %d handled? %s", event, self, unicode, handled ? "true" : "false");
#endif
            break;
        }
        case NSEventTypeKeyUp: {
            unsigned short scanCode = event.keyCode;
            UniChar unicode = get_unicode(event);
            handled = uno_get_window_key_up_callback()(self, get_virtual_key(scanCode), get_modifiers(event.modifierFlags), scanCode, unicode);
#if DEBUG
            NSLog(@"NSEventTypeKeyUp: %@ window %p unicode %d handled? %s", event, self, unicode, handled ? "true" : "false");
#endif
            break;
        }
        case NSEventTypeFlagsChanged: {
            // raise separate events for each modifiers and for key up and down
            handled |= [self processModifiers:NSEventModifierFlagControl event:event];
            handled |= [self processModifiers:NSEventModifierFlagOption event:event];
            handled |= [self processModifiers:NSEventModifierFlagShift event:event];
            handled |= [self processModifiers:NSEventModifierFlagCommand event:event];
            _previousFlags = event.modifierFlags;
#if DEBUG
            NSLog(@"NSEventTypeFlagsChanged: %@ handled? %s", event, handled ? "true" : "false");
#endif
            break;
        }
#if DEBUG
        default: {
            NSLog(@"Unhandled Event: %@", event);
            break;
        }
#endif
    }

    if (_osVersion.majorVersion >= 15) {
        [MouseButtons track:event];
    }
    
    if (mouse != MouseEventsNone) {
        struct MouseEventData data;
        memset(&data, 0, sizeof(struct MouseEventData));
        data.eventType = mouse;
        data.inContact = inContact;
        data.mods = get_modifiers(event.modifierFlags);
        if ([self getPositionFrom:event x:&data.x y:&data.y]) {
#if false
            // check subtype for most mouse events
            // FIXME: does not work, the mouse also issue the NSEventSubtypeTabletPoint subevent and this cause  assertions
            // *** Assertion failure in -[NSEvent pointingDeviceID], NSEvent.m:4683
            if ((pdt == PointerDeviceTypeMouse) && (mouse != MouseEventsEntered) && (mouse != MouseEventsExited)) {
                if ((event.subtype == NSEventSubtypeTabletPoint) || (event.subtype == NSEventSubtypeTabletProximity)) {
                    NSLog(@"PointerDeviceTypePen detected %d", event.subtype);
                    pdt = PointerDeviceTypePen;
                }
            }
#endif
            data.pointerDeviceType = pdt;
            
            // mouse
            // FIXME: NSEvent.pressedMouseButtons is returning a wrong value in Sequoia when using an extenal trackpad
            if (_osVersion.majorVersion >= 15) {
                data.mouseButtons = (uint32)[MouseButtons mask];
            } else {
                data.mouseButtons = (uint32)NSEvent.pressedMouseButtons;
            }

            // Pen
            if (pdt == PointerDeviceTypePen) {
                // do not call if event is not from a pen -> *** Assertion failure in -[NSEvent tilt], NSEvent.m:4625
                NSPoint tilt = event.tilt;
                data.tiltX = (float)tilt.x;
                data.tiltY = (float)tilt.y;
                data.pressure = event.pressure;
                data.pid = (uint32)event.pointingDeviceID;
            } else {
                data.pid = 1;
            }

            // scrollwheel
            if (mouse == MouseEventsScrollWheel) {
                // do not call if not in the scrollwheel event -> *** Assertion failure in -[NSEvent scrollingDeltaX], NSEvent.m:2202

                // trackpad / magic mouse sends about 10x more events than a _normal_ (PC) mouse
                // this is often refered as a line scroll versus a pixel scroll
                double factor = event.hasPreciseScrollingDeltas ? 1.0 : 10.0;
                data.scrollingDeltaX = (int32_t)(event.scrollingDeltaX * factor);
                data.scrollingDeltaY = (int32_t)(event.scrollingDeltaY * factor);
#if DEBUG_MOUSE // very noisy
                NSLog(@"NSEventTypeMouse*: %@ %g %g delta %g %g %s scrollingDelta %d %d", event, data.x, data.y, event.deltaX, event.deltaY, event.hasPreciseScrollingDeltas ? "precise" : "non-precise", data.scrollingDeltaX, data.scrollingDeltaY);
#endif
            }

            // other
            NSTimeInterval ts = event.timestamp;
            
            data.frameId = (uint)(ts * 10.0);
            data.timestamp = (uint64)(ts * 1000000.0);

            handled = uno_get_window_mouse_event_callback()(self, &data);
#if DEBUG_MOUSE // very noisy
            NSLog(@"NSEventTypeMouse*: %@ %g %g handled? %s", event, data.x, data.y, handled ? "true" : "false");
#endif
        }
    }

    if (!handled) {
        [super sendEvent:event];
    }
}

- (BOOL)processModifiers:(NSEventModifierFlags)candidate event:(NSEvent*)event {
    NSEventModifierFlags flags = event.modifierFlags;
    bool down = (_previousFlags & candidate) == 0 && (flags & candidate) != 0;
    bool up = (_previousFlags & candidate) != 0 && (flags & candidate) == 0;

    VirtualKey key = VirtualKeyNone;
    VirtualKeyModifiers mod = VirtualKeyModifiersNone;
    unsigned short scanCode = 0;
    UniChar unicode = 0;
    if (down || up) {
        scanCode = event.keyCode;
        key = get_virtual_key(scanCode);
        mod = get_modifiers(candidate);
        unicode = get_unicode(event);
    }

    bool handled = false;
    if (down) {
        handled |= uno_get_window_key_down_callback()(self, key, mod, scanCode, unicode);
    }
    if (up) {
        handled |= uno_get_window_key_up_callback()(self, key, mod, scanCode, unicode);
    }
#if DEBUG
    NSLog(@"NSEventTypeFlagsChanged: down: %s up: %s", down ? "TRUE" : "false", up ? "TRUE" : "false");
#endif
    return handled;
}

- (void) windowWillStartLiveResize:(NSNotification *) notification {
    NSScreen *screen = ((NSWindow*) notification.object).screen;
    NSView* contentView = self.contentView;
#if DEBUG
    NSLog(@"UNOWindow %p windowWillStartLiveResize scaling from %g to %g", self, contentView.layer.contentsScale, screen.backingScaleFactor);
#endif
    // This does not flow automatically when moving the window from a retina screen to a normal screen
    contentView.layer.contentsScale = screen.backingScaleFactor;
    // prevent content stretching during window resize by pinning to top-left. cf. https://github.com/unoplatform/uno/issues/22159
    contentView.layerContentsPlacement = NSViewLayerContentsPlacementTopLeft;
}

- (void) windowDidEndLiveResize:(NSNotification *) notification {
    NSScreen *screen = ((NSWindow*) notification.object).screen;
    NSView* contentView = self.contentView;
#if DEBUG
    NSLog(@"UNOWindow %p windowDidEndLiveResize scaling from %g to %g", self, contentView.layer.contentsScale, screen.backingScaleFactor);
#endif
    // Ensure the scale stays in sync up to the end
    contentView.layer.contentsScale = screen.backingScaleFactor;
    // Reset the layerContentsPlacement property to its default value
    contentView.layerContentsPlacement = NSViewLayerContentsPlacementScaleAxesIndependently;
}

- (void) performMiniaturize:(id) sender {
    self.overlappedPresenterState = OverlappedPresenterStateMinimized;
    [super performMiniaturize:sender];
}

- (void)windowWillMiniaturize:(NSNotification *)notification {
#if DEBUG
    NSLog(@"UNOWindow %p windowWillMiniaturize %@", self, notification);
#endif
    self.overlappedPresenterState = OverlappedPresenterStateMinimized;
}

- (void)windowDidMiniaturize:(NSNotification *)notification {
#if DEBUG
    NSLog(@"UNOWindow %p windowDidMiniaturize %@", self, notification);
#endif
    self.overlappedPresenterState = OverlappedPresenterStateMinimized;
}

- (void)windowDidDeminiaturize:(NSNotification *)notification {
#if DEBUG
    NSLog(@"UNOWindow %p windowDidDeminiaturize %@", self, notification);
#endif
    self.overlappedPresenterState = OverlappedPresenterStateRestored;
}

- (BOOL)windowShouldZoom:(NSWindow *)window toFrame:(NSRect)newFrame {
    // if we disable the (green) maximize button then we don't allow zooming
    return window.collectionBehavior != (NSWindowCollectionBehaviorFullScreenAuxiliary|NSWindowCollectionBehaviorFullScreenNone|NSWindowCollectionBehaviorFullScreenDisallowsTiling);
}

- (void) performZoom:(id) sender {
    [super performZoom:sender];
    self.overlappedPresenterState = OverlappedPresenterStateMaximized;
}

- (void) zoom:(id) sender {
    // this call is a toggle
    self.overlappedPresenterState = self.isZoomed ? OverlappedPresenterStateRestored : OverlappedPresenterStateMaximized;
    [super zoom:sender];
}

- (BOOL)performKeyEquivalent:(NSEvent *)event {
    // Handle Command+W (close window) and Command+Q (quit app)
    if ([event type] == NSEventTypeKeyDown && ([event modifierFlags] & NSEventModifierFlagCommand)) {
        NSString *characters = [event charactersIgnoringModifiers];
        
        // Command+W - Close window
        if ([characters isEqualToString:@"w"]) {
#if DEBUG
            NSLog(@"UNOWindow %p performKeyEquivalent Command+W", self);
#endif
            [self performClose:self];
            return YES;
        }
        
        // Command+Q - Quit application
        if ([characters isEqualToString:@"q"]) {
#if DEBUG
            NSLog(@"UNOWindow %p performKeyEquivalent Command+Q", self);
#endif
            [[NSApplication sharedApplication] terminate:self];
            return YES;
        }
    }
    
    return [super performKeyEquivalent:event];
}

- (void)windowDidMove:(NSNotification *)notification {
    CGPoint position = self.frame.origin;
#if DEBUG
    NSLog(@"UNOWindow %p windowDidMove %@ x: %g y: %g", self, notification, position.x, position.y);
#endif
    uno_get_window_move_event_callback()(self, position.x, position.y);
}

- (void)windowDidResize:(NSNotification *)notification {
    // the UNOMetalViewDelegate has its own resize callback but we need something for the software fallback
    if (self.metalViewDelegate == nil) {
        // consider the title bar height
        CGSize size = [self contentRectForFrameRect: self.frame].size;
#if DEBUG
        NSLog(@"UNOWindow %p windowDidResize %@ w: %g h: %g", self, notification, size.width, size.height);
#endif
        uno_get_window_resize_event_callback()(self, size.width, size.height);
    }
}

- (bool)windowShouldClose:(NSWindow *)sender
{
    // see `AppWindow.Closing`
    bool result = uno_get_window_should_close_callback()(self) ? YES : NO;
#if DEBUG
    NSLog(@"UNOWindow %p windowShouldClose %@ -> %s", self, sender, result ? "true" : "false");
#endif
    return result;
}

- (void)windowWillClose:(NSNotification *)notification
{
#if DEBUG
    NSLog(@"UNOWindow %p windowWillClose %@", self, notification);
#endif
    NSNotificationCenter *center = [NSNotificationCenter defaultCenter];
    [center removeObserver:windowDidChangeScreen name:NSWindowDidChangeScreenNotification object:self];
    [center removeObserver:windowDidChangeScreen name:NSApplicationDidChangeScreenParametersNotification object:self];

    uno_get_window_close_callback()(self);
}

@end
