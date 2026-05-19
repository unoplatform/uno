//
//  UNODragDrop.m
//

#import "UNODragDrop.h"

static drag_drop_fn_ptr drag_entered;
static drag_drop_fn_ptr drag_updated;
static drag_drop_fn_ptr drag_exited;
static drag_drop_fn_ptr drag_performed;

extern VirtualKeyModifiers get_modifiers(NSEventModifierFlags mods);

void uno_drag_drop_set_callbacks(drag_drop_fn_ptr entered, drag_drop_fn_ptr updated, drag_drop_fn_ptr exited, drag_drop_fn_ptr performed)
{
    drag_entered = entered;
    drag_updated = updated;
    drag_exited = exited;
    drag_performed = performed;
}

void uno_window_register_for_drag_drop(NSWindow* window)
{
    if (!window || ![window isKindOfClass:[UNOWindow class]]) {
        return;
    }
    NSView *view = ((UNOWindow*)window).renderingView;
    if (!view) {
        return;
    }
    [view registerForDraggedTypes:@[
        NSPasteboardTypeFileURL,
        NSPasteboardTypeString,
        NSPasteboardTypeHTML,
        NSPasteboardTypeRTF,
        NSPasteboardTypeURL,
        NSPasteboardTypePNG,
        NSPasteboardTypeTIFF,
    ]];
#if DEBUG
    NSLog(@"uno_window_register_for_drag_drop %@ view: %@", window, view);
#endif
}

static uint32 mask_from_ns_operation(NSDragOperation op)
{
    uint32 r = UnoDragDropOperationNone;
    if (op & NSDragOperationCopy) r |= UnoDragDropOperationCopy;
    if (op & NSDragOperationMove) r |= UnoDragDropOperationMove;
    if (op & NSDragOperationLink) r |= UnoDragDropOperationLink;
    // Some apps (e.g., Finder) advertise NSDragOperationGeneric without the specific copy/move/link bits.
    // Map Generic to Copy so the managed side accepts the drag.
    if (op & NSDragOperationGeneric) r |= UnoDragDropOperationCopy;
    return r;
}

static NSDragOperation ns_operation_from_mask(uint32 m)
{
    NSDragOperation r = NSDragOperationNone;
    if (m & UnoDragDropOperationCopy) r |= NSDragOperationCopy;
    if (m & UnoDragDropOperationMove) r |= NSDragOperationMove;
    if (m & UnoDragDropOperationLink) r |= NSDragOperationLink;
    return r;
}

static void fill_drag_drop_data(struct DragDropData* data, NSView* view, id<NSDraggingInfo> info)
{
    memset(data, 0, sizeof(struct DragDropData));

    NSPoint windowPoint = info.draggingLocation;
    NSPoint viewPoint = [view convertPoint:windowPoint fromView:nil];
    data->x = viewPoint.x;
    data->y = viewPoint.y;

    data->mods = get_modifiers([NSEvent modifierFlags]);
    data->allowedOperations = mask_from_ns_operation(info.draggingSourceOperationMask);

    NSPasteboard *pb = info.draggingPasteboard;

    // File URLs (including files dragged from Finder)
    NSArray<NSURL*>* urls = [pb readObjectsForClasses:@[[NSURL class]]
                                              options:@{ NSPasteboardURLReadingFileURLsOnlyKey : @YES }];
    if (urls.count > 0) {
        data->fileCount = (uint32)urls.count;
        data->fileUrls = (char**)calloc(urls.count, sizeof(char*));
        for (NSUInteger i = 0; i < urls.count; i++) {
            const char* p = urls[i].path.UTF8String;
            if (p) {
                data->fileUrls[i] = strdup(p);
            }
        }
    }

    NSString *html = [pb stringForType:NSPasteboardTypeHTML];
    if (html) data->htmlContent = strdup(html.UTF8String);
    NSString *rtf = [pb stringForType:NSPasteboardTypeRTF];
    if (rtf) data->rtfContent = strdup(rtf.UTF8String);
    NSString *text = [pb stringForType:NSPasteboardTypeString];
    if (text) data->textContent = strdup(text.UTF8String);
    NSString *url = [pb stringForType:NSPasteboardTypeURL];
    if (url) data->uri = strdup(url.UTF8String);

    // image drops: prefer a file path if a file URL was provided (avoids loading into memory)
    if (data->fileCount == 0) {
        NSData *image = [pb dataForType:NSPasteboardTypePNG];
        if (!image) {
            image = [pb dataForType:NSPasteboardTypeTIFF];
        }
        if (image) {
            // We don't copy large blobs into C heap here; managed side handles bitmap via fileUrls when possible.
            // Leave bitmapPath as null — text/html/rtf/uri fields cover the common cases from pasteboards.
        }
    }
}

static void free_drag_drop_data(struct DragDropData* data)
{
    if (!data) return;
    if (data->textContent) { free(data->textContent); data->textContent = NULL; }
    if (data->htmlContent) { free(data->htmlContent); data->htmlContent = NULL; }
    if (data->rtfContent)  { free(data->rtfContent);  data->rtfContent  = NULL; }
    if (data->uri)         { free(data->uri);         data->uri         = NULL; }
    if (data->bitmapPath)  { free(data->bitmapPath);  data->bitmapPath  = NULL; }
    if (data->fileUrls) {
        for (uint32 i = 0; i < data->fileCount; i++) {
            if (data->fileUrls[i]) free(data->fileUrls[i]);
        }
        free(data->fileUrls);
        data->fileUrls = NULL;
        data->fileCount = 0;
    }
}

NSDragOperation uno_drag_drop_handle_entered(NSView* view, id<NSDraggingInfo> info)
{
    if (!drag_entered) {
        return NSDragOperationNone;
    }
    struct DragDropData data;
    fill_drag_drop_data(&data, view, info);
    uint32 accepted = drag_entered(view.window, &data);
    free_drag_drop_data(&data);
#if DEBUG
    NSLog(@"uno_drag_drop_handle_entered at (%g,%g) accepted: 0x%x", data.x, data.y, accepted);
#endif
    return ns_operation_from_mask(accepted);
}

NSDragOperation uno_drag_drop_handle_updated(NSView* view, id<NSDraggingInfo> info)
{
    if (!drag_updated) {
        return NSDragOperationNone;
    }
    struct DragDropData data;
    memset(&data, 0, sizeof(struct DragDropData));
    NSPoint windowPoint = info.draggingLocation;
    NSPoint viewPoint = [view convertPoint:windowPoint fromView:nil];
    data.x = viewPoint.x;
    data.y = viewPoint.y;
    data.mods = get_modifiers([NSEvent modifierFlags]);
    data.allowedOperations = mask_from_ns_operation(info.draggingSourceOperationMask);

    uint32 accepted = drag_updated(view.window, &data);
    return ns_operation_from_mask(accepted);
}

void uno_drag_drop_handle_exited(NSView* view, id<NSDraggingInfo> info)
{
    if (!drag_exited) {
        return;
    }
    struct DragDropData data;
    memset(&data, 0, sizeof(struct DragDropData));
    drag_exited(view.window, &data);
}

BOOL uno_drag_drop_handle_performed(NSView* view, id<NSDraggingInfo> info)
{
    if (!drag_performed) {
        return NO;
    }
    struct DragDropData data;
    fill_drag_drop_data(&data, view, info);
    uint32 accepted = drag_performed(view.window, &data);
    free_drag_drop_data(&data);
    return accepted != UnoDragDropOperationNone;
}
