//
//  UNONative.m
//

#import "UNONative.h"

static NSMutableSet<NSView*> *elements;

@implementation UNORedView : NSView

// make the background red for easier tracking
- (BOOL)wantsUpdateLayer
{
    return !self.hidden;
}

- (void)updateLayer
{
    self.layer.backgroundColor = NSColor.redColor.CGColor;
}

@synthesize visible;

- (void)detach {
    // nothing needed
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

    NSView* sample = [[UNORedView alloc] initWithFrame:label.frame];
    [sample addSubview:label];
#if DEBUG
    NSLog(@"uno_native_create_sample #%p label: %@", sample, label.stringValue);
#endif
    [window.contentViewController.view addSubview:sample];
    return sample;
}

void uno_native_arrange(NSView<UNONativeElement> *element, double arrangeLeft, double arrangeTop, double arrangeWidth, double arrangeHeight)
{
    NSRect arrange = NSMakeRect(arrangeLeft, arrangeTop, arrangeWidth, arrangeHeight);
    element.frame = arrange;
#if DEBUG
    NSLog(@"uno_native_arrange %p arrange(%g,%g,%g,%g)", element,
          arrangeLeft, arrangeTop, arrangeWidth, arrangeHeight);
#endif
}

void uno_native_attach(NSView* element)
{
#if DEBUG
    NSLog(@"uno_native_attach %p -> %s attached", element, [elements containsObject:element] ? "already" : "not previously");
#endif
    if (!elements) {
        elements = [[NSMutableSet alloc] initWithCapacity:10];
    }
    // note: it's too early to add a mask since the layer has not been set yet
    [elements addObject:element];
}

void uno_native_detach(NSView *element)
{
#if DEBUG
    NSLog(@"uno_native_detach %p", element);
#endif
    element.layer.mask = nil;
    if (elements) {
        if ([element conformsToProtocol:@protocol(UNONativeElement)]) {
            id<UNONativeElement> native = (id<UNONativeElement>) element;
            [native detach];
        }
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
    CGSize size = element.subviews.firstObject.frame.size;
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

void uno_native_set_visibility(NSView<UNONativeElement>* element, bool visible)
{
#if DEBUG
    NSLog(@"uno_native_set_visibility #%p : hidden %s -> visible %s", element, element.hidden ? "TRUE" : "FALSE", visible ? "TRUE" : "FALSE");
#endif
    element.visible = visible;
    // hidden is controlled by both visible and clipping
    if (!visible)
        element.hidden = true;
}
