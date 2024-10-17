using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Windows_UI_Xaml_Shapes
{
	[Sample("Shapes", IsManualTest = true)]
	public sealed partial class Path_Clipping : Page
	{
		public Path_Clipping()
		{
			this.InitializeComponent();
			Loaded += (_, _) => sv.ScrollToVerticalOffset(9999);
		}
	}
}
