using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Tests.App.Views;
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
	public sealed partial class Test_Control : UserControl
	{
		public Grid TopGrid => topGrid;
		public CheckBox TestCheckBox => testCheckBox;
		public TextBlock TestTextBlock => testTextBlock;
		public TextBlock TestTextBlock2 => testTextBlock2;
		public Border TestBorder => testBorder;
		public Grid TestGrid => testGrid;
		public Button StyledButton => styledButton;
		public MyControl TestMyControl => testMyControl;
		public ResourceTestControl InlineTemplateControl => inlineTemplateControl;
		public ResourceTestControl TemplateFromResourceControl => templateFromResourceControl;
		public RadioButton TestRadioButton => testRadioButton;
		public RadioButton TestRadioButtonExplicit => testRadioButtonExplicit;
		public StylesTestControl StylesTestControl => stylesTestControl;
		public StylesTestControl StylesTestControlExplicit => stylesTestControlExplicit;
		public StylesTestButton StylesTestButton => stylesTestButton;
		public StylesTestButton StylesTestButtonExplicit => stylesTestButtonExplicit;
		public StylesTestButtonCustomKey StylesTestButtonCustomKey => stylesTestButtonCustomKey;
		public StylesTestButtonCustomKey StylesTestButtonCustomKeyExplicit => stylesTestButtonCustomKeyExplicit;
		public StylesTestRadioButton StylesTestRadioButton => stylesTestRadioButton;
		public CommandBar TestCommandBar => testCommandBar;
		public CommandBar TestCommandBar2 => testCommandBar2;

		public Test_Control()
		{
			this.InitializeComponent();
		}
	}
}
