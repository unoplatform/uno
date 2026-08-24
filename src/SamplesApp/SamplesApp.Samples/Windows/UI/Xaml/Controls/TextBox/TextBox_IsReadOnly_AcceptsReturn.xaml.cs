using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml_Controls.TextBoxTests
{
	[Sample("TextBox", Description = "#2700: Setting IsReadOnly=True breaks AcceptReturns=True on android")]
	public sealed partial class TextBox_IsReadOnly_AcceptsReturn : UserControl
	{
		public TextBox_IsReadOnly_AcceptsReturn()
		{
			this.InitializeComponent();
		}
	}
}
