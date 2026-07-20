# Contract: WinUI to Mobile Capability Matrix

This matrix is the coverage source of truth for Skia Android and Skia iOS.

Support classes:

- **Direct**: represented by a native property, action, relation, or event.
- **Derived**: represented by a combination of native state, role, ordering, or text.
- **Custom**: represented through a native custom action/content channel.
- **Internal**: preserved in the live peer contract for automation/diagnostics, but no
  native assistive-technology equivalent exists.
- **Unsupported**: no safe equivalent; behavior is explicitly omitted rather than fabricated.

## 1. Automation element properties

| WinUI property | Android projection | iOS projection | Class / fallback |
|----------------|--------------------|----------------|------------------|
| BoundingRectangle | `BoundsInScreen` | `AccessibilityFrameInContainerSpace` | Direct |
| HasKeyboardFocus | `Focused` plus focus event | Current accessibility element/focus notification | Direct/Derived |
| HelpText | `HintText` or tooltip channel | `AccessibilityHint` | Direct |
| IsContentElement | Peer-tree inclusion | Element/container inclusion | Derived |
| IsControlElement | Peer-tree inclusion and screen-reader focusability | Element/container inclusion | Derived |
| IsEnabled | `Enabled` | `NotEnabled` trait/action suppression | Direct |
| IsKeyboardFocusable | `Focusable`/input-focus action | Determines focus handoff and actions | Derived |
| IsOffscreen | `VisibleToUser`, bounds, tree visibility | Frame intersection and container visibility | Direct/Derived |
| IsPassword | `Password`; redact text | Secure-text trait/value redaction | Direct |
| IsRequiredForForm | Native required flag when available; otherwise state metadata | Custom content/hint | Direct/Derived |
| ItemStatus | `StateDescription` on API 30+; extras or appended descriptive text on earlier APIs | `AccessibilityValue` or custom content | Derived |
| ItemType | Class/role description | Trait/container type or custom content | Derived |
| LabeledBy | `SetLabeledBy(host, virtualId)` | Resolve accessible label from peer relationship | Direct/Derived |
| LiveSetting | Native live-region/announcement policy | Announcement policy and updates-frequently trait | Direct/Derived |
| LocalizedControlType | Localized role description | Trait plus localized custom content where needed | Derived |
| Name | `Text` or `ContentDescription` by node kind | `AccessibilityLabel` | Direct |
| AcceleratorKey | Preserve in extras/custom action metadata | Preserve in custom content | Internal/Custom; do not announce by default |
| AccessKey | Preserve in extras/custom action metadata | Preserve in custom content | Internal/Custom; no mobile keyboard contract |
| AutomationId | Normalized resource/unique ID | `AccessibilityIdentifier` | Direct; never the spoken name |
| Orientation | Range/collection orientation or state metadata | Adjustable/container context or custom content | Direct/Derived |
| ClassName | `ClassName` | Trait-derived; no independent class string | Direct/Derived |
| ClickablePoint | Bounds/hit-test target | `AccessibilityActivationPoint` when needed | Direct |
| ControlType | Class name, role description, state, patterns | Traits, container type, actions | Derived |
| ControlledPeers | Preserve IDs/extras; custom action only when meaningful | Preserve internally/custom content | Internal/Custom |
| Annotations | Extras/spans/custom metadata | Attributed text or custom content | Derived/Custom |
| Level | Collection/hierarchy metadata or extras | Custom content/rotor context | Derived |
| PositionInSet | Collection item metadata | Container ordering/custom content | Direct/Derived |
| SizeOfSet | Collection metadata | Container ordering/custom content | Direct/Derived |
| LandmarkType | Pane/role metadata | Landmark container type/rotor | Derived |
| LocalizedLandmarkType | Localized pane/role description | Localized custom content/rotor label | Derived |
| DescribedBy | Hint/tooltip/error description | `AccessibilityHint`/custom content | Derived |
| FlowsFrom | Traversal-before/reading-order metadata when same tree | Container order/custom navigation | Derived |
| FlowsTo | Traversal-after/reading-order metadata when same tree | Container order/custom navigation | Derived |
| FullDescription | Long description/hint/custom content | `AccessibilityHint` or custom content | Derived |
| IsDataValidForForm | Content-invalid/error/state description | Hint/value/custom content | Direct/Derived |
| IsPeripheral | Tree/focus policy only | Tree/focus policy only | Internal |
| Culture | Text locale list | `AccessibilityLanguage` | Direct |
| HeadingLevel | `Heading=true`; exact level retained in metadata | Header trait; exact level retained internally | Direct for heading, unsupported exact spoken level |
| IsDialog | Window/pane semantics and modal subtree | `AccessibilityViewIsModal` plus screen change | Direct/Derived |

