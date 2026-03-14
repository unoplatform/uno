//
//  UNOAccessibility.m
//

#import "UNOAccessibility.h"
#import "UNOWindow.h"

// Native macOS notifications not publicly documented (matching Flutter's AccessibilityBridgeMac)
static NSString* const kAccessibilityLiveRegionCreatedNotification = @"AXLiveRegionCreated";
static NSString* const kAccessibilityLiveRegionChangedNotification = @"AXLiveRegionChanged";
static NSString* const kAccessibilityExpandedChanged = @"AXExpandedChanged";

// Global dictionary mapping handles to accessibility elements
static NSMutableDictionary<NSNumber*, UNOAccessibilityElement*> *g_elements = nil;

static UNOAccessibilityElement *g_rootElement = nil;
static UNOAccessibilityElement *g_focusedElement = nil;
static NSWindow *g_window = nil;

// Callbacks to managed code
static accessibility_invoke_fn_ptr g_invokeCallback = NULL;
static accessibility_focus_fn_ptr g_focusCallback = NULL;
static accessibility_increment_fn_ptr g_incrementCallback = NULL;
static accessibility_decrement_fn_ptr g_decrementCallback = NULL;
static accessibility_expand_collapse_fn_ptr g_expandCollapseCallback = NULL;

#pragma mark - UNOAccessibilityElement

@implementation UNOAccessibilityElement

- (instancetype)initWithHandle:(intptr_t)handle parent:(nullable id)parent {
	self = [super init];
	if (self) {
		_unoHandle = handle;
		_unoParent = parent;
		_unoChildren = [[NSMutableArray alloc] init];
		_unoFocusable = NO;
		_unoVisible = YES;
		_unoEnabled = YES;
		_unoRequired = NO;
		_unoFrame = NSZeroRect;
		_unoHeadingLevel = 0;
		_unoIsPassword = NO;
		_unoHasExpandCollapse = NO;
		_unoIsExpanded = NO;
		_unoHasRangeValue = NO;
		_unoRangeMin = 0;
		_unoRangeMax = 100;
		_unoIsSelected = NO;
		_unoPositionInSet = 0;
		_unoSizeOfSet = 0;
	}
	return self;
}

#pragma mark - NSAccessibility protocol - Role

