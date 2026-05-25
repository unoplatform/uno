//
//  UNOAccessibility.m
//
//  Per-window macOS accessibility implementation. Each open NSWindow owns a
//  UNOAccessibilityContext attached via objc_setAssociatedObject. Managed
//  callers drive context lifecycle via uno_accessibility_init_context /
//  uno_accessibility_destroy_context; all other entry points resolve to a
//  context either from the window argument or from the element's back-pointer.
//

#import "UNOAccessibility.h"
#import "UNOWindow.h"
#import <objc/runtime.h>

// Native macOS notifications not publicly documented
static NSString* const kAccessibilityLiveRegionCreatedNotification = @"AXLiveRegionCreated";
static NSString* const kAccessibilityLiveRegionChangedNotification = @"AXLiveRegionChanged";
static NSString* const kAccessibilityExpandedChanged = @"AXExpandedChanged";

// Associated-object key used to attach a UNOAccessibilityContext to an NSWindow.
// Address is used as a unique identifier; the byte value itself is never read.
static const void * const kUNOAccessibilityContextKey = &kUNOAccessibilityContextKey;

// Callbacks to managed code — set once at process startup, not per-window.
static accessibility_invoke_fn_ptr g_invokeCallback = NULL;
static accessibility_focus_fn_ptr g_focusCallback = NULL;
static accessibility_increment_fn_ptr g_incrementCallback = NULL;
static accessibility_decrement_fn_ptr g_decrementCallback = NULL;
static accessibility_expand_collapse_fn_ptr g_expandCollapseCallback = NULL;
static accessibility_set_value_fn_ptr g_setValueCallback = NULL;

#pragma mark - UNOAccessibilityContext

@implementation UNOAccessibilityContext

- (instancetype)initWithWindow:(NSWindow *)window {
	self = [super init];
	if (self) {
		_window = window;
		_elements = [[NSMutableDictionary alloc] init];
	}
	return self;
}

@end

UNOAccessibilityContext * _Nullable uno_a11y_context_for_window(NSWindow *window) {
	if (!window) {
		return nil;
	}
	id associated = objc_getAssociatedObject(window, kUNOAccessibilityContextKey);
	if ([associated isKindOfClass:[UNOAccessibilityContext class]]) {
		return (UNOAccessibilityContext *)associated;
	}
	return nil;
}

#pragma mark - UNOAccessibilityElement

@implementation UNOAccessibilityElement

- (instancetype)initWithHandle:(intptr_t)handle parent:(nullable id)parent context:(nullable UNOAccessibilityContext *)context {
	self = [super init];
	if (self) {
		_unoHandle = handle;
		_unoParent = parent;
		_unoContext = context;
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
		_unoIsReadOnly = NO;
		_unoIsModal = NO;
		_unoPositionInSet = 0;
		_unoSizeOfSet = 0;
	}
	return self;
}

#pragma mark - NSAccessibility protocol - Role

