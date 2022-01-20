using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Tests.Windows_UI_Xaml_Markup.XamlReaderTests
{
	public partial class When_xName_Reload : UserControl
	{
#pragma warning disable 649
		private Button Button1_field_private;

		public Button Button1_field_public;

		private Button Button2_property_private { get; set; }

		public Button Button2_property_public { get; set; }


		public Button Button2_property_private_Getter => Button2_property_private;
		public Button Button1_field_private_Getter => Button1_field_private;
	}
}
