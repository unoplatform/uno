using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Private.Infrastructure;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UITests.Windows_UI_Xaml.Performance
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	[SampleControlInfo("Performance", "Dopes")]
	public sealed partial class Performance_Dopes : Page
	{
		volatile bool breakTest = false;
		const int max = 600;

		private readonly UnitTestDispatcherCompat _dispatcher;

		public Performance_Dopes()
		{
			this.InitializeComponent();
			_dispatcher = UnitTestDispatcherCompat.From(this);
		}

		private void StartTestST()
		{
			var rand = new Random(0);

			breakTest = false;

			var width = absolute.ActualWidth;
			var height = absolute.ActualHeight;

			const int step = 20;
			var labels = new TextBlock[step * 2];

			var processed = 0;

			long prevTicks = 0;
			long prevMs = 0;
			int prevProcessed = 0;
			double avgSum = 0;
			int avgN = 0;
			var sw = new Stopwatch();

			Action loop = null;

			loop = () =>
			{
				var now = sw.ElapsedMilliseconds;

				if (breakTest)
				{
					var avg = avgSum / avgN;
					dopes.Text = string.Format("{0:0.00} Dopes/s (AVG)", avg).PadLeft(21);
					return;
				}

				//60hz, 16ms to build the frame
				while (sw.ElapsedMilliseconds - now < 16)
				{
					var label = new TextBlock()
					{
						Text = "Dope",
						Foreground = new SolidColorBrush(Color.FromArgb(0xFF, (byte)(rand.NextDouble() * 255), (byte)(rand.NextDouble() * 255), (byte)(rand.NextDouble() * 255)))
					};

					label.RenderTransform = new RotateTransform() { Angle = rand.NextDouble() * 360 };

					Canvas.SetLeft(label, rand.NextDouble() * width);
					Canvas.SetTop(label, rand.NextDouble() * height);

					if (processed > max)
					{
						absolute.Children.RemoveAt(0);
					}

					absolute.Children.Add(label);

					processed++;

					if (sw.ElapsedMilliseconds - prevMs > 500)
					{

						var r = (double)(processed - prevProcessed) / ((double)(sw.ElapsedTicks - prevTicks) / Stopwatch.Frequency);
						prevTicks = sw.ElapsedTicks;
						prevProcessed = processed;

						if (processed > max)
						{
							dopes.Text = string.Format("{0:0.00} Dopes/s", r).PadLeft(15);
							avgSum += r;
							avgN++;
						}

						prevMs = sw.ElapsedMilliseconds;
					}
				}

				_ = _dispatcher.RunAsync(UnitTestDispatcherCompat.Priority.Low, () => loop());
			};

			sw.Start();

			_ = _dispatcher.RunAsync(UnitTestDispatcherCompat.Priority.Normal, () => loop());
		}

		private void StartTestReuseST()
		{
			var rand = new Random(0);

			breakTest = false;

			var width = absolute.ActualWidth;
			var height = absolute.ActualHeight;

			const int step = 20;
			var labels = new TextBlock[step * 2];

			var processed = 0;

			long prevTicks = 0;
			long prevMs = 0;
			int prevProcessed = 0;
			double avgSum = 0;
			int avgN = 0;
			var sw = new Stopwatch();

			Action loop = null;

			System.Collections.Concurrent.ConcurrentBag<TextBlock> _cache = new System.Collections.Concurrent.ConcurrentBag<TextBlock>();

			loop = () =>
			{
				var now = sw.ElapsedMilliseconds;

				if (breakTest)
				{
					var avg = avgSum / avgN;
					dopes.Text = string.Format("{0:0.00} Dopes/s (AVG)", avg).PadLeft(21);
					return;
				}

				//60hz, 16ms to build the frame
				while (sw.ElapsedMilliseconds - now < 16)
				{
					if (!_cache.TryTake(out var label))
					{
						label = new TextBlock();
					}

					label.Text = "Dope";
					label.Foreground = new SolidColorBrush(Color.FromArgb(0xFF, (byte)(rand.NextDouble() * 255), (byte)(rand.NextDouble() * 255), (byte)(rand.NextDouble() * 255)));

					label.RenderTransform = new RotateTransform() { Angle = rand.NextDouble() * 360 };

					Canvas.SetLeft(label, rand.NextDouble() * width);
					Canvas.SetTop(label, rand.NextDouble() * height);

					if (processed > max)
					{
						_cache.Add(absolute.Children[0] as TextBlock);
						absolute.Children.RemoveAt(0);
					}

					absolute.Children.Add(label);

					processed++;

					if (sw.ElapsedMilliseconds - prevMs > 500)
					{

						var r = (double)(processed - prevProcessed) / ((double)(sw.ElapsedTicks - prevTicks) / Stopwatch.Frequency);
						prevTicks = sw.ElapsedTicks;
						prevProcessed = processed;

						if (processed > max)
						{
							dopes.Text = string.Format("{0:0.00} Dopes/s", r).PadLeft(15);
							avgSum += r;
							avgN++;
						}

						prevMs = sw.ElapsedMilliseconds;
					}
				}

				_ = _dispatcher.RunAsync(UnitTestDispatcherCompat.Priority.Low, () => loop());
			};

			sw.Start();

			_ = _dispatcher.RunAsync(UnitTestDispatcherCompat.Priority.Low, () => loop());
		}

		private void SetControlsAtStart()
		{
			startChangeST.Visibility = startST.Visibility = startGridST.Visibility = Visibility.Collapsed;
			stop.Visibility = dopes.Visibility = Visibility.Visible;
			absolute.Children.Clear();
			grid.Children.Clear();
			dopes.Text = "Warming up..";
		}

		private void startMT_Clicked(System.Object sender, object e)
		{
			SetControlsAtStart();
			//StartTestMT2();
		}

		private void startST_Clicked(System.Object sender, object e)
		{
			SetControlsAtStart();
			StartTestST();
		}

		private void startGridST_Clicked(System.Object sender, object e)
		{
			SetControlsAtStart();
			//StartTestGridST();
		}

		private void startChangeST_Clicked(System.Object sender, object e)
		{
			SetControlsAtStart();
			//StartTestChangeST();
		}

		private void startChangeReuse_Clicked(System.Object sender, object e)
		{
			SetControlsAtStart();
			StartTestReuseST();
		}

		private void Stop_Clicked(System.Object sender, object e)
		{
			breakTest = true;
			stop.Visibility = Visibility.Collapsed;
			startChangeST.Visibility = startST.Visibility = startGridST.Visibility = Visibility.Visible;
		}

	}
}
