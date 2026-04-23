//
//  UNOSoftView.m
//

#import "UNOSoftView.h"
#import "UNOWindow.h"
#import "UnoNativeMac.h"

@implementation UNOSoftView {
    NSMutableAttributedString *_markedText;
    NSRange _markedRange;
    NSRange _selectedRange;
    NSTextInputContext *_imeInputContext;
    BOOL _keyEventHandledByIME;
}

- (instancetype)initWithFrame:(NSRect)frameRect {
    self = [super initWithFrame:frameRect];
    if (self) {
        _markedText = [[NSMutableAttributedString alloc] init];
        _markedRange = NSMakeRange(NSNotFound, 0);
        _selectedRange = NSMakeRange(0, 0);
        _imeActive = NO;
    }
    return self;
}

- (BOOL)acceptsFirstResponder {
    return YES;
}

- (void)drawRect:(NSRect)dirtyRect
{
    double width = dirtyRect.origin.x + dirtyRect.size.width;
    double height = dirtyRect.origin.y + dirtyRect.size.height;
#if DEBUG
    NSLog (@"drawRect: window %p width %f height %f", self.window, width, height);
#endif
    CGFloat scale = self.window.backingScaleFactor;

    int size = 0;
    int rowBytes = 0;
    void *data = nil;
    // call managed code
    uno_get_soft_draw_callback()((__bridge void*) self.window, width, height, &data, &rowBytes, &size);
    if (size == 0) {
        return;
    }

    CGDataProviderRef provider = CGDataProviderCreateWithData(NULL, data, size, NULL);
    CGColorSpaceRef colorspace = CGColorSpaceCreateDeviceRGB();
    CGImageRef image = CGImageCreate(width * scale,
                                     height * scale,
                                     /* bitsPerComponent */ 8,
                                     /* bitsPerPixel */ 32,
                                     /* bytesPerRow */ rowBytes,
                                     colorspace,
                                     /* CGBitmapInfo */ kCGBitmapByteOrder32Big | kCGImageAlphaPremultipliedLast,
                                     provider,
                                     /* const CGFloat *decode */ NULL,
                                     /* shouldInterpolate */ FALSE,
                                     kCGRenderingIntentDefault);
#if DEBUG_SCREENSHOT
    CFURLRef url = (__bridge CFURLRef)[NSURL fileURLWithPath:@"/Users/poupou/native.jpeg"];
    CGImageDestinationRef destination = CGImageDestinationCreateWithURL(url, kUTTypePNG, 1, NULL);
    CGImageDestinationAddImage(destination, image, nil);
    if (!CGImageDestinationFinalize(destination)) {
        NSLog(@"Failed to write image %p to %@", image, url);
    }
    CFRelease(destination);
#endif

    CGRect rect = CGRectMake(0, 0, width, height);
    CGSize content = [self.window contentRectForFrameRect: rect].size;
    rect = CGRectMake(0, content.height - rect.size.height, rect.size.width, rect.size.height);
    CGContextDrawImage(NSGraphicsContext.currentContext.CGContext, rect, image);

    CGImageRelease(image);
    CGColorSpaceRelease(colorspace);
    CGDataProviderRelease(provider);
}

- (void)setFrameSize:(NSSize)newSize
{
#if DEBUG
    NSLog(@"setFrameSize: %p %f x %f", self.window, newSize.width, newSize.height);
#endif
    [super setFrameSize:newSize];
    uno_get_resize_callback()((__bridge void*) self.window, newSize.width, newSize.height);
}

// Override inputContext to ensure we always have a valid NSTextInputContext
// when IME is active, so the input method system can intercept key events.
- (NSTextInputContext *)inputContext {
    if (_imeActive) {
        if (!_imeInputContext) {
            _imeInputContext = [[NSTextInputContext alloc] initWithClient:self];
        }
        return _imeInputContext;
    }
    return [super inputContext];
}

// When IME is active, route key events through the text input system.
// _keyEventHandledByIME tracks whether insertText:/setMarkedText: was called.
- (void)keyDown:(NSEvent *)event {
    if (_imeActive) {
        _keyEventHandledByIME = NO;
        NSTextInputContext *ctx = self.inputContext;
        if (ctx) {
            [ctx handleEvent:event];
        } else {
            [self interpretKeyEvents:@[event]];
        }
#if DEBUG
        NSLog(@"UNOSoftView keyDown: inputContext=%p handledByIME=%s event=%@",
              ctx, _keyEventHandledByIME ? "YES" : "NO", event);
#endif
    }
    // If not IME active, UNOWindow.sendEvent: handles the key event directly
}

