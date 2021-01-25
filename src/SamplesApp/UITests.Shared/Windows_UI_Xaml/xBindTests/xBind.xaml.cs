using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Xaml.xBind
{
	[SampleControlInfo("XBind", "xBind")]
	public sealed partial class xBind : UserControl
	{
		public XbindViewModel ViewModel { get; set; }

		public xBind()
		{
			this.InitializeComponent();

			ViewModel = new XbindViewModel(Dispatcher);
		}
	}
}
