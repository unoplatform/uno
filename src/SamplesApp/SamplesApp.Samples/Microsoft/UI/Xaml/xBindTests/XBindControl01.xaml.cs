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

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Uno.UI.Samples.Content.UITests.XBind
{
	public sealed partial class XBindControl01 : UserControl
	{
		private int _bindingCounter;

		public XBindControl01()
		{
			this.InitializeComponent();
		}

		public int MyValue => 42;

		public int BindingCounter => ++_bindingCounter;

		public Data01 Data { get; } = new Data01();
	}

	[Bindable]
	public class Data01
	{
		public int Value01 { get; } = 43;

		public Data02 Data02 { get; } = new Data02();
	}

	[Bindable]
	public class Data02
	{
		private int _bindingCounter;

		public int Value02 { get; } = 44;

		public int BindingCounter => ++_bindingCounter;
	}
}
