using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls
{
	public sealed partial class BindingLeak_xBind_View : UserControl
	{
		public BindingLeak_xBind_View()
		{
			this.InitializeComponent();
		}

		public BindingLeak_ViewModel ViewModel { get; set; }
	}
}
