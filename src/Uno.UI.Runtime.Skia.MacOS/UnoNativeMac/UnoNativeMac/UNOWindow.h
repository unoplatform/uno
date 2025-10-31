//
//  UNOWindowDelegate.h
//

#pragma once

#import "UnoNativeMac.h"
#import "UNOMetalViewDelegate.h"

NS_ASSUME_NONNULL_BEGIN

typedef void (*uno_drawable_resize_fn_ptr)(void* /* window */, double /* width */, double /* height */);
uno_drawable_resize_fn_ptr uno_get_resize_callback(void);
void uno_set_resize_callback(uno_drawable_resize_fn_ptr p);

typedef void (*window_move_or_resize_fn_ptr)(NSWindow* /* window */, double /* x or width */, double /* y or height */);
window_move_or_resize_fn_ptr uno_get_window_move_callback(void);
window_move_or_resize_fn_ptr uno_get_window_resize_callback(void);

@interface windowDidChangeScreenNoteClass : NSObject

+ (windowDidChangeScreenNoteClass*) init;

- (void) windowDidChangeScreenNotification:(NSNotification*) note;
- (void) applicationDidChangeScreenParametersNotification:(NSNotification*) note;
@end

@interface UNOWindow : NSWindow <NSWindowDelegate>

+ (void)initialize;
- (instancetype)initWithContentRect:(NSRect)contentRect styleMask:(NSWindowStyleMask)style backing:(NSBackingStoreType)backingStoreType defer:(BOOL)flag;
- (void)dealloc;

@property (strong) UNOMetalViewDelegate* metalViewDelegate;

- (void)sendEvent:(NSEvent *)event;

- (BOOL)windowShouldZoom:(NSWindow *)window toFrame:(NSRect)newFrame;
- (void)windowDidMove:(NSNotification *)notification;
- (bool)windowShouldClose:(NSWindow *)sender;
- (void)windowWillClose:(NSNotification *)notification;

@end

NSWindow* uno_app_get_main_window(void);

NSWindow* uno_window_create(double width, double height);
void uno_window_activate(NSWindow *window);
void uno_window_invalidate(NSWindow *window);
void uno_window_close(NSWindow *window);
void uno_window_move(NSWindow *window, double x, double y);
bool uno_window_resize(NSWindow *window, double width, double height);

void uno_window_get_position(NSWindow *window, double *x, double *y);
char* uno_window_get_title(NSWindow *window);
void uno_window_set_title(NSWindow *window, const char* title);

bool uno_window_is_full_screen(NSWindow *window);
bool uno_window_enter_full_screen(NSWindow *window);
void uno_window_exit_full_screen(NSWindow *window);

void uno_window_minimize(NSWindow *window, bool activateWindow);
void uno_window_restore(NSWindow *window, bool activateWindow);

bool uno_window_clip_svg(UNOWindow* window, const char* svg);

typedef NS_ENUM(sint32, OverlappedPresenterState) {
    OverlappedPresenterStateMaximized,
    OverlappedPresenterStateMinimized,
    OverlappedPresenterStateRestored,
};
OverlappedPresenterState uno_window_get_overlapped_presenter_state(NSWindow *window);

void uno_window_set_always_on_top(NSWindow* window, bool isAlwaysOnTop);
void uno_window_set_border_and_title_bar(NSWindow *window, bool hasBorder, bool hasTitleBar);
void uno_window_set_maximizable(NSWindow* window, bool isMaximizable);
void uno_window_set_minimizable(NSWindow* window, bool isMinimizable);
bool uno_window_set_modal(NSWindow *window, bool isModal);
void uno_window_set_resizable(NSWindow *window, bool isResizable);
void uno_window_set_min_size(NSWindow* window, double width, double height);
void uno_window_set_max_size(NSWindow* window, double width, double height);

