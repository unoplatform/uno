using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_Border
	{
		[TestMethod]
		public async Task Check_DataContext_Propagation()
		{
			var tb = new TextBlock();
			tb.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath("TestText") });
			var SUT = new Border
			{
				Child = tb
			};

			var root = new Grid
			{
				DataContext = new MyContext()
			};

			root.Children.Add(SUT);

			WindowHelper.WindowContent = root;

			await WindowHelper.WaitFor(() => tb.Text == "Vampire squid");
		}

		private class MyContext
		{
			public string TestText => "Vampire squid";
		}
	}
}
