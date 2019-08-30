using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Tests.App.Xaml;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Uno.UI.Tests.Windows_UI_Xaml
{
	[TestClass]
	public class Given_ResourceDictionary
	{
		[TestMethod]
		public void When_Simple_Add_And_Retrieve()
		{
			var rd = new ResourceDictionary();
			rd["Grin"] = new SolidColorBrush(Colors.DarkOliveGreen);

			var retrieved = rd["Grin"];

			Assert.IsTrue(rd.ContainsKey("Grin"));

			Assert.AreEqual(Colors.DarkOliveGreen, ((SolidColorBrush)retrieved).Color);

			rd.TryGetValue("Grin", out var retrieved2);
			Assert.AreEqual(Colors.DarkOliveGreen, ((SolidColorBrush)retrieved2).Color);
		}

		[TestMethod]
		public void When_Key_Not_Present()
		{
			var rd = new ResourceDictionary();

			//var retrieved = rd["Nope"]; //Throws on UWP

			;
		}

		[TestMethod]
		public void When_Merged()
		{
			var rdInner = new ResourceDictionary();
			rdInner["Grin"] = new SolidColorBrush(Colors.DarkOliveGreen);

			var rd = new ResourceDictionary();
			rd.MergedDictionaries.Add(rdInner);

			Assert.IsTrue(rd.ContainsKey("Grin"));

			var retrieved = rd["Grin"];
			Assert.AreEqual(Colors.DarkOliveGreen, ((SolidColorBrush)retrieved).Color);

			rd.TryGetValue("Grin", out var retrieved2);
			Assert.AreEqual(Colors.DarkOliveGreen, ((SolidColorBrush)retrieved2).Color);
		}

		[TestMethod]
		public void When_Deep_Merged()
		{
			var rd1 = new ResourceDictionary();
			rd1["Grin"] = new SolidColorBrush(Colors.DarkOliveGreen);

			var rd2 = new ResourceDictionary();
			rd2.MergedDictionaries.Add(rd1);

			var rd3 = new ResourceDictionary();
			rd3.MergedDictionaries.Add(rd2);

			var rd = new ResourceDictionary();
			rd.MergedDictionaries.Add(rd3);

			Assert.IsTrue(rd.ContainsKey("Grin"));

			Assert.IsFalse(rd.ContainsKey("Blu"));

			var retrieved = rd["Grin"];
			Assert.AreEqual(Colors.DarkOliveGreen, ((SolidColorBrush)retrieved).Color);

			rd.TryGetValue("Grin", out var retrieved2);
			Assert.AreEqual(Colors.DarkOliveGreen, ((SolidColorBrush)retrieved2).Color);
		}

		[TestMethod]
		public void When_Duplicates_Merged()
		{
			var rd1 = new ResourceDictionary();
			rd1["Grin"] = new SolidColorBrush(Colors.DarkOliveGreen);

			var rd2 = new ResourceDictionary();
			rd2["Grin"] = new SolidColorBrush(Colors.ForestGreen);

			var rd = new ResourceDictionary();
			rd.MergedDictionaries.Add(rd1);
			rd.MergedDictionaries.Add(rd2);

			var retrieved = rd["Grin"];

			//https://docs.microsoft.com/en-us/windows/uwp/design/controls-and-patterns/resourcedictionary-and-xaml-resource-references#merged-resource-dictionaries
			Assert.AreEqual(Colors.ForestGreen, ((SolidColorBrush)retrieved).Color);
		}



		[TestMethod]
		public void When_Duplicates_Merged_And_Last_Is_Null()
		{
			var rd1 = new ResourceDictionary();
			rd1["Grin"] = new SolidColorBrush(Colors.DarkOliveGreen);

			var rd2 = new ResourceDictionary();
			rd2["Grin"] = null;

			var rd = new ResourceDictionary();
			rd.MergedDictionaries.Add(rd1);
			rd.MergedDictionaries.Add(rd2);

			var retrieved = rd["Grin"];

			//https://docs.microsoft.com/en-us/windows/uwp/design/controls-and-patterns/resourcedictionary-and-xaml-resource-references#merged-resource-dictionaries
			Assert.IsNull(retrieved);

			Assert.IsTrue(rd.ContainsKey("Grin"));
		}

		[TestMethod]
		public void When_Has_Themes()
		{
			var rd = new ResourceDictionary();
			var dflt = new ResourceDictionary();
			rd.ThemeDictionaries["Default"] = dflt;

			dflt["Blu"] = new SolidColorBrush(Colors.DodgerBlue);

			var retrievedExplicit = (rd.ThemeDictionaries["Default"] as ResourceDictionary)["Blu"];
			Assert.AreEqual(Colors.DodgerBlue, ((SolidColorBrush)retrievedExplicit).Color);
			;

			var retrieved = rd["Blu"];
			Assert.AreEqual(Colors.DodgerBlue, ((SolidColorBrush)retrieved).Color);
			;
			Assert.IsTrue(rd.ContainsKey("Blu"));
		}

		[TestMethod]
		public void When_Has_Themes_And_Direct()
		{
			var rd = new ResourceDictionary();
			var dflt = new ResourceDictionary();
			rd.ThemeDictionaries["Default"] = dflt;

			dflt["Blu"] = new SolidColorBrush(Colors.DodgerBlue);

			rd["Blu"] = new SolidColorBrush(Colors.CornflowerBlue);

			var retrievedExplicit = (rd.ThemeDictionaries["Default"] as ResourceDictionary)["Blu"];
			Assert.AreEqual(Colors.DodgerBlue, ((SolidColorBrush)retrievedExplicit).Color);
			;

			var retrieved = rd["Blu"];
			Assert.AreEqual(Colors.CornflowerBlue, ((SolidColorBrush)retrieved).Color);
			;
		}

		[TestMethod]
		public void When_Has_Themes_And_Inherited_Direct()
		{
			var rd = new ResourceDictionary();
			var dflt = new ResourceDictionary();
			rd.ThemeDictionaries["Default"] = dflt;

			dflt["Blu"] = new SolidColorBrush(Colors.DodgerBlue);

			var rd2 = new ResourceDictionary();
			rd2["Blu"] = new SolidColorBrush(Colors.CornflowerBlue);
			rd.MergedDictionaries.Add(rd2);

			var retrievedExplicit = (rd.ThemeDictionaries["Default"] as ResourceDictionary)["Blu"];
			Assert.AreEqual(Colors.DodgerBlue, ((SolidColorBrush)retrievedExplicit).Color);
			;

			var retrieved = rd["Blu"];
			Assert.AreEqual(Colors.CornflowerBlue, ((SolidColorBrush)retrieved).Color);
			;
		}

		[TestMethod]
		public void When_Has_Inherited_Themes()
		{
			var rd = new ResourceDictionary();

			var rd2 = new ResourceDictionary();
			var dflt = new ResourceDictionary();
			rd2.ThemeDictionaries["Default"] = dflt;
			dflt["Blu"] = new SolidColorBrush(Colors.DodgerBlue);
			rd.MergedDictionaries.Add(rd2);
			;

			var retrieved = rd["Blu"];
			Assert.AreEqual(Colors.DodgerBlue, ((SolidColorBrush)retrieved).Color);
			;
			Assert.IsTrue(rd.ContainsKey("Blu"));
		}

		[TestMethod]
		public void When_Has_Multiple_Themes()
		{
#if !NETFX_CORE
			UnitTestsApp.App.EnsureApplication();			
#endif

			var rd = new ResourceDictionary();
			var light = new ResourceDictionary();
			light["Blu"] = new SolidColorBrush(Colors.LightBlue);
			var dark = new ResourceDictionary();
			dark["Blu"] = new SolidColorBrush(Colors.DarkBlue);

			rd.ThemeDictionaries["Light"] = light;
			rd.ThemeDictionaries["Dark"] = dark;

			Assert.IsTrue(rd.ContainsKey("Blu"));

			var retrieved1 = rd["Blu"];
			Assert.AreEqual(ApplicationTheme.Light, Application.Current.RequestedTheme);
			Assert.AreEqual(Colors.LightBlue, ((SolidColorBrush)retrieved1).Color);

#if !NETFX_CORE //Not legal on UWP to change theme after app has launched
			try
			{
				Application.Current.ForceSetRequestedTheme(ApplicationTheme.Dark);
				var retrieved2 = rd["Blu"];
				Assert.AreEqual(Colors.DarkBlue, ((SolidColorBrush)retrieved2).Color);
			}
			finally
			{
				Application.Current.ForceSetRequestedTheme(ApplicationTheme.Light);
			}
#endif
		}

		[TestMethod]
		public void When_Has_Inactive_Theme()
		{
#if !NETFX_CORE
			UnitTestsApp.App.EnsureApplication();
#endif
			var rd = new ResourceDictionary();
			var dark = new ResourceDictionary();
			dark["Blu"] = new SolidColorBrush(Colors.DarkBlue);

			rd.ThemeDictionaries["Dark"] = dark;

			Assert.AreEqual(ApplicationTheme.Light, Application.Current.RequestedTheme);
			Assert.IsFalse(rd.ContainsKey("Blu"));
			;
		}

		[TestMethod]
		public void When_Has_Default_And_Light()
		{
			UnitTestsApp.App.EnsureApplication();

			var rd = new ResourceDictionary();
			var dflt = new ResourceDictionary();
			dflt["Blu"] = new SolidColorBrush(Colors.Aqua);
			var light = new ResourceDictionary();

			rd.ThemeDictionaries["Default"] = dflt;
			rd.ThemeDictionaries["Light"] = light;

			Assert.AreEqual(ApplicationTheme.Light, Application.Current.RequestedTheme);

			Assert.IsFalse(rd.ContainsKey("Blu"));

			var inner = new ResourceDictionary();
			var lightInner = new ResourceDictionary();
			lightInner["Blu"] = new SolidColorBrush(Colors.DarkSlateBlue);
			inner.ThemeDictionaries["Light"] = lightInner;
			rd.MergedDictionaries.Add(inner);

			Assert.IsTrue(rd.ContainsKey("Blu"));

			Assert.AreEqual(Colors.DarkSlateBlue, (rd["Blu"] as SolidColorBrush).Color);
		}

		[TestMethod]
		public void When_Has_Custom_Theme()
		{
			var rd = new ResourceDictionary();
			var pink = new ResourceDictionary();
			pink["Color1"] = new SolidColorBrush(Colors.HotPink);

			rd.ThemeDictionaries["Pink"] = pink;

#if !NETFX_CORE
			UnitTestsApp.App.EnsureApplication();

			ApplicationHelper.RequestedCustomTheme = "Pink";

			Assert.IsTrue(rd.ContainsKey("Color1"));

			var retrieved = rd["Color1"];
			Assert.AreEqual(Colors.HotPink, ((SolidColorBrush)retrieved).Color);

			ApplicationHelper.RequestedCustomTheme = null;
#endif
		}

		[TestMethod]
		public void When_Resource_On_Top_Control_Xaml()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var control = new Test_Control();
			app.HostView.Children.Add(control);

			var brush = control.Resources["LocalColorBrush"] as SolidColorBrush;
			Assert.AreEqual(Colors.Linen, brush.Color);
		}

		[TestMethod]
		public void When_Resource_On_Inner_Control_Xaml()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var control = new Test_Control();
			app.HostView.Children.Add(control);

			var brush = control.TestGrid.Resources["LocalToGridColorBrush"] as SolidColorBrush;

			Assert.AreEqual(Colors.LimeGreen, brush.Color);
		}

		[TestMethod]
		public void When_Resource_In_Merged_Source_Xaml()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			AssertContainsColorBrushResource(app.Resources, "SuperiorColorBrush", Colors.MediumSpringGreen);
			AssertContainsColorBrushResource(app.Resources, "StrangeColorBrush", Colors.Gainsboro);
		}

		[TestMethod]
		public void When_Resource_In_Merged_Source_Xaml_Check_Source()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var source = app.Resources.MergedDictionaries.First().Source;
			Assert.AreEqual("/Files/App/Xaml/Test_Dictionary.xaml", source.AbsolutePath);
			Assert.AreEqual("ms-resource:///Files/App/Xaml/Test_Dictionary.xaml", source.AbsoluteUri);
		}

		[TestMethod]
		public void When_Resource_In_Merged_Inline_Xaml()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			AssertContainsColorBrushResource(app.Resources, "JustHangingOutInMergedDictColorBrush", Colors.Maroon);
			AssertContainsColorBrushResource(app.Resources, "HangingOutInRecursiveMergedColorBrush", Colors.DarkMagenta);
		}

		private void AssertContainsColorBrushResource(ResourceDictionary resources, string key, Color expected)
		{
			var brush = resources[key] as SolidColorBrush;
			Assert.IsNotNull(brush);
			Assert.AreEqual(expected, brush.Color);
		}
	}
}