- (NSAccessibilityRole)accessibilityRole {
	// If a landmark role is set, map it to the appropriate NSAccessibilityRole
	if (_unoLandmarkRole) {
		if ([_unoLandmarkRole isEqualToString:@"main"])       return NSAccessibilityGroupRole;
		if ([_unoLandmarkRole isEqualToString:@"navigation"]) return NSAccessibilityGroupRole;
		if ([_unoLandmarkRole isEqualToString:@"search"])     return NSAccessibilityGroupRole;
		if ([_unoLandmarkRole isEqualToString:@"form"])       return NSAccessibilityGroupRole;
		if ([_unoLandmarkRole isEqualToString:@"region"])     return NSAccessibilityGroupRole;
	}

	if (!_unoRole) {
		return NSAccessibilityGroupRole;
	}

	// Map role strings (matching AutomationControlType / shared AriaMapper roles)
	// to NSAccessibilityRole. These mappings align with Flutter's
	// FlutterPlatformNodeDelegateMac pattern.
	if ([_unoRole isEqualToString:@"button"])        return NSAccessibilityButtonRole;
	if ([_unoRole isEqualToString:@"calendar"])      return NSAccessibilityGridRole;
	if ([_unoRole isEqualToString:@"checkbox"])      return NSAccessibilityCheckBoxRole;
	if ([_unoRole isEqualToString:@"combobox"])      return NSAccessibilityComboBoxRole;
	if ([_unoRole isEqualToString:@"edit"])          return NSAccessibilityTextFieldRole;
	if ([_unoRole isEqualToString:@"textbox"])       return NSAccessibilityTextFieldRole;
	if ([_unoRole isEqualToString:@"textarea"])      return NSAccessibilityTextAreaRole;
	if ([_unoRole isEqualToString:@"link"])          return NSAccessibilityLinkRole;
	if ([_unoRole isEqualToString:@"hyperlink"])     return NSAccessibilityLinkRole;
	if ([_unoRole isEqualToString:@"image"])         return NSAccessibilityImageRole;
	if ([_unoRole isEqualToString:@"img"])           return NSAccessibilityImageRole;
	if ([_unoRole isEqualToString:@"listitem"])      return NSAccessibilityRowRole;
	if ([_unoRole isEqualToString:@"option"])        return NSAccessibilityRowRole;
	if ([_unoRole isEqualToString:@"list"])          return NSAccessibilityListRole;
	if ([_unoRole isEqualToString:@"listbox"])       return NSAccessibilityListRole;
	if ([_unoRole isEqualToString:@"menu"])          return NSAccessibilityMenuRole;
	if ([_unoRole isEqualToString:@"menubar"])       return NSAccessibilityMenuBarRole;
	if ([_unoRole isEqualToString:@"menuitem"])      return NSAccessibilityMenuItemRole;
	if ([_unoRole isEqualToString:@"progressbar"])   return NSAccessibilityProgressIndicatorRole;
	if ([_unoRole isEqualToString:@"radio"])         return NSAccessibilityRadioButtonRole;
	if ([_unoRole isEqualToString:@"scrollbar"])     return NSAccessibilityScrollBarRole;
	if ([_unoRole isEqualToString:@"slider"])        return NSAccessibilitySliderRole;
	if ([_unoRole isEqualToString:@"spinbutton"])    return NSAccessibilityIncrementorRole;
	if ([_unoRole isEqualToString:@"spinner"])       return NSAccessibilityIncrementorRole;
	if ([_unoRole isEqualToString:@"status"])        return NSAccessibilityGroupRole;
	if ([_unoRole isEqualToString:@"statusbar"])     return NSAccessibilityGroupRole;
	if ([_unoRole isEqualToString:@"tab"])           return NSAccessibilityRadioButtonRole;
	if ([_unoRole isEqualToString:@"tablist"])       return NSAccessibilityTabGroupRole;
	if ([_unoRole isEqualToString:@"tabitem"])       return NSAccessibilityRadioButtonRole;
	if ([_unoRole isEqualToString:@"tabpanel"])      return NSAccessibilityGroupRole;
	if ([_unoRole isEqualToString:@"label"])         return NSAccessibilityStaticTextRole;
	if ([_unoRole isEqualToString:@"text"])          return NSAccessibilityStaticTextRole;
	if ([_unoRole isEqualToString:@"toolbar"])       return NSAccessibilityToolbarRole;
	if ([_unoRole isEqualToString:@"tooltip"])       return NSAccessibilityHelpTagRole;
	if ([_unoRole isEqualToString:@"tree"])          return NSAccessibilityOutlineRole;
	if ([_unoRole isEqualToString:@"treeitem"])      return NSAccessibilityRowRole;
	if ([_unoRole isEqualToString:@"custom"])        return NSAccessibilityGroupRole;
	if ([_unoRole isEqualToString:@"generic"])       return NSAccessibilityGroupRole;
	if ([_unoRole isEqualToString:@"group"])         return NSAccessibilityGroupRole;
	if ([_unoRole isEqualToString:@"thumb"])         return NSAccessibilityValueIndicatorRole;
	if ([_unoRole isEqualToString:@"datagrid"])      return NSAccessibilityTableRole;
	if ([_unoRole isEqualToString:@"grid"])          return NSAccessibilityTableRole;
	if ([_unoRole isEqualToString:@"dataitem"])      return NSAccessibilityRowRole;
	if ([_unoRole isEqualToString:@"row"])           return NSAccessibilityRowRole;
	if ([_unoRole isEqualToString:@"document"])      return NSAccessibilityGroupRole;
	if ([_unoRole isEqualToString:@"splitbutton"])   return NSAccessibilityButtonRole;
	if ([_unoRole isEqualToString:@"window"])        return NSAccessibilityWindowRole;
	if ([_unoRole isEqualToString:@"dialog"])        return NSAccessibilityGroupRole;
	if ([_unoRole isEqualToString:@"pane"])          return NSAccessibilityGroupRole;
	if ([_unoRole isEqualToString:@"region"])        return NSAccessibilityGroupRole;
	if ([_unoRole isEqualToString:@"header"])        return NSAccessibilityGroupRole;
	if ([_unoRole isEqualToString:@"heading"])       return NSAccessibilityGroupRole;
	if ([_unoRole isEqualToString:@"headeritem"])    return NSAccessibilityGroupRole;
	if ([_unoRole isEqualToString:@"table"])         return NSAccessibilityTableRole;
	if ([_unoRole isEqualToString:@"titlebar"])      return NSAccessibilityGroupRole;
	if ([_unoRole isEqualToString:@"separator"])     return NSAccessibilitySplitterRole;
	if ([_unoRole isEqualToString:@"semanticzoom"]) return NSAccessibilityGroupRole;
	if ([_unoRole isEqualToString:@"appbar"])        return NSAccessibilityToolbarRole;
	if ([_unoRole isEqualToString:@"flipview"])      return NSAccessibilityGroupRole;
	if ([_unoRole isEqualToString:@"switch"])        return NSAccessibilityCheckBoxRole;

	return NSAccessibilityGroupRole;
}

