---
uid: Uno.Contributing.DependencyPropertyGenerator
---

# The DependencyProperty Generator

Uno provides an internal source generator that writes the boilerplate of dependency properties: the `{Name}Property` identifier and its registration, the CLR accessors, and a cache of the property's current value.

The generator is very specific to the internals of Uno, so it isn't available outside of Uno. It runs on `Uno.UI` and `Uno.UI.UnitTests`, and is driven by the internal `Uno.UI.Xaml.GeneratedDependencyPropertyAttribute`. The user code is a C# 13 partial property or partial method definition, which the generator implements.

## Instance dependency properties

Put the attribute on a partial property definition:

```csharp
[GeneratedDependencyProperty(DefaultValue = 0.0d, ChangedCallback = true, Options = FrameworkPropertyMetadataOptions.AffectsMeasure)]
public partial double Spacing { get; set; }

private void OnSpacingChanged(double oldValue, double newValue)
{
}
```

The generator declares `public static DependencyProperty SpacingProperty { get; }`, registers it with `DependencyProperty.Register`, and implements `Spacing`: the getter reads the local cache and the setter calls `SetValue`.

- The containing type must be a partial class deriving from `DependencyObject`. Nested and generic types are supported, but every containing type must be partial too.
- The supported accessor shapes are `{ get; set; }`, a get-only `{ get; }`, and a setter with narrower accessibility, such as `{ get; private set; }`. Read-only dependency properties use `private set`: Uno doesn't use `DependencyPropertyKey`.
- `init` accessors, static and abstract properties, indexers, ref returns and explicit interface implementations aren't supported.
- The modifiers of the definition (`public`, `new`, `virtual`, `override` and so on) and of its accessors are repeated on the implementation. XML docs and attributes such as `[NotImplemented]` stay on the definition: C# merges them across the two parts.

The generated identifier is a static get-only property, never a field, with the same accessibility as the CLR property. It gets the `new` modifier when a base type already has an accessible member with the same name.

## Attached dependency properties

Put the attribute on a static partial `Get{Name}` method definition, and optionally declare a partial `Set{Name}` method:

```csharp
[GeneratedDependencyProperty(DefaultValue = 0.0d, AttachedBackingFieldOwner = typeof(UIElement), Options = FrameworkPropertyMetadataOptions.AutoConvert | FrameworkPropertyMetadataOptions.AffectsArrange)]
public static partial double GetLeft(UIElement element);

public static partial void SetLeft(UIElement element, double length);
```

The generator declares `LeftProperty` with the accessibility of `GetLeft`, registers it with `DependencyProperty.RegisterAttached`, and implements both methods.

- The containing type must be a static class or a class deriving from `DependencyObject`.
- `Get{Name}` must be static and non-generic, return the property value, and take a single parameter whose type derives from `DependencyObject`.
- `Set{Name}` is optional. When it's a partial definition, it must be static, return `void`, and take the same target type and the property type.
- The generator always emits the private helpers `Get{Name}Value(target)` and `Set{Name}Value(target, value)`. A hand-written, non-partial `Set{Name}` is left alone, so it can validate the value before calling the helper:

```csharp
[GeneratedDependencyProperty(DefaultValue = 1, ChangedCallback = true, AttachedBackingFieldOwner = typeof(UIElement), Options = FrameworkPropertyMetadataOptions.AffectsMeasure)]
public static partial int GetRowSpan(UIElement element);

public static void SetRowSpan(UIElement element, int rowSpan)
{
    if (rowSpan <= 0)
    {
        throw new ArgumentException("The value must be above zero", nameof(rowSpan));
    }

    SetRowSpanValue(element, rowSpan);
}
```

## Declaring the identifier explicitly

Usually, the `{Name}Property` identifier isn't declared at all. Declare it only when the identifier needs an accessibility that differs from the CLR property, or attributes like `[NotImplemented]` (including attributes under `#if`):

```csharp
internal static partial DependencyProperty KeyboardAcceleratorsProperty { get; }

[GeneratedDependencyProperty(DefaultValue = null, ChangedCallback = true)]
public partial IList<KeyboardAccelerator> KeyboardAccelerators { get; private set; }
```

The declaration must be a static get-only partial property definition of type `DependencyProperty`, in the same type. The generator implements it with the declared modifiers, over a private static read-only field, because partial properties can't have initializers. Any other member named `{Name}Property` is an error.

