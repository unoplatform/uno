using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Markup
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_XamlReader
	{
		[TestMethod]
		public void When_Enum_HasNumericalValue()
		{
			XamlReader.Load("""
				<StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					Orientation="0" />
				""");
		}
	}
}
