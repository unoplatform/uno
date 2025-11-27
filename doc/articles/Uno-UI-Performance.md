---
uid: Uno.Development.Performance
---

# Uno.UI - Performance

This article lists various performance tips to optimize your Uno Platform application.

Here's what to look for:

- Make sure to choose the right renderer ([Skia](xref:uno.features.renderer.skia) or [Native](xref:uno.features.renderer.native)) for your application, depending on the feature set you'll be using.
- Make sure to always have the simplest visual tree. There's nothing faster than something you don't draw.
- Reduce panels in panels depth. Use Grids and relative panels where possible.
- Force the size of images anywhere possible, using explicit `Width` and `Height` properties.
- `Collapsed` elements are not considered when measuring and arranging the visual tree, which makes them almost costless. Consider `x:Load` below for further optimizations.
- When binding or animating (via `VisualState.Setters`) the `Visibility` property, make sure to enable lazy loading:
 `x:Load="False"`.
- Use `x:Load={x:Bind MyVisibility}` where appropriate as toggling from `true` to `false` effectively removes a part of the visual tree from memory. Note that setting back to true re-creates the visual tree.
- `ListView` and `GridView`
  - Don't use template selectors inside the ItemTemplate, prefer using the `ItemTemplateSelector` on `ListView`/`GridView`.
  - The default [`ListViewItem` and `GridViewItem` styles](https://github.com/unoplatform/uno/blob/74b7d5d0e953fcdd94223f32f51665af7ce15c60/src/Uno.UI/UI/Xaml/Style/Generic/Generic.xaml#L951) are very feature-rich, yet that makes them quite slow. For instance, if you know that you're not likely to use selection features for a specific ListView, create a simpler ListViewItem style that some visual states, or the elements that are only used for selection.
  - If items content frequently change (e.g. live data in TextBlock) on iOS and Android, ListView items rendering can require the use of the `not_win:AreDimensionsConstrained="True"` [uno-specific property](https://github.com/unoplatform/uno/blob/7355d66f77777b57c660133d5ec011caaa810e29/src/Uno.UI/UI/Xaml/FrameworkElement.cs#L86).
  
    This attribute prevents items in a list from requesting their parent to be re-measured when their properties change. It's safe to use the `AreDimensionsConstrained` property when items always have the same size regardless of bound data, and the items and list are stretched in the non-scrolling direction. If item sizes can change when the bound data changes (eg, if they contain bound text that can wrap over multiple lines, images of undetermined size, etc), or if the list is wrapped to the items, then you shouldn't set `AreDimensionsConstrained` because the list does need to remeasure itself when item data changes in that case.

    You'll need to set the property on the top-level element of your item templates, as follows:

      ```xml
      <ResourceDictionary xmlns:xamarin="http://uno.ui/xamarin" mc:Ignorable="d xamarin" ...>
      <DataTemplate x:Key="MyTemplate">
        <Grid Height="44" not_win:AreDimensionsConstrained="True">
        ...
        </Grid>
      </DataTemplate>
      ```

    Note that WinUI does not need this, and the issue is [tracked in Uno here](https://github.com/unoplatform/uno/issues/6910).
  - Avoid controls that contain inline popups, menus, or flyouts. Doing so will create as many popups as there are items visible on the screen. As in general, there is only one popup visible at a time, it is generally best to move the popup to a separate static resource.

- Updating items in `ItemsControl` can be quite expensive, using `ItemsRepeater` is generally faster at rendering similar content.
- Animations
  - Prefer `Opacity` animations to `Visibility` animations (this avoids some measuring performance issues).
    - Unless the visual tree of the element is very big, where in this case `Visibility` is better suited.
  - Prefer Storyboard setters to `ObjectAnimationUsingKeyFrames` if there is only one key frame.
  - Prefer changing the properties of a visual element instead of switching opacity or visibility of an element.
    - Manually created `Storyboard` instances do not stop automatically. Make sure that if you invoke `Storyboard.Begin()`, invoke `Storyboard.Stop()` when the animated content is unloaded, otherwise resources may be spent animating invisible content.
    - `ProgressRing` and `ProgressBar` controls indeterminate mode generally consume rendering time. Make sure to set those to determinate modes when not visible.
    - Troubleshooting of animations can be done by enabling the following logger:

      ```csharp
      builder.AddFilter("Windows.UI.Xaml.Media.Animation", LogLevel.Debug);
      builder.AddFilter("Microsoft.UI.Xaml.Media.Animation", LogLevel.Debug);
      ```

      The logger will provide all the changes done to animated properties, with element names.

- Image Assets
  - Try using an image that is appropriate for the DPI and screen size.
  - Whenever possible, specify and explicit Width and Height on `Image`.
  - The pixel size of an image will impact the loading time of the image. If the image is intentionally blurry, prefer reducing the physical size of the image over
   the compressed disk size of the image.
- Paths
  - Prefer reusing paths, duplication costs Main and GPU memory.
  - Prefer using custom fonts over paths where possible. Fonts are rendered extremely fast and have a very low initialization time.
- Bindings
  - Using `x:Bind` is generally faster as it involves less or no reflection.
  - Prefer bindings with short paths.
  - To shorten paths, use the `DataContext` property on containers, such as `StackPanel` or `Grid`.
  - Adding a control to loaded `Panel` or `ContentControl` does propagate the parent's DataContext immediately. If the new control has its `DataContext` immediately overridden to something else, ensure to set the DataContext before adding the control to its parent. This will avoid having bindings be refreshed twice needlessly.
  - Add the `Windows.UI.Xaml.BindableAttribute` or `System.ComponentModel.BindableAttribute` on non-DependencyObject classes.
    - When data binding to classes not inheriting from `DependencyObject`, in Debug configuration only, the following message may appear:

      ```console
      The Bindable attribute is missing and the type [XXXX] is not known by the MetadataProvider.
      Reflection was used instead of the binding engine and generated static metadata. Add the Bindable  attribute to prevent this message and performance issues.
      ```

      This message indicates that the binding engine will fall back on reflection based code, which is generally slow. To compensate for this, Uno use the `BindableTypeProvidersSourceGenerator`, which generates static non-generic code to avoid reflection operations during binding operations.
      This attribute is inherited and is generally used on ViewModel based classes.
- [`x:Phase`](https://learn.microsoft.com/windows/uwp/xaml-platform/x-phase-attribute)
  - For `ListView` instances with large templates, consider the use of x:Phase to reduce the number of bindings processed during item materialization.
  - It is only supported for items inside `ListViewItem` templates, it will be ignored for others.
  - It is also supported as `not_win:Phase` on controls that do not have bindings. This feature is not supported by WinUI.
  - It is only supported for elements under the `DataTemplate` of a `ListViewItem`. The
 attribute is ignored for templates of `ContentControl` instances, or any other control.
  - When binding to Brushes with a solid color, prefer binding to the `Color` property like this if the brush type does not change:

      ```xml
      <TextBlock Text="My Text">
          <TextBlock.Foreground>
              <SolidColorBrush Color="{x:Bind Color, Mode=OneWay, FallbackValue=Red}" />
          </TextBlock.Foreground>
      </TextBlock>
      ```

- Resources
  - Avoid using `x:Name` in `ResourceDictionary` as those force early instantiation of the resource
  - Use [`Uno.XamlMerge.Task`](https://github.com/unoplatform/uno.xamlmerge.task) to merge all top-level `App.xaml` resource dictionaries

## WebAssembly specifics

- Building your application in **Release** configuration is critical to get the best performance.
- Make sure to use the latest stable release of [Uno.Wasm.Bootstrap packages](https://www.nuget.org/packages/Uno.Wasm.Bootstrap)
- Enable [AOT or PG-AOT](xref:Uno.Wasm.Bootstrap.Runtime.Execution) to get the best performance.
- Consider enabling the [`Jiterpreter`](xref:Uno.Wasm.Bootstrap.Runtime.Execution#jiterpreter-mode) mode for faster performance.
- When [recording a PG-AOT profile](xref:Uno.Wasm.Bootstrap.Runtime.Execution#profile-guided-aot), make sure to run through most of your application before saving the profile.
- Adjusting the GC configuration may be useful to limit the collection runs on large allocations. Add the following to your `csproj` file:

    ```xml
    <ItemGroup>
      <WasmShellMonoEnvironment Include="MONO_GC_PARAMS" Value="soft-heap-limit=512m,nursery-size=64m,evacuation-threshold=66,major=marksweep" />
    </ItemGroup>
    ```

  You can adjust the `nursery-size` and `soft-heap-limit` based on your application's memory consumption characteristics. See the [.NET GC configuration](https://learn.microsoft.com/xamarin/android/internals/garbage-collection#configuration) for more details.

- The size of the application can be reduced by:

  - Enabling the [IL Linker](features/using-il-linker-webassembly.md)
  - Enabling [XAML Resources Trimming](features/resources-trimming.md)

## Android specifics

- Adjust the [GC configuration](https://learn.microsoft.com/xamarin/android/internals/garbage-collection#configuration) by modifying the `environment.conf` file with parameters matching your application
- Enable `LLVM` in `Release` with `-p:EnableLLVM=true` for better runtime performance at the expense of package size and longer compilation times
- Enable `Marshal Methods` in `Release` with `-p:AndroidEnableMarshalMethods=true` to improve startup performance (.NET 8 +)
- [Enable Startup Tracing](https://devblogs.microsoft.com/dotnet/performance-improvements-in-dotnet-maui/#record-a-custom-aot-profile) by running the following:

    ```bash
    dotnet add package Mono.AotProfiler.Android
    dotnet build -t:BuildAndStartAotProfiling
    # Wait until the app launches, then navigate around the most common screens
    dotnet build -t:FinishAotProfiling
    ```

  This will produce a `custom.aprof` in your project directory. Move the file to the `Android` folder and add the following to your `csproj`:

    ```xml
    <ItemGroup>
      <AndroidAotProfile Include="Android/custom.aprof" />
    </ItemGroup>
    ```

- Enable `Full AOT` instead of `Startup Tracing` in `Release` with `-p:AndroidEnableProfiledAot=false` to get the best runtime performance

  This will make your package size **significantly** larger and your compilation times longer.

  You may combine this with `-p:EnableLLVM=true` and `-p:AndroidEnableMarshalMethods=true` to get even better performance.

- Use [String Resource Trimming](xref:Uno.Features.StringResourceTrimming) to improve package size and startup time

## iOS Native Renderer Specifics

On iOS with the native renderer enabled, memory leaks can happen very frequently when using cross-references on UIElement instances. Some high level analysis can be done using [this C# analyzer](https://github.com/jonathanpeppers/memory-analyzers), to determine obvious patterns that cause memory leaks.

You'll find below other known memory leak patterns on iOS Native:
- `VisualStateManager` must be set on the root element of a XAML file. Placing it on any other control will cause a native controls leak.

## Skia Targets Specifics

- On Desktop targets, it's possible to change the composition refresh rate using `FeatureConfiguration.CompositionTarget.FrameRate`. The default value is 60 (frames per second).
- On all targets:
  - It's possible to set `DebugSettings.EnableFrameRateCounter` in `App.OnLaunched` in order to view a top-left indicator. It indicates the current frames per second, as well as the time spent rendering a composition frame, in milliseconds.
  - If the indicator does not change, this means that the UI is not refreshing.
  - If it is, but nothing is changing visually, it could be that a XAML or Composition animation is still running, see the `ProgressRing` section in this document.

## Advanced performance Tracing

### Profiling applications

A profiling guide for Uno Platform apps is [available here](xref:Uno.Tutorials.ProfilingApplications).

### FrameworkTemplatePool

The framework template pool manages the pooling of ControlTemplates and DataTemplates, and in most cases, the recycling of controls should be high.

- `CreateTemplate` is raised when a new instance of a template is created. This is an expensive operation that should be performed as rarely as possible.
- `RecycleTemplate` is raised when an active instance of a template is placed into the pool. This should happen often.
- `ReuseTemplate` is raised when a pooled template is provided to a control asking for a specific data template.
- `ReleaseTemplate` is raised when a pooled template instance has not been used for a while.

If the `ReuseTemplate` occurrences is low, this usually means that there is a memory leak to investigate.