// https://learn.microsoft.com/en-us/uwp/api/windows.system.virtualkey?view=winrt-22621
typedef NS_ENUM(sint32, VirtualKey) {
    VirtualKeyNone = 0,
    VirtualKeyLeftButton = 1,
    VirtualKeyRightButton = 2,
    VirtualKeyCancel = 3,
    VirtualKeyMiddleButton = 4,
    VirtualKeyXButton1 = 5,
    VirtualKeyXButton2 = 6,
    VirtualKeyBack = 8,
    VirtualKeyTab = 9,
    VirtualKeyClear = 12,
    VirtualKeyEnter = 13,
    VirtualKeyShift = 16,
    VirtualKeyControl = 17,
    VirtualKeyMenu = 18,
    VirtualKeyPause = 19,
    VirtualKeyCapitalLock = 20,
    VirtualKeyKana = 21,
    VirtualKeyHangul = 21,
    VirtualKeyImeOn = 22,
    VirtualKeyJunja = 23,
    VirtualKeyFinal = 24,
    VirtualKeyHanja = 25,
    VirtualKeyKanji = 25,
    VirtualKeyImeOff = 26,
    VirtualKeyEscape = 27,
    VirtualKeyConvert = 28,
    VirtualKeyNonConvert = 29,
    VirtualKeyAccept = 30,
    VirtualKeyModeChange = 31,
    VirtualKeySpace = 32,
    VirtualKeyPageUp = 33,
    VirtualKeyPageDown = 34,
    VirtualKeyEnd = 35,
    VirtualKeyHome = 36,
    VirtualKeyLeft = 37,
    VirtualKeyUp = 38,
    VirtualKeyRight = 39,
    VirtualKeyDown = 40,
    VirtualKeySelect = 41,
    VirtualKeyPrint = 42,
    VirtualKeyExecute = 43,
    VirtualKeySnapshot = 44,
    VirtualKeyInsert = 45,
    VirtualKeyDelete = 46,
    VirtualKeyHelp = 47,
    VirtualKeyNumber0 = 48,
    VirtualKeyNumber1 = 49,
    VirtualKeyNumber2 = 50,
    VirtualKeyNumber3 = 51,
    VirtualKeyNumber4 = 52,
    VirtualKeyNumber5 = 53,
    VirtualKeyNumber6 = 54,
    VirtualKeyNumber7 = 55,
    VirtualKeyNumber8 = 56,
    VirtualKeyNumber9 = 57,
    VirtualKeyA = 65,
    VirtualKeyB = 66,
    VirtualKeyC = 67,
    VirtualKeyD = 68,
    VirtualKeyE = 69,
    VirtualKeyF = 70,
    VirtualKeyG = 71,
    VirtualKeyH = 72,
    VirtualKeyI = 73,
    VirtualKeyJ = 74,
    VirtualKeyK = 75,
    VirtualKeyL = 76,
    VirtualKeyM = 77,
    VirtualKeyN = 78,
    VirtualKeyO = 79,
    VirtualKeyP = 80,
    VirtualKeyQ = 81,
    VirtualKeyR = 82,
    VirtualKeyS = 83,
    VirtualKeyT = 84,
    VirtualKeyU = 85,
    VirtualKeyV = 86,
    VirtualKeyW = 87,
    VirtualKeyX = 88,
    VirtualKeyY = 89,
    VirtualKeyZ = 90,
    VirtualKeyLeftWindows = 91,
    VirtualKeyRightWindows = 92,
    VirtualKeyApplication = 93,
    VirtualKeySleep = 95,
    VirtualKeyNumberPad0 = 96,
    VirtualKeyNumberPad1 = 97,
    VirtualKeyNumberPad2 = 98,
    VirtualKeyNumberPad3 = 99,
    VirtualKeyNumberPad4 = 100,
    VirtualKeyNumberPad5 = 101,
    VirtualKeyNumberPad6 = 102,
    VirtualKeyNumberPad7 = 103,
    VirtualKeyNumberPad8 = 104,
    VirtualKeyNumberPad9 = 105,
    VirtualKeyMultiply = 106,
    VirtualKeyAdd = 107,
    VirtualKeySeparator = 108,
    VirtualKeySubtract = 109,
    VirtualKeyDecimal = 110,
    VirtualKeyDivide = 111,
    VirtualKeyF1 = 112,
    VirtualKeyF2 = 113,
    VirtualKeyF3 = 114,
    VirtualKeyF4 = 115,
    VirtualKeyF5 = 116,
    VirtualKeyF6 = 117,
    VirtualKeyF7 = 118,
    VirtualKeyF8 = 119,
    VirtualKeyF9 = 120,
    VirtualKeyF10 = 121,
    VirtualKeyF11 = 122,
    VirtualKeyF12 = 123,
    VirtualKeyF13 = 124,
    VirtualKeyF14 = 125,
    VirtualKeyF15 = 126,
    VirtualKeyF16 = 127,
    VirtualKeyF17 = 128,
    VirtualKeyF18 = 129,
    VirtualKeyF19 = 130,
    VirtualKeyF20 = 131,
    VirtualKeyF21 = 132,
    VirtualKeyF22 = 133,
    VirtualKeyF23 = 134,
    VirtualKeyF24 = 135,
    VirtualKeyNavigationView = 136,
    VirtualKeyNavigationMenu = 137,
    VirtualKeyNavigationUp = 138,
    VirtualKeyNavigationDown = 139,
    VirtualKeyNavigationLeft = 140,
    VirtualKeyNavigationRight = 141,
    VirtualKeyNavigationAccept = 142,
    VirtualKeyNavigationCancel = 143,
    VirtualKeyNumberKeyLock = 144,
    VirtualKeyScroll = 145,
    VirtualKeyLeftShift = 160,
    VirtualKeyRightShift = 161,
    VirtualKeyLeftControl = 162,
    VirtualKeyRightControl = 163,
    VirtualKeyLeftMenu = 164,
    VirtualKeyRightMenu = 165,
    VirtualKeyGoBack = 166,
    VirtualKeyGoForward = 167,
    VirtualKeyRefresh = 168,
    VirtualKeyStop = 169,
    VirtualKeySearch = 170,
    VirtualKeyFavorites = 171,
    VirtualKeyGoHome = 172,
    VirtualKeyGamepadA = 195,
    VirtualKeyGamepadB = 196,
    VirtualKeyGamepadX = 197,
    VirtualKeyGamepadY = 198,
    VirtualKeyGamepadRightShoulder = 199,
    VirtualKeyGamepadLeftShoulder = 200,
    VirtualKeyGamepadLeftTrigger = 201,
    VirtualKeyGamepadRightTrigger = 202,
    VirtualKeyGamepadDPadUp = 203,
    VirtualKeyGamepadDPadDown = 204,
    VirtualKeyGamepadDPadLeft = 205,
    VirtualKeyGamepadDPadRight = 206,
    VirtualKeyGamepadMenu = 207,
    VirtualKeyGamepadView = 208,
    VirtualKeyGamepadLeftThumbstickButton = 209,
    VirtualKeyGamepadRightThumbstickButton = 210,
    VirtualKeyGamepadLeftThumbstickUp = 211,
    VirtualKeyGamepadLeftThumbstickDown = 212,
    VirtualKeyGamepadLeftThumbstickRight = 213,
    VirtualKeyGamepadLeftThumbstickLeft = 214,
    VirtualKeyGamepadRightThumbstickUp = 215,
    VirtualKeyGamepadRightThumbstickDown = 216,
    VirtualKeyGamepadRightThumbstickRight = 217,
    VirtualKeyGamepadRightThumbstickLeft = 218,
};

