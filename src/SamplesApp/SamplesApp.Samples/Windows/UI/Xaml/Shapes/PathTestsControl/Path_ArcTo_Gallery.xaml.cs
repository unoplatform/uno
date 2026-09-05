using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace SamplesApp.Windows_UI_Xaml_Shapes.PathTestsControl
{
	[Sample("Path", Name = "Path_ArcTo_Gallery", Description = "Visual gallery covering elliptical-arc cases for Path.Data — used to validate parity with native WinUI after the fix for issue #2228.", IsManualTest = true)]
	public sealed partial class Path_ArcTo_Gallery : UserControl
	{
		public Path_ArcTo_Gallery()
		{
			this.InitializeComponent();
		}
	}
}
