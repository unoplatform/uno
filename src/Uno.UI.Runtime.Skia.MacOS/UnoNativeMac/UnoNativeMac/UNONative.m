//
//  UNONative.m
//

#import "UNONative.h"

static NSMutableSet<NSView*> *elements;

@implementation UNOFlippedView : NSView

// behave like UIView/UWP/WinUI, where the origin is top/left, instead of bottom/left
-(BOOL) isFlipped {
    return YES;
}

// make the background red for easier tracking
- (BOOL)wantsUpdateLayer
{
    return YES;
}

- (void)updateLayer
{
    self.layer.backgroundColor = NSColor.redColor.CGColor;
}

@end

NSView* uno_native_create_sample(NSWindow *window, const char* _Nullable text)
{
    // no NSLabel on macOS
    NSTextField* label = [[NSTextField alloc] initWithFrame:NSMakeRect(0, 0, 100, 100)];
    label.bezeled = NO;
    label.drawsBackground = NO;
    label.editable = NO;
    label.selectable = NO;
    label.stringValue = [NSString stringWithUTF8String:text];
    label.frame = NSMakeRect(0, 0, label.fittingSize.width, label.fittingSize.height);

    NSView* sample = [[UNOFlippedView alloc] initWithFrame:label.frame];
    [sample addSubview:label];
#if DEBUG
    NSLog(@"uno_native_create_sample #%p label: %@", sample, label.stringValue);
#endif
    [window.contentViewController.view addSubview:sample];
    return sample;
}

void uno_native_arrange(NSView *element, double arrangeLeft, double arrangeTop, double arrangeWidth, double arrangeHeight, double clipLeft, double clipTop, double clipWidth, double clipHeight)
{
#if DEBUG
    NSLog(@"uno_native_arrange %p arrange(%g,%g,%g,%g) clip(%g,%g,%g,%g)", element, arrangeLeft, arrangeTop, arrangeWidth, arrangeHeight, clipLeft, clipTop, clipWidth, clipHeight);
#endif
    element.frame = NSMakeRect(arrangeLeft, arrangeTop, arrangeWidth, arrangeWidth);
    // TODO
}

void uno_native_attach(NSView* element)
{
#if DEBUG
    NSLog(@"uno_native_attach %p -> %s attached", element, [elements containsObject:element] ? "already" : "not");
#endif
    if (!elements) {
        elements = [[NSMutableSet alloc] initWithCapacity:10];
    }
    [elements addObject:element];
}

void uno_native_detach(NSView *element)
{
#if DEBUG
    NSLog(@"uno_native_detach %p", element);
#endif
    if (elements) {
        [elements removeObject:element];
    }
}

bool uno_native_is_attached(NSView* element)
{
    bool attached = elements ? [elements containsObject:element] : NO;
#if DEBUG
    NSLog(@"uno_native_is_attached %s", attached ? "YES" : "NO");
#endif
    return attached;
}

void uno_native_measure(NSView* element, double childWidth, double childHeight, double availableWidth, double availableHeight, double* width, double* height)
{
    // FIXME
    CGSize size = element.subviews.firstObject.frame.size; // element.fittingSize;
    *width = size.width;
    *height = size.height;
#if DEBUG
    NSLog(@"uno_native_measure %p : child %g x %g / available %g x %g -> %g x %g", element, childWidth, childHeight, availableWidth, availableHeight, *width, *height);
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
