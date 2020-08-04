#nullable enable

using System.Linq;
using Windows.UI.Composition;

namespace Windows.UI.Xaml.Hosting
{
	public partial class ElementCompositionPreview
	{
		private const string ChildVisualName = "childVisual";

#if !__SKIA__
		static readonly Compositor _compositor = new Compositor();
#endif

		public static Visual GetElementVisual(UIElement element)
		{
#if __SKIA__
			return element.Visual;
#else
			return new Composition.Visual(_compositor) { NativeOwner = element };
#endif
		}

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
	}
}
