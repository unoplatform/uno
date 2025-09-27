using System;
using System.Threading.Tasks;
using Microsoft.UI.Composition;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UITests.Windows_UI_Xaml.Performance
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	[Sample("Performance")]
	public sealed partial class Performance_1000ButtonsContinuousRendering : Page
	{
		public Performance_1000ButtonsContinuousRendering()
		{
			this.InitializeComponent();

			Loaded += (s, e) =>
			{
				colorStoryboard.Begin();
#if __SKIA__
				NumberBoxValueChanged(this, new NumberBoxValueChangedEventArgs(0, 100));
#endif
			};

			Unloaded += (s, e) =>
			{
				colorStoryboard.Stop();
			};
		}

		private async void NumberBoxValueChanged(object sender, NumberBoxValueChangedEventArgs e)
		{
#if __SKIA__
			wp.Children.Clear();
			var val = (int)Math.Round(Math.Max(0, e.NewValue));
			for (var i = 0; i < val; i++)
			{
				wp.Children.Add(new Button { Content = i.ToString() });
			}

			await Task.Delay(TimeSpan.FromSeconds(1));
			tb.Text = $"Number of visuals in WrapPanel: {wp.Visual.GetSubTreeVisualCount()}";
#else
			await Task.CompletedTask;
#endif
		}
	}
}
