using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using Uno.UI;
using Windows.UI.Xaml.Controls.Primitives;

namespace UITests.Windows_UI_Xaml.UIElementTests
{
	[Sample("UIElement")]
	public sealed partial class UIElement_MeasureDirtyPath : Page
	{
		public UIElement_MeasureDirtyPath()
		{
			this.InitializeComponent();
		}

		private void changeOptimizeMeasure(object sender, RoutedEventArgs e)
		{
#if !WINAPPSDK
			if (sender is ToggleButton { IsChecked: true })
			{
				FeatureConfiguration.UIElement.UseInvalidateMeasurePath = true;
			}
			else
			{
				FeatureConfiguration.UIElement.UseInvalidateMeasurePath = false;
			}
#endif
		}

		internal void changeOptimizeElements(object sender, RoutedEventArgs e)
		{
#if !WINAPPSDK
			if (sender is ToggleButton { IsChecked: true })
			{
				FrameworkElementHelper.SetUseMeasurePathDisabled(elements2, true);
			}
			else
			{
				FrameworkElementHelper.SetUseMeasurePathDisabled(elements2, false);
			}
#endif
		}
	}
}
