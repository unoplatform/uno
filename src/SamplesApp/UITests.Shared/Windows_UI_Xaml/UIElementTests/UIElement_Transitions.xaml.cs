using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Samples.Controls;
using Windows.UI;

namespace UITests.Windows_UI_Xaml.UIElementTests;

[Sample("UIElement", IsManualTest = true, Description = "Demonstrates UIElement property transitions: OpacityTransition, ScaleTransition, RotationTransition, TranslationTransition, and BackgroundTransition")]
public sealed partial class UIElement_Transitions : Page
{
	public UIElement_Transitions()
	{
		this.InitializeComponent();
	}

	// Opacity
	private void OpacityLow_Click(object sender, RoutedEventArgs e) => OpacityTarget.Opacity = 0.2;
	private void OpacityMid_Click(object sender, RoutedEventArgs e) => OpacityTarget.Opacity = 0.6;
	private void OpacityFull_Click(object sender, RoutedEventArgs e) => OpacityTarget.Opacity = 1.0;

	// Scale
	private void ScaleSmall_Click(object sender, RoutedEventArgs e) => ScaleTarget.Scale = new Vector3(0.5f, 0.5f, 1f);
	private void ScaleNormal_Click(object sender, RoutedEventArgs e) => ScaleTarget.Scale = new Vector3(1f, 1f, 1f);
	private void ScaleLarge_Click(object sender, RoutedEventArgs e) => ScaleTarget.Scale = new Vector3(1.5f, 1.5f, 1f);
	private void ScaleWide_Click(object sender, RoutedEventArgs e) => ScaleTarget.Scale = new Vector3(2f, 1f, 1f);

	// Rotation
	private void Rotate0_Click(object sender, RoutedEventArgs e) => RotationTarget.Rotation = 0;
	private void Rotate45_Click(object sender, RoutedEventArgs e) => RotationTarget.Rotation = 45;
	private void Rotate90_Click(object sender, RoutedEventArgs e) => RotationTarget.Rotation = 90;
	private void Rotate180_Click(object sender, RoutedEventArgs e) => RotationTarget.Rotation = 180;

	// Translation
	private void TranslateOrigin_Click(object sender, RoutedEventArgs e) => TranslationTarget.Translation = Vector3.Zero;
	private void TranslateRight_Click(object sender, RoutedEventArgs e) => TranslationTarget.Translation = new Vector3(100, 0, 0);
	private void TranslateFar_Click(object sender, RoutedEventArgs e) => TranslationTarget.Translation = new Vector3(200, 0, 0);
	private void TranslateDown_Click(object sender, RoutedEventArgs e) => TranslationTarget.Translation = new Vector3(0, 50, 0);

	// Background
	private void BgBlue_Click(object sender, RoutedEventArgs e) => BackgroundTarget.Background = new SolidColorBrush(Color.FromArgb(255, 58, 134, 255));
	private void BgGreen_Click(object sender, RoutedEventArgs e) => BackgroundTarget.Background = new SolidColorBrush(Color.FromArgb(255, 6, 214, 160));
	private void BgPink_Click(object sender, RoutedEventArgs e) => BackgroundTarget.Background = new SolidColorBrush(Color.FromArgb(255, 239, 71, 111));
	private void BgOrange_Click(object sender, RoutedEventArgs e) => BackgroundTarget.Background = new SolidColorBrush(Color.FromArgb(255, 255, 107, 53));

	// Combined
	private bool _combinedToggle;
	private void CombinedAnimate_Click(object sender, RoutedEventArgs e)
	{
		_combinedToggle = !_combinedToggle;
		if (_combinedToggle)
		{
			CombinedTarget.Opacity = 0.5;
			CombinedTarget.Scale = new Vector3(1.3f, 1.3f, 1f);
			CombinedTarget.Rotation = 45;
		}
		else
		{
			CombinedTarget.Opacity = 0.8;
			CombinedTarget.Scale = new Vector3(0.7f, 0.7f, 1f);
			CombinedTarget.Rotation = -30;
		}
	}

	private void CombinedReset_Click(object sender, RoutedEventArgs e)
	{
		_combinedToggle = false;
		CombinedTarget.Opacity = 1.0;
		CombinedTarget.Scale = new Vector3(1f, 1f, 1f);
		CombinedTarget.Rotation = 0;
	}
}