- (NSAccessibilitySubrole)accessibilitySubrole {
	// Landmark subroles for VoiceOver rotor navigation
	if (_unoLandmarkRole) {
		if ([_unoLandmarkRole isEqualToString:@"main"])       return @"AXLandmarkMain";
		if ([_unoLandmarkRole isEqualToString:@"navigation"]) return @"AXLandmarkNavigation";
		if ([_unoLandmarkRole isEqualToString:@"search"])     return @"AXLandmarkSearch";
		if ([_unoLandmarkRole isEqualToString:@"region"])     return @"AXLandmarkRegion";
		if ([_unoLandmarkRole isEqualToString:@"form"])       return @"AXLandmarkForm";
	}

	// Switch subrole for ToggleSwitch elements
	if ([_unoRole isEqualToString:@"switch"]) {
		return NSAccessibilitySwitchSubrole;
	}

	// Support heading levels for VoiceOver heading navigation (VO+Command+H)
	if (_unoHeadingLevel > 0 && _unoHeadingLevel <= 9) {
		return @"AXHeading";
	}

	// Secure text field subrole for password boxes
	if (_unoIsPassword) {
		return NSAccessibilitySecureTextFieldSubrole;
	}

	// Expand/collapse disclosure triangle subrole
	if (_unoHasExpandCollapse) {
		return NSAccessibilityOutlineRowSubrole;
	}

	return nil;
}

- (NSString *)accessibilityRoleDescription {
	NSString *baseDescription = nil;

	if (_unoRoleDescription) {
		baseDescription = _unoRoleDescription;
	} else {
		NSAccessibilitySubrole subrole = [self accessibilitySubrole];
		baseDescription = NSAccessibilityRoleDescription([self accessibilityRole], subrole);
	}

	// Append "X of Y" position info for VoiceOver announcement.
	// VoiceOver only automatically announces position for list rows and radio buttons,
	// so we include it in the role description for other control types.
	if (_unoPositionInSet > 0 && _unoSizeOfSet > 0) {
		NSAccessibilityRole role = [self accessibilityRole];
		// Skip for roles where VoiceOver handles position natively
		if (role != NSAccessibilityRowRole && role != NSAccessibilityRadioButtonRole) {
			return [NSString stringWithFormat:@"%@, %ld of %ld",
				baseDescription ?: @"",
				(long)_unoPositionInSet,
				(long)_unoSizeOfSet];
		}
	}

	return baseDescription;
}

#pragma mark - NSAccessibility protocol - Label / Value / Help

- (NSString *)accessibilityLabel {
	return _unoLabel;
}

- (id)accessibilityValue {
	NSAccessibilityRole role = [self accessibilityRole];

	// For checkboxes/switches/radio buttons, VoiceOver expects NSNumber:
	// 0 = unchecked, 1 = checked, 2 = mixed
	if (role == NSAccessibilityCheckBoxRole || role == NSAccessibilityRadioButtonRole) {
		if (_unoValue) {
			if ([_unoValue isEqualToString:@"1"]) return @1;
			if ([_unoValue isEqualToString:@"mixed"]) return @2;
		}
		return @0; // unchecked by default
	}

	if (_unoValue) {
		// For sliders/progress, return as number
		if (_unoHasRangeValue) {
			return @([_unoValue doubleValue]);
		}
		return _unoValue;
	}

	// For static text, the value is the label
	if ([_unoRole isEqualToString:@"label"] || [_unoRole isEqualToString:@"text"]) {
		return _unoLabel;
	}

	return nil;
}

- (NSString *)accessibilityTitle {
	NSAccessibilityRole role = [self accessibilityRole];
	// For buttons and controls, the title is the label
	if (role == NSAccessibilityButtonRole ||
		role == NSAccessibilityCheckBoxRole ||
		role == NSAccessibilityRadioButtonRole) {
		return _unoLabel;
	}
	// For text fields/areas, the title serves as the field label (e.g., "Name", "Email")
	if (role == NSAccessibilityTextFieldRole ||
		role == NSAccessibilityTextAreaRole) {
		return _unoLabel;
	}
	// For groups (named containers, landmarks), the title is the group name
	// so VoiceOver announces it when entering the group
	if (role == NSAccessibilityGroupRole && _unoLabel) {
		return _unoLabel;
	}
	// For sliders, the title is the header label
	if (role == NSAccessibilitySliderRole) {
		return _unoLabel;
	}
	// For combo boxes, the title is the header label
	if (role == NSAccessibilityComboBoxRole) {
		return _unoLabel;
	}
	// For lists, the title is the list name
	if (role == NSAccessibilityListRole) {
		return _unoLabel;
	}
	return nil;
}

