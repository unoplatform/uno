using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace Uno.UI.Tests.Given_ResourceDictionary
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class When_Nested_With_Sibling_Ref_And_Event : Page
	{
		public When_Nested_With_Sibling_Ref_And_Event()
		{
			this.InitializeComponent();
		}

		private void AnEventHandler(SwipeItem sender, SwipeItemInvokedEventArgs args)
		{
		}
	}
}
