using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Tests.App.Xaml;

namespace Uno.UI.Tests.Windows_UI_Xaml
{
	[TestClass]
	public class Given_HighContrastAdjustment
	{
		[TestInitialize]
		public void Init()
		{
			UnitTestsApp.App.EnsureApplication();
		}

		[TestMethod]
		public void When_Application_Default_Is_Auto()
		{
			Assert.AreEqual(ApplicationHighContrastAdjustment.Auto, Application.Current.HighContrastAdjustment);
		}

		[TestMethod]
		public void When_Application_Value_Is_Set()
		{
			var originalValue = Application.Current.HighContrastAdjustment;

			try
			{
				Application.Current.HighContrastAdjustment = ApplicationHighContrastAdjustment.None;

				Assert.AreEqual(ApplicationHighContrastAdjustment.None, Application.Current.HighContrastAdjustment);
			}
			finally
			{
				Application.Current.HighContrastAdjustment = originalValue;
			}
		}

		[TestMethod]
		public void When_UIElement_Default_Is_Application()
		{
			Assert.AreEqual(ElementHighContrastAdjustment.Application, new Grid().HighContrastAdjustment);
		}

		[TestMethod]
		public void When_UIElement_Inherits_Parent_Value()
		{
			var root = new Grid();
			var child = new TextBlock();

			root.Children.Add(child);
			root.HighContrastAdjustment = ElementHighContrastAdjustment.None;

			Assert.AreEqual(ElementHighContrastAdjustment.None, child.HighContrastAdjustment);
		}
	}
}