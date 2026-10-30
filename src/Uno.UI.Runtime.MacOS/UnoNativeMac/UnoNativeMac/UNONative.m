//
//  UNONative.m
//

#import "UNONative.h"

static NSMutableSet<NSView*> *elements;
static NSMutableSet<NSView*> *transients;

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

@end

@implementation UNONativeViewHost {
    NSRect _originalFrame;
    NSAutoresizingMaskOptions _originalAutoresizingMask;
}

@synthesize originalSuperView;

- (instancetype)initWithHostedView:(NSView*)view
{
    NSRect frame = view.frame;
    self = [super initWithFrame:NSMakeRect(0, 0, frame.size.width, frame.size.height)];
    if (self) {
        _originalFrame = frame;
        _originalAutoresizingMask = view.autoresizingMask;
        // layer-backed so `uno_window_clip_svg` can mask it
        self.wantsLayer = YES;
        [view removeFromSuperview];
        view.frame = self.bounds;
        view.autoresizingMask = NSViewWidthSizable | NSViewHeightSizable;
        [self addSubview:view];
        _hostedView = view;
    }
    return self;
}

- (BOOL)isFlipped
{
    return YES;
}

- (NSSize)naturalSize
{
    NSSize intrinsic = self.hostedView.intrinsicContentSize;
    return NSMakeSize(intrinsic.width == NSViewNoIntrinsicMetric ? _originalFrame.size.width : intrinsic.width,
                      intrinsic.height == NSViewNoIntrinsicMetric ? _originalFrame.size.height : intrinsic.height);
}

- (void)dispose {
    NSView* view = self.hostedView;
#if DEBUG
    NSLog(@"UNONativeViewHost %p disposing hosted view %p with superview %p", self, view, self.superview);
#endif
    if (self.superview) {
        [self removeFromSuperview];
    }
    // Hand the view back to the app as it was wrapped, unless another host or the app already took it.
    if (view.superview == self) {
        [view removeFromSuperview];
        view.autoresizingMask = _originalAutoresizingMask;
        view.frame = _originalFrame;
    }
    self.hostedView = nil;
}

@end

void uno_native_track(NSView<UNONativeElement>* element)
{
    if (!transients) {
        transients = [[NSMutableSet alloc] initWithCapacity:10];
    }
    [transients addObject:element];
}

NSView* uno_native_create_sample(const char* _Nullable text)
{
    // no NSLabel on macOS
    NSTextField* label = [[NSTextField alloc] initWithFrame:NSMakeRect(0, 0, 100, 100)];
    label.bezeled = NO;
    label.drawsBackground = NO;
    label.editable = NO;
    label.selectable = NO;
    label.stringValue = text ? [NSString stringWithUTF8String:text] : @"";
    label.frame = NSMakeRect(0, 0, label.fittingSize.width, label.fittingSize.height);

    UNORedView* sample = [[UNORedView alloc] initWithFrame:label.frame];
    [sample addSubview:label];
#if DEBUG
    NSLog(@"uno_native_create_sample #%p label: %@", sample, label.stringValue);
#endif
    return sample;
}

NSView* _Nullable uno_native_create_host(id view)
{
    // anything else, e.g. an NSWindow, would raise an Objective-C exception that aborts the process
    if (![view isKindOfClass:NSView.class]) {
        return nil;
    }
    UNONativeViewHost* host = [[UNONativeViewHost alloc] initWithHostedView:view];
#if DEBUG
    NSLog(@"uno_native_create_host #%p hosting %p", host, view);
#endif
    uno_native_track(host);
    return host;
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

void uno_native_attach(NSView<UNONativeElement>* element, NSWindow* window)
{
#if DEBUG
    NSLog(@"!!uno_native_attach %p window %p", element, window);
#endif
    // set on every attach, so the element follows its ContentPresenter to another window
    element.originalSuperView = ((UNOWindow*)window).renderingView;
    bool already_attached = NO;
    if (!elements) {
        elements = [[NSMutableSet alloc] initWithCapacity:10];
    } else {
        already_attached = [elements containsObject:element];
    }
#if DEBUG
    NSLog(@"uno_native_attach %p -> %s attached", element, already_attached ? "already" : "not previously");
#endif
    if (!already_attached) {
        // note: it's too early to add a mask since the layer has not been set yet
        [elements addObject:element];
    }
    // `elements` owns it again, so drop the reference `uno_native_detach` took to survive the round trip.
    [transients removeObject:element];
    [element.originalSuperView addSubview:element];
}

void uno_native_detach(NSView<UNONativeElement>* element)
{
#if DEBUG
    NSLog(@"uno_native_detach %p", element);
#endif
    element.layer.mask = nil;

    // once removed from superview the instance can be freed by the runtime unless we keep another reference to it
    uno_native_track(element);
    [elements removeObject:element];
    [element removeFromSuperview];
}

bool uno_native_is_attached(NSView<UNONativeElement>* element)
{
    bool attached = elements ? [elements containsObject:element] : NO;
#if DEBUG
    NSLog(@"uno_native_is_attached %s", attached ? "YES" : "NO");
#endif
    return attached;
}

void uno_native_measure(NSView<UNONativeElement>* element, double childWidth, double childHeight, double availableWidth, double availableHeight, double* width, double* height)
{
    // A hosted view has no managed child, so its child size is always 0: an unconstrained axis takes its natural size.
    bool hasNaturalSize = [element respondsToSelector:@selector(naturalSize)];
    double resolvedWidth = isfinite(availableWidth) ? availableWidth : (isfinite(childWidth) && !hasNaturalSize ? childWidth : -1.0);
    double resolvedHeight = isfinite(availableHeight) ? availableHeight : (isfinite(childHeight) && !hasNaturalSize ? childHeight : -1.0);
    if (resolvedWidth < 0 || resolvedHeight < 0)
    {
        CGSize size = hasNaturalSize ? [element naturalSize] : element.subviews.firstObject.frame.size;
        if (resolvedWidth < 0)
        {
            resolvedWidth = size.width;
        }
        if (resolvedHeight < 0)
        {
            resolvedHeight = size.height;
        }
    }

    *width = resolvedWidth;
    *height = resolvedHeight;
#if DEBUG
    NSLog(@"uno_native_measure %p : child %g x %g / available %g x %g -> %g x %g", element, childWidth, childHeight, availableWidth, availableHeight, *width, *height);
#endif
}

void uno_native_set_opacity(NSView<UNONativeElement>* element, double opacity)
{
#if DEBUG
    NSLog(@"uno_native_set_opacity #%p : %g -> %g", element, element.alphaValue, opacity);
#endif
    element.alphaValue = opacity;
}

void uno_native_dispose(NSView<UNONativeElement>* element)
{
#if DEBUG
    NSLog(@"uno_native_dispose #%p", element);
#endif
    if (!element) {
        return;
    }
    [element dispose];
    // Disposal is terminal: drop BOTH strong references, so the view cannot be resurrected through
    // `elements` by a later attach. `transients` alone is not enough — an element detached before it
    // is disposed has already been moved out of `elements`, and one disposed while still attached
    // would otherwise stay retained there forever.
    [transients removeObject:element];
    [elements removeObject:element];
}
