//
//  UNOClipboard.m
//

#import "UNOClipboard.h"

void uno_clipboard_clear(void)
{
    [[NSPasteboard generalPasteboard] clearContents];
}

void* uno_clipboard_get_content(void)
{
    // TODO
    return nil;
}

void uno_clipboard_set_content(void* htmlContent)
{
    // TODO
    NSPasteboard *pasteboard = [NSPasteboard generalPasteboard];
    NSMutableArray *types = [NSMutableArray arrayWithCapacity:10];
    [pasteboard declareTypes:types owner:nil];
//    [pasteboard setString:<#(nonnull NSString *)#> forType:<#(nonnull NSPasteboardType)#>]
}

static NSTimer *timer;
static NSInteger lastCount;

void uno_clipboard_start_content_changed(void)
{
    lastCount = [[NSPasteboard generalPasteboard] changeCount];
    timer = [NSTimer scheduledTimerWithTimeInterval:1 repeats:true block:^(NSTimer * _Nonnull timer) {
        NSInteger currentCount = [[NSPasteboard generalPasteboard] changeCount];
        if (lastCount != currentCount) {
            // TODO raise event in managed code
            // uno_clipboard_on_content_changed()();
            lastCount = currentCount;
        }
    }];
}

void uno_clipboard_stop_content_changed(void)
{
    [timer invalidate];
}
