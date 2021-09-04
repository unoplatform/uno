using System.Runtime.CompilerServices;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UITests.Shared.Windows_UI_Input.GestureRecognizerTests
{
	[Sample("Gesture recognizer")]
	public sealed partial class ManipulationPosition : Page
    {
        public ManipulationPosition()
        {
            this.InitializeComponent();
        }

		private void Border_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
		{
			UpdateText(e.Position);
		}

		private void Border_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
		{
			UpdateText(e.Position);
		}

		private void Border_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
		{
			UpdateText(e.Position);
		}

		private void UpdateText(Point p, [CallerMemberName] string caller = null)
		{
			Result.Text = $"{caller.Substring("Border_".Length)}: {p.X:N1}, {p.Y:N1}";
		}
	}
}
