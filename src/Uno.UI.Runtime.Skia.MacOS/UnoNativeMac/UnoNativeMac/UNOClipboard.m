//
//  UNOClipboard.m
//

#import "UNOClipboard.h"

static clipboard_changed_fn_ptr clipboard_changed;

void uno_clipboard_clear(void)
{
    [[NSPasteboard generalPasteboard] clearContents];
}

void uno_clipboard_get_content(const char** htmlContent, const char** rtfContent, const char** textContent, const char** uri, const char** fileUrl)
{
    NSPasteboard *pasteboard = [NSPasteboard generalPasteboard];
    NSString *html = [pasteboard stringForType:NSPasteboardTypeHTML];
    if (html) {
        *htmlContent = strdup([html UTF8String]);
    }
    NSString *rtf = [pasteboard stringForType:NSPasteboardTypeRTF];
    if (rtf) {
        *rtfContent = strdup([rtf UTF8String]);
    }
    NSString *text = [pasteboard stringForType:NSPasteboardTypeString];
    if (text) {
        *textContent = strdup([text UTF8String]);
    }
    NSString *url = [pasteboard stringForType:NSPasteboardTypeURL];
    if (url) {
        *uri = strdup([url UTF8String]);
    }
    NSString *furl = [pasteboard stringForType:NSPasteboardTypeFileURL];
    if (furl) {
        *fileUrl = strdup([[[[NSURL fileURLWithPath:furl] filePathURL] absoluteString] UTF8String]);
    }
}

bool uno_clipboard_set_content(char* htmlContent, char* rtfContent, char* textContent, char* uri)
{
    int arraySize = 0;
    if (htmlContent)
        arraySize++;
    if (rtfContent)
        arraySize++;
    if (textContent)
        arraySize++;
    if (uri)
        arraySize++;

    int pos = 0;
    NSMutableArray *types = [NSMutableArray arrayWithCapacity:arraySize];
    if (htmlContent)
        types[pos++] = NSPasteboardTypeHTML;
    if (rtfContent)
        types[pos++] = NSPasteboardTypeRTF;
    if (textContent)
        types[pos++] = NSPasteboardTypeString;
    if (uri)
        types[pos++] = NSPasteboardTypeURL;
    NSPasteboard *pasteboard = [NSPasteboard generalPasteboard];
    [pasteboard declareTypes:types owner:nil];

    bool result = TRUE;
    if (htmlContent) {
        NSString *content = [NSString stringWithUTF8String:htmlContent];
        result &= [pasteboard setString:content forType:NSPasteboardTypeHTML];
    }
    if (rtfContent) {
        NSString *content = [NSString stringWithUTF8String:rtfContent];
        result &= [pasteboard setString:content forType:NSPasteboardTypeRTF];
    }
    if (textContent) {
        NSString *content = [NSString stringWithUTF8String:textContent];
        result &= [pasteboard setString:content forType:NSPasteboardTypeString];
    }
    if (uri) {
        NSString *content = [NSString stringWithUTF8String:uri];
        result &= [pasteboard setString:content forType:NSPasteboardTypeURL];
    }
#if DEBUG
    NSLog(@"uno_clipboard_set_content %d -> %s", arraySize, result ? "TRUE" : "FALSE");
#endif
    return result;
}

static NSTimer *timer;
static NSInteger lastCount;

void uno_clipboard_start_content_changed(void)
{
    lastCount = [[NSPasteboard generalPasteboard] changeCount];
    timer = [NSTimer scheduledTimerWithTimeInterval:1 repeats:true block:^(NSTimer * _Nonnull timer) {
        NSInteger currentCount = [[NSPasteboard generalPasteboard] changeCount];
        if (lastCount != currentCount) {
            uno_clipboard_get_content_changed_callback()();
            lastCount = currentCount;
        }
    }];
}

void uno_clipboard_stop_content_changed(void)
{
    [timer invalidate];
}

clipboard_changed_fn_ptr uno_clipboard_get_content_changed_callback(void)
{
    return clipboard_changed;
}

void uno_clipboard_set_content_changed_callback(clipboard_changed_fn_ptr p)
{
    clipboard_changed = p;
}
