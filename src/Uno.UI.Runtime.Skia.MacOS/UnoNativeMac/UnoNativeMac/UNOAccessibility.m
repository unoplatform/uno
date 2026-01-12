//
//  UNOAccessibility.m
//  Accessibility support for Uno Platform macOS Skia target
//

#import "UNOAccessibility.h"
#import "UNOWindow.h"

// Static callback pointers
static accessibility_get_child_count_fn_ptr _getChildCount;
static accessibility_get_child_data_fn_ptr _getChildData;
static accessibility_hit_test_fn_ptr _hitTest;
static accessibility_get_focused_element_fn_ptr _getFocusedElement;
static accessibility_perform_action_fn_ptr _performAction;
static accessibility_element_freed_fn_ptr _elementFreed;

void uno_set_accessibility_callbacks(
    accessibility_get_child_count_fn_ptr getChildCount,
    accessibility_get_child_data_fn_ptr getChildData,
    accessibility_hit_test_fn_ptr hitTest,
    accessibility_get_focused_element_fn_ptr getFocusedElement,
    accessibility_perform_action_fn_ptr performAction,
    accessibility_element_freed_fn_ptr elementFreed)
{
    _getChildCount = getChildCount;
    _getChildData = getChildData;
    _hitTest = hitTest;
    _getFocusedElement = getFocusedElement;
    _performAction = performAction;
    _elementFreed = elementFreed;
}

bool uno_accessibility_is_voiceover_running(void)
{
    return [[NSWorkspace sharedWorkspace] isVoiceOverEnabled];
}

void uno_accessibility_post_notification(void* window, int32_t elementId, const char* notificationType)
{
    if (!window || !notificationType) {
        return;
    }

    NSWindow* nsWindow = (__bridge NSWindow*)window;
    NSView* contentView = nsWindow.contentViewController.view;

    NSAccessibilityNotificationName notification = nil;
    NSString* notificationStr = [NSString stringWithUTF8String:notificationType];

    if ([notificationStr isEqualToString:@"LayoutChanged"]) {
        notification = NSAccessibilityLayoutChangedNotification;
    } else if ([notificationStr isEqualToString:@"FocusChanged"]) {
        notification = NSAccessibilityFocusedUIElementChangedNotification;
    } else if ([notificationStr isEqualToString:@"ValueChanged"]) {
        notification = NSAccessibilityValueChangedNotification;
    } else if ([notificationStr isEqualToString:@"TitleChanged"]) {
        notification = NSAccessibilityTitleChangedNotification;
    } else if ([notificationStr isEqualToString:@"SelectedChildrenChanged"]) {
        notification = NSAccessibilitySelectedChildrenChangedNotification;
    } else if ([notificationStr isEqualToString:@"Announcement"]) {
        // For announcements, we need to use a different approach
        // Post through the application
        return;
    }

    if (notification) {
        NSAccessibilityPostNotification(contentView, notification);
    }
}

void uno_accessibility_invalidate(void* window)
{
    if (!window) {
        return;
    }

    NSWindow* nsWindow = (__bridge NSWindow*)window;
    NSView* contentView = nsWindow.contentViewController.view;

    if ([contentView conformsToProtocol:@protocol(UNOAccessibilityContainer)]) {
        [(id<UNOAccessibilityContainer>)contentView invalidateAccessibilityElements];
    }

    NSAccessibilityPostNotification(contentView, NSAccessibilityLayoutChangedNotification);
}

#pragma mark - UNOAccessibilityElement Implementation

@implementation UNOAccessibilityElement {
    NSString* _label;
    NSString* _hint;
    NSString* _value;
    NSAccessibilityRole _role;
    NSRect _frame;
    BOOL _isEnabled;
    BOOL _isFocusable;
    BOOL _isSelected;
    BOOL _isExpanded;
}

- (instancetype)initWithElementId:(int32_t)elementId containerView:(NSView*)containerView
{
    self = [super init];
    if (self) {
        _elementId = elementId;
        _containerView = containerView;
        _isEnabled = YES;
        _isFocusable = NO;
        _isSelected = NO;
        _isExpanded = NO;
    }
    return self;
}

