using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Uno.UI.Tests.App.Xaml;
using System.Windows.Data;

namespace Uno.UI.Tests.ComboBoxTests
{
	[TestClass]
    public class Given_ComboBox
	{
		[TestInitialize]
		public void Init()
		{
			UnitTestsApp.App.EnsureApplication();
		}

		[TestMethod]
		public void When_ComboBox_Is_First_Opening()
		{
			var itemsPresenter = new ItemsPresenter();

			var popup = new PopupBase()
			{
				Child = itemsPresenter
			};

			var grid = new Grid()
			{
				Children =
				{
					popup.Name<PopupBase>("Popup"),
					new Border().Name<Border>("PopupBorder")
				}
			};

			var style = new Style(typeof(ComboBox))
			{
				Setters =  {
					new Setter<ComboBox>("Template", t =>
						t.Template = Funcs.Create(() => grid)
					)
				}
			};

			const int initialCount = 10;
			var array = Enumerable.Range(0, initialCount).Select(i => i * 10).ToArray();
			var source = new CollectionViewSource
			{
				Source = array
			};

			var view = source.View;

			var comboBox = new ComboBox()
			{
				Style = style,
				ItemsSource = view
			};

			comboBox.IsDropDownOpen = true;

			Assert.IsNotNull(comboBox.InternalItemsPanelRoot);
			Assert.IsNotNull(comboBox.ItemsPanelRoot);
		}
    }
}
