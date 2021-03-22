#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	public partial class MyTextBox : TextBox
	{
		public TextBlock? PlaceholderTextContentPresenter { get; private set; }

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			PlaceholderTextContentPresenter = GetTemplateChild("PlaceholderTextContentPresenter") as TextBlock;
		}
	}
}
