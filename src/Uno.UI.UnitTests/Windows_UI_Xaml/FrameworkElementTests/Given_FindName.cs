using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;
using AwesomeAssertions.Execution;
using Microsoft.UI.Xaml;

namespace Uno.UI.Tests.Windows_UI_Xaml.FrameworkElementTests
{
	[TestClass]
#if !IS_UNIT_TESTS
	[RuntimeTests.RunsOnUIThread]
#endif
	public class Given_FindName
	{
		/// <summary>
		/// WinUI resolves a name only from the namescope it was registered in, so a name declared inside
		/// a template does not resolve from outside it. Uno keeps its historical tree walk as a fallback.
		/// </summary>
		[TestMethod]
		public void When_Strict_Mode_Name_Resolves_From_The_Namescope_Only()
		{
			var SUT = new Grid();
			var named = new Border { Name = "strict" };
			SUT.Children.Add(named);

			Uno.UI.FeatureConfiguration.FrameworkElement.UseLegacyFindNameTreeWalk = false;
			try
			{
				Assert.IsNull(SUT.FindName("strict"), "a detached tree has no namescope to resolve from");

				NameScope scope = new() { Owner = SUT };
				scope.RegisterName("strict", named);

				Assert.AreEqual(named, SUT.FindName("strict"));
			}
			finally
			{
				Uno.UI.FeatureConfiguration.FrameworkElement.UseLegacyFindNameTreeWalk = true;
			}
		}

		[TestMethod]
		public void When_Loaded_From_Xaml_Reader_Root_Finds_Its_Names()
		{
			var SUT = (Grid)Microsoft.UI.Xaml.Markup.XamlReader.Load(
				"""
				<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
					<Border x:Name="readerBorder" />
				</Grid>
				""");

			Uno.UI.FeatureConfiguration.FrameworkElement.UseLegacyFindNameTreeWalk = false;
			try
			{
				Assert.AreEqual(SUT.Children.Single(), SUT.FindName("readerBorder"));
			}
			finally
			{
				Uno.UI.FeatureConfiguration.FrameworkElement.UseLegacyFindNameTreeWalk = true;
			}
		}

		[TestMethod]
		public void When_SimpleElement()
		{
			var SUT = new Grid();

			SUT.Children.Add(new Border { Name = "test" });

			Assert.AreEqual(SUT.Children.First(), SUT.FindName("test"));
		}

		[TestMethod]
		public void When_ContextFlyout()
		{
			var SUT = new Grid();

			var test1 = new MenuFlyoutItem { Name = "test1" };
			var test2 = new MenuFlyoutItem { Name = "test2" };

			SUT.ContextFlyout = new MenuFlyout
			{
				Items = {
					test1,
					test2
				}
			};

			Assert.AreEqual(test1, SUT.FindName("test1"));
			Assert.AreEqual(test2, SUT.FindName("test2"));
		}

		[TestMethod]
		public void When_ButtonFlyout()
		{
			var SUT = new Grid();
			var button = new Button() { Style = new Style() };

			SUT.Children.Add(button);

			var test1 = new MenuFlyoutItem { Name = "test1" };
			var test2 = new MenuFlyoutItem { Name = "test2" };

			button.Flyout = new MenuFlyout
			{
				Items = {
					test1,
					test2
				}
			};

			Assert.AreEqual(test1, SUT.FindName("test1"));
			Assert.AreEqual(test2, SUT.FindName("test2"));
		}
	}
}
