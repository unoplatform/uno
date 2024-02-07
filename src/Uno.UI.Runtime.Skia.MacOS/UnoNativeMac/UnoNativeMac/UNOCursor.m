//
//  UNOCursor.m
//

#import "UNOCursor.h"

void uno_cursor_hide(void)
{
    [NSCursor hide];
}

void uno_cursor_unhide(void)
{
    [NSCursor unhide];
}

bool uno_cursor_set(CoreCursorType cursor)
{
    switch(cursor) {
        case CoreCursorTypeArrow:
            [[NSCursor arrowCursor] set];
            break;
        case CoreCursorTypeCross:
            [[NSCursor crosshairCursor] set];
            break;
        case CoreCursorTypeHand:
            [[NSCursor pointingHandCursor] set];
            break;
        case CoreCursorTypeIBeam:
            [[NSCursor IBeamCursor] set];
            break;
        case CoreCursorTypeSizeNorthSouth:
            [[NSCursor resizeUpDownCursor] set];
            break;
        case CoreCursorTypeSizeWestEast:
            [[NSCursor resizeLeftRightCursor] set];
            break;
        case CoreCursorTypeUniversalNo:
            [[NSCursor operationNotAllowedCursor] set];
            break;
        default:
            // FIXME: we could provide our own cursors for the others
            // unsupported by macOS, using default cursor
            [[NSCursor arrowCursor] set];
            return false;
    }
    return true;
}
