using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Samples.Controls;
using MUXPopup = Microsoft.UI.Xaml.Controls.Primitives.Popup;

namespace UITests.Shared.Windows_UI_Xaml_Controls.Popup
{
	[Sample("Popup", Name = "Popup_Windowed", IsManualTest = true, IgnoreInSnapshotTests = true, Description = "Popups which leave the window, for checking real OS input and transparency.")]
	public sealed partial class Popup_Windowed : Page
	{
		private const double PopupWidth = 300;

		private readonly MUXPopup _popup;
		private int _clicks;

		public Popup_Windowed()
		{
			this.InitializeComponent();

			var button = new global::Microsoft.UI.Xaml.Controls.Button { Content = "Click me", HorizontalAlignment = HorizontalAlignment.Stretch };
			button.Click += (_, _) => ClickCount.Text = $"Clicks: {++_clicks}";

			var slider = new global::Microsoft.UI.Xaml.Controls.Slider { Minimum = 0, Maximum = 100 };
			slider.ValueChanged += (_, e) => SliderValue.Text = $"Slider: {e.NewValue:0}";

			var content = new StackPanel { Spacing = 8, Padding = new Thickness(16) };
			content.Children.Add(new TextBlock { Text = "A popup out of the window" });
			content.Children.Add(button);
			content.Children.Add(slider);

			_popup = new MUXPopup
			{
				ShouldConstrainToRootBounds = false,
				IsLightDismissEnabled = false,
				Child = new Border
				{
					Width = PopupWidth,
					CornerRadius = new CornerRadius(8),
					Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
					Translation = new global::System.Numerics.Vector3(0, 0, 32),
					Shadow = new ThemeShadow(),
					Child = content,
				},
			};

			_popup.Closed += (_, _) => OpenPopup.IsChecked = false;
			Unloaded += (_, _) => _popup.IsOpen = false;
		}

		private void OnOpenPopupChecked(object sender, RoutedEventArgs e)
		{
			// Half of it past the right edge of the window.
			_popup.XamlRoot = XamlRoot;
			_popup.HorizontalOffset = XamlRoot.Size.Width - PopupWidth / 2;
			_popup.VerticalOffset = 60;
			_popup.IsOpen = true;
		}

		private void OnOpenPopupUnchecked(object sender, RoutedEventArgs e) => _popup.IsOpen = false;
	}
}