// https://learn.microsoft.com/en-us/uwp/api/windows.system.virtualkeymodifiers?view=winrt-22621
typedef NS_OPTIONS(uint32, VirtualKeyModifiers) {
    VirtualKeyModifiersNone = 0,
    VirtualKeyModifiersControl = 1 << 0,
    VirtualKeyModifiersMenu = 1 << 1,
    VirtualKeyModifiersShift = 1 << 2,
    VirtualKeyModifiersWindows = 1 << 3,
};

// keep in sync with MacOSUnoCorePointerInputSource.cs
typedef NS_ENUM(uint32, MouseEvents) {
    MouseEventsNone,
    MouseEventsEntered,
    MouseEventsExited,
    MouseEventsDown,
    MouseEventsUp,
    MouseEventsMoved,
    MouseEventsScrollWheel,
};

// https://learn.microsoft.com/en-us/uwp/api/windows.devices.input.pointerdevicetype?view=winrt-22621
typedef NS_ENUM(uint32, PointerDeviceType) {
    PointerDeviceTypeTouch,
    PointerDeviceTypePen,
    PointerDeviceTypeMouse,
};

struct MouseEventData {
    MouseEvents eventType;
    CGFloat x;
    CGFloat y;
    // mouse
    int32_t inContact;
    uint32 mouseButtons;
    // pen
    float tiltX;
    float tiltY;
    float pressure;
    // scrollwheel
    int32_t scrollingDeltaX;
    int32_t scrollingDeltaY;
    // others
    VirtualKeyModifiers mods;
    PointerDeviceType pointerDeviceType;
    uint32 frameId;
    uint64 timestamp;
    uint32 pid;
};

typedef int32_t (*window_key_callback_fn_ptr)(UNOWindow* window, VirtualKey key, VirtualKeyModifiers mods, uint32 scanCode, UniChar unicode);
typedef int32_t (*window_mouse_callback_fn_ptr)(UNOWindow* window, struct MouseEventData *data);
void uno_set_window_events_callbacks(window_key_callback_fn_ptr keyDown, window_key_callback_fn_ptr keyUp, window_mouse_callback_fn_ptr pointer, window_move_or_resize_fn_ptr move, window_move_or_resize_fn_ptr resize);

typedef bool (*window_should_close_fn_ptr)(UNOWindow* window);
window_should_close_fn_ptr uno_get_window_should_close_callback(void);

typedef void (*window_close_fn_ptr)(UNOWindow* window);
window_close_fn_ptr uno_get_window_close_callback(void);

void uno_set_window_close_callbacks(window_should_close_fn_ptr shouldClose, window_close_fn_ptr close);

typedef void (*window_close_fn_ptr)(UNOWindow* window);
window_close_fn_ptr uno_get_window_close_callback(void);

void uno_set_window_close_callbacks(window_should_close_fn_ptr shouldClose, window_close_fn_ptr close);

void uno_window_get_metal_handles(UNOWindow* window, void*_Nonnull* _Nonnull device, void*_Nonnull* _Nonnull queue);

typedef void (*window_did_change_screen_fn_ptr)(NSWindow* window, uint32 width, uint32 height, CGFloat backingScaleFactor);
window_did_change_screen_fn_ptr uno_get_window_did_change_screen_callback(void);

typedef void (*window_did_change_screen_parameters_fn_ptr)(NSWindow* window);
window_did_change_screen_parameters_fn_ptr uno_get_window_did_change_screen_parameters_callback(void);

void uno_set_window_screen_change_callbacks(window_did_change_screen_fn_ptr screen, window_did_change_screen_parameters_fn_ptr parameters);

void uno_window_notify_screen_change(NSWindow *window);


@interface UNOMetalFlippedView : MTKView

@property(getter=isFlipped, readonly) BOOL flipped;

@property CAShapeLayer *clipLayer;

@end

NS_ASSUME_NONNULL_END
