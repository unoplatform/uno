# Contract: Mobile Accessibility Adapters

This contract is internal. It adds no public Uno API. Android and iOS expose the existing
WinUI `AutomationPeer` contract through their native accessibility frameworks.

## 1. Host lifecycle contract

Every Skia mobile XamlRoot host must implement `IAccessibilityOwner`.

```text
Host startup
  -> AccessibilityRouter.EnsureInitialized()
  -> create per-window platform adapter
  -> expose adapter through IAccessibilityOwner.Accessibility
  -> register XamlRoot -> owner
  -> AccessibilityRouter.SetActive(owner) on activation

Host close
  -> adapter.Dispose()
  -> AccessibilityRouter.NotifyDisposed(owner)
  -> unregister XamlRoot/owner
```

Required behavior:

- Router initialization occurs before the first XAML child-add callback.
- One adapter owns one XamlRoot/native window.
- Activation and disposal are idempotent.
- Delayed callbacks after disposal are ignored.
- Native tree queries remain available to automation clients even when a screen reader is off.

## 2. Tree contract

The native tree is derived from the resolved automation-peer graph.

```text
Enumerate(root):
  peer = root.GetOrCreateAutomationPeer()
  walk peer children in peer order
  if peer is Control or Content view:
      register native node
      attach eligible child nodes
  else:
      omit peer node and promote eligible descendants
```

Rules:

- `AccessibilityView.Raw` excludes the node from Control and Content views.
- Custom peer children and item peers are included.
- `EventsSource` is resolved before state or events are projected.
- A node's native parent/child relationship must match the promoted peer tree.
- Relationships are applied after all referenced node IDs exist.
- Keyboard tab order is not used as accessibility reading order.
- Off-screen/virtualized nodes follow platform visibility rules but keep deterministic identity
  while they remain realized or accessibility-required.

## 3. Pull projection contract

Android node population and iOS element getters query the live resolved peer on demand.
No exhaustive cached semantic snapshot is maintained.

Each query must be able to obtain:

```text
identity:
  AutomationId, ClassName, ControlType, LocalizedControlType

name/description:
  Name, LabeledBy, HelpText, FullDescription, ItemStatus, ItemType, Culture

state:
  Enabled, Focused, KeyboardFocusable, Offscreen, Password, Required,
  DataValid, ReadOnly, Selected, Checked, Expanded, Dialog, Peripheral

position/navigation:
  BoundingRectangle, ClickablePoint, Orientation, HeadingLevel,
  LandmarkType, PositionInSet, SizeOfSet, Level

relations:
  ControlledPeers, DescribedBy, FlowsFrom, FlowsTo, Annotations

patterns:
  every PatternInterface returned by GetPattern()
```

Query requirements:

- Execute peer access on the Uno UI thread.
- Revalidate node generation and owner before every query.
- Redact secure values before crossing into native node text or diagnostics.
- Keep AutomationId separate from the accessible name.
- Return unavailable/null/false for stale nodes instead of throwing through native callbacks.

## 4. Property invalidation contract

Mobile adapters override `NotifyPropertyChangedEventCore`:

```csharp
protected override void NotifyPropertyChangedEventCore(
    AutomationPeer peer,
    AutomationProperty property,
    object oldValue,
    object newValue)
{
    base.NotifyPropertyChangedEventCore(peer, property, oldValue, newValue);
    InvalidateResolvedNativeNode(peer, property);
}
```

The exact method name may differ, but behavior is normative:

- Base routing remains authoritative for known typed updates.
- Every routed property change invalidates the resolved native node.
- Native values are pulled again; the adapter does not trust a cached copy.
- The adapter emits a native event only when the platform requires one for announcement or
  focus; invalidation alone is sufficient for passive property re-query.
- Multiple invalidations in one dispatcher cycle are coalesced.
- A complete producer audit identifies how every live property in the capability matrix raises
  a property change or peer invalidation.

## 5. Provider action contract

Platform action IDs normalize to these internal operations:

| Operation | Required provider/state |
|-----------|-------------------------|
| Activate | Invoke, Toggle, or SelectionItem activation path |
| Toggle | `IToggleProvider` |
| Select | `ISelectionItemProvider` |
| AddToSelection | `ISelectionItemProvider` |
| RemoveFromSelection | `ISelectionItemProvider` |
| Expand | `IExpandCollapseProvider` |
| Collapse | `IExpandCollapseProvider` |
| Increment/Decrement | `IRangeValueProvider` or platform-adjustable value |
| SetRangeValue | `IRangeValueProvider` |
| SetValue/SetText | `IValueProvider` and not read-only |
| SetTextSelection | supported text provider/native input bridge |
| Scroll | `IScrollProvider` |
| ScrollIntoView | `IScrollItemProvider` |
| Realize | `IVirtualizedItemProvider` |
| Dismiss/Close | window/popup provider or explicit peer action |
| ChangeView (Phase E) | `IMultipleViewProvider` |
| Move/Resize/Rotate | Transform/Transform2 when representable |
| Custom | Explicitly mapped provider behavior with localized label |

Execution rules:

1. Resolve the live peer and EventsSource.
2. Verify the provider, enabled state, read-only state, and argument validity.
3. Marshal execution to the Uno UI thread.
4. Preserve WinUI provider exceptions internally.
5. Translate disabled/unavailable/invalid execution to native failure without crashing.
6. On success, invalidate the node and emit the applicable native event.
7. Never advertise an action that cannot execute in the current state.

