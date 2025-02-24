using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236


namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	public sealed partial class Measure_Children_In_Canvas : UserControl
	{
		public Measure_Children_In_Canvas()
		{
			this.InitializeComponent();
		}
		public Border Get_InBorder()
		{
			return BBorderInCanvas;
		}

		public Border Get_OutBorder()
		{
			return BBorderOutCanvas;
		}
	}
}
