using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;


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
