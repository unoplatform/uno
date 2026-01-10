using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;

namespace UITests.Shared.Windows_UI_Xaml.xBind
{
	[Sample("x:Bind", "xBind")]
	public sealed partial class xBind : UserControl
	{
		internal XbindViewModel ViewModel { get; set; }

		public xBind()
		{
			this.InitializeComponent();

			ViewModel = new XbindViewModel(UnitTestDispatcherCompat.From(this));
		}
	}
}
