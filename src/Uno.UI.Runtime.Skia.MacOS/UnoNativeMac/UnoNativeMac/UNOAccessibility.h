//
//  UNOAccessibility.h
//  Accessibility support for Uno Platform macOS Skia target
//

#pragma once

#import <Cocoa/Cocoa.h>
#import "UnoNativeMac.h"

NS_ASSUME_NONNULL_BEGIN

// Accessibility element data passed from managed code
typedef struct {
    int32_t elementId;          // Unique ID for this element
    int32_t parentId;           // Parent element ID (-1 for root)
    CGRect frame;               // Bounding rectangle in window coordinates
    const char* _Nullable label;         // Accessibility label (name)
    const char* _Nullable hint;          // Accessibility hint (help text)
    const char* _Nullable value;         // Accessibility value (for sliders, etc.)
    const char* _Nullable role;          // Role string (e.g., "button", "slider")
    bool isEnabled;             // Whether the element is enabled
    bool isFocusable;           // Whether the element can receive focus
    bool isSelected;            // Whether the element is selected
    bool isExpanded;            // Whether the element is expanded (for expandable items)
} UNOAccessibilityElementData;

// Callback types for managed code
typedef int32_t (*accessibility_get_child_count_fn_ptr)(void* window);
typedef bool (*accessibility_get_child_data_fn_ptr)(void* window, int32_t index, UNOAccessibilityElementData* data);
typedef int32_t (*accessibility_hit_test_fn_ptr)(void* window, CGFloat x, CGFloat y);
typedef int32_t (*accessibility_get_focused_element_fn_ptr)(void* window);
typedef bool (*accessibility_perform_action_fn_ptr)(void* window, int32_t elementId, const char* action);
typedef void (*accessibility_element_freed_fn_ptr)(void* window, int32_t elementId);

// Callback registration
void uno_set_accessibility_callbacks(
    accessibility_get_child_count_fn_ptr getChildCount,
    accessibility_get_child_data_fn_ptr getChildData,
    accessibility_hit_test_fn_ptr hitTest,
    accessibility_get_focused_element_fn_ptr getFocusedElement,
    accessibility_perform_action_fn_ptr performAction,
    accessibility_element_freed_fn_ptr elementFreed
);

// Notification functions (called from managed code)
void uno_accessibility_post_notification(void* window, int32_t elementId, const char* notificationType);
void uno_accessibility_invalidate(void* window);

// Check if VoiceOver is running
bool uno_accessibility_is_voiceover_running(void);

// Accessibility element class for virtual elements
@interface UNOAccessibilityElement : NSAccessibilityElement

@property (nonatomic) int32_t elementId;
@property (nonatomic, weak) NSView* _Nullable containerView;

- (instancetype)initWithElementId:(int32_t)elementId containerView:(NSView*)containerView;
- (void)updateWithData:(UNOAccessibilityElementData)data;

@end

// Protocol for views that support Uno accessibility
@protocol UNOAccessibilityContainer <NSAccessibilityGroup>

- (void)invalidateAccessibilityElements;

@end

NS_ASSUME_NONNULL_END
