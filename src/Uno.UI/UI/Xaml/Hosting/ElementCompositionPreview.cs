#nullable enable

using System.Linq;
using Windows.UI.Composition;

namespace Windows.UI.Xaml.Hosting;

/// <summary>
/// Enables access to composition visual objects that back XAML elements in the XAML composition tree.
/// </summary>
public partial class ElementCompositionPreview
{
#if __SKIA__
	private const string ChildVisualName = "childVisual";
#else
	static readonly Compositor _compositor = new Compositor();
#endif

	/// <summary>
	/// Retrieves the Windows.UI.Composition.Visual object that backs a XAML element in the XAML composition tree.
	/// </summary>
	/// <param name="element">The element for which to retrieve the Visual.</param>
	/// <returns>The Windows.UI.Composition.Visual object that backs the XAML element.</returns>
	public static Visual GetElementVisual(UIElement element)
	{
#if __SKIA__
		return element.Visual;
#else
		return new Composition.Visual(_compositor)
		{
			NativeOwner = element,
			// TODO: Switch to CompositionTargetGetter and assign a lambda instead.
			// This will make the Visual usable even if GetElementVisual is called when
			// the element is not loaded (note that XamlRoot is null when element isn't yet loaded)
			CompositionTarget = element.XamlRoot?.VisualTree.ContentRoot.CompositionTarget,
		};
#endif
	}

	/// <summary>
	/// Sets a custom Windows.UI.Composition.Visual as the last child of the element's visual tree.
	/// </summary>
	/// <param name="element">The element to add the child Visual to.</param>
	/// <param name="visual">The Visual to add to the element's visual tree.</param>
	public static void SetElementChildVisual(UIElement element, Visual visual)
	{
#if __IOS__
		element.Layer.AddSublayer(visual.NativeLayer);
		visual.NativeOwner = element;
		element.ClipsToBounds = false;

		if (element is FrameworkElement fe)
		{
			fe.SizeChanged +=
				(s, e) => visual.NativeLayer.Frame = new CoreGraphics.CGRect(0, 0, element.Frame.Width, element.Frame.Height);
		}
#elif __SKIA__

		var container = new Composition.ContainerVisual(element.Visual.Compositor) { Comment = ChildVisualName };
		container.Children.InsertAtTop(visual);

		if (element.Visual.Children.FirstOrDefault(v => v.Comment == ChildVisualName) is Composition.ContainerVisual cv)
		{
			element.Visual.Children.Remove(cv);
		}

		element.Visual.Children.InsertAtTop(container);
#endif
	}

#if __SKIA__
	public static void SetIsTranslationEnabled(UIElement element, bool value)
	{
		element.Visual.IsTranslationEnabled = value;
	}
#endif
}
