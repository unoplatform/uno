# Data Model: Mobile Accessibility and Automation

This feature has no persisted business data. Its data model is the per-window runtime state
that projects the live WinUI automation-peer tree into Android and iOS native accessibility
objects.

## 1. Mobile accessibility window

One instance exists per XamlRoot/native window.

| Field | Meaning |
|-------|---------|
| `Owner` | Platform host implementing `IAccessibilityOwner` |
| `XamlRoot` | XAML tree whose peers belong to this adapter |
| `RootElement` | Current root used for tree traversal |
| `IsActive` | Whether this owner receives global announcements/focus routing |
| `IsAttached` | Native host/container is attached and queryable |
| `IsDisposed` | No further callbacks or native actions may execute |
| `Nodes` | Stable node registry for this window |
| `FocusedNodeId` | Current native accessibility focus, if any |
| `ActiveModalNodeId` | Root of the current modal subtree, if any |
| `PendingInvalidations` | Coalesced node/subtree/event invalidation queue |

Relationships:

- Owns zero or more `Mobile semantic node registrations`.
- Owns exactly one platform adapter (`AndroidSkiaAccessibility` or
  `AppleUIKitAccessibility`).
- Is selected through `AccessibilityRouter` using the element's XamlRoot, never
  `Window.Current`.

Validation rules:

- A node from one window must never resolve through another window's registry.
- A disposed window cannot accept native actions, focus, announcements, or delayed updates.
- The root and modal scope must belong to the same XamlRoot.

## 2. Mobile semantic node registration

This is identity/lifecycle state only. It does not cache all automation properties.

| Field | Meaning |
|-------|---------|
| `NodeId` | Stable logical identity within the window |
| `VisualHandle` | Current Uno visual handle used by shared callbacks |
| `Owner` | Weak reference to the live `DependencyObject`/`UIElement` |
| `ParentNodeId` | Accessible parent after Raw-node pruning/promotion |
| `ChildNodeIds` | Accessible children in peer reading order |
| `PlatformIdentity` | Android virtual ID or iOS element object |
| `Generation` | Prevents stale callbacks from targeting a reused registration |
| `LifecycleState` | `Registered`, `Realized`, `Hidden`, `Removing`, or `Removed` |
| `DirtyFlags` | Bounds, properties, actions, relations, children, or focus need re-query |

Invariants:

1. Identity is stable while the peer remains realized.
2. A removed ID is not rebound to another owner within the same window lifetime.
3. Owner and peer references held by registries or native elements are weak.
4. Every child has one accessible parent; Raw nodes may be absent while their descendants
   are promoted.
5. Native action callbacks validate generation and live owner before resolving a peer.

## 3. Resolved peer projection

An ephemeral projection is computed when Android or iOS asks for node data. It is discarded
after the native node has been populated.

| Group | Values pulled from the live peer |
|-------|----------------------------------|
| Identity | AutomationId, class name, resolved peer, EventsSource |
| Tree | Control/Content/Raw membership, parent, children, position, size, level |
| Basic semantics | Name, localized control type, help/full description, culture |
| State | Enabled, focused, focusable, off-screen, password, required, valid, read-only |
| Navigation | Heading, landmark, dialog, peripheral, orientation |
| Geometry | Bounding rectangle, clickable point, visibility/clipping |
| Patterns | All supported `PatternInterface` providers |
| Relations | LabeledBy, ControlledPeers, DescribedBy, FlowsTo/From, annotations |
| Live behavior | LiveSetting, ItemStatus, available events/actions |

Projection rules:

- Resolve `EventsSource` before reading state or routing an event.
- Never use AutomationId as the accessible name.
- Never expose secure text values.
- Query on the Uno UI thread.
- Platform code may consume only the fields it can represent; unsupported values remain
  documented in the capability matrix.

## 4. Native action descriptor

Native actions normalize to an internal operation before provider execution.

| Field | Meaning |
|-------|---------|
| `Kind` | Activate, Toggle, Select, Add/RemoveSelection, Expand, Collapse, Increment, Decrement, SetValue, SetText, SetSelection, Scroll, ScrollIntoView, Realize, Dismiss, or Custom |
| `Arguments` | Value, text, selection range, scroll direction/amount, or custom payload |
| `RequiredPattern` | `PatternInterface` needed to execute the operation |
| `IsMutating` | Whether disabled/read-only checks apply |
| `NativeActionId` | Android action ID or iOS selector/custom-action identity |
| `LocalizedLabel` | Label used only for native custom actions |

Execution transition:

1. Native callback resolves the node and current peer.
2. The adapter verifies lifecycle, enabled/read-only state, and required provider.
3. Execution is marshaled to the Uno UI thread.
4. The provider operation runs.
5. Success invalidates the node and emits the matching native event.
6. Disabled/unavailable errors translate to native action failure without crashing.

## 5. Android virtual-node registry

