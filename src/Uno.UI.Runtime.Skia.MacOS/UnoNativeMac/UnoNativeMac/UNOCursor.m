//
//  UNOCursor.m
//

#import "UNOCursor.h"

void uno_cursor_hide(void)
{
    [NSCursor hide];
}

void uno_cursor_show(void)
{
    [NSCursor unhide];
}

// macOS ships with more cursors than what AppKit API provides :(
bool uno_cursor_set(CoreCursorType cursorType)
{
    NSCursor *cursor;
    bool result = true;

    switch(cursorType) {
        case CoreCursorTypeArrow:
            cursor = [NSCursor arrowCursor];
            break;
        case CoreCursorTypeCross:
            cursor = [NSCursor crosshairCursor];
            break;
        case CoreCursorTypeCustom:
            // TODO: unsupported by host
            cursor = [NSCursor arrowCursor]; // back to default
            result = false;
            break;
        case CoreCursorTypeHand:
            cursor = [NSCursor pointingHandCursor];
            break;
        case CoreCursorTypeHelp:
            cursor = [UNOCursor helpCursor];
            break;
        case CoreCursorTypeIBeam:
            cursor = [NSCursor IBeamCursor];
            break;
        case CoreCursorTypeSizeAll:
            cursor = [UNOCursor sizeAllCursor];
            break;
        case CoreCursorTypeSizeNortheastSouthwest:
            cursor = [UNOCursor sizeNortheastSouthwestCursor];
            break;
        case CoreCursorTypeSizeNorthSouth:
            cursor = [NSCursor resizeUpDownCursor];
            break;
        case CoreCursorTypeSizeNorthwestSoutheast:
            cursor = [UNOCursor sizeNorthwestSoutheastCursor];
            break;
        case CoreCursorTypeSizeWestEast:
            cursor = [NSCursor resizeLeftRightCursor];
            break;
        case CoreCursorTypeUniversalNo:
            cursor = [NSCursor operationNotAllowedCursor];
            break;
        case CoreCursorTypeUpArrow:
            // unsupported by macOS
            // TODO: provide custom cursor ?
            cursor = [NSCursor resizeUpCursor]; // closest approximation
            result = false;
            break;
        case CoreCursorTypeWait:
            cursor = [UNOCursor waitCursor];
            break;
        case CoreCursorTypePin:
            // unsupported by macOS
            // TODO: provide custom cursor ?
            cursor = [NSCursor pointingHandCursor]; // closest approximation
            result = false;
            break;
        case CoreCursorTypePerson:
            // unsupported by macOS
            // TODO: provide custom cursor ?
            cursor = [NSCursor pointingHandCursor]; // closest approximation
            result = false;
            break;
        default:
            cursor = [NSCursor arrowCursor];
            break;
    }

    [cursor set];
    return result;
}

@implementation UNOCursor

+ (instancetype)helpCursor {
    UNOCursor* c = [self new];
    if (c) {
        c->type = 40;
    }
    return c;
}

+ (instancetype)sizeAllCursor {
    UNOCursor* c = [self new];
    if (c) {
        c->type = 39;
    }
    return c;
}

+ (instancetype)sizeNortheastSouthwestCursor {
    UNOCursor* c = [self new];
    if (c) {
        c->type = 30;
    }
    return c;
}

+ (instancetype)sizeNorthwestSoutheastCursor {
    UNOCursor* c = [self new];
    if (c) {
        c->type = 34;
    }
    return c;
}

+ (instancetype)waitCursor {
    UNOCursor* c = [self new];
    if (c) {
        c->type = 4;
    }
    return c;
}

- (long long)_coreCursorType
{
    return self->type;
}

@end
