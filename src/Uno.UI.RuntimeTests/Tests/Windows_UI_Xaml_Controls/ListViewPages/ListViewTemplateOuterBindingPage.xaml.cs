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

namespace Uno.UI.RuntimeTests.ListViewPages
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class ListViewTemplateOuterBindingPage : Page
	{
		public ListViewTemplateOuterBindingPage()
		{
			this.InitializeComponent();

			DataContext = new MyViewModel();
		}

		private class MyViewModel
		{
			public int[] Items { get; } = Enumerable.Range(0, 3).ToArray();

			public string TextFromOuterContext => "OuterContextText";
		}
	}
}
