using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
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

namespace Uno.UI.Samples.Content.UITests.XBind
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	[SampleControlInfo("x:Bind", "PhaseBinding_StartOne")]
	public sealed partial class PhaseBinding_StartOne : Page
	{
		public PhaseBinding_StartOne()
		{
			this.InitializeComponent();

			MyItems = Enumerable.Range(0, 1000).Select(c => new MyItem { Value01 = c, Value02 = (c * 2).ToString() }).ToArray();
		}

		public MyItem[] MyItems { get; set; }
	}
}