## 2. Provider patterns

Each row includes the pattern's associated state properties and operations.

| `PatternInterface` | Android projection | iOS projection | Class / fallback |
|--------------------|--------------------|----------------|------------------|
| Invoke | Click action | `AccessibilityActivate` | Direct |
| Selection | Collection selection mode/state | Selected children/container semantics | Direct/Derived |
| Value | Text/value, editable/read-only, set-text action | `AccessibilityValue`; native text/custom set action | Direct/Derived |
| RangeValue | RangeInfo, set-progress, increment/decrement | Adjustable trait, value, increment/decrement | Direct |
| Scroll | Scrollable state and directional scroll actions | `AccessibilityScroll` | Direct |
| ScrollItem | Show-on-screen/scroll-to-position | Reveal item then layout/focus notification | Direct/Derived |
| ExpandCollapse | Expanded state and expand/collapse actions | Value/state plus custom Expand/Collapse actions | Direct/Custom |
| Grid | Collection row/column metadata | Data-table/container semantics | Direct/Derived |
| GridItem | Collection item row/column/span | Table-cell/custom content | Direct/Derived |
| MultipleView | Localized custom actions for supported views | Localized custom actions | Custom |
| Window | Window/modal state, close/dismiss where available | Modal/screen state and escape/dismiss | Direct/Derived |
| SelectionItem | Selected state; select/clear actions | Selected trait; activate/custom selection action | Direct |
| Dock | Localized custom dock actions when meaningful | Localized custom actions | Custom/Internal |
| Table | Collection info plus header relationships | Data-table container and header context | Direct/Derived |
| TableItem | Collection item plus header labels | Cell/header custom content | Direct/Derived |
| Toggle | Checkable/checked/mixed; click action | Value/state plus activate | Direct |
| Transform | Parameterized automation-hook operations; no dead parameterless AT action | Parameterized automation-hook operations; no dead parameterless AT action | Custom/Internal |
| Text | Text, selection, editing/granularity actions | Native text accessibility/input bridge | Direct/Derived |
| ItemContainer | Used internally to locate realized peer items | Used internally to locate realized peer items | Internal |
| VirtualizedItem | Realize action for realized providers; container scroll realizes ownerless items | Realize action for realized providers; container scroll realizes ownerless items | Derived |
| Text2 | Provider retained in typed fallback diagnostics | Provider retained in typed fallback diagnostics | Internal/Derived |
| TextChild | Provider retained in typed fallback diagnostics | Provider retained in typed fallback diagnostics | Internal/Derived |
| TextRange | Provider retained in typed fallback diagnostics | Provider retained in typed fallback diagnostics | Internal/Derived; UIA range object not copied |
| Annotation | Type names in typed fallback/details metadata | Annotation type names through AX custom content and details | Derived/Custom |
| Drag | Provider state retained internally; no callable WinUI drag operation exists | Provider state retained internally; no callable WinUI drag operation exists | Internal/Custom |
| DropTarget | Provider state retained internally; no callable WinUI drop operation exists | Provider state retained internally; no callable WinUI drop operation exists | Internal/Custom |
| ObjectModel | Preserve provider internally | Preserve provider internally | Unsupported native transport |
| Spreadsheet | Grid/table semantics plus typed fallback diagnostics | Typed fallback diagnostics | Internal/Derived |
| SpreadsheetItem | Cell coordinates plus typed fallback diagnostics | Cell coordinates plus typed fallback diagnostics | Internal/Derived |
| Styles | Provider retained in typed fallback diagnostics | Provider retained in typed fallback diagnostics | Internal/Derived |
| Transform2 | Transform actions plus zoom where supported | Custom zoom/transform actions | Custom |
| SynchronizedInput | Preserve provider/event diagnostics | Preserve provider/event diagnostics | Unsupported native transport |
| TextEdit | Native set-text/selection/change semantics | Native text input/edit semantics | Direct/Derived |
| CustomNavigation | Traversal relationships when expressible; provider retained internally | Container order when expressible; provider retained internally | Internal/Derived |

