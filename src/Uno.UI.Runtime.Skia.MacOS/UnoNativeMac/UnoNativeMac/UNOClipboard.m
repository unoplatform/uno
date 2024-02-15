//
//  UNOClipboard.m
//

#import "UNOClipboard.h"

static clipboard_changed_fn_ptr clipboard_changed;

void uno_clipboard_clear(void)
{
    [[NSPasteboard generalPasteboard] clearContents];
}

void uno_clipboard_get_content(struct ClipboardData* data)
{
    NSPasteboard *pasteboard = [NSPasteboard generalPasteboard];

    NSString *furl = [pasteboard stringForType:NSPasteboardTypeFileURL];

    NSData *image = [pasteboard dataForType:@"public.jpeg"];
    if (image) {
        data->bitmapFormat = strdup("image/jpeg");
    } else {
        image = [pasteboard dataForType:@"public.png"];
        if (image) {
            data->bitmapFormat = strdup("image/png");
        } else {
            // TIFF is often present in addition to JPEG or PNG data, but we do not want to load it unless there's no alternative format
            image = [pasteboard dataForType:@"public.tiff"];
            if (image) {
                data->bitmapFormat = strdup("image/tiff");
            }
        }
    }

    if (image) {
#if DEBUG
        NSLog(@"uno_clipboard_get_content bitmap %s", data->bitmapFormat);
#endif
        // if we have a `public.file-url` then we can return the path to the file, which avoids loading it into the app's memory (until/if needed)
        if (furl) {
            NSURL *url = [NSURL URLWithString:furl];
            if (url.isFileURL) {
                data->bitmapPath = strdup(url.fileSystemRepresentation);
                furl = NULL;
            }
        } else {
            // we only have the binary data of the image
            data->bitmapSize = image.length;
            data->bitmapData = malloc(image.length);
            memcpy(data->bitmapData, image.bytes, data->bitmapSize);
        }
    }

    NSString *html = [pasteboard stringForType:NSPasteboardTypeHTML];
    if (html) {
        data->htmlContent = strdup([html UTF8String]);
    }
    NSString *rtf = [pasteboard stringForType:NSPasteboardTypeRTF];
    if (rtf) {
        data->rtfContent = strdup([rtf UTF8String]);
    }
    NSString *text = [pasteboard stringForType:NSPasteboardTypeString];
    if (text) {
        data->textContent = strdup([text UTF8String]);
    }
    NSString *url = [pasteboard stringForType:NSPasteboardTypeURL];
    if (url) {
        data->uri = strdup([url UTF8String]);
    }

    if (furl) {
        NSURL *url = [NSURL fileURLWithPath:furl];
        if (url.isFileURL) {
            data->fileUrl = strdup(furl.UTF8String);
        }
#if DEBUG
    NSLog(@"NSPasteboardTypeFileURL %@", furl);
#endif
    }
}

//bool uno_clipboard_set_content(char* htmlContent, char* rtfContent, char* textContent, char* uri)
bool uno_clipboard_set_content(struct ClipboardData* data)
{
    int arraySize = 0;
    if (data->htmlContent)
        arraySize++;
    if (data->rtfContent)
        arraySize++;
    if (data->textContent)
        arraySize++;
    if (data->uri)
        arraySize++;

    int pos = 0;
    NSMutableArray *types = [NSMutableArray arrayWithCapacity:arraySize];
    if (data->htmlContent)
        types[pos++] = NSPasteboardTypeHTML;
    if (data->rtfContent)
        types[pos++] = NSPasteboardTypeRTF;
    if (data->textContent)
        types[pos++] = NSPasteboardTypeString;
    if (data->uri)
        types[pos++] = NSPasteboardTypeURL;
    NSPasteboard *pasteboard = [NSPasteboard generalPasteboard];
    [pasteboard declareTypes:types owner:nil];

    bool result = TRUE;
    if (data->htmlContent) {
        NSString *content = [NSString stringWithUTF8String:data->htmlContent];
        result &= [pasteboard setString:content forType:NSPasteboardTypeHTML];
    }
    if (data->rtfContent) {
        NSString *content = [NSString stringWithUTF8String:data->rtfContent];
        result &= [pasteboard setString:content forType:NSPasteboardTypeRTF];
    }
    if (data->textContent) {
        NSString *content = [NSString stringWithUTF8String:data->textContent];
        result &= [pasteboard setString:content forType:NSPasteboardTypeString];
    }
    if (data->uri) {
        NSString *content = [NSString stringWithUTF8String:data->uri];
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
