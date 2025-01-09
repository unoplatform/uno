using System;
using System.Collections.Generic;
using System.ComponentModel;
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
