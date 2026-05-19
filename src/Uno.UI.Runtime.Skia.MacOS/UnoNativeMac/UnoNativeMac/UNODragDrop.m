//
//  UNODragDrop.m
//

#import "UNODragDrop.h"

static drag_drop_fn_ptr drag_entered;
static drag_drop_fn_ptr drag_updated;
static drag_drop_fn_ptr drag_exited;
static drag_drop_fn_ptr drag_performed;
static drag_session_ended_fn_ptr drag_session_ended;

// Per-view allowed operations for the currently running outbound session.
// macOS calls draggingSession:sourceOperationMaskForDraggingContext: on the source
// each time the dragging target context changes; we remember the value we want to
// advertise for the session currently originating from this view.
static NSMapTable<NSView*, NSNumber*>* active_source_masks;

extern VirtualKeyModifiers get_modifiers(NSEventModifierFlags mods);

void uno_drag_drop_set_callbacks(drag_drop_fn_ptr entered, drag_drop_fn_ptr updated, drag_drop_fn_ptr exited, drag_drop_fn_ptr performed)
{
    drag_entered = entered;
    drag_updated = updated;
    drag_exited = exited;
    drag_performed = performed;
}

void uno_drag_drop_set_session_ended_callback(drag_session_ended_fn_ptr endedCallback)
{
    drag_session_ended = endedCallback;
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
    if (html) {
        const char* c = html.UTF8String;
        if (c) data->htmlContent = strdup(c);
    }
    NSString *rtf = [pb stringForType:NSPasteboardTypeRTF];
    if (rtf) {
        const char* c = rtf.UTF8String;
        if (c) data->rtfContent = strdup(c);
    }
    NSString *text = [pb stringForType:NSPasteboardTypeString];
    if (text) {
        const char* c = text.UTF8String;
        if (c) data->textContent = strdup(c);
    }
    NSString *url = [pb stringForType:NSPasteboardTypeURL];
    if (url) {
        const char* c = url.UTF8String;
        if (c) data->uri = strdup(c);
    }

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

#pragma mark - Outbound drag source

static NSMapTable<NSView*, NSNumber*>* ensure_active_source_masks(void)
{
    if (!active_source_masks) {
        active_source_masks = [NSMapTable mapTableWithKeyOptions:NSPointerFunctionsWeakMemory
                                                    valueOptions:NSPointerFunctionsStrongMemory];
    }
    return active_source_masks;
}

NSDragOperation uno_drag_source_operation_mask(NSView* view, NSDraggingContext context)
{
    NSNumber* mask = [ensure_active_source_masks() objectForKey:view];
    if (mask) {
        return (NSDragOperation)mask.unsignedIntegerValue;
    }
    // Default to a generic copy when no session is known — matches macOS conventions.
    return NSDragOperationCopy;
}

void uno_drag_source_session_ended(NSView* view, NSDragOperation operation)
{
    [ensure_active_source_masks() removeObjectForKey:view];

    if (!drag_session_ended) {
        return;
    }
    uint32 op = mask_from_ns_operation(operation);
    drag_session_ended(view.window, op);
}

BOOL uno_drag_start(NSWindow* window, struct DragSourceData* data)
{
    if (!window || !data) {
        return NO;
    }
    if (![window isKindOfClass:[UNOWindow class]]) {
        return NO;
    }
    NSView* view = ((UNOWindow*)window).renderingView;
    if (!view) {
        return NO;
    }

    // beginDraggingSessionWithItems must be called from a mouse-event context.
    NSEvent* event = [NSApp currentEvent];
    NSEventType type = event.type;
    BOOL isMouse =
        type == NSEventTypeLeftMouseDown    || type == NSEventTypeLeftMouseUp    ||
        type == NSEventTypeRightMouseDown   || type == NSEventTypeRightMouseUp   ||
        type == NSEventTypeOtherMouseDown   || type == NSEventTypeOtherMouseUp   ||
        type == NSEventTypeLeftMouseDragged || type == NSEventTypeRightMouseDragged ||
        type == NSEventTypeOtherMouseDragged|| type == NSEventTypeMouseMoved;
    if (!event || !isMouse) {
        // AppKit rejects beginDraggingSession without a mouse-tracking event — surface this
        // to the managed side so it can signal "None" rather than silently failing.
#if DEBUG
        NSLog(@"uno_drag_start: no current mouse event, aborting");
#endif
        return NO;
    }

    NSPasteboardItem* pbItem = [[NSPasteboardItem alloc] init];

    if (data->textContent) {
        NSString* s = [NSString stringWithUTF8String:data->textContent];
        if (s) {
            [pbItem setString:s forType:NSPasteboardTypeString];
        }
    }
    if (data->htmlContent) {
        NSString* s = [NSString stringWithUTF8String:data->htmlContent];
        if (s) {
            [pbItem setString:s forType:NSPasteboardTypeHTML];
        }
    }
    if (data->rtfContent) {
        NSString* s = [NSString stringWithUTF8String:data->rtfContent];
        if (s) {
            [pbItem setString:s forType:NSPasteboardTypeRTF];
        }
    }
    if (data->uri) {
        NSString* s = [NSString stringWithUTF8String:data->uri];
        if (s) {
            [pbItem setString:s forType:NSPasteboardTypeURL];
        }
    }
    if (data->bitmapData && data->bitmapSize > 0) {
        NSData* blob = [NSData dataWithBytes:data->bitmapData length:data->bitmapSize];
        [pbItem setData:blob forType:NSPasteboardTypePNG];
    }

    NSMutableArray<NSDraggingItem*>* dragItems = [NSMutableArray arrayWithCapacity:1 + data->fileCount];

    NSImage* dragImage = nil;
    if (data->bitmapData && data->bitmapSize > 0) {
        NSData* blob = [NSData dataWithBytes:data->bitmapData length:data->bitmapSize];
        dragImage = [[NSImage alloc] initWithData:blob];
    }
    if (!dragImage) {
        // macOS requires a non-nil image for the dragging item; use a small transparent
        // placeholder and let the destination app render its own feedback.
        NSSize sz = NSMakeSize(32, 32);
        dragImage = [[NSImage alloc] initWithSize:sz];
        [dragImage lockFocus];
        [[NSColor colorWithWhite:0 alpha:0] setFill];
        NSRectFill(NSMakeRect(0, 0, sz.width, sz.height));
        [dragImage unlockFocus];
    }

    NSPoint mouseInWindow = event.locationInWindow;
    NSPoint mouseInView = [view convertPoint:mouseInWindow fromView:nil];
    NSSize imageSize = dragImage.size;
    NSRect frame = NSMakeRect(mouseInView.x - imageSize.width / 2.0,
                              mouseInView.y - imageSize.height / 2.0,
                              imageSize.width,
                              imageSize.height);

    // Primary item: non-file payloads (text/html/rtf/uri/bitmap).
    // If only files are being dragged we skip this item.
    BOOL hasPrimaryPayload = (data->textContent || data->htmlContent ||
                              data->rtfContent || data->uri ||
                              (data->bitmapData && data->bitmapSize > 0));
    if (hasPrimaryPayload) {
        NSDraggingItem* item = [[NSDraggingItem alloc] initWithPasteboardWriter:pbItem];
        [item setDraggingFrame:frame contents:dragImage];
        [dragItems addObject:item];
    }

    // File URLs — one NSDraggingItem per file so Finder/other targets see them individually.
    if (data->fileUrls && data->fileCount > 0) {
        for (uint32 i = 0; i < data->fileCount; i++) {
            const char* path = data->fileUrls[i];
            if (!path) continue;
            NSString* s = [NSString stringWithUTF8String:path];
            NSURL* url = [NSURL fileURLWithPath:s];
            if (!url) continue;
            NSDraggingItem* item = [[NSDraggingItem alloc] initWithPasteboardWriter:url];
            [item setDraggingFrame:frame contents:dragImage];
            [dragItems addObject:item];
        }
    }

    if (dragItems.count == 0) {
#if DEBUG
        NSLog(@"uno_drag_start: no drag items produced, aborting");
#endif
        return NO;
    }

    // Remember the allowed operation mask for this session — queried back by
    // draggingSession:sourceOperationMaskForDraggingContext: on the view.
    NSDragOperation mask = ns_operation_from_mask(data->allowedOperations);
    if (mask == NSDragOperationNone) {
        mask = NSDragOperationCopy;
    }
    [ensure_active_source_masks() setObject:@((NSUInteger)mask) forKey:view];

    id<NSDraggingSource> source = (id<NSDraggingSource>)view;
    NSDraggingSession* session = [view beginDraggingSessionWithItems:dragItems event:event source:source];
#if DEBUG
    NSLog(@"uno_drag_start: session %@ started with %lu item(s), mask 0x%lx", session, (unsigned long)dragItems.count, (unsigned long)mask);
#endif
    return session != nil;
}
