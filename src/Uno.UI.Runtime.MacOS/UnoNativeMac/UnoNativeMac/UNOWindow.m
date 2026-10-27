//
//  UNOWindow.m
//

#import "UNOWindow.h"
#import "MouseButtons.h"
#import "UNOApplication.h"
#import "UNOSoftView.h"
#import "UNOAccessibility.h"
#import "UNODragDrop.h"

static NSWindow *main_window;
static NSArray<NSButton*>* uno_window_get_caption_buttons(NSWindow *window);
static NSMutableSet<NSWindow*> *windows;
static id windowDidChangeScreen;

static window_did_change_screen_fn_ptr window_did_change_screen;
static window_did_change_screen_parameters_fn_ptr window_did_change_screen_parameters;

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

@implementation UNOMetalFlippedView {
    NSMutableAttributedString *_markedText;
    NSRange _markedRange;
    NSRange _selectedRange;
    NSTextInputContext *_imeInputContext;
    BOOL _keyEventHandledByIME;
}

// behave like UIView/UWP/WinUI, where the origin is top/left, instead of bottom/left
-(BOOL) isFlipped {
    return YES;
}

-(BOOL) acceptsFirstResponder {
    return YES;
}

-(instancetype) initWithFrame:(CGRect)frameRect device:(id<MTLDevice>)device {
    self = [super initWithFrame:frameRect device:device];
    if (self) {
        _markedText = [[NSMutableAttributedString alloc] init];
        _markedRange = NSMakeRange(NSNotFound, 0);
        _selectedRange = NSMakeRange(0, 0);
        _imeActive = NO;
    }
    return self;
}

// Override inputContext to ensure we always have a valid NSTextInputContext
// when IME is active. MTKView (a rendering view) may not provide one by default,
// which would cause interpretKeyEvents: to bypass the input method entirely.
- (NSTextInputContext *)inputContext {
    if (_imeActive) {
        if (!_imeInputContext) {
            _imeInputContext = [[NSTextInputContext alloc] initWithClient:self];
        }
        return _imeInputContext;
    }
    return [super inputContext];
}

// When IME is active, route key events through the text input system.
// _keyEventHandledByIME is set to YES by insertText:/setMarkedText: if the
// input method consumed the event. If NO after handleEvent:, the event was a
// non-composition key (arrow, backspace, tab, etc.) and sendEvent: should
// fall through to normal key handling.
- (void)keyDown:(NSEvent *)event {
    if (_imeActive) {
        _keyEventHandledByIME = NO;
        NSTextInputContext *ctx = self.inputContext;
        if (ctx) {
            [ctx handleEvent:event];
        } else {
            [self interpretKeyEvents:@[event]];
        }
#if DEBUG
        NSLog(@"UNOMetalFlippedView keyDown: inputContext=%p handledByIME=%s event=%@",
              ctx, _keyEventHandledByIME ? "YES" : "NO", event);
#endif
    }
    // If not IME active, UNOWindow.sendEvent: handles the key event directly
}

#pragma mark - NSTextInputClient

- (void)insertText:(id)string replacementRange:(NSRange)replacementRange {
    _keyEventHandledByIME = YES;

    NSString *text;
    if ([string isKindOfClass:[NSAttributedString class]]) {
        text = [(NSAttributedString *)string string];
    } else {
        text = (NSString *)string;
    }

#if DEBUG
    NSLog(@"UNOMetalFlippedView insertText: '%@' replacementRange: {%lu, %lu}", text, (unsigned long)replacementRange.location, (unsigned long)replacementRange.length);
#endif

    // Clear marked text
    [_markedText setAttributedString:[[NSAttributedString alloc] init]];
    _markedRange = NSMakeRange(NSNotFound, 0);

    ime_insert_text_callback_fn_ptr callback = uno_get_ime_insert_text_callback();
    if (callback && text.length > 0) {
        unichar *buffer = malloc(sizeof(unichar) * text.length);
        [text getCharacters:buffer range:NSMakeRange(0, text.length)];
        callback((UNOWindow *)self.window, buffer, (int32_t)text.length);
        free(buffer);
    }
}

- (void)setMarkedText:(id)string selectedRange:(NSRange)selectedRange replacementRange:(NSRange)replacementRange {
    _keyEventHandledByIME = YES;

    NSString *text;
    if ([string isKindOfClass:[NSAttributedString class]]) {
        text = [(NSAttributedString *)string string];
        [_markedText setAttributedString:(NSAttributedString *)string];
    } else {
        text = (NSString *)string;
        [_markedText setAttributedString:[[NSAttributedString alloc] initWithString:text]];
    }

    if (text.length > 0) {
        _markedRange = NSMakeRange(0, text.length);
    } else {
        _markedRange = NSMakeRange(NSNotFound, 0);
    }
    _selectedRange = selectedRange;

#if DEBUG
    NSLog(@"UNOMetalFlippedView setMarkedText: '%@' selectedRange: {%lu, %lu}", text, (unsigned long)selectedRange.location, (unsigned long)selectedRange.length);
#endif

    ime_set_marked_text_callback_fn_ptr callback = uno_get_ime_set_marked_text_callback();
    if (callback) {
        unichar *buffer = NULL;
        if (text.length > 0) {
            buffer = malloc(sizeof(unichar) * text.length);
            [text getCharacters:buffer range:NSMakeRange(0, text.length)];
        }
        callback((UNOWindow *)self.window, buffer, (int32_t)text.length, (int32_t)selectedRange.location, (int32_t)selectedRange.length);
        if (buffer) free(buffer);
    }
}

- (void)unmarkText {
#if DEBUG
    NSLog(@"UNOMetalFlippedView unmarkText");
#endif
    [_markedText setAttributedString:[[NSAttributedString alloc] init]];
    _markedRange = NSMakeRange(NSNotFound, 0);

    ime_unmark_text_callback_fn_ptr callback = uno_get_ime_unmark_text_callback();
    if (callback) {
        callback((UNOWindow *)self.window);
    }
}

- (BOOL)hasMarkedText {
    return _markedRange.location != NSNotFound && _markedRange.length > 0;
}

- (NSRange)markedRange {
    return _markedRange;
}

- (NSRange)selectedRange {
    return _selectedRange;
}

