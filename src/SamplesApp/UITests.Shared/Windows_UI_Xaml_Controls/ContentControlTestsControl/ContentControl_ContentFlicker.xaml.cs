using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.ContentControlTestsControl
{
	[Sample("ContentControl", Name = "ContentControl_ContentFlicker",
		Description = "Repro for https://github.com/unoplatform/uno/issues/2807: " +
		              "Changing Content of ContentControl causes flicker on WASM. " +
		              "Click 'Switch Content' rapidly and observe for blank flashes between transitions.")]
	public sealed partial class ContentControl_ContentFlicker : UserControl
	{
		private int _counter;

		public ContentControl_ContentFlicker()
		{
			InitializeComponent();
			TheContentControl.Content = "Content A (initial)";
			UpdateCounter();
		}

		private void OnSwitchClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			_counter++;
			TheContentControl.Content = _counter % 2 == 0
				? "Content A (even)"
				: "Content B (odd)";
			UpdateCounter();
		}

		private void UpdateCounter()
		{
			CounterText.Text = $"Switches: {_counter}";
		}
	}
}