- (NSAccessibilityRole)accessibilityRole {
	if (_unoLandmarkRole) {
		if ([_unoLandmarkRole isEqualToString:@"main"])       return NSAccessibilityGroupRole;
		if ([_unoLandmarkRole isEqualToString:@"navigation"]) return NSAccessibilityGroupRole;
		if ([_unoLandmarkRole isEqualToString:@"search"])     return NSAccessibilityGroupRole;
		if ([_unoLandmarkRole isEqualToString:@"form"])       return NSAccessibilityGroupRole;
		if ([_unoLandmarkRole isEqualToString:@"region"])     return NSAccessibilityGroupRole;
	}

	if (!_unoRole) {
		// Elements with a label but no explicit role are accessible groups (named containers).
		// Elements with no role AND no label should be transparent to VoiceOver —
		// use NSAccessibilityUnknownRole so VoiceOver doesn't announce "group".
		if (_unoLabel && _unoLabel.length > 0) {
			return NSAccessibilityGroupRole;
		}
		return NSAccessibilityUnknownRole;
	}

	// Map role strings to NSAccessibilityRole
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

	if ([_unoRole isEqualToString:@"switch"]) {
		return NSAccessibilitySwitchSubrole;
	}

	if ([_unoRole isEqualToString:@"dialog"]) {
		return NSAccessibilityDialogSubrole;
	}

	// VoiceOver heading navigation (VO+Command+H)
	if (_unoHeadingLevel > 0 && _unoHeadingLevel <= 9) {
		return @"AXHeading";
	}

	if (_unoIsPassword) {
		return NSAccessibilitySecureTextFieldSubrole;
	}

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

- (void)setAccessibilityValue:(id)value {
	NSAccessibilityRole role = [self accessibilityRole];

	// Only allow value setting for editable text fields
	if ((role == NSAccessibilityTextFieldRole || role == NSAccessibilityTextAreaRole) &&
		!_unoIsReadOnly && !_unoIsPassword && g_setValueCallback) {
		NSString *stringValue = nil;
		if ([value isKindOfClass:[NSString class]]) {
			stringValue = (NSString *)value;
		} else if (value) {
			stringValue = [value description];
		}
		if (stringValue) {
			g_setValueCallback(_unoHandle, [stringValue UTF8String]);
		}
	}
}

- (BOOL)isAccessibilityEditable {
	NSAccessibilityRole role = [self accessibilityRole];
	if (role == NSAccessibilityTextFieldRole || role == NSAccessibilityTextAreaRole) {
		return !_unoIsReadOnly;
	}
	return NO;
}

- (NSString *)accessibilityTitle {
	NSAccessibilityRole role = [self accessibilityRole];
	if (role == NSAccessibilityButtonRole ||
		role == NSAccessibilityCheckBoxRole ||
		role == NSAccessibilityRadioButtonRole) {
		return _unoLabel;
	}
	// VoiceOver uses the title as the field label (e.g., "Name", "Email")
	if (role == NSAccessibilityTextFieldRole ||
		role == NSAccessibilityTextAreaRole) {
		return _unoLabel;
	}
	// VoiceOver announces the group name when entering the group
	if (role == NSAccessibilityGroupRole && _unoLabel) {
		return _unoLabel;
	}
	if (role == NSAccessibilitySliderRole) {
		return _unoLabel;
	}
	if (role == NSAccessibilityComboBoxRole) {
		return _unoLabel;
	}
	if (role == NSAccessibilityListRole) {
		return _unoLabel;
	}
	// VoiceOver announces the item text along with the role
	if (role == NSAccessibilityRowRole) {
		return _unoLabel;
	}
	if (role == NSAccessibilityMenuItemRole) {
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
	// Remove expand/collapse attributes for elements that don't support them.
	// Without this, VoiceOver announces "collapsed" on buttons, textboxes, etc.
	if (!_unoHasExpandCollapse) {
		[names removeObject:NSAccessibilityExpandedAttribute];
		[names removeObject:NSAccessibilityDisclosureLevelAttribute];
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
	NSWindow *window = _unoContext.window;
	if (!window) {
		return NSZeroRect;
	}
	// _unoFrame is in window coordinates (top-left origin, as used by Uno)
	// Convert from Uno's top-left origin to macOS screen coordinates (bottom-left origin)
	NSView *contentView = [window contentView];
	CGFloat contentHeight = contentView.bounds.size.height;

	NSRect windowRect = NSMakeRect(
		_unoFrame.origin.x,
		contentHeight - _unoFrame.origin.y - _unoFrame.size.height,
		_unoFrame.size.width,
		_unoFrame.size.height
	);

	NSRect screenRect = [window convertRectToScreen:windowRect];

	return screenRect;
}

#pragma mark - NSAccessibility protocol - Hierarchy

- (id)accessibilityParent {
	if (_unoParent) {
		return _unoParent;
	}
	// If no parent, return the window (root of the accessibility tree)
	return _unoContext.window;
}

- (NSWindow *)accessibilityWindow {
	return _unoContext.window;
}

- (BOOL)isAccessibilityFocused {
	return _unoContext.focusedElement == self;
}

- (NSArray *)accessibilityChildren {
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

- (BOOL)isAccessibilityModal {
	return _unoIsModal;
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
	return 0; // Level 0 = top level
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

- (BOOL)respondsToSelector:(SEL)selector {
	// Prevent VoiceOver from querying expand/collapse state on non-expandable elements.
	// Without this, VoiceOver announces "collapsed" on buttons, text boxes, etc.
	if (!_unoHasExpandCollapse) {
		if (selector == @selector(isAccessibilityExpanded) ||
			selector == @selector(isAccessibilityDisclosed) ||
			selector == @selector(setAccessibilityDisclosed:) ||
			selector == @selector(accessibilityDisclosureLevel)) {
			return NO;
		}
	}
	return [super respondsToSelector:selector];
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

#pragma mark - NSAccessibilityTable protocol

- (NSArray *)accessibilityRows {
	NSAccessibilityRole role = [self accessibilityRole];
	if (role != NSAccessibilityTableRole && role != NSAccessibilityOutlineRole && role != NSAccessibilityListRole) {
		return @[];
	}

	NSMutableArray *rows = [NSMutableArray array];
	for (UNOAccessibilityElement *child in _unoChildren) {
		NSAccessibilityRole childRole = [child accessibilityRole];
		if (childRole == NSAccessibilityRowRole && child.unoVisible) {
			[rows addObject:child];
		}
	}
	return rows;
}

- (NSArray *)accessibilityVisibleRows {
	return [self accessibilityRows];
}

- (NSArray *)accessibilitySelectedRows {
	NSMutableArray *selected = [NSMutableArray array];
	for (UNOAccessibilityElement *child in [self accessibilityRows]) {
		if (child.unoIsSelected) {
			[selected addObject:child];
		}
	}
	return selected;
}

- (NSArray *)uno_visibleCellChildren {
	NSMutableArray *cells = [NSMutableArray array];
	for (UNOAccessibilityElement *child in _unoChildren) {
		if (!child.unoVisible) {
			continue;
		}

		if ([child accessibilityRole] != NSAccessibilityRowRole) {
			[cells addObject:child];
		}
	}
	return cells;
}

- (NSArray *)uno_columnCandidates {
	NSMutableArray *columns = [NSMutableArray array];

	for (UNOAccessibilityElement *child in _unoChildren) {
		if (!child.unoVisible) {
			continue;
		}

		if (child.unoRole &&
			([child.unoRole isEqualToString:@"columnheader"] || [child.unoRole isEqualToString:@"header"])) {
			[columns addObject:child];
		}
	}

	if (columns.count > 0) {
		return columns;
	}

	for (UNOAccessibilityElement *row in [self accessibilityRows]) {
		NSArray *rowCells = [row uno_visibleCellChildren];
		if (rowCells.count > 0) {
			return rowCells;
		}
	}

	return @[];
}

- (NSArray *)accessibilityColumns {
	return [self uno_columnCandidates];
}

- (NSArray *)accessibilityVisibleColumns {
	return [self accessibilityColumns];
	}

- (NSArray *)accessibilityVisibleCells {
	NSMutableArray *cells = [NSMutableArray array];
	for (UNOAccessibilityElement *row in [self accessibilityVisibleRows]) {
		[cells addObjectsFromArray:[row uno_visibleCellChildren]];
	}
	return cells;
	}

- (NSArray *)accessibilitySelectedCells {
	NSMutableArray *cells = [NSMutableArray array];
	for (UNOAccessibilityElement *row in [self accessibilitySelectedRows]) {
		[cells addObjectsFromArray:[row uno_visibleCellChildren]];
	}
	return cells;
	}

- (NSArray *)accessibilityColumnHeaderUIElements {
	return [self accessibilityColumns];
}

- (id)accessibilityHeader {
	NSArray *columns = [self accessibilityColumns];
	if (columns.count > 0) {
		return columns.firstObject;
	}
	return nil;
}

#pragma mark - NSAccessibilityNavigableStaticText protocol

- (NSString *)uno_textContent {
	NSAccessibilityRole role = [self accessibilityRole];
	if (role == NSAccessibilityTextFieldRole || role == NSAccessibilityTextAreaRole) {
		return _unoValue ?: @"";
	}
	if (role == NSAccessibilityStaticTextRole) {
		return _unoLabel ?: @"";
	}
	return @"";
}

- (NSArray<NSValue *> *)uno_lineRanges {
	NSString *text = [self uno_textContent];
	if (text.length == 0) {
		return @[[NSValue valueWithRange:NSMakeRange(0, 0)]];
	}

	NSMutableArray<NSValue *> *ranges = [NSMutableArray array];
	NSUInteger start = 0;
	while (start <= text.length) {
		NSRange searchRange = NSMakeRange(start, text.length - start);
		NSRange newlineRange = [text rangeOfString:@"\n" options:0 range:searchRange];
		NSUInteger end = newlineRange.location == NSNotFound ? text.length : newlineRange.location;
		[ranges addObject:[NSValue valueWithRange:NSMakeRange(start, end - start)]];

		if (newlineRange.location == NSNotFound) {
			break;
		}

		start = newlineRange.location + 1;
		if (start == text.length) {
			[ranges addObject:[NSValue valueWithRange:NSMakeRange(start, 0)]];
			break;
		}
	}

	return ranges;
}

- (NSInteger)uno_lineIndexForCharacterIndex:(NSInteger)index {
	NSArray<NSValue *> *lineRanges = [self uno_lineRanges];
	if (lineRanges.count == 0) {
		return 0;
	}

	NSInteger clampedIndex = MAX(0, index);
	for (NSInteger i = 0; i < (NSInteger)lineRanges.count; i++) {
		NSRange lineRange = [[lineRanges objectAtIndex:i] rangeValue];
		if (clampedIndex < (NSInteger)NSMaxRange(lineRange) || (lineRange.length == 0 && clampedIndex == (NSInteger)lineRange.location)) {
			return i;
		}
	}

	return (NSInteger)lineRanges.count - 1;
}

- (NSRect)uno_frameForLineIndex:(NSInteger)lineIndex totalLines:(NSInteger)lineCount {
	NSRect frame = [self accessibilityFrame];
	if (lineCount <= 1 || NSIsEmptyRect(frame)) {
		return frame;
	}

	CGFloat lineHeight = frame.size.height / MAX((CGFloat)lineCount, 1.0);
	CGFloat originY = NSMaxY(frame) - ((lineIndex + 1) * lineHeight);
	return NSMakeRect(frame.origin.x, originY, frame.size.width, lineHeight);
}

- (NSInteger)accessibilityNumberOfCharacters {
	return (NSInteger)[[self uno_textContent] length];
}

- (NSString *)accessibilitySelectedText {
	NSString *text = [self uno_textContent];
	NSInteger start = _unoSelectionStart;
	NSInteger length = _unoSelectionLength;
	if (length <= 0 || start < 0 || (NSUInteger)(start + length) > text.length) {
		return @"";
	}
	return [text substringWithRange:NSMakeRange((NSUInteger)start, (NSUInteger)length)];
}

- (NSRange)accessibilitySelectedTextRange {
	NSInteger start = _unoSelectionStart;
	NSInteger length = _unoSelectionLength;
	if (start < 0) {
		start = 0;
	}
	if (length < 0) {
		length = 0;
	}
	return NSMakeRange((NSUInteger)start, (NSUInteger)length);
}

- (NSInteger)accessibilityInsertionPointLineNumber {
	return [self uno_lineIndexForCharacterIndex:_unoSelectionStart];
}

- (NSString *)accessibilityStringForRange:(NSRange)range {
	NSString *text = [self uno_textContent];
	if (range.location + range.length <= text.length) {
		return [text substringWithRange:range];
	}
	return @"";
}

- (NSRange)accessibilityRangeForLine:(NSInteger)line {
	NSArray<NSValue *> *lineRanges = [self uno_lineRanges];
	if (line < 0 || line >= (NSInteger)lineRanges.count) {
		return NSMakeRange(NSNotFound, 0);
	}
	return [[lineRanges objectAtIndex:line] rangeValue];
}

- (NSRange)accessibilityRangeForPosition:(NSPoint)point {
	NSString *text = [self uno_textContent];
	if (text.length == 0) {
		return NSMakeRange(0, 0);
	}

	NSArray<NSValue *> *lineRanges = [self uno_lineRanges];
	if (lineRanges.count == 0) {
		return NSMakeRange(0, text.length);
	}

	NSRect frame = [self accessibilityFrame];
	if (NSIsEmptyRect(frame) || !NSPointInRect(point, frame)) {
		return NSMakeRange(0, text.length);
	}

	CGFloat lineHeight = frame.size.height / MAX((CGFloat)lineRanges.count, 1.0);
	CGFloat offsetFromTop = NSMaxY(frame) - point.y;
	NSInteger lineIndex = (NSInteger)(offsetFromTop / MAX(lineHeight, 1.0));
	lineIndex = MAX(0, MIN(lineIndex, (NSInteger)lineRanges.count - 1));
	return [[lineRanges objectAtIndex:lineIndex] rangeValue];
}

- (NSRect)accessibilityFrameForRange:(NSRange)range {
	NSString *text = [self uno_textContent];
	if (text.length == 0) {
		return [self accessibilityFrame];
	}

	NSRange clampedRange = NSIntersectionRange(range, NSMakeRange(0, text.length));
	NSArray<NSValue *> *lineRanges = [self uno_lineRanges];
	if (lineRanges.count == 0) {
		return [self accessibilityFrame];
	}

	NSInteger startLine = [self uno_lineIndexForCharacterIndex:(NSInteger)clampedRange.location];
	NSInteger endIndex = clampedRange.length > 0
		? (NSInteger)NSMaxRange(clampedRange) - 1
		: (NSInteger)clampedRange.location;
	NSInteger endLine = [self uno_lineIndexForCharacterIndex:endIndex];

	NSRect frame = [self uno_frameForLineIndex:startLine totalLines:(NSInteger)lineRanges.count];
	for (NSInteger line = startLine + 1; line <= endLine; line++) {
		frame = NSUnionRect(frame, [self uno_frameForLineIndex:line totalLines:(NSInteger)lineRanges.count]);
	}

	return frame;
}

- (NSInteger)accessibilityLineForIndex:(NSInteger)index {
	return [self uno_lineIndexForCharacterIndex:index];
}

#pragma mark - NSAccessibility protocol - Hit Testing

- (id)accessibilityHitTest:(NSPoint)point {
	if (!_unoContext.window) {
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

	NSRect frame = [self accessibilityFrame];
	if (NSPointInRect(point, frame)) {
		return self;
	}

	return nil;
}

@end

#pragma mark - C API for P/Invoke

void uno_accessibility_init_context(NSWindow* window) {
	if (!window) {
		return;
	}

	// Idempotent: if a context is already attached, reset its contents but keep
	// the context object so any outstanding weak references from elements
	// remain valid.
	UNOAccessibilityContext *existing = uno_a11y_context_for_window(window);
	if (existing) {
		[existing.elements removeAllObjects];
		existing.rootElement = nil;
		existing.focusedElement = nil;
#if DEBUG
		NSLog(@"UNOAccessibility: re-initialized context for window %p", window);
#endif
		return;
	}

	UNOAccessibilityContext *context = [[UNOAccessibilityContext alloc] initWithWindow:window];
	objc_setAssociatedObject(window, kUNOAccessibilityContextKey, context, OBJC_ASSOCIATION_RETAIN);
#if DEBUG
	NSLog(@"UNOAccessibility: initialized context for window %p", window);
#endif
}

void uno_accessibility_destroy_context(NSWindow* window) {
	if (!window) {
		return;
	}

	UNOAccessibilityContext *context = uno_a11y_context_for_window(window);
	if (!context) {
		return;
	}

	// Break back-pointers from elements so any pending VoiceOver queries
	// see a detached element rather than following a dangling context.
	for (UNOAccessibilityElement *element in context.elements.allValues) {
		element.unoContext = nil;
	}
	[context.elements removeAllObjects];
	context.rootElement = nil;
	context.focusedElement = nil;
	context.window = nil;

	objc_setAssociatedObject(window, kUNOAccessibilityContextKey, nil, OBJC_ASSOCIATION_RETAIN);
#if DEBUG
	NSLog(@"UNOAccessibility: destroyed context for window %p", window);
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

void uno_accessibility_set_value_callback(accessibility_set_value_fn_ptr setValue) {
	g_setValueCallback = setValue;
}

static UNOAccessibilityElement* _Nullable findElement(UNOAccessibilityContext *context, intptr_t handle) {
	if (!context) {
		return nil;
	}
	return context.elements[@(handle)];
}

// Resolves an element from its handle by scanning every open NSWindow's
// context. Used by per-element setters that take only a handle. Typical
// application state has a small number of windows so linear scan is fine.
static UNOAccessibilityElement* _Nullable findElementGlobal(intptr_t handle) {
	for (NSWindow *window in [NSApp windows]) {
		UNOAccessibilityContext *context = uno_a11y_context_for_window(window);
		if (!context) {
			continue;
		}
		UNOAccessibilityElement *element = context.elements[@(handle)];
		if (element) {
			return element;
		}
	}
	return nil;
}

void uno_accessibility_add_element(NSWindow* window,
	intptr_t parentHandle, intptr_t handle, int32_t index,
	float width, float height, float x, float y,
	const char* role, const char* label,
	bool focusable, bool visible) {

	UNOAccessibilityContext *context = uno_a11y_context_for_window(window);
	if (!context) {
#if DEBUG
		NSLog(@"UNOAccessibility: add_element with no context for window %p — dropping (handle=%ld)", window, (long)handle);
#endif
		return;
	}

	UNOAccessibilityElement *parent = nil;
	if (parentHandle != 0) {
		parent = findElement(context, parentHandle);
	}

	// If an element with this handle already exists in THIS context, remove it
	// from its old parent to prevent orphaned references. Handles are process-
	// unique GCHandle intptrs; two contexts never share a handle.
	UNOAccessibilityElement *existing = context.elements[@(handle)];
	if (existing) {
		id existingParent = existing.unoParent;
		if (existingParent && [existingParent isKindOfClass:[UNOAccessibilityElement class]]) {
			[((UNOAccessibilityElement *)existingParent).unoChildren removeObject:existing];
		}
	}

	UNOAccessibilityElement *element = [[UNOAccessibilityElement alloc]
		initWithHandle:handle
		parent:parent ?: (id)context.window
		context:context];
	element.unoFrame = NSMakeRect(x, y, width, height);
	element.unoRole = role ? [NSString stringWithUTF8String:role] : nil;
	element.unoLabel = label ? [NSString stringWithUTF8String:label] : nil;
	element.unoFocusable = focusable;
	element.unoVisible = visible;

	context.elements[@(handle)] = element;

	if (parent) {
		if (index >= 0 && index < (int32_t)parent.unoChildren.count) {
			[parent.unoChildren insertObject:element atIndex:index];
		} else {
			[parent.unoChildren addObject:element];
		}
	} else {
		// This is a root-level element
		context.rootElement = element;
	}

#if DEBUG
	NSLog(@"UNOAccessibility: [window %p] added element handle=%ld role=%s label=%s parent=%ld", window, (long)handle, role ?: "(null)", label ?: "(null)", (long)parentHandle);
#endif
}

void uno_accessibility_remove_element(NSWindow* window, intptr_t parentHandle, intptr_t handle) {
	UNOAccessibilityContext *context = uno_a11y_context_for_window(window);
	UNOAccessibilityElement *element = findElement(context, handle);
	if (!element) {
		return;
	}

	// Use iterative removal instead of recursion to prevent stack overflow
	// when removing deeply nested element trees.
	NSMutableArray<NSNumber*> *handlesToRemove = [[NSMutableArray alloc] init];
	NSMutableArray<UNOAccessibilityElement*> *queue = [[NSMutableArray alloc] initWithObjects:element, nil];

	while (queue.count > 0) {
		@autoreleasepool {
			UNOAccessibilityElement *current = queue.firstObject;
			[queue removeObjectAtIndex:0];
			[handlesToRemove addObject:@(current.unoHandle)];

			for (UNOAccessibilityElement *child in current.unoChildren) {
				[queue addObject:child];
			}
		}
	}

	UNOAccessibilityElement *parent = findElement(context, parentHandle);
	if (parent) {
		[parent.unoChildren removeObject:element];
	}

	for (NSNumber *key in handlesToRemove) {
		UNOAccessibilityElement *elem = context.elements[key];
		if (elem) {
			if (context.focusedElement == elem) {
				context.focusedElement = nil;
			}
			if (context.rootElement == elem) {
				context.rootElement = nil;
			}
			elem.unoContext = nil;
		}
		[context.elements removeObjectForKey:key];
	}

#if DEBUG
	NSLog(@"UNOAccessibility: [window %p] removed element handle=%ld (and %lu descendants)", window, (long)handle, (unsigned long)(handlesToRemove.count - 1));
#endif
}

void uno_accessibility_update_label(intptr_t handle, const char* label) {
	UNOAccessibilityElement *element = findElementGlobal(handle);
	if (element) {
		element.unoLabel = label ? [NSString stringWithUTF8String:label] : nil;
		NSAccessibilityPostNotification(element, NSAccessibilityTitleChangedNotification);
	}
}

void uno_accessibility_update_frame(intptr_t handle, float width, float height, float x, float y) {
	UNOAccessibilityElement *element = findElementGlobal(handle);
	if (element) {
		element.unoFrame = NSMakeRect(x, y, width, height);
	}
}

void uno_accessibility_update_visibility(intptr_t handle, bool visible) {
	UNOAccessibilityElement *element = findElementGlobal(handle);
	if (element) {
		element.unoVisible = visible;
		if (element.unoParent && [element.unoParent isKindOfClass:[UNOAccessibilityElement class]]) {
			NSAccessibilityPostNotification(element.unoParent, NSAccessibilityLayoutChangedNotification);
		}
	}
}

void uno_accessibility_update_focusable(intptr_t handle, bool focusable) {
	UNOAccessibilityElement *element = findElementGlobal(handle);
	if (element) {
		element.unoFocusable = focusable;
	}
}

void uno_accessibility_update_value(intptr_t handle, const char* value) {
	UNOAccessibilityElement *element = findElementGlobal(handle);
	if (element) {
		element.unoValue = value ? [NSString stringWithUTF8String:value] : nil;
		NSAccessibilityPostNotification(element, NSAccessibilityValueChangedNotification);
	}
}

void uno_accessibility_update_enabled(intptr_t handle, bool enabled) {
	UNOAccessibilityElement *element = findElementGlobal(handle);
	if (element) {
		element.unoEnabled = enabled;
		NSAccessibilityPostNotification(element, NSAccessibilityValueChangedNotification);
	}
}

void uno_accessibility_update_help(intptr_t handle, const char* help) {
	UNOAccessibilityElement *element = findElementGlobal(handle);
	if (element) {
		element.unoHelp = help ? [NSString stringWithUTF8String:help] : nil;
	}
}

void uno_accessibility_update_description(intptr_t handle, const char* description) {
	UNOAccessibilityElement *element = findElementGlobal(handle);
	if (element) {
		element.unoDescription = description ? [NSString stringWithUTF8String:description] : nil;
	}
}

void uno_accessibility_update_role_description(intptr_t handle, const char* roleDescription) {
	UNOAccessibilityElement *element = findElementGlobal(handle);
	if (element) {
		element.unoRoleDescription = roleDescription ? [NSString stringWithUTF8String:roleDescription] : nil;
	}
}

void uno_accessibility_update_role(intptr_t handle, const char* role) {
	UNOAccessibilityElement *element = findElementGlobal(handle);
	if (element) {
		element.unoRole = role ? [NSString stringWithUTF8String:role] : nil;
		NSAccessibilityPostNotification(element, NSAccessibilityLayoutChangedNotification);
	}
}

void uno_accessibility_update_heading_level(intptr_t handle, int32_t headingLevel) {
	UNOAccessibilityElement *element = findElementGlobal(handle);
	if (element) {
		element.unoHeadingLevel = headingLevel;
	}
}

void uno_accessibility_update_is_password(intptr_t handle, bool isPassword) {
	UNOAccessibilityElement *element = findElementGlobal(handle);
	if (element) {
		element.unoIsPassword = isPassword;
	}
}

void uno_accessibility_update_expand_collapse(intptr_t handle, bool hasExpandCollapse, bool isExpanded) {
	UNOAccessibilityElement *element = findElementGlobal(handle);
	if (element) {
		element.unoHasExpandCollapse = hasExpandCollapse;
		element.unoIsExpanded = isExpanded;
		if (hasExpandCollapse) {
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
	UNOAccessibilityElement *element = findElementGlobal(handle);
	if (element) {
		element.unoHasRangeValue = hasRangeValue;
	}
}

void uno_accessibility_update_range_bounds(intptr_t handle, double minValue, double maxValue) {
	UNOAccessibilityElement *element = findElementGlobal(handle);
	if (element) {
		element.unoRangeMin = minValue;
		element.unoRangeMax = maxValue;
	}
}

void uno_accessibility_update_selected(intptr_t handle, bool isSelected) {
	UNOAccessibilityElement *element = findElementGlobal(handle);
	if (element) {
		element.unoIsSelected = isSelected;
		NSAccessibilityPostNotification(element, NSAccessibilitySelectedChildrenChangedNotification);
	}
}

void uno_accessibility_update_position_in_set(intptr_t handle, int32_t position, int32_t setSize) {
	UNOAccessibilityElement *element = findElementGlobal(handle);
	if (element) {
		element.unoPositionInSet = position;
		element.unoSizeOfSet = setSize;
	}
}

void uno_accessibility_update_landmark(intptr_t handle, const char* landmarkRole) {
	UNOAccessibilityElement *element = findElementGlobal(handle);
	if (element) {
		element.unoLandmarkRole = landmarkRole ? [NSString stringWithUTF8String:landmarkRole] : nil;
		NSAccessibilityPostNotification(element, NSAccessibilityLayoutChangedNotification);
	}
}

void uno_accessibility_update_required(intptr_t handle, bool required) {
	UNOAccessibilityElement *element = findElementGlobal(handle);
	if (element) {
		element.unoRequired = required;
	}
}

void uno_accessibility_update_read_only(intptr_t handle, bool readOnly) {
	UNOAccessibilityElement *element = findElementGlobal(handle);
	if (element) {
		element.unoIsReadOnly = readOnly;
	}
}

void uno_accessibility_update_selection(intptr_t handle, int32_t selectionStart, int32_t selectionLength) {
	UNOAccessibilityElement *element = findElementGlobal(handle);
	if (element) {
		element.unoSelectionStart = selectionStart;
		element.unoSelectionLength = selectionLength;
		NSAccessibilityPostNotification(element, NSAccessibilitySelectedTextChangedNotification);
	}
}

void uno_accessibility_update_modal(intptr_t handle, bool isModal) {
	UNOAccessibilityElement *element = findElementGlobal(handle);
	if (element) {
		element.unoIsModal = isModal;
		NSWindow *window = element.unoContext.window;
		if (window) {
			if (isModal) {
				// When a modal opens, VoiceOver needs the modal flag and a focus/layout refresh
				// so it restricts navigation to the dialog subtree.
				NSAccessibilityPostNotification(window, NSAccessibilityFocusedUIElementChangedNotification);
			}
			NSAccessibilityPostNotification(window, NSAccessibilityLayoutChangedNotification);
		}
	}
}

void uno_accessibility_set_focused(intptr_t handle) {
	UNOAccessibilityElement *element = findElementGlobal(handle);
	if (element) {
		UNOAccessibilityContext *context = element.unoContext;
		if (context) {
			context.focusedElement = element;
		}
		NSAccessibilityPostNotification(element, NSAccessibilityFocusedUIElementChangedNotification);
#if DEBUG
		NSLog(@"UNOAccessibility: focus set to handle=%ld label=%@", (long)handle, element.unoLabel);
#endif
	}
}

void uno_accessibility_announce(NSWindow* window, const char* text, bool assertive) {
	if (!text) return;

	NSString *announcement = [NSString stringWithUTF8String:text];

	// Use NSAccessibilityAnnouncementRequestedNotification to make VoiceOver speak.
	// Posting on the specific window (when provided) associates the announcement
	// with that window's context in screen-reader multi-window scenarios.
	NSDictionary *userInfo = @{
		NSAccessibilityAnnouncementKey: announcement,
		NSAccessibilityPriorityKey: assertive
			? @(NSAccessibilityPriorityHigh)
			: @(NSAccessibilityPriorityMedium)
	};

	id target = window ?: (id)NSApp;
	NSAccessibilityPostNotificationWithUserInfo(
		target,
		NSAccessibilityAnnouncementRequestedNotification,
		userInfo
	);

#if DEBUG
	NSLog(@"UNOAccessibility: [window %p] announce '%@' (assertive=%d)", window, announcement, assertive);
#endif
}

void uno_accessibility_post_layout_changed(NSWindow* window) {
	UNOAccessibilityContext *context = uno_a11y_context_for_window(window);
	if (context.rootElement) {
		NSAccessibilityPostNotification(context.rootElement, NSAccessibilityLayoutChangedNotification);
#if DEBUG
		NSLog(@"UNOAccessibility: [window %p] posted layout changed notification", window);
#endif
	}
}

void uno_accessibility_post_children_changed(NSWindow* window) {
	// NSAccessibilityCreatedNotification on the window is the only way
	// to make VoiceOver pick up structural changes reliably.
	if (window) {
		NSAccessibilityPostNotification(window, NSAccessibilityCreatedNotification);
#if DEBUG
		NSLog(@"UNOAccessibility: [window %p] posted children changed (created) notification", window);
#endif
	}
}

void uno_accessibility_post_live_region_created(intptr_t handle) {
	UNOAccessibilityElement *element = findElementGlobal(handle);
	if (element) {
		NSAccessibilityPostNotification(element, kAccessibilityLiveRegionCreatedNotification);
	}
}

void uno_accessibility_post_live_region_changed(intptr_t handle) {
	UNOAccessibilityElement *element = findElementGlobal(handle);
	if (element) {
		NSAccessibilityPostNotification(element, kAccessibilityLiveRegionChangedNotification);
	}
}

NSArray* uno_accessibility_get_root_children(NSWindow* window) {
	UNOAccessibilityContext *context = uno_a11y_context_for_window(window);
	if (!context.rootElement) {
		return nil;
	}
	return @[context.rootElement];
}

id uno_accessibility_get_focused_element(NSWindow* window) {
	UNOAccessibilityContext *context = uno_a11y_context_for_window(window);
	return context.focusedElement;
}
