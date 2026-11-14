---
uid: Uno.Development.MigratingToUno7.WinUIParity
---

# WinUI API alignment

Part of [Migrating to Uno Platform 7.0](xref:Uno.Development.MigratingToUno7).

**Read this if** the compiler rejects code that uses `Background`, `DataContext` or `SetBinding` on an element that doesn't have them, `UserControl.Content`, `PasswordBox.Text`, `new Control()`, or the fields of `Thickness`, `GridLength` or `Duration`.

**Skip it if** your code builds cleanly and already runs on a WinAppSDK head. Code written against WinUI is largely unaffected.

## Type-hierarchy changes

7.0 realigns several types to their WinUI base classes. Most code is unaffected — the
change only breaks code that used the Uno-only members leaked by the wrong base.

See also: [`ContentControl` subclasses without a `ControlTemplate`](xref:Uno.Development.MigratingToUno7.Behavior#contentcontrol-subclasses-without-a-controltemplate).

### `DependencyObject` is a class, not an interface

Uno declared it as an interface so that
`UIElement` could inherit the platform's native view type on Android and iOS. With Skia
rendering everywhere that constraint is gone, and `DependencyObject` is now an ordinary base
class as it is in WinUI. Two consequences:

- A type that inherits `DependencyObject` **no longer needs the `partial` keyword** — the
  source generator that used to supply the interface implementation is gone. Existing
  `partial` declarations keep compiling, so nothing forces you to change them.
- A type can no longer inherit some *other* base class and pick up `DependencyObject`
  alongside it. That only ever worked because it was an interface, and WinUI never allowed
  it. Such a type has to be reworked to derive from `DependencyObject` (or a type that does).

See also: [Custom value types default to `null`](xref:Uno.Development.MigratingToUno7.Behavior#custom-value-types-default-to-null).

### `UserControl` derives from `Control`, not `ContentControl`

WinUI's hierarchy is
`Control → UserControl → Page`; Uno inserted an extra `ContentControl` level. That level is
gone, so `is ContentControl` is no longer `true` for a `UserControl` or a `Page`, and the
`ContentControl`-only surface they used to inherit — `ContentTemplate`,
`ContentTemplateSelector`, `ContentTransitions` — is no longer available on them. WinUI's
`UserControl` never had it.

`Content` survives but is **retyped from `object` to `UIElement`**, as in WinUI. Assigning a
non-`UIElement` (a view model, a string) used to work through the `ContentControl` template
machinery and is now a compile error; put the value on a child element's property, or bind a
`ContentControl` you place inside the `UserControl` yourself. XAML that nests a single element
inside a `UserControl` — the overwhelmingly common case — is unaffected.

### `MediaPlayerPresenter`

`MediaPlayerPresenter` now derives directly from **`FrameworkElement`** (matching
WinUI) instead of `Border`, removing the extra `Border` level. The `Border`-only surface
it used to expose — `Child`, `Background`, `BorderBrush`, `BorderThickness`, `CornerRadius`,
`Padding`, `BackgroundSizing`, `ChildTransitions` — is gone, and `is Border` is no longer
`true` for a presenter. The video surface is hosted internally, so playback and `Stretch` are
unchanged. `MediaPlayerElement` no longer paints a black backdrop (WinUI doesn't), so letterbox
bars are transparent; set a `Background` on a parent, or retemplate, if you relied on black bars.

### `ImageBrush`

`ImageBrush` now derives from a real **`TileBrush`** (`Brush → TileBrush →
ImageBrush`, matching WinUI) instead of directly from `Brush`. `AlignmentX`,
`AlignmentY`, and `Stretch` behave the same for callers but are now **declared on
`TileBrush`** — code that referenced the static DPs by their declaring type
(`ImageBrush.StretchProperty`, `AlignmentXProperty`, `AlignmentYProperty`) still resolves
via inheritance, and `is TileBrush` is now `true` for an `ImageBrush`. Instance usage
(`imageBrush.Stretch`, XAML `Stretch="…"`) is unaffected.

### `FadeInThemeAnimation` / `FadeOutThemeAnimation`

`FadeInThemeAnimation` / `FadeOutThemeAnimation` now derive directly from `Timeline`
(matching WinUI) instead of Uno's `DoubleAnimation`. The `DoubleAnimation`-only members
they used to inherit — `From`, `To`, `By`, `EasingFunction`, and
`EnableDependentAnimation` — are gone; WinUI never exposed them on these theme
animations. `TargetName` and the `Timeline` members (`Duration`, `BeginTime`,
`RepeatBehavior`, `FillBehavior`) are unchanged. Set only `TargetName` (as the built-in
styles do); the fade always animates `Opacity` to its fixed target (1 for fade-in, 0 for
fade-out).

### `PasswordBox`

`PasswordBox` now derives directly from **`Control`** (matching WinUI) instead of
`TextBox`, and `is TextBox` is no longer `true` for a password box. The `TextBox`-only
surface it used to inherit is gone — `Text`, `TextChanged`, `TextChanging`,
`BeforeTextChanging`, `SelectedText`, `SelectionStart`, `SelectionLength`, `IsReadOnly`,
`AcceptsReturn`, `TextWrapping`, `TextAlignment`, `IsSpellCheckEnabled`,
`CanUndo`/`CanRedo`, `Undo()`/`Redo()`,
`CopySelectionToClipboard()`/`CutSelectionToClipboard()`, and `ProofingMenuFlyout`. None of
these exists on WinUI's `PasswordBox`, so code written against WinUI is unaffected. Use
**`Password`** to read or write the value and **`PasswordChanged`** in place of
`TextChanged`; the password is no longer reachable as text. Everything WinUI does expose —
`PasswordChar`, `PasswordRevealMode`, `Header`, `HeaderTemplate`, `PlaceholderText`,
`Description`, `InputScope`, `SelectionHighlightColor`, `SelectionFlyout`,
`CanPasteClipboardContent`, `ContextMenuOpening`, `Paste`, `SelectAll()`,
`PasteFromClipboard()` — behaves as before; it is declared on `PasswordBox` itself now
rather than inherited. `MaxLength` now limits `Password` directly: it previously applied
only through the inherited `Text` mirror, so an over-long value assigned to `Password` was
accepted while the mirror rejected it.

**`BeforeTextChanging` has no replacement.** `PasswordChanging` remains unimplemented, and
its `PasswordBoxPasswordChangingEventArgs` carries no `Cancel` member, so it would not
substitute even once implemented — nothing can veto password input. Bound the length with
`MaxLength` and validate after the fact in `PasswordChanged`. Dropping the inherited
`IsSpellCheckEnabled` also stops a password box spell-checking its own masked text, which
removes the squiggly underline it used to draw.

## Members restricted to their WinUI declaring types

Several members that Uno declared on the wrong type are now declared exactly where WinUI
declares them. None changes behavior where the member survives — they break code that reached
the member through a type that never had it in WinUI.

### `Background` is no longer on `FrameworkElement`

WinUI declares `Brush Background` on
exactly eight types, and 7.0 now matches: `Control`, `Panel`, `Border`, `ContentPresenter`,
`ItemsRepeater` and `ScrollPresenter` (all `FrameworkElement` subclasses), plus `SwipeItem`
and `TextHighlighter` (both `DependencyObject`). Anything deriving from one of those still
has `Background` by inheritance — `Grid`, `StackPanel`, `Page`, `CalendarViewBaseItem`,
every templated control — so ordinary XAML and ordinary control code are unaffected.

What breaks is the surface that Uno added on top: `TextBlock`, `RichTextBlock`, `Image`,
`Glyphs`, `AnimatedVisualPlayer`, `Shape` (and `Rectangle`/`Ellipse`/`Line`/`Path`/
`Polygon`/`Polyline`), and every other bare `FrameworkElement` subclass no longer expose
`Background` at all — WinUI never gave them one. Reading or setting it through a
`FrameworkElement`-typed reference is now a compile error, and
`<Setter Property="Background" />` in a style whose `TargetType` is one of those types no
longer resolves.

To migrate: retype the reference to the declaring type (`Control`, `Panel`, `Border`, …)
where the object really is one; paint text and shapes with `Foreground` and `Fill`
instead; and where you genuinely need a filled rectangle behind an element that no longer
has `Background`, wrap it in a `Border` or `Grid`. `FrameworkElement.BackgroundProperty`
likewise moves — reference the DP on its declaring type (`Control.BackgroundProperty`,
`Panel.BackgroundProperty`, …). The protected `OnBackgroundChanged` override moved with
it and is no longer part of the public surface.

See also: [`SystemBackdrop` no longer rewrites your backgrounds](xref:Uno.Development.MigratingToUno7.Behavior#systembackdrop-no-longer-rewrites-your-contents-backgrounds).

### `DataContext` is on `FrameworkElement` only

Uno's source generator emitted
`DataContext`, `DataContextProperty`, `DataContextChanged` and `OnDataContextChanged` onto
*every* `DependencyObject`. WinUI has them on `FrameworkElement` alone, and 7.0 now matches:
they are declared once, on `FrameworkElement`. Every element in the visual tree still has
`DataContext`, and inheritance down the tree is unchanged.

Non-`FrameworkElement` `DependencyObject`s lose the member entirely — `Brush` and its
subclasses, `Transform`, `GradientStop`, `Setter`, `Style`, `DependencyObjectCollection`,
`ElementFactory`, `FlyoutBase`, and so on. Reading or setting `.DataContext` on one, or
subscribing to its `DataContextChanged`, is now a compile error, as is referencing
`Brush.DataContextProperty` and friends.

**`{Binding}` on these objects keeps working.** A non-`FrameworkElement` has no
`DataContext` of its own, but — as in WinUI — its bindings resolve against the
`DataContext` of the `FrameworkElement` it is attached to, through its inheritance
context. `ColumnDefinition.Width`, `RowDefinition.Height`, `Run.Text`,
`StateTrigger.IsActive` and the `Value` of a `Setter` in a `VisualState` all still bind to
the surrounding view model. The change is to the
API surface only: an object like this can no longer be given a `DataContext` of its own, so
bind on the owning element or set the value from code instead.

Uno tracks the inheritance context more strictly than WinUI. An object shared by several
owners permanently loses its inheritance context once a second owner attaches it. WinUI makes
two exceptions that Uno does not implement: an object whose first parent is a
`ResourceDictionary` keeps the dictionary owner's context however many elements use it, and
one whose first parent is a `ContentControl` keeps its context for up to two parents. A brush
declared as a resource and used by more than one element is the common case: its
`{Binding}`s still resolve on WinUI but stop resolving on Uno, so don't rely on bindings
inside shared resources.

`{Binding}` in `Setter.Value` depends on where the `Setter` is. In a `VisualState` it resolves
however the state is entered, using the value at that moment: as in WinUI, the setter doesn't
follow later changes until the state is entered again. In a `Style` it doesn't resolve, on WinUI
or on Uno Platform.

Flyouts are the most commonly hit case, and they keep working: `FlyoutBase` no longer
carries a `DataContext`, but the placement target's `DataContext` is forwarded onto the
flyout presenter when the flyout opens and cleared when it closes, so `{Binding}` inside
flyout content still resolves against the target's view model — as it does in WinUI.

### `SetBinding` is on `FrameworkElement` only

Uno declared `SetBinding(DependencyProperty,
BindingBase)`, plus an Uno-only `SetBinding(string, BindingBase)` overload, on every
`DependencyObject`. WinUI has `FrameworkElement.SetBinding` alone, and 7.0 now matches.
To bind a property of a non-`FrameworkElement` object (a `Brush`, a `Transform`, …) from
code, use `BindingOperations.SetBinding(target, property, binding)`, as in WinUI. XAML is
unaffected, but a library that contains XAML must be recompiled against 7.0.

### `Transitions` is on `UIElement`

Uno declared `Transitions` and `TransitionsProperty` on
`FrameworkElement`; WinUI declares them on `UIElement`. Code using the property is
unaffected, and `FrameworkElement.TransitionsProperty` still compiles (C# resolves the inherited
static member), but a library compiled against 6.x must be recompiled against 7.0.

### `Grid` attached-property accessors take a `FrameworkElement`

`Grid.GetRow`/`SetRow`,
`GetColumn`/`SetColumn`, `GetRowSpan`/`SetRowSpan` and `GetColumnSpan`/`SetColumnSpan`
took a `UIElement` in Uno; WinUI takes a `FrameworkElement`, and 7.0 now matches. Pass a
`FrameworkElement`-typed reference, or set the attached property directly with
`element.SetValue(Grid.RowProperty, value)`.

## WinRT projection alignment

A few WinRT members were projected differently by Uno than by WinUI's own C# projection. 7.0
aligns them. Most call sites stay source-compatible, but the underlying signatures change, so
recompile against 7.0 rather than swapping assemblies in place.

See also: [Uno-only implicit conversions](xref:Uno.Development.MigratingToUno7.RemovedApis#uno-only-implicit-conversions-and-static-fields-removed).

### `Thickness` and `CornerRadius` members are properties, not fields

`Thickness.Left`,
`Top`, `Right`, `Bottom` and `CornerRadius.TopLeft`, `TopRight`, `BottomRight`, `BottomLeft`
are `{ get; set; }` properties, as they are in WinUI. Reading and assigning them is
unchanged; what no longer compiles is passing one by reference:

```diff
- Normalize(ref thickness.Left);
+ thickness.Left = Normalize(thickness.Left);
```

The same applies to `Uno.UI.Composition.Thickness` and `Uno.UI.Composition.CornerRadius`,
and to the members of `Matrix` (`M11`…`OffsetY`), `Matrix3D` (`M11`…`M44`),
`RepeatBehavior` (`Count`, `Duration`, `Type`) and `GeneratorPosition` (`Index`, `Offset`).

### `KeyTime.TimeSpan` is a read-only property

WinUI exposes it as get-only; build a
`KeyTime` with `KeyTime.FromTimeSpan` or the implicit conversion from `TimeSpan`:

```diff
- KeyTime t = default;
- t.TimeSpan = TimeSpan.FromSeconds(1);
+ KeyTime t = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1));
```

### A few parameters are renamed to match WinUI

The `PointCollection` indexer parameter
is `index` (was `i`), and `RepeatBehavior.ToString(IFormatProvider)` takes `formatProvider`
(was `provider`). The `Equals` overloads and `==`/`!=` operators of `KeyTime`,
`RepeatBehavior`, `Thickness`, `CornerRadius`, `GridLength` and `Duration` take WinUI's
parameter names too (for example `keyTime1`/`keyTime2` instead of `t1`/`t2`, and
`thickness` instead of `other`). Only named arguments are affected.

### `Matrix.ToString()` formats like WinUI

`Matrix` gains WinUI's
`ToString(IFormatProvider)` overload, and `ToString()` now goes through it: the identity
matrix prints `Identity`, and under a culture whose decimal separator is `,` the values are
separated by `;` (for example `1,5;0;0;2;3;4` in `de-DE`). Code that parses or compares the
string should format the components itself.

### `Matrix`, `Matrix3D` and `RepeatBehavior` implement `IFormattable` like WinUI

A format
string now reaches the numbers, so `$"{matrix:F2}"` prints `1.50,0.00,…`. `Matrix3D`'s public
`ToString(string, IFormatProvider)` became an explicit `IFormattable` implementation: cast to
`IFormattable` or use string interpolation instead of calling it directly.

```diff
- var text = matrix3D.ToString("F2", CultureInfo.InvariantCulture);
+ var text = ((IFormattable)matrix3D).ToString("F2", CultureInfo.InvariantCulture);
```

### `ToggleSwitch`'s `OnToggled`, `OnHeaderChanged`, `OnOnContentChanged` and `OnOffContentChanged` are no longer virtual

WinUI's `ToggleSwitch` is sealed and these
are plain `protected` methods. Uno keeps `ToggleSwitch` unsealed, so a subclass still
compiles but can no longer override these hooks; handle the `Toggled` event or watch the
matching dependency property instead.

### `Inline` is no longer abstract

WinUI's `Inline` is a concrete class with a protected
constructor; Uno's now matches. Existing subclasses are unaffected.

### `GridLength.Value` and `GridLength.GridUnitType` are read-only properties

WinUI exposes
both as get-only, so a `GridLength` is built entirely through its constructor. Code that
mutated a `default` instance has to construct one instead:

```diff
- GridLength star = default;
- star.GridUnitType = GridUnitType.Star;
- star.Value = 1.0;
+ GridLength star = new(1.0, GridUnitType.Star);
```

### `GridLength`'s second constructor parameter is renamed to `type`

WinUI's signature is
`GridLength(double value, GridUnitType type)`; Uno called it `gridUnitType`. Only named
arguments are affected:

```diff
- new GridLength(1.0, gridUnitType: GridUnitType.Star)
+ new GridLength(1.0, type: GridUnitType.Star)
```

### `Duration.TimeSpan` is a read-only property

WinUI exposes it as get-only, so a
`Duration` carrying a time span comes from the constructor. Assigning the old field left
the duration in its `Automatic` default state, so `HasTimeSpan` stayed `false`; the
constructor sets both:

```diff
- Duration d = default;
- d.TimeSpan = TimeSpan.FromSeconds(2);
+ Duration d = new(TimeSpan.FromSeconds(2));
```

### `Duration.Type` is no longer public

WinUI's `Duration` exposes no `Type` member at
all; the state is reached through `HasTimeSpan` and comparison against `Duration.Automatic`
and `Duration.Forever`. The `DurationType` enum itself stays public, as it is in WinUI:

```diff
- if (d.Type == DurationType.TimeSpan) { }
+ if (d.HasTimeSpan) { }

- if (d.Type == DurationType.Forever) { }
+ if (d == Duration.Forever) { }
```

### `DataTemplate.LoadContent()` returns `DependencyObject`

WinUI's return type is
`DependencyObject`, not `UIElement`. Pattern-matching call sites (`is FrameworkElement fe`,
`as UIElement`) are unaffected; an explicitly typed local needs a cast:

```diff
- UIElement root = template.LoadContent();
+ UIElement root = (UIElement)template.LoadContent();
```

### `Windows.Foundation.GuidHelper.Equals` takes `in Guid` instead of `ref Guid`

`in`
arguments are passed without a keyword, so drop `ref` at the call site:

```diff
- GuidHelper.Equals(ref first, ref second);
+ GuidHelper.Equals(first, second);
```

### `SwipeItems` exposes only its `IList<SwipeItem>` members

WinUI projects the
underlying `IVector<SwipeItem>` as `IList<SwipeItem>`, so the raw vector methods Uno also
made public are gone. `First`, `GetMany` and `ReplaceAll` are removed, and `GetAt`, `Size`,
`SetAt`, `InsertAt`, `RemoveAt(uint)`, `Append`, `RemoveAtEnd` and
`IndexOf(SwipeItem, out uint)` are no longer public. Use the list members instead:

```diff
- var first = items.GetAt(0);
+ var first = items[0];

- if (items.Size > 0) { }
+ if (items.Count > 0) { }

- items.Append(item);
+ items.Add(item);
```

`VectorChanged` is still raised, but only through `IObservableVector<SwipeItem>`:

```diff
- items.VectorChanged += OnItemsChanged;
+ ((IObservableVector<SwipeItem>)items).VectorChanged += OnItemsChanged;
```

### `Windows.Storage.ThumbnailMode` is removed

WinRT has no such type, and it made
`ThumbnailMode` ambiguous (`CS0104`) in any file importing both `Windows.Storage` and
`Windows.Storage.FileProperties`. Use `Windows.Storage.FileProperties.ThumbnailMode`, as in
WinUI.

## Constructor accessibility

Constructors now have the accessibility WinUI gives them. Code that only constructs the concrete
WinUI types is unaffected.

See also: [Collection initializers](xref:Uno.Development.MigratingToUno7.RemovedApis#collection-initializers-on-panels-border-and-scrollviewer-removed).

### Base classes are no longer directly constructible

`Control`, `Panel`, `ButtonBase`,
`SelectorItem` and `PickerFlyoutBase` have `protected` constructors, as in WinUI.
Derive from them instead (a bare `class MyControl : Control { }` behaves like the old
`new Control()`).

### `Timeline` and `ObjectKeyFrame` have `protected` constructors

`Timeline` and `ObjectKeyFrame` have `protected` constructors, as in WinUI, so
`new Timeline()` and `new ObjectKeyFrame()` no longer compile. Construct a concrete animation or
key frame (`DoubleAnimation`, `DiscreteObjectKeyFrame`, …) instead.

### Templates can no longer be built from a factory delegate

The `Func<View>` constructors of
`DataTemplate`, `ControlTemplate`, `ItemsPanelTemplate` and `FrameworkTemplate` are removed, and so are
their public builder-delegate constructors, `(object owner, FrameworkTemplateBuilder factory)` and
`(object owner, NewFrameworkTemplateBuilder factory)`; WinUI has none of them. `NewFrameworkTemplateBuilder`
is removed, and `FrameworkTemplateBuilder` moved from `Microsoft.UI.Xaml` to `Uno.UI`, with a new
`(object owner, TemplateMaterializationSettings settings)` signature, as an `[EditorBrowsable(Never)]`
delegate for XAML-generated code. `MarkupHelper.CreateDataTemplate` and its `ControlTemplate` and
`ItemsPanelTemplate` siblings are `[EditorBrowsable(Never)]` helpers for that code too, not a replacement
to call by hand. Instead, as in WinUI, either declare the template in XAML with an `x:Key` and look it up
by that key:

```xml
<Page.Resources>
    <DataTemplate x:Key="MyTemplate">
        <TextBlock Text="{Binding Name}" />
    </DataTemplate>
</Page.Resources>
```

```csharp
var template = (DataTemplate)Resources["MyTemplate"];
```

or, for a template whose markup is only known at runtime, load it with `XamlReader.Load`:

```csharp
var template = (DataTemplate)XamlReader.Load(
    """
    <DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
        <TextBlock Text="{Binding Name}" />
    </DataTemplate>
    """);
```

### `Shape` has a `protected` constructor

`Shape` has a `protected` constructor, as in WinUI. It was already abstract, so only
derived classes calling `base()` see the change, and they keep compiling.

### Types WinUI never lets you construct lose their implicit public constructor

This covers
template settings (`ProgressBarTemplateSettings`, `ComboBoxTemplateSettings`, …), event
arguments the framework raises (`ContentDialogOpenedEventArgs`, `HyperlinkClickEventArgs`,
…), automation pattern identifiers, and static-like helpers (`VisualTreeHelper`,
`XamlReader`, `BindingOperations`, `ToolTipService`, `FontWeights`, …). Use their static
members, or get the instance from the framework.

### `ObjectKeyFrameCollection` has a public constructor

`ObjectKeyFrameCollection` has a public constructor, as in WinUI.

### `XamlLight.CompositionLight` is `protected`

`XamlLight.CompositionLight` is `protected`, as in WinUI. Set it from your `XamlLight`
subclass.

## Related pages

- [Removed public APIs](xref:Uno.Development.MigratingToUno7.RemovedApis) if the member is gone altogether.
- [Behavior changes to re-test](xref:Uno.Development.MigratingToUno7.Behavior) for changes that compile but act differently.
- [XAML and default styles](xref:Uno.Development.MigratingToUno7.Xaml) for changes in markup.
- [Errors, symptoms and code search](xref:Uno.Development.MigratingToUno7.Lookup) to go from an error or symptom to its fix.
- [Upgrade steps](xref:Uno.Development.MigratingToUno7) to return to the main path.
