using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Uno.Extensions;
using Uno.UI.Extensions;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Uno.UI.Samples.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml_Controls.TextBoxTests
{
	[Sample("TextBox", Name = "TextBox_Formatting_Flicker", Description = "Continuing to enter value past the max length specified, should not cause the text box content to be changed again.")]
	public sealed partial class TextBox_Formatting_Flicker : UserControl
	{
		private int _textChangedCounter = 0;

		public TextBox_Formatting_Flicker()
		{
			this.InitializeComponent();

			SomeTextBox.MaxLength = $"modified {0:D3} times".Length;
			SomeTextBox.TextChanged += (s, e) => Scoreboard.Text = $"TextChanged: {++_textChangedCounter}";
		}

	}
}
