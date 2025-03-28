using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;
using SamplesApp.UITests.Windows_UI_Xaml_Controls.Models;

namespace UITests.Shared.Windows_UI_Xaml_Controls.ChatBox
{
	[SampleControlInfo("Other controls", "ChatBox", typeof(ChatBoxViewModel))]
	public sealed partial class ChatBox : UserControl
	{
		public ChatBox()
		{
			this.InitializeComponent();
		}
	}
}