## 3. Pattern property groups

| Pattern | WinUI state/property group | Android | iOS |
|---------|----------------------------|---------|-----|
| Toggle | ToggleState | Checked/checkable/mixed state | Value/selected state |
| Value | Value, IsReadOnly | Text, editable, set-text | Value, editable/native input |
| RangeValue | Value, Minimum, Maximum, SmallChange, LargeChange, IsReadOnly | RangeInfo and set-progress/increment actions | Adjustable value and increment/decrement |
| ExpandCollapse | ExpandCollapseState | Expanded/collapsed state/actions | Value plus custom actions |
| Selection | CanSelectMultiple, IsSelectionRequired, Selection | Collection selection mode and selected nodes | Container semantics and selected traits |
| SelectionItem | IsSelected, SelectionContainer | Selected flag and parent collection | Selected trait and container |
| Scroll | Horizontal/VerticalScrollPercent, ViewSize, Scrollable | Scroll state/actions; percentages retained in extras if not spoken | Scroll actions; percentages internal/custom content |
| Grid | RowCount, ColumnCount | CollectionInfo | Data-table/container metadata |
| GridItem | Row, Column, RowSpan, ColumnSpan, ContainingGrid | CollectionItemInfo | Cell/custom content |
| MultipleView | CurrentView, SupportedViews, ViewName | Custom action set/state | Custom action set/state |
| Window | CanMaximize/Minimize, IsModal, IsTopmost, interaction/visual state | Window/modal/custom actions | Modal/screen/custom actions |
| Dock | DockPosition | Custom action/state metadata | Custom action/state metadata |
| Table | RowOrColumnMajor, RowHeaders, ColumnHeaders | Collection/header relations | Data-table/header context |
| TableItem | RowHeaderItems, ColumnHeaderItems | Labeled/header metadata | Header custom content |
| Transform | CanMove, CanResize, CanRotate | Advertised custom actions | Advertised custom actions |
| Text/Text2/TextEdit | Document range, selection, active position, edit changes | Native text/selection actions and events | Native text input/selection and notifications |
| Annotation | Type, type name, author, date, target | Extras/custom metadata | Attributed/custom content |
| Drag | IsGrabbed, DropEffect(s), GrabbedItems | Typed internal fallback | Typed internal fallback |
| DropTarget | DropTargetEffect(s) | Typed internal fallback | Typed internal fallback |
| SpreadsheetItem | Formula, annotations | Typed internal fallback | Typed internal fallback |
| Styles | Style ID/name, fill, shape, extended properties | Typed internal fallback | Typed internal fallback |
| Transform2 | CanZoom, ZoomLevel, Min/MaxZoom | Zoom custom actions/state | Zoom custom actions/state |

## 4. Relations

| WinUI relation | Android | iOS | Fallback |
|----------------|---------|-----|----------|
| LabeledBy | Direct virtual-node relation | Resolve name from label peer | Never emit a dangling target |
| ControlledPeers / ControllerFor | Preserve node IDs/extras | Preserve internally/custom content | No fabricated parent/child relation |
| DescribedBy | Hint/error/description channel | Hint/custom content | Concatenate only descriptive text, not identity |
| FlowsTo / FlowsFrom | Traversal order when targets share a live tree | Container order/custom navigation | Omit stale/cross-window targets |
| Annotations | Extras/spans/custom metadata | Attributed text/custom content | Preserve internal objects for automation |

## 5. Automation events

