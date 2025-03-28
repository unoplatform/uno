using System.Collections.Generic;
using System.Reflection;
using Uno.UI.Samples.Controls;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.TextBoxControl
{
	[SampleControlInfo("TextBox", "PasswordBox_AutoFill", IsManualTest = true)]
	public sealed partial class PasswordBox_AutoFill : UserControl
	{
		public PasswordBox_AutoFill()
		{
			this.InitializeComponent();
		}
	}
}
