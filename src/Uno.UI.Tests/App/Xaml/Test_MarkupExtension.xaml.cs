using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Uno.UI.Tests.App.Xaml
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class Test_MarkupExtension : UserControl
	{
		public TextBlock TestText1 => Text1;
		public TextBlock TestText2 => Text2;
		public TextBlock TestText3 => Text3;
		public TextBlock TestText4 => Text4;
		public TextBlock TestText5 => Text5;
		public TextBlock TestText6 => Text6;
		public TextBlock TestText7 => Text7;
		public TextBlock TestText8 => Text8;
		public TextBlock TestText9 => Text9;
		public TextBlock TestText10 => Text10;
		public TextBlock TestText11 => Text11;
		public TextBlock TestText12 => Text12;
		public TextBlock TestText13 => Text13;

		public Test_MarkupExtension()
		{
			this.InitializeComponent();
		}
	}
}