| `AutomationEvents` value | Android translation | iOS translation | Class / fallback |
|--------------------------|--------------------|-----------------|------------------|
| ToolTipOpened | Content/announcement event | Layout/announcement notification | Derived |
| ToolTipClosed | Content-change event | Layout-change notification | Derived |
| MenuOpened | Window-state/content change | Screen/layout change to menu | Direct/Derived |
| MenuClosed | Window-state/content change | Screen/layout change to restored target | Direct/Derived |
| AutomationFocusChanged | Accessibility-focus event | Layout/screen change with target element | Direct |
| InvokePatternOnInvoked | Click event after success | State/layout notification if needed | Direct/Derived |
| SelectionItemPatternOnElementAddedToSelection | Selected/content event | Selected trait + layout notification | Direct |
| SelectionItemPatternOnElementRemovedFromSelection | Selected/content event | Selected trait + layout notification | Direct |
| SelectionItemPatternOnElementSelected | View-selected event | Selected trait + layout/focus notification | Direct |
| SelectionPatternOnInvalidated | Collection content change | Layout change | Direct/Derived |
| TextPatternOnTextSelectionChanged | Text-selection-changed event | Native text selection/layout notification | Direct/Derived |
| TextPatternOnTextChanged | View-text-changed event | Native text/layout notification | Direct/Derived |
| AsyncContentLoaded | Subtree content change | Layout change | Derived |
| PropertyChanged | Node invalidation plus property-specific event | Re-query plus property-specific notification | Direct/Derived |
| StructureChanged | Window content changed, subtree type | Layout changed | Direct |
| DragStart | Invalidate live provider state; native drag transport unavailable | Invalidate live provider state; native drag transport unavailable | Internal/Custom |
| DragCancel | Invalidate live provider state; native drag transport unavailable | Invalidate live provider state; native drag transport unavailable | Internal/Custom |
| DragComplete | Invalidate live provider state; native drag transport unavailable | Invalidate live provider state; native drag transport unavailable | Internal/Custom |
| DragEnter | Invalidate drop-target state | Invalidate drop-target state | Internal/Custom |
| DragLeave | Invalidate drop-target state | Invalidate drop-target state | Internal/Custom |
| Dropped | Invalidate drop-target/content state | Invalidate drop-target/content state | Internal/Custom |
| LiveRegionChanged | Live-region content event/announcement | Announcement notification | Direct |
| InputReachedTarget | Diagnostic only | Diagnostic only | Unsupported native transport |
| InputReachedOtherElement | Diagnostic only | Diagnostic only | Unsupported native transport |
| InputDiscarded | Diagnostic only | Diagnostic only | Unsupported native transport |
| WindowClosed | Windows/content change | Screen change to restored target | Direct |
| WindowOpened | Window-state change | Screen change to new window/modal | Direct |
| ConversionTargetChanged | Text/selection content change | Native text/layout notification | Derived |
| TextEditTextChanged | View-text-changed event | Native text/layout notification | Direct/Derived |
| LayoutInvalidated | Subtree content change | Layout changed | Direct |

Separate `RaiseNotificationEvent` contract:

- Android: shared throttle/debounce -> native announcement event/API.
- iOS: shared throttle/debounce -> `UIAccessibility` announcement notification.

## 6. Native limitations that must remain explicit

- Android and iOS do not expose UIA COM provider objects, HRESULT transport, or UIA runtime IDs.
- Exact WinUI heading levels have no reliable native spoken-level equivalent on either mobile
  platform; heading semantics are direct, exact numeric level remains metadata/internal.
- ObjectModel and SynchronizedInput have no native accessibility transport.
- Full UIA TextRange object identity/granularity is translated to each platform's native text
  editing/selection API rather than copied.
- Unrealized item peers have no native node until a real container exists. Native container
  scrolling performs realization; IDs are stable when the same container is recycled.
- Move, resize, rotate, absolute zoom, dock, and window-state operations remain available to
  automation hooks with explicit arguments. Only fixed, meaningful actions are advertised to
  TalkBack or VoiceOver.
- Drag/DropTarget, spreadsheet/style object models, and richer UIA text-range identities are
  recorded in typed fallback diagnostics and are never advertised as dead native actions.
- Some newer Android node fields/actions are API-level dependent; older versions use the
  documented derived/custom fallback.
- iOS has fewer direct relationship APIs; name, hint, container order, custom content, and
  rotor/action channels are used without inventing false structural relationships.

Every limitation requires:

1. a capability classification in this file;
2. a test for the chosen projection or fallback;
3. diagnostics when an app requests a semantic that is intentionally internal/unsupported;
4. no claim of direct native support in documentation or release notes.
