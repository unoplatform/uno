//
//  UNOCursor.h
//

#pragma once

#import "UnoNativeMac.h"

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
void uno_cursor_show(void);
bool uno_cursor_set(CoreCursorType cursorType);

// adapted from https://gitlab.gnome.org/GNOME/gtk/-/blob/main/gdk/macos/gdkmacoscursor.c
@interface NSCursor()
-(long long)_coreCursorType;
@end

@interface UNOCursor : NSCursor {
@private
    int type;
}

+ (instancetype)helpCursor;
+ (instancetype)sizeAllCursor;
+ (instancetype)sizeNortheastSouthwestCursor;
+ (instancetype)sizeNorthwestSoutheastCursor;
+ (instancetype)waitCursor;
@end

NS_ASSUME_NONNULL_END
