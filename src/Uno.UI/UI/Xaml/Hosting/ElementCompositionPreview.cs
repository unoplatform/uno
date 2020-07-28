using System.Linq;

namespace Windows.UI.Xaml.Hosting
{
	public partial class ElementCompositionPreview
	{
		private const string ChildVisualName = "childVisual";

		public static global::Windows.UI.Composition.Visual GetElementVisual(global::Windows.UI.Xaml.UIElement element)
		{
#if __SKIA__
			return element.Visual;
#else
			return new Composition.Visual(null) { NativeOwner = element };
#endif
		}

		public static void SetElementChildVisual(global::Windows.UI.Xaml.UIElement element, global::Windows.UI.Composition.Visual visual)
		{
#if __IOS__
            element.Layer.AddSublayer(visual.NativeLayer);
            visual.NativeOwner = element;
            element.ClipsToBounds = false;
            (element as FrameworkElement).SizeChanged += 
                (s, e) => visual.NativeLayer.Frame = new CoreGraphics.CGRect(0, 0, element.Frame.Width, element.Frame.Height);
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
	}
}