| Map | Contract |
|-----|----------|
| `ConditionalWeakTable<DependencyObject, IdHolder>` | Stable owner-to-virtual-ID lookup without retaining owners |
| `Dictionary<int, WeakReference<DependencyObject>>` | Virtual-ID-to-owner routing for TalkBack/UIAutomator callbacks |
| `Dictionary<nint, int>` | Visual-handle-to-virtual-ID lookup for shared property/bounds callbacks |

Rules:

- IDs are monotonically allocated per window.
- Dead weak references are skipped and compacted during tree enumeration.
- Child removal immediately removes the handle mapping and schedules reverse-map cleanup.
- `GetVisibleVirtualViews` returns peer reading order, not keyboard tab order.
- Relationships are resolved only after all participating IDs have been assigned.

The native node is populated on demand with bounds-in-screen, class/role, label, state,
range, collection, text, relation, identifier, and action metadata.

## 6. iOS accessibility element

`UnoUIAccessibilityElement` is a stable managed `UIAccessibilityElement` owned by the
window adapter.

| Field | Meaning |
|-------|---------|
| `NodeId` | Logical node identity |
| `Adapter` | Weak reference used to resolve the current projection/action |
| `AccessibilityContainer` | Existing Skia/Metal view |
| `AccessibilityFrameInContainerSpace` | Current bounds in UIKit logical points |
| `Generation` | Stale-callback guard |

The adapter also maintains:

| Map | Contract |
|-----|----------|
| `Dictionary<nint, UnoUIAccessibilityElement>` | Visual-handle-to-stable-element lookup for `SkiaAccessibilityBase.Update*` and bounds callbacks |
| `Dictionary<NodeId, UnoUIAccessibilityElement>` | Logical identity and stale-generation validation |

Its property getters and action overrides pull from the live peer through the adapter.
The container's `AccessibilityElements` array preserves peer reading order and stable
element objects.

Rules:

- The element does not retain an AutomationPeer or UIElement strongly.
- Removal deletes the element from both the registry and container array.
- Activation/increment/decrement/scroll/escape/custom actions route through the shared
  provider-action contract.
- VoiceOver/XCUITest can query the tree even when VoiceOver is not running.

## 7. Capability mapping

Each WinUI semantic has one mapping record:

| Field | Meaning |
|-------|---------|
| `WinUISemantic` | Property, pattern, relation, or event |
| `AndroidMapping` | Direct node field/action/event, derived/custom mapping, or fallback |
| `iOSMapping` | Direct element property/action/notification, derived/custom mapping, or fallback |
| `SupportClass` | `Direct`, `Derived`, `CustomAction`, `InternalOnly`, or `Unsupported` |
| `LiveProducer` | Property/event source that invalidates the node |
| `SecurityRule` | Redaction or exposure rule |
| `TestCase` | Native observable that proves the mapping |

The mapping is defined in `contracts/capability-matrix.md` and is the coverage source of truth.

## 8. State transitions

### Host initialization

`Host starts` -> router initialized -> XamlRoot host implements `IAccessibilityOwner` ->
platform adapter created -> owner activated -> root tree available for native queries.

### Native node query

`Native query` -> validate node/generation -> resolve owner -> create/resolve peer ->
resolve EventsSource -> pull projection -> populate Android node or iOS element property.

### Property change

`Peer raises property change` -> router resolves window -> mobile adapter calls base routing ->
adapter marks node dirty/invalidate -> native service re-queries live projection -> native
change event emitted when required.

If the node has not received a native identity yet, retain a pending dirty entry keyed by
visual handle. First registration/query pulls final state; an already-exposed subtree falls
back to one structural invalidation rather than silently losing the update.

### Structure change

`Child add/remove/reorder` -> shared visual hook -> router -> update node registry and reading
order -> assign/remove native identity -> emit subtree/layout change -> recover native focus
if the focused node disappeared.

### Bounds/scroll change

`Visual offset/size or scroll changes` -> shared hook/subscription -> mark affected node or
subtree bounds dirty -> Android `InvalidateVirtualView` or iOS layout notification -> native
query pulls new geometry.

### Modal transition

`Modal opens` -> set active modal node -> suppress background nodes -> emit window/screen
change -> focus valid modal node.

`Modal closes` -> restore prior scope -> emit change -> restore a live focus target.

### Disposal

`Window closes` -> mark disposed -> detach subscriptions -> clear native container/provider
state -> clear registries -> notify router -> ignore delayed callbacks.

## 9. Required consistency checks

- Node count follows realized/accessibility-required peers, not historical elements.
- No native node references an absent relation target.
- No action is advertised when its current provider/state cannot execute it.
- Native role, state, value, and action metadata come from the same peer projection.
- Accessibility focus and XAML focus cannot recursively trigger each other.
- Full-tree invalidation occurs only for root/structure changes, not every render frame.
- Password values never enter projections consumed by identifiers, labels, hints, logs, or tests.