- (NSRect)firstRectForCharacterRange:(NSRange)range actualRange:(nullable NSRangePointer)actualRange {
    ime_get_caret_rect_callback_fn_ptr callback = uno_get_ime_get_caret_rect_callback();
    if (callback) {
        double x = 0, y = 0, w = 0, h = 0;
        callback((UNOWindow *)self.window, &x, &y, &w, &h);
        // Convert from view coordinates to screen coordinates
        NSRect viewRect = NSMakeRect(x, y, w, h);
        NSRect windowRect = [self convertRect:viewRect toView:nil];
        NSRect screenRect = [self.window convertRectToScreen:windowRect];
        return screenRect;
    }
    return NSZeroRect;
}

- (NSUInteger)characterIndexForPoint:(NSPoint)point {
    return NSNotFound;
}

- (nullable NSAttributedString *)attributedSubstringForProposedRange:(NSRange)range actualRange:(nullable NSRangePointer)actualRange {
    return nil;
}

- (NSArray<NSAttributedStringKey> *)validAttributesForMarkedText {
    return @[];
}

- (void)doCommandBySelector:(SEL)selector {
    // No-op: editor commands (moveLeft:, moveToLeftEndOfLine:, deleteBackward:, ...) are
    // handled by the managed key processing once the event falls through, and calling super
    // would reach NSResponder's default which beeps on every unhandled navigation key.
}

#pragma mark - NSDraggingDestination

- (NSDragOperation)draggingEntered:(id<NSDraggingInfo>)sender {
    return uno_drag_drop_handle_entered(self, sender);
}

- (NSDragOperation)draggingUpdated:(id<NSDraggingInfo>)sender {
    return uno_drag_drop_handle_updated(self, sender);
}

- (void)draggingExited:(nullable id<NSDraggingInfo>)sender {
    uno_drag_drop_handle_exited(self, sender);
}

- (BOOL)performDragOperation:(id<NSDraggingInfo>)sender {
    return uno_drag_drop_handle_performed(self, sender);
}

#pragma mark - NSDraggingSource

- (NSDragOperation)draggingSession:(NSDraggingSession *)session sourceOperationMaskForDraggingContext:(NSDraggingContext)context {
    return uno_drag_source_operation_mask(self, context);
}

- (void)draggingSession:(NSDraggingSession *)session endedAtPoint:(NSPoint)screenPoint operation:(NSDragOperation)operation {
    uno_drag_source_session_ended(self, operation);
}

@end

NSWindow* uno_window_create(double width, double height)
{
    CGRect size = NSMakeRect(0, 0, width, height);
    UNOWindow *window = [[UNOWindow alloc] initWithContentRect:size
                                                     styleMask:NSWindowStyleMaskTitled|NSWindowStyleMaskClosable|NSWindowStyleMaskMiniaturizable|NSWindowStyleMaskResizable
                                                       backing:NSBackingStoreBuffered defer:NO];
    
    NSViewController *vc = [[NSViewController alloc] init];
    
    // Create a container view that will hold both the rendering view and any backdrop effect view
    NSView *container = [[NSView alloc] initWithFrame:size];
    container.autoresizesSubviews = YES;
    container.wantsLayer = YES;

    id device = uno_application_get_metal_device();
    if (device) {
        UNOMetalFlippedView *v = [[UNOMetalFlippedView alloc] initWithFrame:size device:device];
        // Disable MTKView auto-draw; frames are driven by the managed render thread, which composes
        // into its own texture and hands it to uno_window_present_texture.
        v.paused = YES;
        v.enableSetNeedsDisplay = NO;
        v.autoresizingMask = NSViewWidthSizable | NSViewHeightSizable;

        // Keep the default triple-buffering (3 drawables). Dropping to 2 saves one VSync of
        // present latency but leaves no headroom: when a frame overruns a VSync interval (GC
        // pause, slow first frame, resize, or a CPU-constrained CI agent) there is no spare
        // drawable, so nextDrawable blocks for ~1s and then returns nil. Losing frames that way
        // is far more costly than the latency it saves.
        CAMetalLayer* metalLayer = (CAMetalLayer*)v.layer;
        metalLayer.maximumDrawableCount = 3;
        window.metalViewDelegate = [[UNOMetalViewDelegate alloc] initWithMetalKitView:v];
        v.delegate = window.metalViewDelegate;
        window.renderingView = v;
    } else {
        UNOSoftView *v = [[UNOSoftView alloc] initWithFrame:size];
        v.autoresizingMask = NSViewWidthSizable | NSViewHeightSizable;
        window.renderingView = v;
    }

    [container addSubview:window.renderingView];
    vc.view = container;
    
    window.contentViewController = vc;
    
    // Notifications
    NSNotificationCenter *center = [NSNotificationCenter defaultCenter];

    assert(windowDidChangeScreen);
    [center addObserver:windowDidChangeScreen selector:@selector(windowDidChangeScreenNotification:) name:NSWindowDidChangeScreenNotification object:window];

    [center addObserver:windowDidChangeScreen selector:@selector(applicationDidChangeScreenParametersNotification:) name:NSApplicationDidChangeScreenParametersNotification object:window];
    
    [windows addObject:window];

    uno_window_register_for_drag_drop(window);

    // do not show the window until activate has been called
    [window makeKeyWindow];
    [window orderOut:nil];

    return window;
}

// AppKit's global origin is the primary screen's bottom-left, even for windows on other displays. WinUI's screen
// coordinates start at the top-left with y growing downward, so window positions are flipped against this edge.
// Always the primary screen (screens[0], the one at (0, 0)), never the window's own: flipping against the screen a
// window happens to be on would make y depend on that screen while x stays global.
static CGFloat uno_desktop_top(void)
{
    return NSMaxY(NSScreen.screens.firstObject.frame);
}

static NSPoint uno_window_top_left(NSWindow *window)
{
    NSRect frame = window.frame;
    return NSMakePoint(NSMinX(frame), uno_desktop_top() - NSMaxY(frame));
}

