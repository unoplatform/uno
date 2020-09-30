using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Tests.Windows_UI_XAML_Controls.UserControlTests
{
	public sealed partial class UserControl_WriteOnlyProperty_UserControl : UserControl
	{
		public string Text
		{
			set => TextDisplay.Text = value;
		}

		public UserControl_WriteOnlyProperty_UserControl()
		{
			InitializeComponent();
		}
	}
}