## 6. Android adapter contract

`AndroidSkiaAccessibility : SkiaAccessibilityBase` coordinates the existing
`UnoExploreByTouchHelper`, which remains created and installed as the render view's
accessibility delegate before a XamlRoot exists.

Required platform behavior:

- `AndroidHost` initializes `AccessibilityRouter`.
- `AndroidSkiaXamlRootHost` implements `IAccessibilityOwner`.
- After XamlRoot creation, the host passes the existing read-only
  `IUnoSkiaRenderView.ExploreByTouchHelper` to the adapter for configuration; the adapter
  detaches its window/root/event state on disposal.
- Activity/window activation calls `AccessibilityRouter.SetActive`.
- Activity/window destruction disposes the adapter and notifies the router.
- `GetVisibleVirtualViews` enumerates the peer tree.
- `GetVirtualViewAt` returns the nearest eligible peer from accessible hit testing.
- `OnPopulateNodeForVirtualView` pulls and applies all supported node data.
- `OnPerformActionForVirtualView` dispatches by Android action ID and arguments.
- Property/bounds changes use `InvalidateVirtualView`.
- If no virtual ID exists yet, retain a pending handle invalidation and let first node
  population pull final state; do not silently drop the change.
- Structure changes use `InvalidateRoot` plus `TYPE_WINDOW_CONTENT_CHANGED`.
- Normal render invalidation does not invalidate the accessibility root.
- Surface-size/orientation changes explicitly invalidate the root once after layout.
- Native calls are marshaled to Android's main looper.

Identity:

```text
DependencyObject --weak--> virtual int ID
virtual int ID --weak--> DependencyObject
Visual.Handle -----------> virtual int ID
```

AutomationId:

- Expose a deterministic normalized resource/unique ID.
- Preserve the original AutomationId in a non-spoken machine-readable channel when supported.
- Never assign it to `ContentDescription`.

Geometry:

- Use bounds in screen coordinates.
- Include render-view location, clipping, transforms, and visibility.

## 7. iOS adapter contract

`AppleUIKitAccessibility : SkiaAccessibilityBase` owns stable managed
`UnoUIAccessibilityElement` objects.

Required platform behavior:

- `AppleUIKitHost` initializes `AccessibilityRouter`.
- `RootViewController` implements `IAccessibilityOwner`.
- `NativeWindowWrapper` (in `AppleUIKitWindowWrapper.cs`) creates, activates, and disposes
  the per-window adapter.
- The existing Skia/Metal view is the `AccessibilityContainer`.
- Its `AccessibilityElements` collection follows peer reading order.
- Each element pulls properties/actions through a weak adapter reference and stable node ID.
- Frames use `AccessibilityFrameInContainerSpace`.
- Activation, increment, decrement, scroll, escape, and custom actions route to providers.
- Property/structure changes post the narrowest appropriate layout/screen/announcement notification.
- Native calls are marshaled to the UIKit main thread.

AutomationId:

- Set `AccessibilityIdentifier`.
- Do not use the identifier as `AccessibilityLabel`.

Identity:

- Keep one element object for the lifetime of the realized node.
- Remove it from the container and registry before invalidating the logical node.
- A stale element action returns `false` or no-ops according to the UIKit callback contract.

## 8. Focus and modal contract

- XAML keyboard focus and native accessibility focus are tracked separately.
- Native focus may request XAML focus only for an eligible keyboard-focusable peer.
- XAML focus changes may request native focus without re-entry.
- A modal subtree excludes background nodes from native traversal.
- Opening a modal emits the platform's window/screen-change signal.
- Closing a modal restores a live prior target or a deterministic fallback.
- Disabling updates state/actions but does not force XAML focus away.
- Hiding/removing/recycling a native-focused node recovers native focus.

## 9. Event contract

| Event class | Native obligation |
|-------------|-------------------|
| Focus | Move/announce native accessibility focus |
| Property | Invalidate affected node; send value/state event when needed |
| Structure | Invalidate affected subtree/root and announce layout/window change |
| Invoke/toggle/selection | Emit action/selection state event after success |
| Text/edit/selection | Emit native text or selection change |
| Live region/notification | Use shared debounce/throttle then native announcement |
| Window/menu/layout | Emit native window/screen/layout change |
| UIA-only synchronized input/object model | No fabricated native event; retain diagnostics/fallback entry |

Event ordering:

1. Apply/observe the Uno state change.
2. Invalidate native node/tree state.
3. Emit the native event.
4. Let the native service re-query final state.

## 10. Diagnostics contract

Trace-level logging may record:

- window/node registration and removal;
- peer resolution and EventsSource redirection;
- property invalidation category;
- native action ID -> provider operation;
- focus/modal transition;
- native event/notification type;
- stale-node and unavailable-provider rejection.

Logs must not include password text, secure input, or unredacted sensitive values.

## 11. Test access contract

Add narrow internal accessors rather than reflection:

- Android: resolve a live element to virtual ID and obtain its native node from the provider.
- iOS: resolve a live element to its stable `UIAccessibilityElement`.
- Both: inspect registry counts/generations for lifecycle tests.
- Both: inject/observe native event sinks where platform tests cannot intercept the OS service.

These accessors are internal, test-only in purpose, and do not alter public API.
