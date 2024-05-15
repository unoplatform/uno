---
uid: Uno.Contributing.DependencyPropertyGenerator
---

# The DependencyProperty Generator

Uno provides an internal `DependencyPproperty` code generator, used to provide the boiler plate for the property, as well as the handling of local-precedence property value caching.

This generator is not available outside of Uno as it is very specific to the needs of the internals of Uno.

The property getters and setters are not generated to avoid VS2019 (as of 16.7 Pre 2) intellisense issues when opening the Uno.UI solution for the first time or after a repository cleanup.

## DependencyProperty declaration

To declare a dependency property, the following code must be provided:

```csharp
[Uno.UI.Xaml.GeneratedDependencyProperty(DefaultValue = "")]
public static readonly DependencyProperty MyStringProperty = CreateMyStringProperty();

public string MyString
{
    get => GetMyStringValue();
    set => SetMyStringValue(value);
}
```

This will automatically generate the `MyStringProperty` member.

## DependencyProperty property changed

```csharp
[Uno.UI.Xaml.GeneratedDependencyProperty(DefaultValue = "")]
public static readonly DependencyProperty MyStringProperty = CreateMyStringProperty();

public string MyString
{
    get => GetMyStringValue();
    set => SetMyStringValue(value);
}

private void OnMyStringChanged(string oldValue, string newValue) 
{
}
```

Declaring an `OnMyStringChanged` will automatically include that method for the property changed. To ensure that the method is defined and used, set the `PropertyChangedCallback` attribute parameter.

The following callback signature is also supported:

```csharp
private void OnMyStringChanged(DependencyPropertyChangedEventArgs args) 
{
}
```

## Attached DependencyProperties generation

Attached dependency properties need to be declared this way, with the `GeneratedDependencyProperty` located on the `GetXXX` method:

```csharp
[GeneratedDependencyProperty(DefaultValue = 0.0d, AttachedBackingFieldOwner = typeof(UIElement), Attached = true)]
public static readonly DependencyProperty LeftProperty = CreateLeftProperty();

public static double GetLeft(DependencyObject obj)
    => GetLeftValue(obj);

public static void SetLeft(DependencyObject obj, double value)
    => SetLeftValue(obj, value);
```

This dependency property will be declared as `LeftProperty`, with the local-precedence cache backing field included in the `UIElement` class.

## DependencyProperty local precedence caching

This feature is about storing the current value that can be read through the C# property getter, or the `GetXXX` method of an attached property. The objective of this feature is to avoid spending time in the dependency property system to read the value, and avoid the type cast required when getting a `DependencyProperty` value.

Backing fields are automatically generated and maintained current through `FrameworkPropertyMetadata.BackingFieldUpdateCallback` which is invoked when the highest precedence value of a `DependencyProperty` is changed.

## GeneratedDependencyProperty options

Other attribute properties are available on `GeneratedDependencyProperty` to include:

- `PropertyChangedCallback` to force the inclusion of a property changed callback method and fail the build if there is none
- `CoerceCallback` to force the inclusion of a coerce callback method and fail the build if there is none
- `Options` to specify which `FrameworkPropertyMetadataOptions` to use
- `LocalCache` to enable or disable local precedence value caching (enabled by default)
- `AttachedBackingFieldOwner` to provide the type hosting the local cache backing field for attached properties
- `ChangedCallbackName` to control the name of the property changed callback method

## Benchmarks

### WebAssembly, using the interpreter (Uno.Wasm.Bootstrap 1.3.0-dev.42)

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

````console
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
