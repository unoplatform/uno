using Microsoft.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Uno.UI.Samples.Controls;
using Windows.UI;

namespace UITests.Windows_UI_Xaml.VisualStateTests;

[Sample("Visual states", Description = "Going from one visual state to another should roll back properties set by the previous state that won't be set by the setters of the next state and won't be set by the \"next animation\" i.e. the transition if it exists, or the animation.", IsManualTest = true)]
public sealed partial class VisualState_ReturnPreviousValue2 : Page
{
	public VisualState_ReturnPreviousValue2()
	{
		this.InitializeComponent();
		Target.RegisterPropertyChangedCallback(TextBlock.TextProperty, new DependencyPropertyChangedCallback(OnTextChanged));
	}

	private void GoToVisualState1(object sender, RoutedEventArgs e)
	{
		var success = VisualStateManager.GoToState(uc, "VisualState1", true);
		log.Text += $"GoToState VisualState1 Success:{success}";
		log.Text += "\n";
	}

	private void GoToVisualState2(object sender, RoutedEventArgs e)
	{
		var success = VisualStateManager.GoToState(uc, "VisualState2", true);
		log.Text += $"GoToState VisualState2 Success:{success}";
		log.Text += "\n";
	}

	private void GoToVisualState3(object sender, RoutedEventArgs e)
	{
		var success = VisualStateManager.GoToState(uc, "VisualState3", true);
		log.Text += $"GoToState VisualState3 Success:{success}";
		log.Text += "\n";
	}

	private void GoToVisualState4(object sender, RoutedEventArgs e)
	{
		var success = VisualStateManager.GoToState(uc, "VisualState4", true);
		log.Text += $"GoToState VisualState4 Success:{success}";
		log.Text += "\n";
	}

	private void OnTextChanged(object sender, object _)
	{
		log.Text += $"Text changed to {Target.Text}";
		log.Text += "\n";
	}

	private void ClearLog(object sender, RoutedEventArgs e)
	{
		log.Text = "";
	}
}
