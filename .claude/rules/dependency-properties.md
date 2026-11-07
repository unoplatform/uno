---
description: DependencyProperty registration conventions for Uno.UI controls. Auto-loaded when editing Uno.UI source.
paths:
  - "src/Uno.UI/**/*.cs"
---

# DependencyProperty (Uno.UI)

For full templates, copy from existing controls — `StackPanel` (instance `[GeneratedDependencyProperty]`), `Canvas` (attached `[GeneratedDependencyProperty]`), `RangeBase` (manual changed + coerce), `Button` (manual `Register`). Full generator reference: `doc/articles/uno-development/Internal-DependencyProperty-Generator.md`. The non-obvious must-knows:

- **Prefer `[GeneratedDependencyProperty]`** for new Uno-authored properties. Properties ported from WinUI (`*.properties.cpp` or WinUI's generated property code) keep the 1:1 manual `Register` form instead (see the `/winui-port` skill, §2.9). Put the attribute on a partial property definition; the generator implements the property and declares and registers `XProperty`:
  ```csharp
  [GeneratedDependencyProperty(DefaultValue = 0.0d, ChangedCallback = true, Options = FrameworkPropertyMetadataOptions.AffectsMeasure)]
  public partial double Spacing { get; set; }
  ```
  Attached properties put it on a static partial `GetX` method, with an optional partial `SetX`:
  ```csharp
  [GeneratedDependencyProperty(DefaultValue = 0.0d, AttachedBackingFieldOwner = typeof(UIElement),
      Options = FrameworkPropertyMetadataOptions.AutoConvert | FrameworkPropertyMetadataOptions.AffectsArrange)]
  public static partial double GetLeft(UIElement element);

  public static partial void SetLeft(UIElement element, double length);
  ```
  A hand-written (non-partial) `SetX` is left alone — validate there, then call the generated `SetXValue(element, value)`.
- **Don't declare `XProperty`** next to a generated property — a hand-written one is a build error (`UnoInternal0015`), and the old `XProperty { get; } = CreateXProperty()` pattern and `Attached = true` no longer compile. The generated identifier takes the accessibility of the CLR property (or `GetX`). Declare `static partial DependencyProperty XProperty { get; }` only when the identifier needs a different accessibility or attributes such as `[NotImplemented]`. XML docs belong on the CLR property (or `GetX`), not on an identifier declaration.
- **Generated callbacks are wired by name**: `OnXChanged` (or `ChangedCallbackName = nameof(...)`), `CoerceX`, and `GetXDefaultValue()` for non-constant defaults. A method with the conventional name is wired even without `ChangedCallback = true`, so adding or renaming one changes behavior; an unsupported signature is a build error. Changed callbacks may take `(DependencyObject, DependencyPropertyChangedEventArgs)`, `(DependencyPropertyChangedEventArgs)`, `(T oldValue, T newValue)` or `()`. A sender parameter (changed callback, or static coerce callback) must be `object` or a type that the containing type (attached: the target type) converts to implicitly, such as `DependencyObject`; an unrelated sender type doesn't match.
- **Generated DPs register after the hand-written static initializers of the same type** (generated files compile last): a hand-written static field initializer must not read a generated `XProperty` of its own type — it is still `null`.
- **Manual registration** uses `FrameworkPropertyMetadata` (not bare `PropertyMetadata`). Constructor arg order is fixed: `(defaultValue, propertyChangedCallback, coerceValueCallback)`. Swapping changed/coerce compiles but fails at runtime.
- **Primitive defaults use the cached boxes** — `BoolBoxes.False`, `IntBoxes.Zero`, `DoubleBoxes.Zero` (namespace `Uno.UI.Helpers.Boxes`), not `default(T)` or a bare `false`/`0`, which allocate on every registration. Match the box family to the property's declared type: a `typeof(double)` property takes `DoubleBoxes.Zero`, never `IntBoxes.Zero` — the wrong family compiles and then throws on the first `(double)GetValue(...)`. For a non-constant, use `Boxer.Box(value)`. The `UnoInternal0002` analyzer enforces this. The generator applies the boxes itself (and converts `DefaultValue = 0` on a `double` property to `0d`).
- The DP identifier is `public static DependencyProperty XProperty { get; }` (get-only auto-property), never a mutable field.
- Manual callback signatures:
  - changed: `private static void OnXChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)`
  - coerce: `private static object CoerceX(DependencyObject d, object baseValue, DependencyPropertyValuePrecedences _)` — returns `object`, must cast `baseValue` to the property type.
- **Set the right `FrameworkPropertyMetadataOptions`** (`AffectsMeasure`/`AffectsArrange`/`AffectsRender`/`Inherits`/`AutoConvert`). Omitting `AffectsMeasure` on a layout-affecting property leaves stale layout with **no compiler warning**.
- **Read-only properties in Uno use a private setter** — `{ get; private set; }` on a generated property, `private set => SetValue(XProperty, value)` otherwise — *not* the WinUI `DependencyPropertyKey` pattern.
- **Non-generated attached properties** need public static `GetX`/`SetX` accessors, each marked `[DynamicDependency(nameof(GetX))]` / `[DynamicDependency(nameof(SetX))]` so trimming/AOT keeps them.
- `DependencyObject` is a plain **class** on every target (as in WinUI) - inherit from it directly. The pre-7.0 interface and its `DependencyObjectGenerator` are gone.

Place DP definitions in a `MyControl.Properties.cs` partial.
