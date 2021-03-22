#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	public partial class MyComboBox : ComboBox
	{
		public TextBlock? PlaceholderTextBlock { get; private set; }

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			PlaceholderTextBlock = GetTemplateChild("PlaceholderTextBlock") as TextBlock;
		}
	}
}
