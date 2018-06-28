using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace Uno.UI.Tests.ItemsControlTests
{
	[TestClass]
	public class Given_ItemsControl
    {
		[TestMethod]
		public void When_EarlyItems()
		{
			var style = new Style(typeof(Windows.UI.Xaml.Controls.ItemsControl))
			{
				Setters =  {
					new Setter<ItemsControl>("Template", t =>
						t.Template = Funcs.Create(() =>
							new ItemsPresenter()
						)
					)
				}
			};

			var panel = new StackPanel();

			var SUT = new ItemsControl()
			{
				Style = style,
				ItemsPanel = new ItemsPanelTemplate(() => panel),
				Items = {
					new Border { Name = "b1" }
				}
			};

			// Search on the panel for now, as the name lookup is not properly
			// aligned on net46.
			Assert.IsNotNull(panel.FindName("b1"));
		}
	}
}
