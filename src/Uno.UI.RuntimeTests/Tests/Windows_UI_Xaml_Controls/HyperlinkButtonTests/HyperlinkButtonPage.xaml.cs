using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.HyperlinkButtonTests
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class HyperlinkButtonPage : Page
	{
		public HyperlinkButton ShouldBeUnderlinedHyperlinkButton => ShouldBeUnderlined_HyperlinkButton;
		public HyperlinkButton ShouldNotBeUnderlinedHyperlinkButton => ShouldNotBeUnderlined_HyperlinkButton;
		public TextBlock ShouldNotBeUnderlinedTextBlock => ShouldNotBeUnderlined_TextBlock;

		public HyperlinkButtonPage()
		{
			this.InitializeComponent();
		}
	}
}