- (NSString *)accessibilityHelp {
	// accessibilityHelp is read by VoiceOver after a pause (secondary context).
	// It maps to FullDescription or HelpText from WinUI.
	return _unoHelp;
}

- (NSString *)accessibilityPlaceholderValue {
	// accessibilityPlaceholderValue is announced for empty text fields.
	// It maps to the description (PlaceholderText when Header is set).
	NSAccessibilityRole role = [self accessibilityRole];
	if ((role == NSAccessibilityTextFieldRole || role == NSAccessibilityTextAreaRole) && _unoDescription) {
		return _unoDescription;
	}
	return nil;
}

#pragma mark - NSAccessibility protocol - Heading Level

- (NSArray<NSString *> *)accessibilityAttributeNames {
	NSMutableArray *names = [NSMutableArray arrayWithArray:[super accessibilityAttributeNames]];
	if (_unoHeadingLevel > 0) {
		[names addObject:@"AXHeadingLevel"];
	}
	if (_unoPositionInSet > 0) {
		[names addObject:@"AXARIAPosInSet"];
		[names addObject:@"AXARIASetSize"];
	}
	return names;
}

- (id)accessibilityAttributeValue:(NSString *)attribute {
	// VoiceOver queries AXHeadingLevel to announce "heading level N"
	if ([attribute isEqualToString:@"AXHeadingLevel"] && _unoHeadingLevel > 0) {
		return @(_unoHeadingLevel);
	}
	// VoiceOver queries AXARIAPosInSet / AXARIASetSize to announce "X of Y"
	if ([attribute isEqualToString:@"AXARIAPosInSet"] && _unoPositionInSet > 0) {
		return @(_unoPositionInSet);
	}
	if ([attribute isEqualToString:@"AXARIASetSize"] && _unoSizeOfSet > 0) {
		return @(_unoSizeOfSet);
	}
	return [super accessibilityAttributeValue:attribute];
}

#pragma mark - NSAccessibility protocol - Frame (screen coordinates)

- (NSRect)accessibilityFrame {
	if (!g_window) {
		return NSZeroRect;
	}
	// _unoFrame is in window coordinates (top-left origin, as used by Uno)
	// We need to convert to screen coordinates (bottom-left origin, as used by macOS)

	// First, convert from top-left origin to bottom-left origin (within the window content area)
	NSView *contentView = [g_window contentView];
	CGFloat contentHeight = contentView.bounds.size.height;

	NSRect windowRect = NSMakeRect(
		_unoFrame.origin.x,
		contentHeight - _unoFrame.origin.y - _unoFrame.size.height,
		_unoFrame.size.width,
		_unoFrame.size.height
	);

	// Then convert from window coordinates to screen coordinates
	NSRect screenRect = [g_window convertRectToScreen:windowRect];

	return screenRect;
}

#pragma mark - NSAccessibility protocol - Hierarchy

- (id)accessibilityParent {
	if (_unoParent) {
		return _unoParent;
	}
	// If no parent, return the window (root of the accessibility tree)
	return g_window;
}

- (NSWindow *)accessibilityWindow {
	return g_window;
}

- (BOOL)isAccessibilityFocused {
	return g_focusedElement == self;
}

- (NSArray *)accessibilityChildren {
	// Only return visible children
	NSMutableArray *visibleChildren = [[NSMutableArray alloc] init];
	for (UNOAccessibilityElement *child in _unoChildren) {
		if (child.unoVisible) {
			[visibleChildren addObject:child];
		}
	}
	return visibleChildren;
}

#pragma mark - NSAccessibility protocol - State

- (BOOL)isAccessibilityElement {
	if (!_unoVisible) return NO;
	if (_unoFocusable) return YES;
	if (_unoRole != nil) return YES;
	// Named containers (e.g., StackPanel with AutomationProperties.Name="My albums")
	// should be accessibility elements so VoiceOver announces the group name.
	if (_unoLabel != nil && _unoLabel.length > 0) return YES;
	// Landmark regions should always be accessibility elements
	if (_unoLandmarkRole != nil) return YES;
	return NO;
}

- (BOOL)isAccessibilityEnabled {
	return _unoEnabled;
}

- (BOOL)isAccessibilityRequired {
	return _unoRequired;
}

- (BOOL)isAccessibilitySelected {
	return _unoIsSelected;
}