XML docs aren't a reason to declare the identifier: they belong on the CLR property or the `Get{Name}` method.

## Attribute arguments

| Argument | Description |
| --- | --- |
| `DefaultValue` | The default value of the property. See [Default value](#default-value). |
| `Options` | The `FrameworkPropertyMetadataOptions` of the property, such as `AffectsMeasure` or `Inherits`. |
| `ChangedCallback` | Requires an `On{Name}Changed` property changed callback, and fails the build if there's no valid one. |
| `ChangedCallbackName` | The name of the property changed callback, when it isn't `On{Name}Changed`. |
| `CoerceCallback` | Requires a `Coerce{Name}` coerce callback, and fails the build if there's no valid one. |
| `LocalCache` | Enables or disables the local cache. Enabled by default. |
| `AttachedBackingFieldOwner` | The type that holds the local cache of an attached property. |

## Default value

The default value comes from, in order of use:

1. `DefaultValue`. The constant is converted to the property type the way the compiler would convert it, so `DefaultValue = 0` on a `double` property is `0d`. Strings, characters, Booleans, numbers (including `NaN` and infinities), enum values, `null` and `typeof(...)` are supported. A value that doesn't fit the property type is an error.
1. A static parameterless `Get{Name}DefaultValue()` method on the containing type, for values that aren't constants:

    ```csharp
    private static Thickness GetPaddingDefaultValue() => Thickness.Empty;
    ```

    It must return the property type, a type that converts to it by reference or boxing, or `object` for a pre-boxed value. Setting `DefaultValue` as well is an error.
1. Otherwise, `default(T)`.

When the `Uno.UI.Helpers.Boxes.Boxer` class is accessible, the generated code avoids boxing allocations and satisfies the `UnoInternal0002` analyzer. Defaults use the cached boxes of the `Uno.UI.Helpers.Boxes` namespace (for example `BoolBoxes.False` or `DoubleBoxes.Zero`), and setters call `Boxer.Box(value)` when `Boxer` has a `Box` overload for the property type. `Box` overloads are discovered from `Boxer`, so a new overload is used without changes to the generator. Cached boxes are used for the values the generator knows (`false`/`true`, `-1`/`0`/`1` and `0d`/`1d`), only when the corresponding field exists.

## Property changed callback

The callback is a method named `On{Name}Changed`, or `ChangedCallbackName`, declared on the containing type. It's used when `ChangedCallback = true` or `ChangedCallbackName` is set, and also whenever a method with that name exists, even if `ChangedCallback` is `false`.

These signatures are supported, in order of precedence. The first matching overload is used and other overloads are ignored:

| Signature | Notes |
| --- | --- |
| `(DependencyObject sender, DependencyPropertyChangedEventArgs args)` | The sender can also be typed as `object`, or as another type that the containing type (for an instance property) or the target type (for an attached property) converts to implicitly, such as that type or one of its base types. |
| `(DependencyPropertyChangedEventArgs args)` | |
| `(T oldValue, T newValue)` | The parameters can be of any type that `T` converts to implicitly. |
| `()` | |

For an instance property, the callback can be an instance method, including `partial void` and `virtual` methods, or a static method. For an attached property, it must be static, and the sender is the target of the property.

If a callback is required, or a method with the callback name exists, but no overload has a supported signature, the build fails.

## Coerce callback

The coerce callback is a method named `Coerce{Name}` that returns `object`. It's used when `CoerceCallback = true` or when a method with that name exists.

- For an instance property: `object Coerce{Name}(object baseValue)`, optionally followed by a `DependencyPropertyValuePrecedences precedence` parameter. A static method taking the `DependencyObject` instance as its first parameter is also supported.
- For an attached property: `static object Coerce{Name}(DependencyObject target, object baseValue)`, optionally followed by a `DependencyPropertyValuePrecedences precedence` parameter.

The base value parameter can also be typed as the property type, in which case it's cast. The first parameter of a static coerce callback follows the same rule as the sender of a property changed callback: it can be typed as `object`, or as another type that the containing type or the target type converts to implicitly.

## Local cache

Reading a value through the dependency property system is slower than reading a field, and requires a cast. With the local cache, which is enabled by default, the generated getter reads a backing field instead. The field is filled on the first read, then kept up to date through `FrameworkPropertyMetadata.BackingFieldUpdateCallback`, which the property system invokes whenever the effective value of the property changes.

- For instance properties, the backing field is declared on the containing type. The flags that track whether each field is filled, and the values of `bool` properties, are packed into `uint` fields. Set `LocalCache = false` to make the getter call `GetValue` instead.
- For attached properties, the value is only cached when `AttachedBackingFieldOwner` is set. The owner must be a non-generic partial class declared in the same project, typically `UIElement`, and related to the target type: the target type itself, one of its base types, or a type deriving from it. The generator adds the backing fields to that class, and `Get{Name}` uses them for targets of that type, falling back to `GetValue` for other targets. Setting `LocalCache = true` without an owner is an error.
- A local cache holds a strong reference to the value, so it can't be combined with `FrameworkPropertyMetadataOptions.WeakStorage`. Set `LocalCache = false` on such properties.

## Registration order

Registration is eager: each generated identifier is initialized by a static initializer in the generated part of the type, and no static constructor is added. The compiler runs the static initializers of a partial type in the order it reads the files, and generated files come last. So, generated dependency properties are registered after the hand-written static initializers of the same type run, including manual `DependencyProperty.Register` calls. For the same reason, a hand-written static field initializer can't read a generated identifier of its own type, because it's still `null` at that point. Static constructors and instance members aren't affected.

## Diagnostics

The generator reports these errors:

| ID | Reported when |
| --- | --- |
| `UnoInternal0010` | The attribute target isn't a partial definition, or it already has an implementation part. |
| `UnoInternal0011` | The attribute is on a `static DependencyProperty {Name}Property` identifier, the previous pattern. Move it to the partial property or `Get{Name}` method, and remove the identifier. |
| `UnoInternal0012` | The property shape isn't supported: an `init` accessor, no getter, a static or abstract property, an indexer, a ref return, or an explicit interface implementation. |
| `UnoInternal0013` | The containing type isn't a class deriving from `DependencyObject` (or a static class, for attached properties), or one of the containing types isn't partial. |
| `UnoInternal0014` | `Get{Name}` or a partial `Set{Name}` doesn't have a valid attached property accessor signature. |
| `UnoInternal0015` | The type already declares a member named `{Name}Property` that isn't a partial property definition. |
| `UnoInternal0016` | An explicit `{Name}Property` declaration isn't a static get-only partial property definition of type `DependencyProperty`. |
| `UnoInternal0017` | Both `DefaultValue` and a `Get{Name}DefaultValue()` method are present. |
| `UnoInternal0018` | `Get{Name}DefaultValue` isn't a static parameterless method that returns a value. |
| `UnoInternal0019` | `DefaultValue` isn't compatible with the property type, for example `null` for a non-nullable value type, or a number that's out of range. It isn't reported when the property type or the value can't be resolved. |
| `UnoInternal0020` | A property changed callback is required, or a method with its name exists, but no overload has a supported signature. |
| `UnoInternal0021` | A coerce callback is required, or a `Coerce{Name}` method exists, but no overload has a supported signature. |
| `UnoInternal0022` | `LocalCache = true` is set on an attached property without `AttachedBackingFieldOwner`. |
| `UnoInternal0023` | `AttachedBackingFieldOwner` isn't a non-generic partial class declared in the same project, or isn't related to the target type. |
| `UnoInternal0024` | `FrameworkPropertyMetadataOptions.WeakStorage` is combined with a local cache. |
| `UnoInternal0025` | `Get{Name}DefaultValue()` returns a type the property can't read back, for example `int` for a `double` property. Return the property type, a type it converts to by reference or boxing, or `object` for a pre-boxed value. |

An invalid property is skipped, but the other properties of the type are still generated. A type that can't be resolved never causes the identifier to be skipped: when the property type or `DefaultValue` refers to a missing type, the compiler already reports an error, so the generator doesn't report its own and still generates `{Name}Property`. The generator's behavior is covered by the `Given_DependencyPropertyGenerator` tests in `Uno.UI.SourceGenerators.Tests`.

## Interaction with other tools

Source generators can't see each other's output. For this reason, the XAML generator treats a source-declared property or `Get{Name}` method carrying `[GeneratedDependencyProperty]`, on the target type or one of its base types, as a dependency property, so that style setters, `{ThemeResource}` and bindings use the dependency property code paths. Similarly, the WinAppSDK sync tool (`Uno.WinAppSDKSyncGenerator`) runs this generator before comparing Uno.UI with the WinUI API, so that generated members aren't also emitted as `[NotImplemented]` stubs. The tool compiles without the `Generated` folders, so types that only exist as stubs are unresolved there, which is why an unresolved type must not prevent the identifier from being generated.

## Benchmarks

These results were measured on WebAssembly, using the interpreter (Uno.Wasm.Bootstrap 1.3.0-dev.42), when the local cache was introduced.

Before DP caching:

```console
SimpleDPBenchmark.DP_Write: InProcess(Toolchain=InProcessToolchain, IterationCount=5, LaunchCount=1, WarmupCount=1)
Runtime = ; GC = 
Mean = 885.7123 us, StdErr = 12.2709 us (1.39%); N = 5, StdDev = 27.4385 us
Min = 850.0629 us, Q1 = 860.1191 us, Median = 886.3569 us, Q3 = 910.9833 us, Max = 921.5082 us
IQR = 50.8643 us, LowerFence = 783.8227 us, UpperFence = 987.2797 us
ConfidenceInterval = [780.0563 us; 991.3684 us] (CI 99.9%), Margin = 105.6560 us (11.93% of Mean)
Skewness = 0, Kurtosis = 1.19, MValue = 2
-------------------- Histogram --------------------
[846.079 us ; 879.367 us) | @@
[879.367 us ; 907.448 us) | @@
[907.448 us ; 935.549 us) | @
---------------------------------------------------

SimpleDPBenchmark.DP_Read: InProcess(Toolchain=InProcessToolchain, IterationCount=5, LaunchCount=1, WarmupCount=1)
Runtime = ; GC = 
Mean = 83.8832 us, StdErr = 0.7938 us (0.95%); N = 5, StdDev = 1.7749 us
Min = 82.4768 us, Q1 = 82.5403 us, Median = 83.0732 us, Q3 = 85.6311 us, Max = 86.6846 us
IQR = 3.0908 us, LowerFence = 77.9041 us, UpperFence = 90.2674 us
ConfidenceInterval = [77.0487 us; 90.7177 us] (CI 99.9%), Margin = 6.8345 us (8.15% of Mean)
Skewness = 0.6, Kurtosis = 1.39, MValue = 2
-------------------- Histogram --------------------
[81.867 us ; 83.683 us) | @@@
[83.683 us ; 85.486 us) | @
[85.486 us ; 87.593 us) | @
---------------------------------------------------
```

After DP caching:

```console
// * Detailed results *
SimpleDPBenchmark.DP_Write: InProcess(Toolchain=InProcessToolchain, IterationCount=5, LaunchCount=1, WarmupCount=1)
Runtime = ; GC = 
Mean = 938.0835 us, StdErr = 10.0558 us (1.07%); N = 5, StdDev = 22.4856 us
Min = 910.9253 us, Q1 = 915.3540 us, Median = 940.2124 us, Q3 = 959.7485 us, Max = 961.5698 us
IQR = 44.3945 us, LowerFence = 848.7622 us, UpperFence = 1,026.3403 us
ConfidenceInterval = [851.4996 us; 1,024.6674 us] (CI 99.9%), Margin = 86.5839 us (9.23% of Mean)
Skewness = -0.09, Kurtosis = 0.87, MValue = 2
-------------------- Histogram --------------------
[903.848 us ; 939.385 us) | @@
[939.385 us ; 962.397 us) | @@@
---------------------------------------------------

SimpleDPBenchmark.DP_Read: InProcess(Toolchain=InProcessToolchain, IterationCount=5, LaunchCount=1, WarmupCount=1)
Runtime = ; GC = 
Mean = 10.1375 us, StdErr = 0.1325 us (1.31%); N = 5, StdDev = 0.2964 us
Min = 9.6547 us, Q1 = 9.8525 us, Median = 10.2910 us, Q3 = 10.3457 us, Max = 10.3603 us
IQR = 0.4931 us, LowerFence = 9.1128 us, UpperFence = 11.0854 us
ConfidenceInterval = [8.9962 us; 11.2788 us] (CI 99.9%), Margin = 1.1413 us (11.26% of Mean)
Skewness = -0.7, Kurtosis = 1.52, MValue = 2
-------------------- Histogram --------------------
[ 9.503 us ;  9.806 us) | @
[ 9.806 us ; 10.174 us) | @
[10.174 us ; 10.512 us) | @@@
---------------------------------------------------
```
