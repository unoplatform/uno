using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	public sealed partial class Canvas_In_Canvas : UserControl
	{
		public Canvas_In_Canvas()
		{
			this.InitializeComponent();
		}
		public Border Get_CanvasBorderBlue1()
		{
			return CanvasBorderBlue1;
		}
	}
}
