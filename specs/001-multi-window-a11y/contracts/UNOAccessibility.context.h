/*
 * CONTRACT — Phase 1 design artifact.
 * Not built in-place; the shipped header lives at
 *   src/Uno.UI.Runtime.Skia.MacOS/UnoNativeMac/UnoNativeMac/UNOAccessibility.h
 *
 * Feature: 001-multi-window-a11y
 * Delivered in: PR 2 (macOS native per-window context)
 *
 * Summarizes the native C surface changes required to replace
 * process-global g_elements / g_rootElement / g_focusedElement with a
 * per-NSWindow UNOAccessibilityContext attached via objc_setAssociatedObject.
 */

#pragma once

#import <AppKit/AppKit.h>
#import "UNOAccessibilityElement.h"

NS_ASSUME_NONNULL_BEGIN

/*
 * Per-window accessibility state container.
 *
 * One UNOAccessibilityContext exists per open NSWindow that hosts an Uno
 * content tree. Attached to the NSWindow via objc_setAssociatedObject so
 * the context's lifetime follows the NSWindow.
 */
@interface UNOAccessibilityContext : NSObject

@property (weak, nullable) NSWindow *window;
@property (strong) NSMutableDictionary<NSNumber*, UNOAccessibilityElement*> *elements;
@property (strong, nullable) UNOAccessibilityElement *rootElement;
@property (strong, nullable) UNOAccessibilityElement *focusedElement;

- (instancetype)initWithWindow:(NSWindow *)window;

@end

/*
 * Context lookup helper.
 *
 * Returns the context associated with the given window, or nil if none has
 * been initialized yet. Callers must tolerate nil (e.g., native events that
 * fire during teardown).
 */
UNOAccessibilityContext* _Nullable uno_a11y_context_for_window(NSWindow *window);

/*
 * Public C entry points — replace the process-global functions today.
 *
 * All functions that previously operated on g_elements / g_rootElement /
 * g_focusedElement now take an NSWindow* and resolve to the window's
 * UNOAccessibilityContext. Functions that operate on an existing element
 * may resolve the window from the element's unoParent chain.
 */

// Initialization / teardown
void uno_accessibility_init_context(NSWindow *window);
void uno_accessibility_destroy_context(NSWindow *window);

// Tree construction — all take the owning window
void uno_accessibility_create_element(
    NSWindow *window,
    intptr_t handle,
    intptr_t parentHandle,
    bool isRoot);

void uno_accessibility_remove_element(NSWindow *window, intptr_t handle);

// Element attribute mutations — resolve window from element's back-pointer
void uno_accessibility_set_label(intptr_t handle, const char *label);
void uno_accessibility_set_value(intptr_t handle, const char *value);
void uno_accessibility_set_role(intptr_t handle, const char *role);
// ... (other existing per-element setters unchanged in signature;
//      internally they resolve the owning context via the element's window back-pointer.)

// Event announcements — take the owning window
void uno_accessibility_post_layout_changed(NSWindow *window);
void uno_accessibility_post_announcement(
    NSWindow *window,
    const char *text,
    bool assertive);

// Focus
void uno_accessibility_set_focus(NSWindow *window, intptr_t handle);

/*
 * Lifecycle invariants (enforced by callers and by context teardown):
 *
 * 1. uno_accessibility_init_context(window) must be called before any other
 *    accessibility call that references the window. Managed callers invoke
 *    this from MacOSAccessibility's constructor.
 *
 * 2. uno_accessibility_destroy_context(window) must be called before the
 *    NSWindow is released by managed code. Managed callers invoke this
 *    from MacOSAccessibility.Dispose() before clearing the window handle.
 *    After destroy, the context is detached from the NSWindow and all
 *    elements previously registered in it are unreachable.
 *
 * 3. Element handles (process-unique GCHandle intptrs) never collide
 *    across contexts. An element registered in window A's context is
 *    never present in window B's context.
 *
 * 4. The NSAccessibility parent fallback for root elements is the
 *    context's associated NSWindow (replacing the previous g_window
 *    global).
 */

NS_ASSUME_NONNULL_END
