using System;
using Uno.UI.Samples.Controls;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace SamplesApp.Samples.RoutedEvents
{
	[Sample("Routed Events", "Test Page", Description: "Test surface for routed events.")]
	public sealed partial class RoutedEventsPage : Page
	{
		public RoutedEventsPage()
		{
			this.InitializeComponent();

			HookEvents(outer, resultOuter);
			HookEvents(middle, resultMiddle);
			HookEvents(inner, resultInner);
			HookEvents(scroll, resultScroll);

			btn.Click += (s, e) => btn.Content = $"{btn.Content}.Clk";
			btn.Tapped += (s, e) => btn.Content = $"{btn.Content}+T";

			list.Items.Add("A");
			list.Items.Add("B");
			list.Items.Add("C");
			list.Items.Add("D");

			(new Slider()).Value = 3;
		}

		private void HookEvents(Grid grid, TextBlock textBlock)
		{
			void TapHandler(object snd, TappedRoutedEventArgs evt)
			{
				textBlock.Text += $".T({evt.GetPosition(null)})";
				evt.Handled = true;
			}

			void TapHandler2(object snd, TappedRoutedEventArgs evt)
			{
				textBlock.Text += $".t({GetPosition(evt)})";
				//evt.Handled = false;
			}

			grid.AddHandler(TappedEvent, (TappedEventHandler)TapHandler, false);
			grid.AddHandler(TappedEvent, (TappedEventHandler)TapHandler2, true);

			string GetPosition(TappedRoutedEventArgs evt)
			{
				var pos = evt.GetPosition(null);
				return $"{Math.Round(pos.X, 1)}, {Math.Round(pos.Y, 1)}";
			}

			textBlock.Tapped += TapHandler3;

			void TapHandler3(object sender, TappedRoutedEventArgs evt)
			{
				textBlock.Text += $".e({GetPosition(evt)})";
			}

			grid.AddHandler(DoubleTappedEvent, (DoubleTappedEventHandler)DoubleTappedHandler, true);

			void DoubleTappedHandler(object sender, DoubleTappedRoutedEventArgs e)
			{
				textBlock.Text += $".dt";
				e.Handled = true;
			}

			grid.PointerEntered += (s, e) =>
			{
				textBlock.Text += "<<";
				e.Handled = true;
			};

			grid.PointerExited += (s, e) => textBlock.Text += ">>";

			var blue = new SolidColorBrush(Colors.Blue);
			var white = new SolidColorBrush(Colors.WhiteSmoke);

			grid.PointerPressed += (s, e) =>
			{
				textBlock.Text += "_";
				grid.BorderBrush = blue;
			};
			grid.PointerReleased += (s, e) =>
			{
				textBlock.Text += "-";
				grid.BorderBrush = white;
			};

			grid.BorderBrush = white;
			grid.BorderThickness = new Thickness(3.5);

			grid.GotFocus += (s, e) => textBlock.Text += ".F";
			grid.LostFocus += (s, e) => textBlock.Text += ".f";

			grid.KeyDown += (s, e) =>
			{
				textBlock.Text += ".K";
				if (e.Key == global::Windows.System.VirtualKey.E)
				{
					e.Handled = true;
				}
			};

			grid.KeyUp += (s, e) =>
			{
				textBlock.Text += ".k";
				if (e.Key == global::Windows.System.VirtualKey.E)
				{
					e.Handled = true;
				}
			};
		}

		protected override void OnTapped(TappedRoutedEventArgs e)
		{
			base.OnTapped(e);
#if !WINAPPSDK
			Console.WriteLine("Tapped!");
#endif
		}
	}
}
