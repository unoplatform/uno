using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Tests.Windows_UI_Xaml_Markup.XamlReaderTests
{
	public partial class When_Event_Handler_xBind : UserControl
	{
		public int Handler1Count { get; private set; }
		public int Handler2Count { get; private set; }

		private void Handler1(object sender, object args)
		{
			Handler1Count++;
		}

		private void Handler2()
		{
			Handler2Count++;
		}
	}
}