- (void)setAccessibilityFocused:(BOOL)focused {
	if (focused && _unoFocusable && g_focusCallback) {
		g_focusCallback(_unoHandle);
	}
}

#pragma mark - NSAccessibility protocol - Range Value

- (id)accessibilityMinValue {
	if (_unoHasRangeValue) {
		return @(_unoRangeMin);
	}
	return nil;
}

- (id)accessibilityMaxValue {
	if (_unoHasRangeValue) {
		return @(_unoRangeMax);
	}
	return nil;
}

#pragma mark - NSAccessibility protocol - Position in Set

- (NSInteger)accessibilityIndex {
	if (_unoPositionInSet > 0) {
		return _unoPositionInSet - 1; // Convert from 1-based to 0-based
	}
	return NSNotFound;
}

#pragma mark - NSAccessibility protocol - Expand/Collapse

- (NSInteger)accessibilityDisclosureLevel {
	if (_unoHasExpandCollapse) {
		return 0; // Level 0 = top level
	}
	return -1;
}

- (BOOL)isAccessibilityDisclosed {
	return _unoIsExpanded;
}

- (void)setAccessibilityDisclosed:(BOOL)disclosed {
	if (_unoHasExpandCollapse && g_expandCollapseCallback) {
		g_expandCollapseCallback(_unoHandle);
	}
}

- (BOOL)isAccessibilityExpanded {
	return _unoIsExpanded;
}

#pragma mark - NSAccessibility protocol - Actions

- (BOOL)accessibilityPerformPress {
	if (g_invokeCallback) {
		g_invokeCallback(_unoHandle);
		return YES;
	}
	return NO;
}

- (BOOL)accessibilityPerformIncrement {
	if (_unoHasRangeValue && g_incrementCallback) {
		g_incrementCallback(_unoHandle);
		return YES;
	}
	return NO;
}

- (BOOL)accessibilityPerformDecrement {
	if (_unoHasRangeValue && g_decrementCallback) {
		g_decrementCallback(_unoHandle);
		return YES;
	}
	return NO;
}

- (NSArray<NSString *> *)accessibilityActionNames {
	NSMutableArray *actions = [NSMutableArray array];
	NSAccessibilityRole role = [self accessibilityRole];

	if (role == NSAccessibilityButtonRole ||
		role == NSAccessibilityCheckBoxRole ||
		role == NSAccessibilityRadioButtonRole ||
		role == NSAccessibilityLinkRole) {
		[actions addObject:NSAccessibilityPressAction];
	}

	if (_unoHasRangeValue && role == NSAccessibilitySliderRole) {
		[actions addObject:NSAccessibilityIncrementAction];
		[actions addObject:NSAccessibilityDecrementAction];
	}

	if (_unoHasExpandCollapse) {
		if (![actions containsObject:NSAccessibilityPressAction]) {
			[actions addObject:NSAccessibilityPressAction];
		}
	}

	return actions;
}

- (void)accessibilityPerformAction:(NSString *)action {
	if ([action isEqualToString:NSAccessibilityPressAction]) {
		[self accessibilityPerformPress];
	} else if ([action isEqualToString:NSAccessibilityIncrementAction]) {
		[self accessibilityPerformIncrement];
	} else if ([action isEqualToString:NSAccessibilityDecrementAction]) {
		[self accessibilityPerformDecrement];
	}
}

- (NSString *)accessibilityActionDescription:(NSString *)action {
	return NSAccessibilityActionDescription(action);
}

#pragma mark - NSAccessibility protocol - Hit Testing

- (id)accessibilityHitTest:(NSPoint)point {
	if (!g_window) {
		return nil;
	}

	// Check children in reverse order (front-most first)
	NSArray *children = [self accessibilityChildren];
	for (UNOAccessibilityElement *child in [children reverseObjectEnumerator]) {
		id hit = [child accessibilityHitTest:point];
		if (hit) {
			return hit;
		}
	}

	// Check if point is within our frame
	NSRect frame = [self accessibilityFrame];
	if (NSPointInRect(point, frame)) {
		return self;
	}

	return nil;
}

@end

#pragma mark - C API for P/Invoke

void uno_accessibility_init(NSWindow* window) {
	// Clear all stale state from any previous window. When tests create and
	// destroy many windows rapidly, g_elements would otherwise accumulate
	// thousands of stale entries pointing to deallocated views, causing
	// crashes in Metal rendering and the macOS accessibility system.
	if (g_elements) {
		[g_elements removeAllObjects];
	} else {
		g_elements = [[NSMutableDictionary alloc] init];
	}
	g_rootElement = nil;
	g_focusedElement = nil;
	g_window = window;
#if DEBUG
	NSLog(@"UNOAccessibility: initialized for window %p (cleared stale elements)", window);
#endif
}

