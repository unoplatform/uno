using System.Collections.Generic;
using System.Reflection;
using Uno.UI.Samples.Controls;
using Windows.UI.Text;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.TextBoxControl
{
	[Sample("TextBox", "PasswordBox_Simple")]
	public sealed partial class PasswordBox_Simple : UserControl
	{
		int currentMode = 0;
		public PasswordBox_Simple()
		{
			this.InitializeComponent();
		}

		public void ChangeRevealMode(object sender, object args)
		{
			passBox.PasswordRevealMode = (PasswordRevealMode)(++currentMode % 3);
		}
	}
}
