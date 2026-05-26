using System.Threading;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Windows_UI_Xaml_Controls;

[Sample("Buttons", IsManualTest = true)]
public sealed partial class Button_Click_Verify_SynchronizationContext : Page
{
	public Button_Click_Verify_SynchronizationContext()
	{
		this.InitializeComponent();

		tbContextInitComponents.Text = GetTestOutcome();
	}

	private void Button_Click(object sender, RoutedEventArgs e)
	{
		tbContextHandler.Text = GetTestOutcome();
	}

	private static string GetTestOutcome()
	{
		var context = SynchronizationContext.Current.GetType().ToString();
		var correctness = context == "Uno.UI.Dispatching.NativeDispatcherSynchronizationContext" ? " - Correct" : " - WRONG CONTEXT - TEST FAILED!";
		return context + correctness;
	}
}