void uno_accessibility_set_callbacks(accessibility_invoke_fn_ptr invoke, accessibility_focus_fn_ptr focus) {
	g_invokeCallback = invoke;
	g_focusCallback = focus;
}

void uno_accessibility_set_range_callbacks(accessibility_increment_fn_ptr increment, accessibility_decrement_fn_ptr decrement) {
	g_incrementCallback = increment;
	g_decrementCallback = decrement;
}

void uno_accessibility_set_expand_collapse_callback(accessibility_expand_collapse_fn_ptr expandCollapse) {
	g_expandCollapseCallback = expandCollapse;
}

static UNOAccessibilityElement* _Nullable findElement(intptr_t handle) {
#if DEBUG
	if (!g_elements) {
		NSLog(@"UNOAccessibility: WARNING - g_elements is nil! uno_accessibility_init() may not have been called");
		return nil;
	}
	UNOAccessibilityElement *element = g_elements[@(handle)];
	if (!element) {
		NSLog(@"UNOAccessibility: WARNING - Element with handle=%ld not found. Current elements in dict: %lu", (long)handle, (unsigned long)g_elements.count);
	}
	return element;
#else
	return g_elements[@(handle)];
#endif
}

void uno_accessibility_add_element(intptr_t parentHandle, intptr_t handle, int32_t index,
	float width, float height, float x, float y,
	const char* role, const char* label,
	bool focusable, bool visible) {

	// Find or create the parent
	UNOAccessibilityElement *parent = nil;
	if (parentHandle != 0) {
		parent = findElement(parentHandle);
	}

	// If an element with this handle already exists, remove it from its old parent
	// to prevent orphaned references in parent child lists
	UNOAccessibilityElement *existing = g_elements[@(handle)];
	if (existing) {
		id existingParent = existing.unoParent;
		if (existingParent && [existingParent isKindOfClass:[UNOAccessibilityElement class]]) {
			[((UNOAccessibilityElement *)existingParent).unoChildren removeObject:existing];
		}
	}

	// Create the new element
	UNOAccessibilityElement *element = [[UNOAccessibilityElement alloc] initWithHandle:handle parent:parent ?: (id)g_window];
	element.unoFrame = NSMakeRect(x, y, width, height);
	element.unoRole = role ? [NSString stringWithUTF8String:role] : nil;
	element.unoLabel = label ? [NSString stringWithUTF8String:label] : nil;
	element.unoFocusable = focusable;
	element.unoVisible = visible;

	// Register the element
	g_elements[@(handle)] = element;

	// Add it to the parent's children
	if (parent) {
		if (index >= 0 && index < (int32_t)parent.unoChildren.count) {
			[parent.unoChildren insertObject:element atIndex:index];
		} else {
			[parent.unoChildren addObject:element];
		}
	} else {
		// This is a root-level element
		g_rootElement = element;
	}

#if DEBUG
	NSLog(@"UNOAccessibility: added element handle=%ld role=%s label=%s parent=%ld", (long)handle, role ?: "(null)", label ?: "(null)", (long)parentHandle);
#endif
}

void uno_accessibility_remove_element(intptr_t parentHandle, intptr_t handle) {
	UNOAccessibilityElement *element = findElement(handle);
	if (!element) {
		return;
	}

	// Use iterative removal instead of recursion to prevent stack overflow
	// when removing deeply nested element trees. Collect all handles to remove
	// by walking the tree breadth-first, then remove them in a single pass.
	NSMutableArray<NSNumber*> *handlesToRemove = [[NSMutableArray alloc] init];
	NSMutableArray<UNOAccessibilityElement*> *queue = [[NSMutableArray alloc] initWithObjects:element, nil];

	while (queue.count > 0) {
		@autoreleasepool {
			UNOAccessibilityElement *current = queue.firstObject;
			[queue removeObjectAtIndex:0];
			[handlesToRemove addObject:@(current.unoHandle)];

			// Enqueue children for processing
			for (UNOAccessibilityElement *child in current.unoChildren) {
				[queue addObject:child];
			}
		}
	}

	// Remove from parent's children
	UNOAccessibilityElement *parent = findElement(parentHandle);
	if (parent) {
		[parent.unoChildren removeObject:element];
	}

	// Remove all collected elements from global dictionary and clear global refs
	for (NSNumber *key in handlesToRemove) {
		UNOAccessibilityElement *elem = g_elements[key];
		if (elem) {
			if (g_focusedElement == elem) {
				g_focusedElement = nil;
			}
			if (g_rootElement == elem) {
				g_rootElement = nil;
			}
		}
		[g_elements removeObjectForKey:key];
	}

#if DEBUG
	NSLog(@"UNOAccessibility: removed element handle=%ld (and %lu descendants)", (long)handle, (unsigned long)(handlesToRemove.count - 1));
#endif
}

