//
//  UNOAccessibility.h
//

#pragma once

#import "UnoNativeMac.h"

NS_ASSUME_NONNULL_BEGIN

@import AppKit;

// An accessibility element that represents a single XAML UIElement in the accessibility tree.
// VoiceOver queries these objects for role, label, frame, children, etc.
@interface UNOAccessibilityElement : NSAccessibilityElement <NSAccessibilityButton, NSAccessibilityCheckBox, NSAccessibilityStaticText, NSAccessibilityGroup>

@property (nonatomic) intptr_t unoHandle;
@property (nonatomic, strong, nullable) NSString *unoRole;
@property (nonatomic, strong, nullable) NSString *unoLabel;
@property (nonatomic, strong, nullable) NSString *unoValue;
@property (nonatomic, strong, nullable) NSString *unoHelp;
@property (nonatomic, strong, nullable) NSString *unoDescription; // accessibility description (maps to ARIA aria-description)
@property (nonatomic, strong, nullable) NSString *unoRoleDescription;
@property (nonatomic) NSRect unoFrame; // in window coordinates (top-left origin)
@property (nonatomic) BOOL unoFocusable;
@property (nonatomic) BOOL unoVisible;
@property (nonatomic) BOOL unoEnabled;
@property (nonatomic) BOOL unoRequired; // maps to ARIA aria-required
@property (nonatomic) NSInteger unoHeadingLevel; // 0=none, 1-9=heading levels
@property (nonatomic) BOOL unoIsPassword;
@property (nonatomic) BOOL unoHasExpandCollapse;
@property (nonatomic) BOOL unoIsExpanded;
@property (nonatomic) BOOL unoHasRangeValue;
@property (nonatomic) double unoRangeMin;
@property (nonatomic) double unoRangeMax;
@property (nonatomic) BOOL unoIsSelected; // maps to ARIA aria-selected
@property (nonatomic) BOOL unoIsReadOnly;
@property (nonatomic) NSInteger unoSelectionStart; // text caret/selection start index
@property (nonatomic) NSInteger unoSelectionLength; // text selection length (0 = caret only)
@property (nonatomic) NSInteger unoPositionInSet; // maps to ARIA aria-posinset (1-based, 0=unset)
@property (nonatomic) NSInteger unoSizeOfSet; // maps to ARIA aria-setsize (0=unset)
@property (nonatomic, strong, nullable) NSString *unoLandmarkRole; // landmark type (main, navigation, search, form, region)
@property (nonatomic) BOOL unoIsModal; // true for modal dialogs (VoiceOver restricts navigation)
@property (nonatomic, weak, nullable) id unoParent;
@property (nonatomic, strong) NSMutableArray<UNOAccessibilityElement *> *unoChildren;

- (instancetype)initWithHandle:(intptr_t)handle parent:(nullable id)parent;

@end

// Callback types for managed code
typedef void (*accessibility_invoke_fn_ptr)(intptr_t handle);
typedef void (*accessibility_focus_fn_ptr)(intptr_t handle);
typedef void (*accessibility_increment_fn_ptr)(intptr_t handle);
typedef void (*accessibility_decrement_fn_ptr)(intptr_t handle);
typedef void (*accessibility_expand_collapse_fn_ptr)(intptr_t handle);
typedef void (*accessibility_set_value_fn_ptr)(intptr_t handle, const char* _Nonnull value);

// Setup
void uno_accessibility_init(NSWindow* _Nonnull window);
void uno_accessibility_set_callbacks(accessibility_invoke_fn_ptr invoke, accessibility_focus_fn_ptr focus);
void uno_accessibility_set_range_callbacks(accessibility_increment_fn_ptr increment, accessibility_decrement_fn_ptr decrement);
void uno_accessibility_set_expand_collapse_callback(accessibility_expand_collapse_fn_ptr expandCollapse);
void uno_accessibility_set_value_callback(accessibility_set_value_fn_ptr setValue);

// Element management
void uno_accessibility_add_element(intptr_t parentHandle, intptr_t handle, int32_t index,
	float width, float height, float x, float y,
	const char* _Nullable role, const char* _Nullable label,
	bool focusable, bool visible);
void uno_accessibility_remove_element(intptr_t parentHandle, intptr_t handle);

// Property updates
void uno_accessibility_update_label(intptr_t handle, const char* _Nullable label);
void uno_accessibility_update_frame(intptr_t handle, float width, float height, float x, float y);
void uno_accessibility_update_visibility(intptr_t handle, bool visible);
void uno_accessibility_update_focusable(intptr_t handle, bool focusable);
void uno_accessibility_update_value(intptr_t handle, const char* _Nullable value);
void uno_accessibility_update_enabled(intptr_t handle, bool enabled);
void uno_accessibility_update_help(intptr_t handle, const char* _Nullable help);
void uno_accessibility_update_description(intptr_t handle, const char* _Nullable description);
void uno_accessibility_update_role_description(intptr_t handle, const char* _Nullable roleDescription);
void uno_accessibility_update_role(intptr_t handle, const char* _Nullable role);
void uno_accessibility_update_heading_level(intptr_t handle, int32_t headingLevel);
void uno_accessibility_update_is_password(intptr_t handle, bool isPassword);
void uno_accessibility_update_expand_collapse(intptr_t handle, bool hasExpandCollapse, bool isExpanded);
void uno_accessibility_update_has_range_value(intptr_t handle, bool hasRangeValue);
void uno_accessibility_update_range_bounds(intptr_t handle, double minValue, double maxValue);
void uno_accessibility_update_selected(intptr_t handle, bool isSelected);
void uno_accessibility_update_position_in_set(intptr_t handle, int32_t position, int32_t setSize);
void uno_accessibility_update_landmark(intptr_t handle, const char* _Nullable landmarkRole);
void uno_accessibility_update_required(intptr_t handle, bool required);
void uno_accessibility_update_read_only(intptr_t handle, bool readOnly);
void uno_accessibility_update_selection(intptr_t handle, int32_t selectionStart, int32_t selectionLength);
void uno_accessibility_update_modal(intptr_t handle, bool isModal);

// Focus
void uno_accessibility_set_focused(intptr_t handle);

// Announcements and notifications
void uno_accessibility_announce(const char* _Nonnull text, bool assertive);
void uno_accessibility_post_layout_changed(void);
void uno_accessibility_post_children_changed(void);
void uno_accessibility_post_live_region_created(intptr_t handle);
void uno_accessibility_post_live_region_changed(intptr_t handle);

// Query (called from UNOWindow to return a11y children to VoiceOver)
NSArray* _Nullable uno_accessibility_get_root_children(void);
id _Nullable uno_accessibility_get_focused_element(void);

NS_ASSUME_NONNULL_END
