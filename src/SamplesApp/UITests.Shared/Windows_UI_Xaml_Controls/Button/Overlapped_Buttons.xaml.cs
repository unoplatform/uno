using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.Button
{
	[SampleControlInfo("Buttons", "Overlapped_Buttons")]
	public sealed partial class Overlapped_Buttons : Page
	{
		private Dictionary<string, int> clicks = new Dictionary<string, int>();

		public Overlapped_Buttons()
		{
			this.InitializeComponent();

			AddHandler(TappedEvent, new TappedEventHandler(TappedHandler), handledEventsToo: true);

			void TappedHandler(object snd, TappedRoutedEventArgs tappedRoutedEventArgs)
			{
				Write($"[page].Tapped, handled={tappedRoutedEventArgs.Handled}, source={tappedRoutedEventArgs.OriginalSource}");
			}

			Update();
		}
		private void OnClick(object sender, RoutedEventArgs e)
		{
			if (sender is FrameworkElement el)
			{
				Write($"[{el.Name}].Click");

				if (!clicks.TryGetValue(el.Name, out var count))
				{
					count = 0;
				}

				clicks[el.Name] = count + 1;

				Update();
			}
		}

		private void Update()
		{
			l1Clicks.Text = (clicks.TryGetValue("layer1", out var l1) ? l1 : 0).ToString();
			l1innerClicks.Text = (clicks.TryGetValue("layer1_inner", out var l1i) ? l1i : 0).ToString();
			l2Clicks.Text = (clicks.TryGetValue("layer2", out var l2) ? l2 : 0).ToString();
			l3Clicks.Text = (clicks.TryGetValue("layer3", out var l3) ? l3 : 0).ToString();
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
