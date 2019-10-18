#if __WASM__
using System;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Controls
{
	public partial class ProgressRing : Control
	{
		private static void OnForegroundChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{

		}

		private static void OnIsActiveChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			var progressRing = dependencyObject as ProgressRing;
			var isActive = args.NewValue as bool?;

			if (progressRing != null && progressRing.IsLoaded && isActive.HasValue)
			{
				VisualStateManager.GoToState(progressRing, isActive.Value ? "Active" : "Inactive", false);
			}
		}

		protected override void OnLoaded()
		{
			base.OnLoaded();
			// The initial call to OnIsActiveChanged fires before ProgressRing is Loaded, so we also need to set a proper VisualState here
			VisualStateManager.GoToState(this, IsActive ? "Active" : "Inactive", false);
		}

		public ProgressRingTemplateSettings TemplateSettings
		{
			get
			{
				var result = new ProgressRingTemplateSettings()
				{
					EllipseDiameter = 3,
					MaxSideLength = 100
				};

				var size = Width.IsNaN() ? MinWidth : Width; // Strange, but ActualWidth is not working correctly here
				result.EllipseOffset = new Thickness(size * (Math.Sqrt(2) - 1) / 2); // This is the difference between inscribed and circumscribed circle, it ensures that dots will be visible after control rectangle clipping

				return result;
			}
		}
	}
}

#endif