void uno_window_activate(UNOWindow *window)
{
#if DEBUG
    NSLog(@"uno_window_activate %@ state %d", window, window.overlappedPresenterState);
#endif
    switch (window.overlappedPresenterState) {
        case OverlappedPresenterStateRestored: {
            // ordering to front can move the window from what was set before activating it, so we move it back
            NSPoint topLeft = NSMakePoint(NSMinX(window.frame), NSMaxY(window.frame));
            // don't call `uno_window_restore` since we want the pre-activation state (not the current one) to be used
            [window orderFrontRegardless];
            [window setFrameTopLeftPoint:topLeft];
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
    NSLog(@"uno_window_invalidate %@ view: %p", window, ((UNOWindow*)window).renderingView);
#endif
    ((UNOWindow*)window).renderingView.needsDisplay = true;
}

void uno_window_close(NSWindow *window)
{
#if DEBUG
    NSLog(@"uno_window_close %@", window);
#endif
    NSButton *closeButton = [window standardWindowButton:NSWindowCloseButton];
    if (closeButton != nil && !closeButton.hidden && closeButton.enabled) {
        [window performClose:nil];
        return;
    }

    // performClose: only beeps without a usable close button (e.g. a custom XAML title bar), so ask the delegate here
    id<NSWindowDelegate> delegate = window.delegate;
    if (![delegate respondsToSelector:@selector(windowShouldClose:)] || [delegate windowShouldClose:window]) {
        [window close];
    }
}

void uno_window_move(NSWindow *window, double x, double y)
{
#if DEBUG
    NSLog(@"uno_window_move %@ x: %g y: %g", window, x, y);
#endif
    [window setFrameTopLeftPoint:NSMakePoint(x, uno_desktop_top() - y)];
}

bool uno_window_resize(NSWindow *window, double width, double height)
{
#if DEBUG
    NSLog (@"uno_window_resize %@ %f %f", window, width, height);
#endif
    bool result = false;
    if (window) {
        NSRect frame = window.frame;
        CGFloat top = NSMaxY(frame);
        // consider the titlebar height when re-sizing the window
        CGSize content = [window contentRectForFrameRect: frame].size;
        frame.size = CGSizeMake(width, height + (frame.size.height - content.height));
        // keep the top-left corner in place, as AppWindow.Position does not change on resize
        frame.origin.y = top - frame.size.height;
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

bool uno_window_set_system_backdrop(NSWindow *window, int material)
{
#if DEBUG
    NSLog(@"uno_window_set_system_backdrop %@ material: %d", window, material);
#endif
    if (!window) {
        return false;
    }

    NSView *contentView = window.contentView;
    if (!contentView) {
        return false;
    }

    NSView *renderingView = ((UNOWindow*)window).renderingView;

    // Remove any existing NSVisualEffectView
    for (NSView *subview in contentView.subviews) {
        if ([subview isKindOfClass:[NSVisualEffectView class]]) {
            [subview removeFromSuperview];
            break;
        }
    }

    // material 0 = None, just remove backdrop above and restore opacity
    if (material == 0) {
        renderingView.layer.opaque = YES;
        renderingView.layer.backgroundColor = nil;
        if ([renderingView isKindOfClass:[MTKView class]]) {
            ((MTKView*)renderingView).clearColor = MTLClearColorMake(0, 0, 0, 1);
        }
        window.opaque = YES;
        window.backgroundColor = nil;
        return true;
    }

    NSVisualEffectMaterial nsMaterial;
    switch (material) {
        case 1: // Mica (Base) - underWindowBackground gives a wallpaper-tinted effect
            nsMaterial = NSVisualEffectMaterialUnderWindowBackground;
            break;
        case 2: // Mica (BaseAlt) - windowBackground gives stronger tinting
            nsMaterial = NSVisualEffectMaterialWindowBackground;
            break;
        case 3: // Desktop Acrylic
            nsMaterial = NSVisualEffectMaterialHUDWindow;
            break;
        default:
            return false;
    }

    NSVisualEffectView *effectView = [[NSVisualEffectView alloc] initWithFrame:contentView.bounds];
    effectView.autoresizingMask = NSViewWidthSizable | NSViewHeightSizable;
    effectView.material = nsMaterial;
    effectView.blendingMode = NSVisualEffectBlendingModeBehindWindow;
    effectView.state = NSVisualEffectStateFollowsWindowActiveState;

    // Make the rendering view's layer non-opaque so the backdrop shows through transparent areas
    renderingView.layer.opaque = NO;
    renderingView.layer.backgroundColor = CGColorGetConstantColor(kCGColorClear);
    if ([renderingView isKindOfClass:[MTKView class]]) {
        ((MTKView*)renderingView).clearColor = MTLClearColorMake(0, 0, 0, 0);
    }
    window.opaque = NO;
    window.backgroundColor = NSColor.clearColor;

    // Insert below the rendering view so the backdrop is behind the Skia content
    [contentView addSubview:effectView positioned:NSWindowBelow relativeTo:renderingView];

    return true;
}

void uno_window_get_position(NSWindow *window, double *x, double *y)
{
    NSPoint position = uno_window_top_left(window);
#if DEBUG
    NSLog (@"uno_window_get_position %@ %f %f", window, position.x, position.y);
#endif
    *x = position.x;
    *y = position.y;
}

void uno_window_convert_local_to_screen(NSWindow *window, double x, double y, double *screenX, double *screenY)
{
    NSView *view = ((UNOWindow *)window).renderingView ?: window.contentView;
    NSPoint windowPoint = [view convertPoint:NSMakePoint(x, y) toView:nil];
    NSPoint screenPoint = [window convertPointToScreen:windowPoint];
    *screenX = screenPoint.x;
    // AppKit's global origin is the primary screen's bottom-left, even for windows on other displays.
    *screenY = NSMaxY(NSScreen.screens.firstObject.frame) - screenPoint.y;
}

void uno_window_convert_screen_to_local(NSWindow *window, double screenX, double screenY, double *x, double *y)
{
    NSView *view = ((UNOWindow *)window).renderingView ?: window.contentView;
    // Invert against the same desktop-wide origin, not this window's screen or the union of all screens.
    NSPoint screenPoint = NSMakePoint(screenX, NSMaxY(NSScreen.screens.firstObject.frame) - screenY);
    NSPoint windowPoint = [window convertPointFromScreen:screenPoint];
    NSPoint localPoint = [view convertPoint:windowPoint fromView:nil];
    *x = localPoint.x;
    *y = localPoint.y;
}

char* uno_window_get_title(NSWindow *window)
{
    return strdup(window.title.UTF8String);
}

void uno_window_set_title(NSWindow *window, const char* title)
{
    window.title = [NSString stringWithUTF8String:title];
    // a title change relayouts AppKit's titlebar, including the traffic lights
    if ([window isKindOfClass:[UNOWindow class]]) {
        [(UNOWindow*)window layoutCaptionButtons];
    }
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
    // zoom: rather than performZoom:, which needs a zoom button (hidden with a custom title bar) and toggles back when already zoomed
    if (!window.isZoomed) {
        [window zoom:nil];
    }
}

void uno_window_minimize(NSWindow *window, bool activateWindow)
{
#if DEBUG
    NSLog(@"uno_window_minimize %@ %s", window, activateWindow ? "true" : "false");
#endif
    // miniaturize: rather than performMiniaturize:, which needs a minimize button (hidden with a custom title bar).
    // Before the window is shown it does nothing, so record the state uno_window_activate applies later.
    if ([window isKindOfClass:[UNOWindow class]]) {
        ((UNOWindow*)window).overlappedPresenterState = OverlappedPresenterStateMinimized;
    }
    [window miniaturize:nil];
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

static bool uno_window_is_maximizable(NSWindow *window)
{
    return window.collectionBehavior != (NSWindowCollectionBehaviorFullScreenAuxiliary|NSWindowCollectionBehaviorFullScreenNone|NSWindowCollectionBehaviorFullScreenDisallowsTiling);
}

// Disabled, not hidden, like other macOS apps. Set explicitly: AppKit recreates the buttons when the title bar comes back.
static void uno_window_update_caption_button_states(NSWindow *window)
{
    [[window standardWindowButton:NSWindowMiniaturizeButton] setEnabled:(window.styleMask & NSWindowStyleMaskMiniaturizable) != 0];
    [[window standardWindowButton:NSWindowZoomButton] setEnabled:(window.styleMask & NSWindowStyleMaskResizable) != 0 && uno_window_is_maximizable(window)];
}

void uno_window_set_border_and_title_bar(NSWindow *window, bool hasBorder, bool hasTitleBar)
{
    // Without a title bar the window keeps its native frame (shadow, rounded corners, resizing) and only hides the
    // title bar and traffic lights, so the app can draw its own. Only dropping the border as well makes it untitled.
    NSWindowStyleMask style = window.styleMask;
    if (hasBorder || hasTitleBar)
        style |= NSWindowStyleMaskTitled;
    else
        style &= ~NSWindowStyleMaskTitled;
#if DEBUG
    NSLog(@"uno_window_set_border_and_title_bar %@ 0x%x hasBorder %s hasTitleBar %s 0x%x", window, (uint)window.styleMask,
          hasBorder ? "true" : "false", hasTitleBar ? "true" : "false", (uint)style);
#endif
    window.styleMask = style;
    if ([window isKindOfClass:[UNOWindow class]]) {
        UNOWindow *unoWindow = (UNOWindow*)window;
        unoWindow.hasPresenterTitleBar = hasTitleBar;
        [unoWindow applyTitleBarStyle];
    }
    uno_window_update_caption_button_states(window);
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
    } else {
        [window setCollectionBehavior:NSWindowCollectionBehaviorFullScreenAuxiliary|NSWindowCollectionBehaviorFullScreenNone|NSWindowCollectionBehaviorFullScreenDisallowsTiling];
    }
    uno_window_update_caption_button_states(window);
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
        style &= ~NSWindowStyleMaskMiniaturizable;
#if DEBUG
    NSLog(@"uno_window_set_minimizable %@ 0x%x %s 0x%x", window, (uint)window.styleMask, isMinimizable ? "true" : "false", (uint)style);
#endif
    window.styleMask = style;
    uno_window_update_caption_button_states(window);
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
        style &= ~NSWindowStyleMaskResizable;
#if DEBUG
    NSLog(@"uno_window_set_resizable %@ 0x%x %s 0x%x", window, (uint)window.styleMask, isResizable ? "true" : "false", (uint)style);
#endif
    window.styleMask = style;
    uno_window_update_caption_button_states(window);
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

// FIXME: based on uno/src/Uno.WinRT/System/VirtualKeyHelper.macOS.cs where only Shift and Control are considered ?!?
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

static window_activation_changed_fn_ptr window_activation_changed;

void uno_set_window_activation_changed_callback(window_activation_changed_fn_ptr activationChanged)
{
    window_activation_changed = activationChanged;
}

static window_non_client_hit_test_fn_ptr window_non_client_hit_test;

void uno_set_window_non_client_hit_test_callback(window_non_client_hit_test_fn_ptr hitTest)
{
    window_non_client_hit_test = hitTest;
}

static NSArray<NSButton*>* uno_window_get_caption_buttons(NSWindow *window)
{
    NSMutableArray<NSButton*> *buttons = [NSMutableArray arrayWithCapacity:3];
    for (NSNumber *type in @[@(NSWindowCloseButton), @(NSWindowMiniaturizeButton), @(NSWindowZoomButton)]) {
        NSButton *button = [window standardWindowButton:(NSWindowButton)type.integerValue];
        if (button) {
            [buttons addObject:button];
        }
    }
    return buttons;
}

void uno_window_set_extends_content_into_title_bar(UNOWindow* window, bool extend, double titleBarHeight, bool showCaptionButtons)
{
#if DEBUG
    NSLog(@"uno_window_set_extends_content_into_title_bar %@ extend %s height %g showCaptionButtons %s", window, extend ? "true" : "false", titleBarHeight, showCaptionButtons ? "true" : "false");
#endif
    window.extendsContentIntoTitleBar = extend;
    window.titleBarHeight = extend ? titleBarHeight : 0;
    window.showCaptionButtons = showCaptionButtons;
    [window applyTitleBarStyle];
}

int32_t uno_window_get_caption_buttons_state(NSWindow* window)
{
    int32_t state = 0;
    NSWindowButton types[] = { NSWindowCloseButton, NSWindowMiniaturizeButton, NSWindowZoomButton };
    for (int i = 0; i < 3; i++) {
        NSButton *button = [window standardWindowButton:types[i]];
        if (button != nil && !button.hidden && (window.styleMask & NSWindowStyleMaskTitled) != 0) {
            state |= 1 << (2 * i);
        }
        if (button != nil && button.enabled) {
            state |= 1 << (2 * i + 1);
        }
    }
    return state;
}

bool uno_window_get_caption_buttons_bounds(UNOWindow* window, double* minX, double* maxX, double* windowWidth)
{
    *minX = 0;
    *maxX = 0;
    *windowWidth = window.frame.size.width;

    if (!window.extendsContentIntoTitleBar
        || (window.styleMask & NSWindowStyleMaskTitled) == 0
        || (window.styleMask & NSWindowStyleMaskFullScreen) != 0) {
        return false;
    }

    bool found = false;
    for (NSButton *button in uno_window_get_caption_buttons(window)) {
        if (button.hidden) {
            continue;
        }
        NSRect frame = [button convertRect:button.bounds toView:nil];
        *minX = found ? MIN(*minX, NSMinX(frame)) : NSMinX(frame);
        *maxX = found ? MAX(*maxX, NSMaxX(frame)) : NSMaxX(frame);
        found = true;
    }
    return found;
}

// IME callback storage
static ime_insert_text_callback_fn_ptr ime_insert_text_callback;
static ime_set_marked_text_callback_fn_ptr ime_set_marked_text_callback;
static ime_unmark_text_callback_fn_ptr ime_unmark_text_callback;
static ime_get_caret_rect_callback_fn_ptr ime_get_caret_rect_callback;

ime_insert_text_callback_fn_ptr uno_get_ime_insert_text_callback(void)
{
    return ime_insert_text_callback;
}

ime_set_marked_text_callback_fn_ptr uno_get_ime_set_marked_text_callback(void)
{
    return ime_set_marked_text_callback;
}

ime_unmark_text_callback_fn_ptr uno_get_ime_unmark_text_callback(void)
{
    return ime_unmark_text_callback;
}

ime_get_caret_rect_callback_fn_ptr uno_get_ime_get_caret_rect_callback(void)
{
    return ime_get_caret_rect_callback;
}

void uno_set_ime_callbacks(ime_insert_text_callback_fn_ptr insertText,
                           ime_set_marked_text_callback_fn_ptr setMarkedText,
                           ime_unmark_text_callback_fn_ptr unmarkText,
                           ime_get_caret_rect_callback_fn_ptr getCaretRect)
{
    ime_insert_text_callback = insertText;
    ime_set_marked_text_callback = setMarkedText;
    ime_unmark_text_callback = unmarkText;
    ime_get_caret_rect_callback = getCaretRect;
}

void uno_set_ime_active(UNOWindow* window, bool active)
{
    // The NSTextInputClient lives on the rendering view (UNOMetalFlippedView / UNOSoftView),
    // not on the container view returned by contentViewController.view. The container is a
    // plain NSView used to host backdrop effects below the rendering view.
    NSView *renderingView = window.renderingView;
#if DEBUG
    NSLog(@"uno_set_ime_active: window=%p active=%s renderingView=%@ respondsToSelector=%s",
          window, active ? "true" : "false", renderingView,
          [renderingView respondsToSelector:@selector(setImeActive:)] ? "true" : "false");
#endif
    if ([renderingView respondsToSelector:@selector(setImeActive:)]) {
        [(id)renderingView setImeActive:active];
        if (active) {
            // Ensure the rendering view is the first responder when activating IME so
            // NSTextInputContext can locate the NSTextInputClient.
            [window makeFirstResponder:renderingView];
        }
    }
}

void uno_notify_ime_position_changed(UNOWindow* window)
{
    // Object getters and void messages to nil are safe; the container contentView is not the text-input client.
    NSView *renderingView = window.renderingView;
    NSTextInputContext *inputContext = renderingView.inputContext;
    [inputContext invalidateCharacterCoordinates];
}

double uno_window_get_refresh_rate(NSWindow* window)
{
    NSScreen* screen = window.screen;
    if (screen == nil)
    {
        // An off-screen window (never ordered front, or on a headless agent) has no screen;
        // fall back to the main screen so the render loop still gets a sane pace.
        screen = NSScreen.mainScreen;
    }
    if (screen == nil)
    {
        return 0;
    }
    if (@available(macOS 12.0, *))
    {
        NSInteger fps = screen.maximumFramesPerSecond;
        if (fps > 0)
        {
            return (double)fps;
        }
    }
    return 0;
}

void uno_window_get_metal_handles(UNOWindow* window, void** device, void** queue)
{
    *device = (__bridge void *)(uno_application_get_metal_device());
    *queue = (__bridge void *)(window.metalViewDelegate.queue);
#if DEBUG
    NSLog(@"uno_window_get_metal device %p queue %p", device, queue);
#endif
}

void* uno_window_get_metal_layer(UNOWindow* window)
{
    NSView* view = window.renderingView;
    if ([view isKindOfClass:[MTKView class]])
    {
        // MTKView's backing layer is a CAMetalLayer — hand it to the managed Metal-surface backend for CreateMetalSurface.
        return (__bridge void *)(view.layer);
    }
    return NULL;
}

void uno_window_set_external_present(UNOWindow* window, bool enabled)
{
    if (window.metalViewDelegate != nil)
    {
        window.metalViewDelegate.externalPresent = enabled;
    }
    // The managed render thread only drives Skia-on-Metal. An external-present context is ticked from
    // drawInMTKView, so the paused view must redraw on setNeedsDisplay (uno_window_invalidate).
    NSView* view = window.renderingView;
    if ([view isKindOfClass:[MTKView class]])
    {
        ((MTKView*)view).enableSetNeedsDisplay = enabled;
    }
#if DEBUG
    NSLog(@"uno_window_set_external_present %p -> %s", window, enabled ? "true" : "false");
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
        NSLog(@"uno_window_clip_svg %@ %@ %s scale: %g", window, window.renderingView.layer.description, svg, scale);
#endif
        NSArray<__kindof NSView *> *subviews = window.renderingView.subviews;
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
        NSLog(@"uno_window_clip_svg %@ %@ reset", window, window.renderingView.layer.description);
#endif
        NSArray<__kindof NSView *> *subviews = window.renderingView.subviews;
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

void uno_window_resign_native_first_responder(UNOWindow* window)
{
    NSView *cv = window.contentViewController.view;
    NSResponder *fr = window.firstResponder;
    if ([fr isKindOfClass:[NSView class]] && fr != cv && [(NSView *)fr isDescendantOf:cv]) {
        [window makeFirstResponder:cv];
    }
}

typedef NS_ENUM(NSInteger, TitleBarEventRoute) {
    // content first, then AppKit (the regular path)
    TitleBarEventRouteDefault,
    // AppKit only: the native traffic lights
    TitleBarEventRouteAppKit,
    // content only: AppKit's own (transparent) titlebar would start a window drag or zoom of its own
    TitleBarEventRouteContent,
    // consumed: a caption region started a window drag or ran the double-click action
    TitleBarEventRouteHandled,
};

@interface UNOWindow ()
/// Height of AppKit's titlebar container before the traffic lights were moved, to restore it.
@property CGFloat nativeTitleBarContainerHeight;
@property bool captionButtonsMoved;
@end

@implementation UNOWindow : NSWindow

NSEventModifierFlags _previousFlags;
NSOperatingSystemVersion _osVersion;

+ (void)initialize {
    windows = [[NSMutableSet alloc] initWithCapacity:10];
    _osVersion = [[NSProcessInfo processInfo] operatingSystemVersion];
}

// Borderless / title-bar-hidden NSWindows return NO from the default
// canBecomeKeyWindow / canBecomeMainWindow, which silently drops keyboard
// input. Always opt in so all UNOWindow instances still receive key events.
- (BOOL)canBecomeKeyWindow {
    return YES;
}

- (BOOL)canBecomeMainWindow {
    return YES;
}

- (instancetype)initWithContentRect:(NSRect)contentRect styleMask:(NSWindowStyleMask)style backing:(NSBackingStoreType)backingStoreType defer:(BOOL)flag {
    self = [super initWithContentRect:contentRect styleMask:style backing:backingStoreType defer:flag];
    if (self) {
        self.delegate = self;
        self.overlappedPresenterState = OverlappedPresenterStateRestored;
        self.hasPresenterTitleBar = true;
        self.showCaptionButtons = true;
        // `windows` owns the window. Left on, AppKit's own release on close takes that reference under ARC and
        // frees the window mid-close, while managed handles and AppKit bookkeeping still point at it.
        self.releasedWhenClosed = NO;
    }
    return self;
}

- (void)close {
    [super close];
    // Removing the window from `windows` frees it. Queued only now, so it can't run until AppKit has finished
    // closing, and it runs on a later turn, once whatever called `close` (e.g. the close button) has returned.
    dispatch_async(dispatch_get_main_queue(), ^{
        [windows removeObject:self];
    });
}

@synthesize overlappedPresenterState;

- (bool)isContentUnderTitleBar {
    return self.extendsContentIntoTitleBar || !self.hasPresenterTitleBar;
}

- (void)applyTitleBarStyle {
    bool fullSize = [self isContentUnderTitleBar];
    NSWindowStyleMask style = fullSize
        ? self.styleMask | NSWindowStyleMaskFullSizeContentView
        : self.styleMask & ~NSWindowStyleMaskFullSizeContentView;
    if (style != self.styleMask) {
        // AppKit keeps the content size and moves the frame instead. Like Win32, keep the window where it is and
        // let the content grow under (or shrink out of) the title bar.
        NSRect frame = self.frame;
        self.styleMask = style;
        if (!NSEqualRects(frame, self.frame) && (style & NSWindowStyleMaskFullScreen) == 0) {
            [self setFrame:frame display:YES];
        }
    }
    self.titlebarAppearsTransparent = fullSize;
    self.titleVisibility = fullSize ? NSWindowTitleHidden : NSWindowTitleVisible;

    bool hidden = !self.hasPresenterTitleBar || (self.extendsContentIntoTitleBar && !self.showCaptionButtons);
    for (NSButton *button in uno_window_get_caption_buttons(self)) {
        button.hidden = hidden;
    }

    [self layoutCaptionButtons];

    // The content view now covers (or no longer covers) the title bar. A style change raises no windowDidResize,
    // and the paused Metal view does not report its new drawable size either.
    CGSize size = [self contentRectForFrameRect:self.frame].size;
    uno_get_window_resize_event_callback()(self, size.width, size.height);
}

- (void)layoutCaptionButtons {
    if (!self.extendsContentIntoTitleBar && !self.captionButtonsMoved) {
        return;
    }

    NSArray<NSButton*> *buttons = uno_window_get_caption_buttons(self);
    NSView *container = buttons.firstObject.superview.superview;
    if (container == nil || (self.styleMask & NSWindowStyleMaskFullScreen) != 0) {
        return;
    }

    if (!self.captionButtonsMoved) {
        self.nativeTitleBarContainerHeight = NSHeight(container.frame);
    }

    bool move = self.extendsContentIntoTitleBar && self.titleBarHeight > 0;
    CGFloat height = move ? self.titleBarHeight : self.nativeTitleBarContainerHeight;

    // Same approach as Electron's trafficLightPosition: grow the titlebar container (kept at the top of the
    // window) to the app's title bar height, then center the buttons in it.
    NSRect frame = container.frame;
    frame.size.height = height;
    frame.origin.y = NSHeight(self.frame) - height;
    [container setFrame:frame];
    for (NSButton *button in buttons) {
        NSPoint origin = button.frame.origin;
        origin.y = round((height - NSHeight(button.frame)) / 2);
        [button setFrameOrigin:origin];
    }

    self.captionButtonsMoved = move;
}

- (TitleBarEventRoute)routeTitleBarEvent:(NSEvent *)event {
    NSEventType type = event.type;
    bool isDown = type == NSEventTypeLeftMouseDown || type == NSEventTypeRightMouseDown || type == NSEventTypeOtherMouseDown;
    bool isButtonEvent = isDown
        || type == NSEventTypeLeftMouseUp || type == NSEventTypeRightMouseUp || type == NSEventTypeOtherMouseUp
        || type == NSEventTypeLeftMouseDragged || type == NSEventTypeRightMouseDragged || type == NSEventTypeOtherMouseDragged;
    if (!isButtonEvent) {
        return TitleBarEventRouteDefault;
    }

    // The frame view has no superview, so it hit-tests in window coordinates.
    NSView *hit = [self isContentUnderTitleBar] ? [self.contentView.superview hitTest:event.locationInWindow] : nil;
    if (hit != nil && ![hit isDescendantOf:self.contentView]) {
        for (NSButton *button in uno_window_get_caption_buttons(self)) {
            if (!button.hidden && [hit isDescendantOf:button]) {
                return TitleBarEventRouteAppKit;
            }
        }
    }

    // Like Win32, the regions apply whether or not the content extends into the title bar.
    if (type == NSEventTypeLeftMouseDown && window_non_client_hit_test) {
        CGFloat x = 0, y = 0;
        if ([self getPositionFrom:event x:&x y:&y] && window_non_client_hit_test(self, x, y) == NonClientPointerActionDrag) {
            [self activateForMouseDown];
            if (event.clickCount == 2) {
                [self performTitleBarDoubleClickAction];
            } else {
                [self performWindowDragWithEvent:event];
            }
            return TitleBarEventRouteHandled;
        }
    }

    NSView *titlebarContainer = uno_window_get_caption_buttons(self).firstObject.superview.superview;
    if (hit != nil && titlebarContainer != nil && [hit isDescendantOf:titlebarContainer]) {
        if (isDown) {
            [self activateForMouseDown];
        }
        return TitleBarEventRouteContent;
    }

    return TitleBarEventRouteDefault;
}

// Skipping AppKit also skips its click-to-focus (and app activation), so do it here.
- (void)activateForMouseDown {
    NSApplication *app = [NSApplication sharedApplication];
    if (!app.isActive) {
        if (@available(macOS 14.0, *)) {
            [app activate];
        } else {
            [app activateIgnoringOtherApps:YES];
        }
    }
    if (!self.isKeyWindow) {
        [self makeKeyAndOrderFront:nil];
    }
}

// Double-clicking a title bar follows "Double-click a window's title bar to" in System Settings > Desktop & Dock.
- (void)performTitleBarDoubleClickAction {
    NSUserDefaults *defaults = [NSUserDefaults standardUserDefaults];
    NSString *action = [defaults stringForKey:@"AppleActionOnDoubleClick"];
    if (action == nil && [defaults boolForKey:@"AppleMiniaturizeOnDoubleClick"]) {
        // pre-macOS 11 setting
        action = @"Minimize";
    }

    if ([action isEqualToString:@"None"]) {
        return;
    }

    if ([action isEqualToString:@"Minimize"]) {
        if ((self.styleMask & NSWindowStyleMaskMiniaturizable) != 0) {
            [self performMiniaturize:nil];
        }
        return;
    }

    // "Maximize", "Fill" (macOS 15+) and the default; windowShouldZoom:toFrame: still applies IsMaximizable
    [self zoom:nil];
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
    // captured first, routing a press can activate the app
    bool isActive = [[NSApplication sharedApplication] isActive];

    bool handled = false;
    bool contentOnly = false;
    switch ([self routeTitleBarEvent:event]) {
        case TitleBarEventRouteAppKit:
            [super sendEvent:event];
            return;
        case TitleBarEventRouteHandled:
            return;
        case TitleBarEventRouteContent:
            if (!isActive) {
                // Like the rest of the content, the first click into an inactive app only activates it.
                // AppKit's transparent titlebar would start a drag or zoom on top of the XAML element instead.
                return;
            }
            contentOnly = true;
            break;
        case TitleBarEventRouteDefault:
            if (!isActive) {
                [super sendEvent:event];
                return;
            }
            break;
    }
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
            // When IME is active, route key events through the rendering view's
            // text input system (NSTextInputClient) for composition support.
            // The rendering view's keyDown: calls handleEvent: which triggers
            // the NSTextInputClient protocol callbacks (setMarkedText/insertText).
            // If the IME didn't consume the event (arrow keys, backspace, tab, etc.),
            // fall through to normal key handling.
            // Note: the container view returned by contentViewController.view does NOT
            // implement NSTextInputClient — only the rendering view does.
            NSView *renderingView = self.renderingView;
            BOOL imeActive = [renderingView respondsToSelector:@selector(imeActive)] && [(id)renderingView imeActive];
            if (imeActive) {
                [self makeFirstResponder:renderingView];
                [renderingView keyDown:event];
                BOOL consumed = [renderingView respondsToSelector:@selector(keyEventHandledByIME)]
                    && [(id)renderingView keyEventHandledByIME];
                if (consumed) {
                    handled = true;
                } else {
                    // IME didn't handle this key (arrow, backspace, tab, etc.)
                    // Fall through to normal key processing
                    unsigned short scanCode = event.keyCode;
                    UniChar unicode = get_unicode(event);
                    handled = uno_get_window_key_down_callback()(self, get_virtual_key(scanCode), get_modifiers(event.modifierFlags), scanCode, unicode);
                }
            } else {
                unsigned short scanCode = event.keyCode;
                UniChar unicode = get_unicode(event);
                handled = uno_get_window_key_down_callback()(self, get_virtual_key(scanCode), get_modifiers(event.modifierFlags), scanCode, unicode);
            }
#if DEBUG
            NSLog(@"NSEventTypeKeyDown: %@ window %p imeActive %s handled? %s", event, self, imeActive ? "true" : "false", handled ? "true" : "false");
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

    // Keep the latest mouse-drag event available for outbound drag sessions that start after an
    // async hop (e.g. the managed side awaiting bitmap I/O), where [NSApp currentEvent] may no
    // longer be a mouse event when beginDraggingSessionWithItems:event: is finally called.
    uno_drag_drop_track_mouse_event(event);

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
            // FIXME: NSEvent.pressedMouseButtons is returning a wrong value in Sequoia when using an external trackpad
            if (_osVersion.majorVersion >= 15) {
                NSInteger mask = [MouseButtons mask];

                // If we think buttons are down, but AppKit says NONE are down, we likely missed a MouseUp.
                // We trust AppKit and reset our counters, but only when handling a mouse button event.
                if (mask != 0 && ((uint32)NSEvent.pressedMouseButtons) == 0) {
                    NSEventType type = event.type;
                    BOOL isMouseButtonEvent =
                        type == NSEventTypeLeftMouseDown    ||
                        type == NSEventTypeLeftMouseUp      ||
                        type == NSEventTypeRightMouseDown   ||
                        type == NSEventTypeRightMouseUp     ||
                        type == NSEventTypeOtherMouseDown   ||
                        type == NSEventTypeOtherMouseUp     ||
                        type == NSEventTypeLeftMouseDragged ||
                        type == NSEventTypeRightMouseDragged||
                        type == NSEventTypeOtherMouseDragged;

                    if (isMouseButtonEvent) {
                        mask = [MouseButtons buttonMask:event];
                    }
                }

                data.mouseButtons = (uint32)mask;

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

    if (!handled && !contentOnly) {
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
    NSView* renderView = self.renderingView;
#if DEBUG
    NSLog(@"UNOWindow %p windowWillStartLiveResize scaling from %g to %g", self, renderView.layer.contentsScale, screen.backingScaleFactor);
#endif
    // This does not flow automatically when moving the window from a retina screen to a normal screen
    renderView.layer.contentsScale = screen.backingScaleFactor;
    // prevent content stretching during window resize by pinning to top-left. cf. https://github.com/unoplatform/uno/issues/22159
    renderView.layerContentsPlacement = NSViewLayerContentsPlacementTopLeft;
}

- (void) windowDidEndLiveResize:(NSNotification *) notification {
    NSScreen *screen = ((NSWindow*) notification.object).screen;
    NSView* renderView = self.renderingView;
#if DEBUG
    NSLog(@"UNOWindow %p windowDidEndLiveResize scaling from %g to %g", self, renderView.layer.contentsScale, screen.backingScaleFactor);
#endif
    // Ensure the scale stays in sync up to the end
    renderView.layer.contentsScale = screen.backingScaleFactor;
    // Reset the layerContentsPlacement property to its default value
    renderView.layerContentsPlacement = NSViewLayerContentsPlacementScaleAxesIndependently;
    [self layoutCaptionButtons];
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

- (void)windowDidMove:(NSNotification *)notification {
    NSPoint position = uno_window_top_left(self);
#if DEBUG
    NSLog(@"UNOWindow %p windowDidMove %@ x: %g y: %g", self, notification, position.x, position.y);
#endif
    uno_get_window_move_event_callback()(self, position.x, position.y);
}

// The key window is the one receiving keyboard input, the closest match to an activated Win32 window.
- (void)windowDidBecomeKey:(NSNotification *)notification {
    NSEvent *event = NSApp.currentEvent;
    NSEventType type = event.type;
    bool isMouseDown = type == NSEventTypeLeftMouseDown || type == NSEventTypeRightMouseDown || type == NSEventTypeOtherMouseDown;
    // A press on another window means code made this one key from a handler. When the press also activates the app,
    // the current event is the app activation rather than the press, so fall back to the button state.
    bool byPointer = isMouseDown
        // windowNumber, not window: a stale current event can point at a window that was already closed and freed
        ? event.windowNumber == self.windowNumber
        : [NSEvent pressedMouseButtons] != 0 && NSPointInRect([NSEvent mouseLocation], self.frame);
#if DEBUG
    NSLog(@"UNOWindow %p windowDidBecomeKey byPointer %s", self, byPointer ? "true" : "false");
#endif
    if (window_activation_changed) {
        window_activation_changed(self, 1, byPointer ? 1 : 0);
    }
    [self layoutCaptionButtons];
}

- (void)windowDidResignKey:(NSNotification *)notification {
#if DEBUG
    NSLog(@"UNOWindow %p windowDidResignKey", self);
#endif
    if (window_activation_changed) {
        window_activation_changed(self, 0, 0);
    }
    [self layoutCaptionButtons];
}

- (void)windowDidEnterFullScreen:(NSNotification *)notification {
    [self notifyTitleBarLayoutChanged];
}

- (void)windowDidExitFullScreen:(NSNotification *)notification {
    [self layoutCaptionButtons];
    [self notifyTitleBarLayoutChanged];
}

// The insets depend on the full screen state, which may change after the last resize of the transition.
- (void)notifyTitleBarLayoutChanged {
    CGSize size = [self contentRectForFrameRect:self.frame].size;
    uno_get_window_resize_event_callback()(self, size.width, size.height);
}

- (void)windowDidResize:(NSNotification *)notification {
    // AppKit lays its titlebar out again on resize
    [self layoutCaptionButtons];

    // resizing from the top edge moves the top-left corner without raising windowDidMove
    NSPoint position = uno_window_top_left(self);
    uno_get_window_move_event_callback()(self, position.x, position.y);

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

    // Detach the rendering view while the window is alive, so it (and any native element hosted in it) stops
    // observing this window before the window goes away.
    [self.renderingView removeFromSuperview];
}

#pragma mark - NSAccessibility

- (NSArray *)accessibilityChildren {
    uno_accessibility_ensure_tree(self);
    // Merge the native view children with our custom accessibility tree elements
    NSMutableArray *children = [NSMutableArray arrayWithArray:[super accessibilityChildren]];
    NSArray *a11yChildren = uno_accessibility_get_root_children(self);
    if (a11yChildren) {
        [children addObjectsFromArray:a11yChildren];
    }
    return children;
}

- (id)accessibilityFocusedUIElement {
    uno_accessibility_ensure_tree(self);
    // Return the currently focused accessibility element for VoiceOver
    id focusedElement = uno_accessibility_get_focused_element(self);
    if (focusedElement) {
        return focusedElement;
    }
    // Fall back to the default implementation
    return [super accessibilityFocusedUIElement];
}

- (id)accessibilityHitTest:(NSPoint)point {
    uno_accessibility_ensure_tree(self);
    // First check if the point hits any of our custom accessibility elements
    NSArray *a11yChildren = uno_accessibility_get_root_children(self);
    if (a11yChildren) {
        for (id child in [a11yChildren reverseObjectEnumerator]) {
            if ([child respondsToSelector:@selector(accessibilityHitTest:)]) {
                id hit = [child accessibilityHitTest:point];
                if (hit) {
                    return hit;
                }
            }
        }
    }
    // Fall back to the default implementation
    return [super accessibilityHitTest:point];
}

@end
