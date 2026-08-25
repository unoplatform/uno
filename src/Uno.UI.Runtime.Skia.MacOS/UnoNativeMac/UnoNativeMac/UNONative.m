//
//  UNONative.m
//

#import "UNONative.h"
#import <Security/Security.h>
#include <limits.h>
#include <strings.h>

static NSMutableSet<NSView*> *elements;
static NSMutableSet<NSView*> *transients;

@implementation UNORedView : NSView

@synthesize originalSuperView;

// make the background red for easier tracking
- (BOOL)wantsUpdateLayer
{
    return !self.hidden;
}

- (void)updateLayer
{
    self.layer.backgroundColor = NSColor.redColor.CGColor;
}

- (void)detach {
    // nothing needed
}

- (void)dispose {
#if DEBUG
    NSLog(@"UNORedView %p disposing with superview %p", self, self.superview);
#endif
    if (self.superview) {
        [self removeFromSuperview];
    }
}

@end

void uno_native_track(NSView<UNONativeElement>* element)
{
    if (!transients) {
        transients = [[NSMutableSet alloc] initWithCapacity:10];
    }
    [transients addObject:element];
}

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

    UNORedView* sample = [[UNORedView alloc] initWithFrame:label.frame];
    [sample addSubview:label];
#if DEBUG
    NSLog(@"uno_native_create_sample #%p label: %@", sample, label.stringValue);
#endif
    sample.originalSuperView = ((UNOWindow*)window).renderingView;
    uno_native_track(sample);
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

void uno_native_attach(NSView<UNONativeElement>* element)
{
#if DEBUG
    NSLog(@"!!uno_native_attach %p", element);
#endif
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
    CGSize size = element.subviews.firstObject.frame.size;
    
    double resolvedWidth = isfinite(availableWidth) ? availableWidth : (isfinite(childWidth) ? childWidth : -1.0);
    double resolvedHeight = isfinite(availableHeight) ? availableHeight : (isfinite(childHeight) ? childHeight : -1.0);
    if (resolvedWidth < 0)
    {
        resolvedWidth = size.width;
    }
    if (resolvedHeight < 0)
    {
        resolvedHeight = size.height;
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

static NSDictionary* uno_password_vault_query(NSString* scope)
{
    return @{
        (__bridge id)kSecClass: (__bridge id)kSecClassGenericPassword,
        (__bridge id)kSecAttrService: @"uno_passwordvault",
        (__bridge id)kSecAttrAccount: scope
    };
}

static OSStatus uno_password_vault_copy_data(NSDictionary* query, NSData* _Nullable * _Nonnull data)
{
    NSMutableDictionary* readQuery = [query mutableCopy];
    readQuery[(__bridge id)kSecReturnData] = @YES;
    readQuery[(__bridge id)kSecMatchLimit] = (__bridge id)kSecMatchLimitOne;

    CFTypeRef result = NULL;
    OSStatus status = SecItemCopyMatching((__bridge CFDictionaryRef)readQuery, &result);
    if (status == errSecSuccess)
    {
        *data = CFBridgingRelease(result);
    }

    return status;
}

int32_t uno_password_vault_read(const char* scope, uint8_t* _Nullable * _Nonnull data, int32_t* length)
{
    *data = NULL;
    *length = 0;

    NSString* account = [NSString stringWithUTF8String:scope];
    NSData* value = nil;
    OSStatus status = uno_password_vault_copy_data(uno_password_vault_query(account), &value);

    if (status != errSecSuccess)
    {
        return status;
    }

    if (value.length > INT32_MAX)
    {
        return errSecParam;
    }

    if (value.length > 0)
    {
        *data = malloc(value.length);
        if (*data == NULL)
        {
            return errSecAllocate;
        }

        memcpy(*data, value.bytes, value.length);
    }

    *length = (int32_t)value.length;
    return errSecSuccess;
}

int32_t uno_password_vault_write(const char* scope, const uint8_t* data, int32_t length)
{
    if (length < 0 || (length > 0 && data == NULL))
    {
        return errSecParam;
    }

    NSString* account = [NSString stringWithUTF8String:scope];
    NSDictionary* query = uno_password_vault_query(account);
    NSData* value = [NSData dataWithBytes:data length:(NSUInteger)length];
    NSDictionary* update = @{
        (__bridge id)kSecValueData: value
    };

    OSStatus status = SecItemUpdate(
        (__bridge CFDictionaryRef)query,
        (__bridge CFDictionaryRef)update);
    if (status == errSecItemNotFound)
    {
        NSMutableDictionary* item = [query mutableCopy];
        item[(__bridge id)kSecValueData] = value;
        status = SecItemAdd((__bridge CFDictionaryRef)item, NULL);
    }

    return status;
}

void uno_password_vault_free(uint8_t* data, int32_t length)
{
    if (data != NULL && length > 0)
    {
        memset_s(data, (size_t)length, 0, (size_t)length);
    }
    free(data);
}