- (void)dealloc
{
    // Notify managed code that this element is being freed
    if (_elementFreed && _containerView) {
        NSWindow* window = _containerView.window;
        if (window) {
            _elementFreed((__bridge void*)window, _elementId);
        }
    }
}

- (void)updateWithData:(UNOAccessibilityElementData)data
{
    _label = data.label ? [NSString stringWithUTF8String:data.label] : nil;
    _hint = data.hint ? [NSString stringWithUTF8String:data.hint] : nil;
    _value = data.value ? [NSString stringWithUTF8String:data.value] : nil;
    _frame = data.frame;
    _isEnabled = data.isEnabled;
    _isFocusable = data.isFocusable;
    _isSelected = data.isSelected;
    _isExpanded = data.isExpanded;

    // Map role string to NSAccessibilityRole
    if (data.role) {
        NSString* roleStr = [NSString stringWithUTF8String:data.role];
        if ([roleStr isEqualToString:@"button"]) {
            _role = NSAccessibilityButtonRole;
        } else if ([roleStr isEqualToString:@"checkbox"]) {
            _role = NSAccessibilityCheckBoxRole;
        } else if ([roleStr isEqualToString:@"radiobutton"]) {
            _role = NSAccessibilityRadioButtonRole;
        } else if ([roleStr isEqualToString:@"slider"]) {
            _role = NSAccessibilitySliderRole;
        } else if ([roleStr isEqualToString:@"text"]) {
            _role = NSAccessibilityStaticTextRole;
        } else if ([roleStr isEqualToString:@"textfield"]) {
            _role = NSAccessibilityTextFieldRole;
        } else if ([roleStr isEqualToString:@"link"]) {
            _role = NSAccessibilityLinkRole;
        } else if ([roleStr isEqualToString:@"image"]) {
            _role = NSAccessibilityImageRole;
        } else if ([roleStr isEqualToString:@"list"]) {
            _role = NSAccessibilityListRole;
        } else if ([roleStr isEqualToString:@"listitem"]) {
            _role = NSAccessibilityCellRole;
        } else if ([roleStr isEqualToString:@"combobox"]) {
            _role = NSAccessibilityComboBoxRole;
        } else if ([roleStr isEqualToString:@"progressbar"]) {
            _role = NSAccessibilityProgressIndicatorRole;
        } else if ([roleStr isEqualToString:@"tablist"]) {
            _role = NSAccessibilityTabGroupRole;
        } else if ([roleStr isEqualToString:@"tab"]) {
            _role = NSAccessibilityRadioButtonRole; // Tabs often use radio button role
        } else if ([roleStr isEqualToString:@"group"]) {
            _role = NSAccessibilityGroupRole;
        } else {
            _role = NSAccessibilityUnknownRole;
        }
    } else {
        _role = NSAccessibilityUnknownRole;
    }
}

#pragma mark - NSAccessibility Protocol

- (NSAccessibilityRole)accessibilityRole
{
    return _role;
}

- (NSString*)accessibilityLabel
{
    return _label;
}

- (NSString*)accessibilityHelp
{
    return _hint;
}

- (id)accessibilityValue
{
    return _value;
}

- (NSRect)accessibilityFrame
{
    if (!_containerView) {
        return NSZeroRect;
    }

    // Convert from view coordinates to screen coordinates
    NSWindow* window = _containerView.window;
    if (!window) {
        return NSZeroRect;
    }

    // The frame is in window content coordinates, need to convert to screen
    NSRect windowRect = [_containerView convertRect:_frame toView:nil];
    NSRect screenRect = [window convertRectToScreen:windowRect];

    return screenRect;
}

- (id)accessibilityParent
{
    return _containerView;
}

- (BOOL)isAccessibilityEnabled
{
    return _isEnabled;
}

- (BOOL)isAccessibilityFocused
{
    return NO; // Will be determined by the focused element callback
}

- (BOOL)isAccessibilitySelected
{
    return _isSelected;
}

- (BOOL)isAccessibilityExpanded
{
    return _isExpanded;
}

- (BOOL)accessibilityPerformPress
{
    if (!_performAction || !_containerView) {
        return NO;
    }

    NSWindow* window = _containerView.window;
    if (!window) {
        return NO;
    }

    return _performAction((__bridge void*)window, _elementId, "press");
}

