using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
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

namespace Uno.UI.Samples.Content.UITests.XBind
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	[Sample("x:Bind", Name = "Phase_Simple")]
	public sealed partial class PhaseBinding : Page
	{
		public PhaseBinding()
		{
			this.InitializeComponent();

			MyItems = Enumerable.Range(0, 1000).Select(c => new MyItem { Value01 = c, Value02 = (c * 2).ToString() }).ToArray();
		}

		public MyItem[] MyItems { get; set; }
	}

	[Bindable]
	public class MyItem
	{
		public int Value01 { get; set; }
		public string Value02 { get; set; }
	}
}