#pragma mark - NSTextInputClient

- (void)insertText:(id)string replacementRange:(NSRange)replacementRange {
    _keyEventHandledByIME = YES;

    NSString *text;
    if ([string isKindOfClass:[NSAttributedString class]]) {
        text = [(NSAttributedString *)string string];
    } else {
        text = (NSString *)string;
    }

#if DEBUG
    NSLog(@"UNOSoftView insertText: '%@' replacementRange: {%lu, %lu}", text, (unsigned long)replacementRange.location, (unsigned long)replacementRange.length);
#endif

    // Clear marked text
    [_markedText setAttributedString:[[NSAttributedString alloc] init]];
    _markedRange = NSMakeRange(NSNotFound, 0);

    ime_insert_text_callback_fn_ptr callback = uno_get_ime_insert_text_callback();
    if (callback && text.length > 0) {
        unichar *buffer = malloc(sizeof(unichar) * text.length);
        [text getCharacters:buffer range:NSMakeRange(0, text.length)];
        callback((UNOWindow *)self.window, buffer, (int32_t)text.length);
        free(buffer);
    }
}

- (void)setMarkedText:(id)string selectedRange:(NSRange)selectedRange replacementRange:(NSRange)replacementRange {
    _keyEventHandledByIME = YES;

    NSString *text;
    if ([string isKindOfClass:[NSAttributedString class]]) {
        text = [(NSAttributedString *)string string];
        [_markedText setAttributedString:(NSAttributedString *)string];
    } else {
        text = (NSString *)string;
        [_markedText setAttributedString:[[NSAttributedString alloc] initWithString:text]];
    }

    if (text.length > 0) {
        _markedRange = NSMakeRange(0, text.length);
    } else {
        _markedRange = NSMakeRange(NSNotFound, 0);
    }
    _selectedRange = selectedRange;

#if DEBUG
    NSLog(@"UNOSoftView setMarkedText: '%@' selectedRange: {%lu, %lu}", text, (unsigned long)selectedRange.location, (unsigned long)selectedRange.length);
#endif

    ime_set_marked_text_callback_fn_ptr callback = uno_get_ime_set_marked_text_callback();
    if (callback) {
        unichar *buffer = NULL;
        if (text.length > 0) {
            buffer = malloc(sizeof(unichar) * text.length);
            [text getCharacters:buffer range:NSMakeRange(0, text.length)];
        }
        callback((UNOWindow *)self.window, buffer, (int32_t)text.length, (int32_t)selectedRange.location, (int32_t)selectedRange.length);
        if (buffer) free(buffer);
    }
}

- (void)unmarkText {
#if DEBUG
    NSLog(@"UNOSoftView unmarkText");
#endif
    [_markedText setAttributedString:[[NSAttributedString alloc] init]];
    _markedRange = NSMakeRange(NSNotFound, 0);

    ime_unmark_text_callback_fn_ptr callback = uno_get_ime_unmark_text_callback();
    if (callback) {
        callback((UNOWindow *)self.window);
    }
}

- (BOOL)hasMarkedText {
    return _markedRange.location != NSNotFound && _markedRange.length > 0;
}

- (NSRange)markedRange {
    return _markedRange;
}

- (NSRange)selectedRange {
    return _selectedRange;
}

- (NSRect)firstRectForCharacterRange:(NSRange)range actualRange:(nullable NSRangePointer)actualRange {
    ime_get_caret_rect_callback_fn_ptr callback = uno_get_ime_get_caret_rect_callback();
    if (callback) {
        double x = 0, y = 0, w = 0, h = 0;
        callback((UNOWindow *)self.window, &x, &y, &w, &h);
        // Convert from view coordinates to screen coordinates
        NSRect viewRect = NSMakeRect(x, y, w, h);
        NSRect windowRect = [self convertRect:viewRect toView:nil];
        NSRect screenRect = [self.window convertRectToScreen:windowRect];
        return screenRect;
    }
    return NSZeroRect;
}

- (NSUInteger)characterIndexForPoint:(NSPoint)point {
    return NSNotFound;
}

- (nullable NSAttributedString *)attributedSubstringForProposedRange:(NSRange)range actualRange:(nullable NSRangePointer)actualRange {
    return nil;
}

- (NSArray<NSAttributedStringKey> *)validAttributesForMarkedText {
    return @[];
}

- (void)doCommandBySelector:(SEL)selector {
    // Let the system handle unrecognized commands (e.g., moveLeft:, deleteBackward:)
    [super doCommandBySelector:selector];
}

@end
