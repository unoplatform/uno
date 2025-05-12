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
			for (var i = 0; i < 1000; i++)
			{
				(wp as Panel).Children.Add(new Button { Content = i.ToString() });
			}

			Loaded += (s, e) => colorStoryboard.Begin();
		}
	}
}
