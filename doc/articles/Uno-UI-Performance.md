# Uno.UI - Performance

Improving the performance of an Uno.UI based application relates a lot to what a 
UWP application will benefit from.

Here's what to look for:
- Make sure to always have the simplest visual tree. There's nothing faster that something you don't draw.
- Reduce panels in panels depth. Use Grids and relative panels where possible.
- Force the size of images anywhere possible
- When binding the `Visibility` property, make sure to always set TargetNullValue and FallbackValue to collapsed :
	`Visibility="{Binding [IsAvailable], Converter={StaticResource boolToVisibility}, FallbackValue=Collapsed, TargetNullValue=Collapsed}"`
- Collapsed elements are not considered when measuring and arranging the visual tree, which makes them almost cost-less.
- When binding or animating (via visual state setters) the visibility property, make sure to use enable lazy loading:
	`x:DeferLoadStrategy="Lazy"` or `xamarin:DeferLoadStrategy="Lazy"` if visibility is set via bindings (UWP does not support it)
- ListView and GridView
	- Don't use template selectors inside the ItemTemplate, prefer using the ItemTemplateSelector on ListView/GridView
	- The default [ListViewItem and GridViewItem styles](https://github.com/unoplatform/uno/blob/74b7d5d0e953fcdd94223f32f51665af7ce15c60/src/Uno.UI/UI/Xaml/Style/Generic/Generic.xaml#L951) are very feature rich, yet that makes them quite slow. For instance, if you know that you're not likely to use selection features for a specific ListView, create a simpler ListViewItem style that some visual states, or the elements that are only used for selection.
- Animations
	- Prefer `Opacity` animations to `Visibility` animations (this avoids some measuring performance issue)
		- Unless the visual tree of the element is very big, where in this case `Visibility` is better suited.
	- Prefer Storyboard setters to `ObjectAnimationUsingKeyFrames` if there is only one key frame.
	- Prefer changing the properties of a visual element instead of switching opacity or visibility of an element.
- Image Assets
	- Try using an image that is appropriate for the DPI and screen size. 
	- The pixel size of an image will impact the loading time of the image. If the image is intentionally blurry, prefer reducing the physical size of the image over 
	  the compressed disk size of the image.
- Paths
	- Prefer reusing paths, duplication costs Main and GPU memory.
	- Prefer using custom fonts over paths where possible. Fonts are rendered extremely fast, and have a very low initialization time.
- Bindings
	- Prefer bindings with short paths
	- To shorten paths, use the `DataContext` property on containers, such as `StackPanel` or `Grid`.
- [`x:Phase`](https://docs.microsoft.com/en-us/windows/uwp/xaml-platform/x-phase-attribute)
	- For `ListView` instances with large templates, consider the use of x:Phase to reduce the number of bindings processed during item materialization.
	- It is only supported for items inside `ListViewItem` templates, it will be ignored for others.
	- It is also supported as `xamarin:Phase` on controls that do not have bindings. This feature is not supported by UWP.
	- It is only supported for elements under the `DataTemplate` of a `ListViewItem`. The 
	attribute is ignored for templates of `ContentControl` instances, or any other control.


## Performance Tracing

### FrameworkTemplatePool
The framework template pool manages the pooling of ControlTemplate and DataTemplates, and in most cases, the recycling of controls should be high.

- `CreateTemplate` is raised when a new instance of a template is created. This is an expensive operations should happen the least often as possible.
- `RecycleTemplate` is raised when an active instance of a template is placed into the pool. This should happen often.
- `ReuseTemplate` is raised when a pooled template is provided to a control asking for a specific data template.
- `ReleaseTemplate` is raised when a pooled template instance has not been used for a while.

If the `ReuseTemplate` occurrences is low, this usually means that there is a memory leak to investigate.