- (BOOL)accessibilityPerformIncrement
{
    if (!_performAction || !_containerView) {
        return NO;
    }

    NSWindow* window = _containerView.window;
    if (!window) {
        return NO;
    }

    return _performAction((__bridge void*)window, _elementId, "increment");
}

- (BOOL)accessibilityPerformDecrement
{
    if (!_performAction || !_containerView) {
        return NO;
    }

    NSWindow* window = _containerView.window;
    if (!window) {
        return NO;
    }

    return _performAction((__bridge void*)window, _elementId, "decrement");
}

- (BOOL)accessibilityPerformShowMenu
{
    if (!_performAction || !_containerView) {
        return NO;
    }

    NSWindow* window = _containerView.window;
    if (!window) {
        return NO;
    }

    return _performAction((__bridge void*)window, _elementId, "showmenu");
}

@end

#pragma mark - MTKView Accessibility Category

// We need to add accessibility support to the content view
// This will be done by creating a category on UNOMetalFlippedView

@interface UNOAccessibilityContainerView : NSView <UNOAccessibilityContainer>

@property (nonatomic, strong) NSMutableDictionary<NSNumber*, UNOAccessibilityElement*>* accessibilityElementsCache;

@end

@implementation UNOAccessibilityContainerView

- (instancetype)initWithFrame:(NSRect)frameRect
{
    self = [super initWithFrame:frameRect];
    if (self) {
        _accessibilityElementsCache = [NSMutableDictionary dictionary];
    }
    return self;
}

- (BOOL)isAccessibilityElement
{
    return NO; // Container, not an element itself
}

- (NSAccessibilityRole)accessibilityRole
{
    return NSAccessibilityGroupRole;
}

- (NSArray*)accessibilityChildren
{
    if (!_getChildCount || !_getChildData) {
        return @[];
    }

    NSWindow* window = self.window;
    if (!window) {
        return @[];
    }

    int32_t count = _getChildCount((__bridge void*)window);
    if (count <= 0) {
        return @[];
    }

    NSMutableArray* children = [NSMutableArray arrayWithCapacity:count];

    for (int32_t i = 0; i < count; i++) {
        UNOAccessibilityElementData data;
        memset(&data, 0, sizeof(data));

        if (_getChildData((__bridge void*)window, i, &data)) {
            NSNumber* key = @(data.elementId);
            UNOAccessibilityElement* element = _accessibilityElementsCache[key];

            if (!element) {
                element = [[UNOAccessibilityElement alloc] initWithElementId:data.elementId containerView:self];
                _accessibilityElementsCache[key] = element;
            }

            [element updateWithData:data];
            [children addObject:element];
        }
    }

    return children;
}

- (id)accessibilityHitTest:(NSPoint)point
{
    if (!_hitTest) {
        return self;
    }

    NSWindow* window = self.window;
    if (!window) {
        return self;
    }

    // Convert screen point to view coordinates
    NSPoint windowPoint = [window convertPointFromScreen:point];
    NSPoint viewPoint = [self convertPoint:windowPoint fromView:nil];

    // Flip Y coordinate since macOS uses bottom-left origin
    viewPoint.y = self.bounds.size.height - viewPoint.y;

    int32_t elementId = _hitTest((__bridge void*)window, viewPoint.x, viewPoint.y);

    if (elementId >= 0) {
        NSNumber* key = @(elementId);
        UNOAccessibilityElement* element = _accessibilityElementsCache[key];
        if (element) {
            return element;
        }
    }

    return self;
}

- (id)accessibilityFocusedUIElement
{
    if (!_getFocusedElement) {
        return self;
    }

    NSWindow* window = self.window;
    if (!window) {
        return self;
    }

    int32_t elementId = _getFocusedElement((__bridge void*)window);

    if (elementId >= 0) {
        NSNumber* key = @(elementId);
        UNOAccessibilityElement* element = _accessibilityElementsCache[key];
        if (element) {
            return element;
        }
    }

    return self;
}

- (void)invalidateAccessibilityElements
{
    [_accessibilityElementsCache removeAllObjects];
}

@end
