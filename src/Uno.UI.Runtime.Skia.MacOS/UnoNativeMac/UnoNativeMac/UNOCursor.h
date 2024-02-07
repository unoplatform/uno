//
//  UNOCursor.h
//

#pragma once

#import <AppKit/AppKit.h>
#import <Foundation/Foundation.h>

// https://learn.microsoft.com/en-us/uwp/api/windows.ui.core.corecursortype?view=winrt-22621
typedef NS_ENUM(uint32, CoreCursorType) {
    CoreCursorTypeArrow,
    CoreCursorTypeCross,
    CoreCursorTypeCustom,
    CoreCursorTypeHand,
    CoreCursorTypeHelp,
    CoreCursorTypeIBeam,
    CoreCursorTypeSizeAll,
    CoreCursorTypeSizeNortheastSouthwest,
    CoreCursorTypeSizeNorthSouth,
    CoreCursorTypeSizeNorthwestSoutheast,
    CoreCursorTypeSizeWestEast,
    CoreCursorTypeUniversalNo,
    CoreCursorTypeUpArrow,
    CoreCursorTypeWait,
    CoreCursorTypePin,
    CoreCursorTypePerson,
};

NS_ASSUME_NONNULL_BEGIN

void uno_cursor_hide(void);
void uno_cursor_unhide(void);
bool uno_cursor_set(CoreCursorType cursor);

NS_ASSUME_NONNULL_END