void uno_accessibility_update_label(intptr_t handle, const char* label) {
	UNOAccessibilityElement *element = findElement(handle);
	if (element) {
		element.unoLabel = label ? [NSString stringWithUTF8String:label] : nil;
		NSAccessibilityPostNotification(element, NSAccessibilityTitleChangedNotification);
	}
}

void uno_accessibility_update_frame(intptr_t handle, float width, float height, float x, float y) {
	UNOAccessibilityElement *element = findElement(handle);
	if (element) {
		element.unoFrame = NSMakeRect(x, y, width, height);
	}
}

void uno_accessibility_update_visibility(intptr_t handle, bool visible) {
	UNOAccessibilityElement *element = findElement(handle);
	if (element) {
		element.unoVisible = visible;
		if (element.unoParent && [element.unoParent isKindOfClass:[UNOAccessibilityElement class]]) {
			NSAccessibilityPostNotification(element.unoParent, NSAccessibilityLayoutChangedNotification);
		}
	}
}

void uno_accessibility_update_focusable(intptr_t handle, bool focusable) {
	UNOAccessibilityElement *element = findElement(handle);
	if (element) {
		element.unoFocusable = focusable;
	}
}

void uno_accessibility_update_value(intptr_t handle, const char* value) {
	UNOAccessibilityElement *element = findElement(handle);
	if (element) {
		element.unoValue = value ? [NSString stringWithUTF8String:value] : nil;
		NSAccessibilityPostNotification(element, NSAccessibilityValueChangedNotification);
	}
}

void uno_accessibility_update_enabled(intptr_t handle, bool enabled) {
	UNOAccessibilityElement *element = findElement(handle);
	if (element) {
		element.unoEnabled = enabled;
		// Notify VoiceOver of the enabled state change so it re-reads the element
		NSAccessibilityPostNotification(element, NSAccessibilityValueChangedNotification);
	}
}

void uno_accessibility_update_help(intptr_t handle, const char* help) {
	UNOAccessibilityElement *element = findElement(handle);
	if (element) {
		element.unoHelp = help ? [NSString stringWithUTF8String:help] : nil;
	}
}

void uno_accessibility_update_description(intptr_t handle, const char* description) {
	UNOAccessibilityElement *element = findElement(handle);
	if (element) {
		element.unoDescription = description ? [NSString stringWithUTF8String:description] : nil;
	}
}

void uno_accessibility_update_role_description(intptr_t handle, const char* roleDescription) {
	UNOAccessibilityElement *element = findElement(handle);
	if (element) {
		element.unoRoleDescription = roleDescription ? [NSString stringWithUTF8String:roleDescription] : nil;
	}
}

void uno_accessibility_update_role(intptr_t handle, const char* role) {
	UNOAccessibilityElement *element = findElement(handle);
	if (element) {
		element.unoRole = role ? [NSString stringWithUTF8String:role] : nil;
		NSAccessibilityPostNotification(element, NSAccessibilityLayoutChangedNotification);
	}
}

void uno_accessibility_update_heading_level(intptr_t handle, int32_t headingLevel) {
	UNOAccessibilityElement *element = findElement(handle);
	if (element) {
		element.unoHeadingLevel = headingLevel;
	}
}

void uno_accessibility_update_is_password(intptr_t handle, bool isPassword) {
	UNOAccessibilityElement *element = findElement(handle);
	if (element) {
		element.unoIsPassword = isPassword;
	}
}

void uno_accessibility_update_expand_collapse(intptr_t handle, bool hasExpandCollapse, bool isExpanded) {
	UNOAccessibilityElement *element = findElement(handle);
	if (element) {
		element.unoHasExpandCollapse = hasExpandCollapse;
		element.unoIsExpanded = isExpanded;
		if (hasExpandCollapse) {
			// Match Flutter's AccessibilityBridgeMac: use Row notifications for row/treeitem
			// roles, and AXExpandedChanged for everything else
			NSAccessibilityRole role = [element accessibilityRole];
			if (role == NSAccessibilityRowRole || role == NSAccessibilityOutlineRole) {
				NSAccessibilityPostNotification(element,
					isExpanded ? NSAccessibilityRowExpandedNotification : NSAccessibilityRowCollapsedNotification);
			} else {
				NSAccessibilityPostNotification(element, kAccessibilityExpandedChanged);
			}
		}
	}
}

