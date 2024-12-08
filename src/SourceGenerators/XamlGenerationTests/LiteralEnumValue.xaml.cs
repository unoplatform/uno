using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Markup;


namespace XamlGenerationTests.Shared
{
	public sealed partial class LiteralEnumValue : UserControl
	{
		public LiteralEnumValue()
		{
			this.InitializeComponent();
		}
	}

	public enum LiteralEnumValue_Enum { Qwe, Asd, Zxc }
}
