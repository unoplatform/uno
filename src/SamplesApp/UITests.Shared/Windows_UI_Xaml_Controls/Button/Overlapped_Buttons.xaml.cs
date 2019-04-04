using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.Button
{
	[SampleControlInfo("Button", "Overlapped_Buttons")]
	public sealed partial class Overlapped_Buttons : Page
	{
		public Overlapped_Buttons()
		{
			this.InitializeComponent();

			AddHandler(TappedEvent, new TappedEventHandler(TappedHandler), handledEventsToo: true);

			void TappedHandler(object snd, TappedRoutedEventArgs tappedRoutedEventArgs)
			{
				Write($"[page].Tapped, handled={tappedRoutedEventArgs.Handled}, source={tappedRoutedEventArgs.OriginalSource}");
			}

		}
		private void OnClick(object sender, RoutedEventArgs e)
		{
			Write($"[{(sender as FrameworkElement).Name}].Click");
		}

		private void OnTapped(object sender, TappedRoutedEventArgs e)
		{
			Write($"[{(sender as FrameworkElement).Name}].Tapped");
		}

		private void ClearMsgs(object sender, TappedRoutedEventArgs e)
		{
			msgs.Text = "";
		}

		private void Write(string s)
		{
			msgs.Text += s + "\n";
		}
	}
}
