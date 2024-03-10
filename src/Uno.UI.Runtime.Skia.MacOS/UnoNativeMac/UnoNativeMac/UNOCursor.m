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

// adapted from https://github.com/libsdl-org/SDL/blob/76defc5c82204707e1d11a53a561a789d3f1e769/src/video/cocoa/SDL_cocoamouse.m#L111
NSCursor* load_system_cursor(NSString* cursorName)
{
    NSString *cursorPath = [@"/System/Library/Frameworks/ApplicationServices.framework/Versions/A/Frameworks/HIServices.framework/Versions/A/Resources/cursors" stringByAppendingPathComponent:cursorName];
    NSDictionary *info = [NSDictionary dictionaryWithContentsOfFile:[cursorPath stringByAppendingPathComponent:@"info.plist"]];

    const int frames = (int)[[info valueForKey:@"frames"] integerValue];
    NSImage *image = [[NSImage alloc] initWithContentsOfFile:[cursorPath stringByAppendingPathComponent:@"cursor.pdf"]];
    if ((image == nil) || (image.isValid == NO)) {
        return nil;
    }

    // `busybutclickable` has several images for an animation (not supported, the first image is used as the static cursor)
    if (frames > 1) {
        const NSCompositingOperation operation = NSCompositingOperationCopy;
        const NSSize cropped_size = NSMakeSize(image.size.width, (int)(image.size.height / frames));
        NSImage *cropped = [[NSImage alloc] initWithSize:cropped_size];
        if (cropped == nil) {
            return nil;
        }

        [cropped lockFocus];
        const NSRect cropped_rect = NSMakeRect(0, 0, cropped_size.width, cropped_size.height);
        [image drawInRect:cropped_rect fromRect:cropped_rect operation:operation fraction:1];
        [cropped unlockFocus];
        image = cropped;
    }

    return [[NSCursor alloc] initWithImage:image hotSpot:NSMakePoint([[info valueForKey:@"hotx"] doubleValue], [[info valueForKey:@"hoty"] doubleValue])];
}

// macOS ships with more cursors than what AppKit API provides :(
bool uno_cursor_set(CoreCursorType cursorType)
{
    NSCursor *cursor;
    bool as_requested = true;

    switch(cursorType) {
        case CoreCursorTypeArrow:
            cursor = [NSCursor arrowCursor];
            break;
        case CoreCursorTypeCross:
            cursor = [NSCursor crosshairCursor];
            break;
        case CoreCursorTypeCustom:
            // TODO: unsupported by host
            as_requested = false;
            break;
        case CoreCursorTypeHand:
            cursor = [NSCursor pointingHandCursor];
            break;
        case CoreCursorTypeHelp:
            cursor = load_system_cursor(@"help");
            break;
        case CoreCursorTypeIBeam:
            cursor = [NSCursor IBeamCursor];
            break;
        case CoreCursorTypeSizeAll:
            cursor = load_system_cursor(@"move");
            break;
        case CoreCursorTypeSizeNortheastSouthwest:
            cursor = load_system_cursor(@"resizenortheastsouthwest");
            break;
        case CoreCursorTypeSizeNorthSouth:
            cursor = [NSCursor resizeUpDownCursor];
            break;
        case CoreCursorTypeSizeNorthwestSoutheast:
            cursor = load_system_cursor(@"resizenorthwestsoutheast");
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
            as_requested = false;
            break;
        case CoreCursorTypeWait:
            cursor = load_system_cursor(@"busybutclickable");
            break;
        case CoreCursorTypePin:
            // unsupported by macOS
            // TODO: provide custom cursor ?
            as_requested = false;
            break;
        case CoreCursorTypePerson:
            // unsupported by macOS
            // TODO: provide custom cursor ?
            as_requested = false;
            break;
        default:
            as_requested = false;
            break;
    }

    if (!as_requested) {
#if DEBUG
        NSLog(@"uno_cursor_set could not be set to value %d", cursorType);
#endif
        cursor = [NSCursor arrowCursor];
    }
    [cursor set];
    return true;
}
