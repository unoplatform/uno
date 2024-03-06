//
//  UNOWindowDelegate.m
//

#import "UNOWindow.h"
#import "UNOApplication.h"
#import "UNOSoftView.h"

static NSWindow *main_window;
static NSMutableSet<NSWindow*> *windows;
static id windowDidChangeScreen;

static window_did_change_screen_fn_ptr window_did_change_screen;
static window_did_change_screen_parameters_fn_ptr window_did_change_screen_parameters;

// libSkiaSharp
extern void* gr_direct_context_make_metal(id device, id queue);

static resize_fn_ptr window_resize;
static metal_draw_fn_ptr metal_draw;
static soft_draw_fn_ptr soft_draw;

inline resize_fn_ptr uno_get_resize_callback(void)
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

void uno_set_drawing_callbacks(metal_draw_fn_ptr metal, soft_draw_fn_ptr soft, resize_fn_ptr resize)
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

NSWindow* uno_window_create(double width, double height)
{
    CGRect size = NSMakeRect(0, 0, width, height);
    UNOWindow *window = [[UNOWindow alloc] initWithContentRect:size
                                                     styleMask:NSWindowStyleMaskTitled|NSWindowStyleMaskClosable|NSWindowStyleMaskMiniaturizable|NSWindowStyleMaskResizable
                                                       backing:NSBackingStoreBuffered defer:NO];
    
    NSViewController *vc = [[NSViewController alloc] init];
    
    id device = uno_application_get_metal_device();
    if (device) {
        MTKView *v = [[MTKView alloc] initWithFrame:size device:device];
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

    [window makeKeyWindow];
    [window orderFrontRegardless];

    return window;
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

bool uno_window_resize(NSWindow *window, double width, double height)
{
#if DEBUG
    NSLog (@"uno_window_resize %@ %f %f", window, width, height);
#endif
    bool result = false;
    if (window) {
        NSRect frame = window.frame;
        frame.size = CGSizeMake(width, height);
        [window setFrame:frame display:true animate:true];
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

void uno_window_set_title(NSWindow *window, const char* title)
{
    window.title = [NSString stringWithUTF8String:title];
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

static window_mouse_callback_fn_ptr window_mouse_event;

inline static window_mouse_callback_fn_ptr uno_get_window_mouse_event_callback(void)
{
    return window_mouse_event;
}

void uno_set_window_events_callbacks(window_key_callback_fn_ptr keyDown, window_key_callback_fn_ptr keyUp, window_mouse_callback_fn_ptr pointer)
{
    window_key_down = keyDown;
    window_key_up = keyUp;
    window_mouse_event = pointer;
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

void* uno_window_get_metal_context(UNOWindow* window)
{
    id device = uno_application_get_metal_device();
    id queue = window.metalViewDelegate.queue;
#if DEBUG
    NSLog(@"uno_window_get_metal device %p queue %p", device, queue);
#endif
    return gr_direct_context_make_metal(device, queue);
}

@implementation UNOWindow : NSWindow

+ (void)initialize {
    windows = [[NSMutableSet alloc] initWithCapacity:10];
}

- (instancetype)initWithContentRect:(NSRect)contentRect styleMask:(NSWindowStyleMask)style backing:(NSBackingStoreType)backingStoreType defer:(BOOL)flag {
    self = [super initWithContentRect:contentRect styleMask:style backing:backingStoreType defer:flag];
    if (self) {
        self.delegate = self;
    }
    return self;
}

- (void)dealloc {
    [windows removeObject:self];
}

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
            handled = uno_get_window_key_down_callback()(self, get_virtual_key(scanCode), get_modifiers(event.modifierFlags), scanCode);
#if DEBUG
            NSLog(@"NSEventTypeKeyDown: %@ window %p handled? %s", event, self, handled ? "true" : "false");
#endif
            break;
        }
        case NSEventTypeKeyUp: {
            unsigned short scanCode = event.keyCode;
            handled = uno_get_window_key_up_callback()(self, get_virtual_key(scanCode), get_modifiers(event.modifierFlags), scanCode);
#if DEBUG
            NSLog(@"NSEventTypeKeyUp: %@ window %p handled? %s", event, self, handled ? "true" : "false");
#endif
            break;
        }
#if DEBUG
        case NSEventTypeFlagsChanged: {
            NSLog(@"NSEventTypeFlagsChanged: %@", event); // FIXME: needed ?
            break;
        }
        default: {
            NSLog(@"Unhandled Event: %@", event);
            break;
        }
#endif
    }
    
    if (mouse != MouseEventsNone) {
        struct MouseEventData data;
        memset(&data, 0, sizeof(struct MouseEventData));
        data.eventType = mouse;
        data.inContact = inContact;
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
            // mouse
            data.mouseButtons = (uint32)NSEvent.pressedMouseButtons;

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
                data.scrollingDeltaX = (int32_t)event.scrollingDeltaX;
                data.scrollingDeltaY = (int32_t)event.scrollingDeltaY;
            }

            // other
            NSTimeInterval ts = event.timestamp;
            
            // The precision of the frameId is 10 frame per ms ... which should be enough
            data.frameId = (uint)(ts * 1000.0 * 10.0);

            NSDate *now = [[NSDate alloc] init];
            NSDate *boot = [[NSDate alloc] initWithTimeInterval:uno_get_system_uptime() sinceDate:now];
            data.timestamp = (uint64)(boot.timeIntervalSinceNow * 1000000);

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

- (bool)windowShouldClose:(NSWindow *)sender
{
    // see `ISystemNavigationManagerPreviewExtension`
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
