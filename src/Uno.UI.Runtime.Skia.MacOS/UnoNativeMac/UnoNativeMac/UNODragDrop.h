//
//  UNODragDrop.h
//

#pragma once

#import "UnoNativeMac.h"
#import "UNOWindow.h"

NS_ASSUME_NONNULL_BEGIN

// bit flags that map to Windows.ApplicationModel.DataTransfer.DataPackageOperation
// None = 0, Copy = 1, Move = 2, Link = 4
typedef NS_OPTIONS(uint32, UnoDragDropOperation) {
    UnoDragDropOperationNone = 0,
    UnoDragDropOperationCopy = 1,
    UnoDragDropOperationMove = 2,
    UnoDragDropOperationLink = 4,
};

struct DragDropData {
    CGFloat x;
    CGFloat y;
    VirtualKeyModifiers mods;
    uint32 allowedOperations;
    // incoming fields populated from the pasteboard (entered/performed)
    char* _Nullable textContent;
    char* _Nullable htmlContent;
    char* _Nullable rtfContent;
    char* _Nullable uri;
    char* _Nullable * _Nullable fileUrls;
    uint32 fileCount;
    char* _Nullable bitmapPath;
};

// Data passed from managed to native to start an outbound drag session.
// Strings are UTF-8 and fully owned by the caller for the duration of the call.
struct DragSourceData {
    uint32 allowedOperations;
    const char* _Nullable textContent;
    const char* _Nullable htmlContent;
    const char* _Nullable rtfContent;
    const char* _Nullable uri;
    const char* _Nullable * _Nullable fileUrls;
    uint32 fileCount;
    const uint8_t* _Nullable bitmapData;
    uint32 bitmapSize;
};

typedef uint32 (*drag_drop_fn_ptr)(NSWindow* window, struct DragDropData* data);
typedef void (*drag_session_ended_fn_ptr)(NSWindow* window, uint32 operation);

void uno_drag_drop_set_callbacks(drag_drop_fn_ptr entered, drag_drop_fn_ptr updated, drag_drop_fn_ptr exited, drag_drop_fn_ptr performed);
void uno_drag_drop_set_session_ended_callback(drag_session_ended_fn_ptr endedCallback);
void uno_window_register_for_drag_drop(NSWindow* window);
BOOL uno_drag_start(NSWindow* window, struct DragSourceData* data);

// Bridge helpers invoked from NSDraggingDestination methods on the rendering views
NSDragOperation uno_drag_drop_handle_entered(NSView* view, id<NSDraggingInfo> info);
NSDragOperation uno_drag_drop_handle_updated(NSView* view, id<NSDraggingInfo> info);
void uno_drag_drop_handle_exited(NSView* view, id<NSDraggingInfo> _Nullable info);
BOOL uno_drag_drop_handle_performed(NSView* view, id<NSDraggingInfo> info);

// Bridge helpers invoked from NSDraggingSource methods on the rendering views
NSDragOperation uno_drag_source_operation_mask(NSView* view, NSDraggingContext context);
void uno_drag_source_session_ended(NSView* view, NSDragOperation operation);

NS_ASSUME_NONNULL_END