void uno_accessibility_update_has_range_value(intptr_t handle, bool hasRangeValue) {
	UNOAccessibilityElement *element = findElement(handle);
	if (element) {
		element.unoHasRangeValue = hasRangeValue;
	}
}

void uno_accessibility_update_range_bounds(intptr_t handle, double minValue, double maxValue) {
	UNOAccessibilityElement *element = findElement(handle);
	if (element) {
		element.unoRangeMin = minValue;
		element.unoRangeMax = maxValue;
	}
}

void uno_accessibility_update_selected(intptr_t handle, bool isSelected) {
	UNOAccessibilityElement *element = findElement(handle);
	if (element) {
		element.unoIsSelected = isSelected;
		NSAccessibilityPostNotification(element, NSAccessibilitySelectedChildrenChangedNotification);
	}
}

void uno_accessibility_update_position_in_set(intptr_t handle, int32_t position, int32_t setSize) {
	UNOAccessibilityElement *element = findElement(handle);
	if (element) {
		element.unoPositionInSet = position;
		element.unoSizeOfSet = setSize;
	}
}

void uno_accessibility_update_landmark(intptr_t handle, const char* landmarkRole) {
	UNOAccessibilityElement *element = findElement(handle);
	if (element) {
		element.unoLandmarkRole = landmarkRole ? [NSString stringWithUTF8String:landmarkRole] : nil;
		NSAccessibilityPostNotification(element, NSAccessibilityLayoutChangedNotification);
	}
}

void uno_accessibility_update_required(intptr_t handle, bool required) {
	UNOAccessibilityElement *element = findElement(handle);
	if (element) {
		element.unoRequired = required;
	}
}

void uno_accessibility_set_focused(intptr_t handle) {
	UNOAccessibilityElement *element = findElement(handle);
	if (element) {
		g_focusedElement = element;
		NSAccessibilityPostNotification(element, NSAccessibilityFocusedUIElementChangedNotification);
#if DEBUG
		NSLog(@"UNOAccessibility: focus set to handle=%ld label=%@", (long)handle, element.unoLabel);
#endif
	}
}

void uno_accessibility_announce(const char* text, bool assertive) {
	if (!text) return;

	NSString *announcement = [NSString stringWithUTF8String:text];

	// Use NSAccessibilityAnnouncementRequestedNotification to make VoiceOver speak
	NSDictionary *userInfo = @{
		NSAccessibilityAnnouncementKey: announcement,
		NSAccessibilityPriorityKey: assertive
			? @(NSAccessibilityPriorityHigh)
			: @(NSAccessibilityPriorityMedium)
	};

	NSAccessibilityPostNotificationWithUserInfo(
		NSApp,
		NSAccessibilityAnnouncementRequestedNotification,
		userInfo
	);

#if DEBUG
	NSLog(@"UNOAccessibility: announce '%@' (assertive=%d)", announcement, assertive);
#endif
}

void uno_accessibility_post_layout_changed(void) {
	if (g_rootElement) {
		NSAccessibilityPostNotification(g_rootElement, NSAccessibilityLayoutChangedNotification);
#if DEBUG
		NSLog(@"UNOAccessibility: posted layout changed notification");
#endif
	}
}

void uno_accessibility_post_children_changed(void) {
	// Match Flutter's AccessibilityBridgeMac CHILDREN_CHANGED:
	// NSAccessibilityCreatedNotification on the window is the only way
	// to make VoiceOver pick up layout changes reliably.
	if (g_window) {
		NSAccessibilityPostNotification(g_window, NSAccessibilityCreatedNotification);
#if DEBUG
		NSLog(@"UNOAccessibility: posted children changed (created) notification");
#endif
	}
}

void uno_accessibility_post_live_region_created(intptr_t handle) {
	UNOAccessibilityElement *element = findElement(handle);
	if (element) {
		NSAccessibilityPostNotification(element, kAccessibilityLiveRegionCreatedNotification);
	}
}

void uno_accessibility_post_live_region_changed(intptr_t handle) {
	UNOAccessibilityElement *element = findElement(handle);
	if (element) {
		NSAccessibilityPostNotification(element, kAccessibilityLiveRegionChangedNotification);
	}
}

NSArray* uno_accessibility_get_root_children(void) {
	if (!g_rootElement) {
		return nil;
	}
	return @[g_rootElement];
}

id uno_accessibility_get_focused_element(void) {
	return g_focusedElement;
}
