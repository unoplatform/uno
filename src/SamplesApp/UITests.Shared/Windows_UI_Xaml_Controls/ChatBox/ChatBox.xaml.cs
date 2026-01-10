using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;
using SamplesApp.UITests.Windows_UI_Xaml_Controls.Models;

namespace UITests.Shared.Windows_UI_Xaml_Controls.ChatBox
{
	[Sample("Other controls", "ChatBox", typeof(ChatBoxViewModel))]
	public sealed partial class ChatBox : UserControl
	{
		public ChatBox()
		{
			this.InitializeComponent();
		}
	}
}
