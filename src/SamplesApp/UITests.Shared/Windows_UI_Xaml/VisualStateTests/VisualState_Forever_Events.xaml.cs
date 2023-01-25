using System.Linq;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UITests.Shared.Windows_UI_Xaml.VisualStateTests
{
	[Sample("Visual states")]
	public sealed partial class VisualState_Forever_Events : Page
	{
		public VisualState_Forever_Events()
		{
			this.InitializeComponent();
			Loaded += (_, __) =>
			{
				var states = VisualStateManager.GetVisualStateGroups(VisualTreeHelper.GetChild(MyButton, 0) as FrameworkElement).ToList();
				states.First()
				  .CurrentStateChanging += (s, e) => LogsTextBlock.Text += $"Changing to: {e.NewState.Name}\n";
				states.First()
				  .CurrentStateChanged += (s, e) => LogsTextBlock.Text += $"Changed to: {e.NewState.Name}\n";

			};
		}

		private void OnClick(object sender, RoutedEventArgs e)
		{
			VisualStateManager.GoToState(MyButton, "Blinking", true);
		}
	}
}
