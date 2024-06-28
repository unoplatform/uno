//
//  UNONative.m
//

#import "UNONative.h"

static NSMutableSet<NSView*> *elements;

NSView* uno_native_create_sample(NSWindow *window, const char* _Nullable text)
{
    // no NSLabel on macOS
    NSTextField* label = [[NSTextField alloc] initWithFrame:NSMakeRect(100, 300, 200, 200)];
    label.bezeled = NO;
    label.drawsBackground = NO;
    label.editable = NO;
    label.selectable = NO;
#if DEBUG
    NSLog(@"uno_native_create_sample label #%p : %s", label, text);
#endif
    label.stringValue = [NSString stringWithUTF8String:text];

    NSView* sample = [[NSView alloc] initWithFrame:NSMakeRect(0, 0, 300, 600)];
    [sample addSubview:label];
#if DEBUG
    NSLog(@"uno_native_create_sample #%p : %@", sample, label.stringValue);
#endif
    [window.contentViewController.view addSubview:sample];
    if (!elements) {
        elements = [[NSMutableSet alloc] initWithCapacity:10];
    }
    [elements addObject:sample];
    return sample;
}

void uno_native_measure(NSView* element, double childWidth, double childHeight, double availableWidth, double availableHeight, double* width, double* height)
{
    // FIXME
    CGSize size = element.frame.size; // element.fittingSize;
    *width = size.width;
    *height = size.height;
#if DEBUG
    NSLog(@"uno_native_measure #%p : child %g x %g / available %g x %g -> %g x %g", element, childWidth, childHeight, availableWidth, availableHeight, *width, *height);
#endif
}

void uno_native_set_opacity(NSView* element, double opacity)
{
#if DEBUG
    NSLog(@"uno_native_set_opacity #%p : %g -> %g", element, element.alphaValue, opacity);
#endif
    element.alphaValue = opacity;
}

void uno_native_set_visibility(NSView* element, bool visible)
{
#if DEBUG
    NSLog(@"uno_native_set_visibility #%p : %s -> %s", element, element.hidden ? "TRUE" : "FALSE", visible ? "TRUE" : "FALSE");
#endif
    element.hidden = !visible;
}
