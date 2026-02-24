using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Tests.App.Xaml;
using Uno.UI.Tests.Helpers;
using Uno.UI.Tests.ViewLibrary;
using Uno.UI.Tests.Windows_UI_Xaml.Controls;
#if !NETFX_CORE
using Uno.UI.Xaml;
#endif
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

using Colors = Microsoft.UI.Colors;

namespace Uno.UI.Tests.Windows_UI_Xaml
{
	[TestClass]
	public class Given_ResourceDictionary
	{
		[TestInitialize]
		public void Init()
		{
			UnitTestsApp.App.EnsureApplication();
		}

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
		public void When_Simple_Add_And_Retrieve_Type_Key()
		{
			var rd = new ResourceDictionary();

			// NOTE: Intentionally using a type outside of Uno.UI to prevent regressions if someone thought to use Type.GetType.
			// See: https://stackoverflow.com/a/1825156/5108631
			// Type.GetType(...) only works when the type is found in either mscorlib.dll or the currently executing assembly.
			rd[typeof(Given_ResourceDictionary)] = nameof(When_Simple_Add_And_Retrieve_Type_Key);

			var retrieved = rd[typeof(Given_ResourceDictionary)];

			Assert.IsTrue(rd.ContainsKey(typeof(Given_ResourceDictionary)));
			Assert.IsFalse(rd.ContainsKey("Uno.UI.Tests.Windows_UI_Xaml.Given_ResourceDictionary"));

			Assert.AreEqual(nameof(When_Simple_Add_And_Retrieve_Type_Key), retrieved);

			rd.TryGetValue(typeof(Given_ResourceDictionary), out var retrieved2);
			Assert.AreEqual(nameof(When_Simple_Add_And_Retrieve_Type_Key), retrieved2);

			var key = rd.Keys.Single();
			Assert.AreEqual(typeof(Given_ResourceDictionary), key);

			foreach (var kvp in rd)
			{
				Assert.AreEqual(typeof(Given_ResourceDictionary), kvp.Key);
			}
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
			using var _ = ThemeHelper.SetExplicitRequestedTheme(ApplicationTheme.Dark);
			var retrieved2 = rd["Blu"];
			Assert.AreEqual(Colors.DarkBlue, ((SolidColorBrush)retrieved2).Color);
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

			AssertEx.AssertContainsColorBrushResource(app.Resources, "SuperiorColorBrush", Colors.MediumSpringGreen);
			AssertEx.AssertContainsColorBrushResource(app.Resources, "StrangeColorBrush", Colors.Gainsboro);
		}

		[TestMethod]
		public void When_Resource_In_Merged_Source_Xaml_Element()
		{
			var control = new Test_Control();

			AssertEx.AssertContainsColorBrushResource(control.TestGrid.Resources, "AbominableColorBrush", Colors.Teal);
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

			AssertEx.AssertContainsColorBrushResource(app.Resources, "JustHangingOutInMergedDictColorBrush", Colors.Maroon);
			AssertEx.AssertContainsColorBrushResource(app.Resources, "HangingOutInRecursiveMergedColorBrush", Colors.DarkMagenta);
		}

		[TestMethod]
		public void When_Has_Themes_Inline_Xaml()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			AssertEx.AssertContainsColorBrushResource(app.Resources, "PlayfulColorBrush", Colors.RosyBrown);
			AssertEx.AssertContainsColorBrushResource(app.Resources, "LucrativeColorBrush", Colors.Azure);
		}

		[TestMethod]
		public void When_Has_Themes_Merged_Xaml()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var dark = app.Resources.MergedDictionaries.First().ThemeDictionaries["Dark"] as ResourceDictionary;

			AssertEx.AssertContainsColorBrushResource(dark, "MoodyColorBrush", Colors.DarkKhaki);
		}

		[TestMethod]
		public void When_Different_Control_Instances()
		{
			var control1 = new Test_Control();
			var control2 = new Test_Control();
			Assert.IsTrue(control1.Resources.ContainsKey("LocalColorBrush"));
			Assert.IsTrue(control2.Resources.ContainsKey("LocalColorBrush"));

			Assert.IsFalse(ReferenceEquals(control1.Resources, control2.Resources));

			control2.Resources.Remove("LocalColorBrush");
			Assert.IsTrue(control1.Resources.ContainsKey("LocalColorBrush"));
			Assert.IsFalse(control2.Resources.ContainsKey("LocalColorBrush"));
		}

		[TestMethod]
		public void When_xName_In_Dictionary()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			AssertEx.AssertContainsColorBrushResource(app.Resources, "AliceTheColorBrush", Colors.AliceBlue);
		}

		[TestMethod]
		public void When_xName_In_Merged_Dictionary()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			AssertEx.AssertContainsColorBrushResource(app.Resources, "CadetColorBrush", Colors.CadetBlue);
		}

		[TestMethod]
		public void When_xName_In_Dictionary_Reference_Equality()
		{
			var page = new When_xName_In_Dictionary_Reference_Equality();
			Assert.IsTrue(page.Resources.ContainsKey("MutableBrush"));
			Assert.AreEqual(page.MutableBrush, page.Resources["MutableBrush"]);
			Assert.AreEqual(page.MutableBrush, page.TestBorder.Background);
			Assert.AreEqual(Colors.Green, (page.TestBorder.Background as SolidColorBrush).Color);
		}

		[TestMethod]
		public void When_Resource_Referencing_Resource()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			Assert.AreEqual(ApplicationTheme.Light, app.RequestedTheme);

			AssertEx.AssertContainsColorBrushResource(app.Resources, "ReferenceInSameDictionaryColorBrush", Colors.Sienna);
			AssertEx.AssertContainsColorBrushResource(app.Resources, "LexicallyBackwardColorBrush", Colors.Coral);
		}

		[TestMethod]
		public void When_Enumerated()
		{
			var rd = new ResourceDictionary();
			rd["foot"] = "Boot";
			var inner = new ResourceDictionary();
			inner["hand"] = "Glove";
			rd.MergedDictionaries.Add(inner);
			var def = new ResourceDictionary();
			def["eyes"] = "Sunglasses";
			rd.ThemeDictionaries["Default"] = def;

			var keys = new List<object>();
			foreach (var kvp in rd)
			{
				keys.Add(kvp.Key);
			}

			Assert.IsTrue(keys.Contains("foot"));

			Assert.IsFalse(keys.Contains("hand"));
			Assert.IsTrue(rd.ContainsKey("hand"));

			Assert.IsFalse(keys.Contains("eyes"));
			Assert.IsTrue(rd.ContainsKey("eyes"));
		}

		[TestMethod]
		public void When_Enumerated_With_StaticResource_Alias()
		{
			var xcr = new Microsoft.UI.Xaml.Controls.XamlControlsResources();
			var light = xcr.ThemeDictionaries["Light"] as ResourceDictionary;
			Assert.IsNotNull(light);
			KeyValuePair<object, object> fromEnumeration = default;
			foreach (var kvp in light)
			{
				if (kvp.Key.Equals("ButtonForeground"))
				{
					fromEnumeration = kvp;
				}
			}

			Assert.IsInstanceOfType(fromEnumeration.Value, typeof(SolidColorBrush));
		}

		[TestMethod]
		public void When_Implicit_Style_From_Code()
		{
			var control = new Test_Control();
			Assert.IsTrue(control.Resources.ContainsKey(typeof(RadioButton)));
			Assert.IsNotNull(control.Resources[typeof(RadioButton)] as Style);
		}

		[TestMethod]
		public void When_Merged_Dictionary_Local()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			Assert.IsFalse(app.Resources.ContainsKey("NotInAppResources")); // These dictionaries shouldn't end up in App.Resources, or this test won't test anything

			var control = new Test_Control();
			control.TestTextBlock2.DataContext = true;
			AssertEx.AssertHasColor(control.TestTextBlock2.Foreground, Colors.Orange);
		}

		[TestMethod]
		public void When_Merged_Dictionary_Style()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			Assert.IsFalse(app.Resources.ContainsKey("NotInAppResources")); // These dictionaries shouldn't end up in App.Resources, or this test won't test anything

			var page = new Test_Page();

			app.HostView.Children.Add(page);

			page.Measure(new Size(1000, 1000));

			AssertEx.AssertHasColor(page.TestProgressRing.Foreground, Colors.Fuchsia);
		}

		[TestMethod]
		public void When_Local_Dictionary_Intra_Reference()
		{
			var control = new Test_Control();
			control.TestTextBlock2.DataContext = false;
			AssertEx.AssertHasColor(control.TestTextBlock2.Foreground, Colors.LimeGreen);
		}

		[TestMethod]
		public void When_Local_Lexically_Forward()
		{
			var page = new Test_Page();
			page.TestTextBlock2.DataContext = true;

			AssertEx.AssertHasColor(page.TestTextBlock2.Foreground, Colors.MidnightBlue);
		}

		[TestMethod]
		public void When_Direct_Local_Assignation_From_Source()
		{
			var page = new Test_Page();

			AssertEx.AssertContainsColorBrushResource(page.TestStackPanel.Resources, "NotInAppResources", Colors.Cyan);
		}

		[TestMethod]
		public void When_Created_From_Source_In_Codebehind()
		{
			var rd = new ResourceDictionary { Source = new Uri("ms-appx:///Uno.UI.Tests.ViewLibrary/Themes/More/Test_Dictionary_Abs.xaml") };
			AssertEx.AssertContainsColorBrushResource(rd, "BituminousColorBrush", Colors.SlateGray);

			var rd2 = new ResourceDictionary { Source = new Uri("ms-appx:///Uno.UI.Tests.ViewLibrary/Themes/More/Test_Dictionary_Abs.xaml") };

			rd2.Remove("BituminousColorBrush");
			Assert.IsFalse(rd2.ContainsKey("BituminousColorBrush"));
			Assert.IsTrue(rd.ContainsKey("BituminousColorBrush"));
		}

		[TestMethod]
		public void When_Created_From_Local_Source_In_Codebehind()
		{
			var rd = new ResourceDictionary { Source = new Uri("ms-resource:///Files/App/Xaml/Test_Dictionary.xaml") };
			AssertEx.AssertContainsColorBrushResource(rd, "SuperiorColorBrush", Colors.MediumSpringGreen);
		}

		[TestMethod]
		public void When_Created_From_Local_Source_In_Codebehind_AppX()
		{
			var rd = new ResourceDictionary { Source = new Uri("ms-appx:///App/Xaml/Test_Dictionary.xaml") };
			AssertEx.AssertContainsColorBrushResource(rd, "SuperiorColorBrush", Colors.MediumSpringGreen);
		}

		[TestMethod]
		public void When_Created_From_Local_Source_In_Codebehind_Ensure_Lazy()
		{
			var rd = new ResourceDictionary { Source = new Uri("ms-resource:///Files/App/Xaml/Test_Dictionary_Lazy.xaml") };
			AssertEx.AssertContainsColorBrushResource(rd, "LiteralColorBrush", Colors.Fuchsia);
			AssertEx.AssertContainsColorBrushResource(rd, "ThemedLiteralColorBrush", Colors.DarkOrchid);

			Assert.ThrowsExactly<InvalidOperationException>(() =>
			{
				var _ = rd["LazyResource"];
			});

			Assert.ThrowsExactly<InvalidOperationException>(() =>
			{
				var _ = rd["ThemedLazyResource"];
			});

			Assert.IsTrue(rd.ThemeDictionaries.ContainsKey("Nope"));
			Assert.ThrowsExactly<InvalidOperationException>(() =>
			{
				var _ = rd.ThemeDictionaries["Nope"];
			});
		}

		[TestMethod]
		public void When_External_Source()
		{
			var page = new Test_Page();
			var rp = page.TestRelativePanel;
			var rd = rp.Resources;
			AssertEx.AssertContainsColorBrushResource(rd, "BituminousColorBrush", Colors.SlateGray);
		}

		[TestMethod]
		public void When_Local_Source_Absolute()
		{
			var page = new Test_Page();
			var b = page.TestBorder;
			var rd = b.Resources;
			AssertEx.AssertContainsColorBrushResource(rd, "FerociousColorBrush", Colors.Fuchsia);
		}

		[TestMethod]
		public void When_Merged_Xaml_By_Type()
		{
			var page = new Test_Page();
			Assert.AreEqual("Hakuna Matata", page.testSubclassedDictionaryTextBlock.Text);
		}

		[TestMethod]
		public void When_Xaml_By_Type_Ref_Equality()
		{
			var page = new Test_Page_Other();

			Assert.IsInstanceOfType(page.testGrid1.Resources, typeof(Subclassed_Dictionary));

			var color = Colors.WhiteSmoke;
			var b1 = page.testBorder1.Background;
			var b2 = page.testBorder2.Background;
			AssertEx.AssertHasColor(b1, color);
			AssertEx.AssertHasColor(b2, color);

			var areRefEqual = ReferenceEquals(b1, b2);
			Assert.IsFalse(areRefEqual);
		}

		[TestMethod]
		public void When_By_Type_From_Code()
		{
			var dict = new Subclassed_Dictionary();

			Assert.AreEqual("Hakuna Matata", dict["ProblemFreePhilosophy"]);
			AssertEx.AssertContainsColorBrushResource(dict, "PerilousColorBrush", Colors.WhiteSmoke);

			Assert.AreEqual("The Cold", dict["NeverBotheredMeAnyway"]);
		}

		[TestMethod]
		public void When_Nested_StaticResource()
		{
			var dict = new Subclassed_Dictionary();

			var converter = dict["InnerResourceConverter"] as MyConverter;
			var brush = dict["PerilousColorBrush"];
			var text = dict["ProblemFreePhilosophy"];

			Assert.IsNotNull(converter);
			Assert.IsNotNull(brush);
			Assert.AreEqual(brush, converter.Values[0].Value);
			Assert.AreEqual(text, converter.Value);
		}

		[TestMethod]
		public void When_By_Type_With_Template()
		{
			var dict = new Subclassed_Dictionary();

			var template = dict["UproariousTemplate"] as DataTemplate;
			Assert.IsNotNull(template);
			var content = template.LoadContent();
			Assert.IsInstanceOfType(content, typeof(CheckBox));
		}

		[TestMethod]
		public void When_By_xName_And_Key_In_Element_Resources()
		{
			var testControl = new Test_Control();

			Assert.HasCount(2, testControl.SubliminalGradientBrushByName.GradientStops);

			var fromResources = testControl.Resources["SubliminalGradientBrush"] as LinearGradientBrush;
			Assert.IsNotNull(fromResources);
			Assert.HasCount(2, fromResources.GradientStops);
		}

		[TestMethod]
		public void When_Accessing_System_Resource()
		{
			var rd = new ResourceDictionary();

			Assert.IsTrue(rd.ContainsKey("SystemAltHighColor"));
			var systemColor = (Color)rd["SystemAltHighColor"];
			Assert.AreEqual(Colors.White, systemColor);
		}

		[TestMethod]
		public void When_External_Source_Miscased()
		{
			var page = new Test_Page_Other();

			var foreground = page.CaseInsensitiveSourceTextBlock.Foreground as SolidColorBrush;
			Assert.IsNotNull(foreground);
			Assert.AreEqual(Colors.SlateGray, foreground.Color);
		}

#if !NETFX_CORE
		[TestMethod]
		public void When_Relative_Path_With_Leading_Slash_From_Root()
		{
			var withSlash = XamlFilePathHelper.ResolveAbsoluteSource("App.xaml", "/App/Xaml/Test_Dictionary.xaml");
			var withoutSlash = XamlFilePathHelper.ResolveAbsoluteSource("App.xaml", "App/Xaml/Test_Dictionary.xaml");

			Assert.AreEqual(withoutSlash, withSlash);
		}

		[TestMethod]
		public void When_Relative_Path_With_Leading_Slash_From_Non_Root()
		{
			var withSlash = XamlFilePathHelper.ResolveAbsoluteSource("Dictionaries/App.xaml", "/App/Xaml/Test_Dictionary.xaml");
			var withoutSlash = XamlFilePathHelper.ResolveAbsoluteSource("Dictionaries/App.xaml", "App/Xaml/Test_Dictionary.xaml");

			Assert.AreEqual("App/Xaml/Test_Dictionary.xaml", withSlash);
			Assert.AreEqual("Dictionaries/App/Xaml/Test_Dictionary.xaml", withoutSlash);
		}

		[TestMethod]
		public void When_SharedHelpers_FindResource()
		{
			var rdInner = new ResourceDictionary();
			rdInner["Grin"] = new SolidColorBrush(Colors.DarkOliveGreen);

			var rd = new ResourceDictionary();
			rd.MergedDictionaries.Add(rdInner);

			var brush = global::Uno.UI.Helpers.WinUI.SharedHelpers.FindResource("Grin", rd, null);

			Assert.IsNotNull(brush);
			Assert.AreEqual(Colors.DarkOliveGreen, (brush as SolidColorBrush).Color);
		}
#endif

		[TestMethod]
		public void When_XamlControlsResources()
		{
			var xcr = new Microsoft.UI.Xaml.Controls.XamlControlsResources();
			Assert.IsTrue(xcr.ContainsKey(typeof(Button)));
			Assert.IsInstanceOfType(xcr[typeof(Button)], typeof(Style));
		}

		[TestMethod]
		public void When_Needs_Eager_Materialization()
		{
			Assert.IsFalse(TestInitializer.IsInitialized);
			var control = new Test_Control_With_Initializer();
			Assert.IsTrue(TestInitializer.IsInitialized);
		}

		[TestMethod]
		public void When_Space_In_Key()
		{
			var page = new Test_Page_Other();
			var border = page.SpaceInKeyBorder;
			Assert.AreEqual(Colors.SlateBlue, (border.Background as SolidColorBrush).Color);
		}

		[TestMethod]
		public void When_Only_Theme_Dictionaries()
		{
			var page = new Test_Page_Other();
			var tb = page.ThemeDictionaryOnlyTextBlock;
			Assert.AreEqual(Colors.MediumPurple, (tb.Foreground as SolidColorBrush).Color);
		}

		[TestMethod]
		public void When_Source_And_Globbing_From_Included_File()
		{
			var ctrl = new When_Source_And_Globbing_From_Included_File();
			var resources = ctrl.Resources;
			Assert.IsTrue(resources.ContainsKey("GlobPropsMarginButtonStyle"));

			var style = resources["GlobPropsMarginButtonStyle"] as Style;
			var button = new Button();
			button.Style = style;
			Assert.AreEqual(new Thickness(99, 33, 7, 7), button.Margin);
		}

		[TestMethod]
		public void When_Custom_Resource_Dictionary_With_Custom_Property()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var rd = app.Resources.MergedDictionaries.FirstOrDefault(x => x is Subclassed_Dictionary_With_Property);

			Assert.IsNotNull(rd);
			Assert.IsTrue(rd.ContainsKey("TestKey"));
			Assert.AreEqual("Test123", rd["TestKey"]);
		}

		[TestMethod]
		public void When_Custom_Resource_Dictionary_With_Custom_Property_in_Custom_Control()
		{
			var ctrl = new Test_Control_With_Subclassed_ResourceDictionary_With_Custom_Property();
			var resources = ctrl.Resources;

			Assert.IsNotNull(resources);
			Assert.IsTrue(resources.ContainsKey("TestKey"));
			Assert.AreEqual("Test123", resources["TestKey"]);
		}

		[TestMethod]
		public void When_Theme_Dictionary_Is_Cached_Then_Add_And_Clear_Theme()
		{
			var dictionary = new ResourceDictionary();
			dictionary.TryGetValue("TestKey", out _); // This causes _activeThemeDictionary to be cached.

			var lightTheme = new ResourceDictionary();
			lightTheme.Add("TestKey", "TestValue");
			dictionary.ThemeDictionaries.Add("Light", lightTheme); // Cached value is no longer valid due to adding theme.

			// Make sure the cache is updated.
			Assert.IsTrue(dictionary.TryGetValue("TestKey", out var testValue));
			Assert.AreEqual("TestValue", testValue);

			dictionary.ThemeDictionaries.Clear(); // Cached value is no longer valid due to clearing themes.

			Assert.IsFalse(dictionary.TryGetValue("TestKey", out _));
		}

		[TestMethod]
		public void When_Theme_Dictionary_Is_Cached_Then_Add_And_Remove_Theme()
		{
			var dictionary = new ResourceDictionary();
			dictionary.TryGetValue("TestKey", out _); // This causes _activeThemeDictionary to be cached.

			var lightTheme = new ResourceDictionary();
			lightTheme.Add("TestKey", "TestValue");
			dictionary.ThemeDictionaries.Add("Light", lightTheme); // Cached value is no longer valid due to adding theme.

			// Make sure the cache is updated.
			Assert.IsTrue(dictionary.TryGetValue("TestKey", out var testValue));
			Assert.AreEqual("TestValue", testValue);

			Assert.IsTrue(dictionary.ThemeDictionaries.Remove("Light")); // Cached value is no longer valid due to removing theme.

			Assert.IsFalse(dictionary.TryGetValue("TestKey", out _));
		}


		[TestMethod]
		public void When_Default_Theme_Dictionary_Is_Cached()
		{
			var defaultDictionary = new ResourceDictionary();
			defaultDictionary.Add("TestKey", "TestValueFromDefaultDictionary");

			var dictionary = new ResourceDictionary();
			dictionary.ThemeDictionaries.Add("Default", defaultDictionary);
			Assert.IsTrue(dictionary.TryGetValue("TestKey", out var testValue));
			Assert.AreEqual("TestValueFromDefaultDictionary", testValue);

			var lightDictionary = new ResourceDictionary();
			lightDictionary.Add("TestKey", "TestValueFromLightDictionary");
			dictionary.ThemeDictionaries.Add("Light", lightDictionary);
			Assert.IsTrue(dictionary.TryGetValue("TestKey", out testValue));
			Assert.AreEqual("TestValueFromLightDictionary", testValue);
		}

		[TestMethod]
		public void When_Default_Theme_Dictionary_Should_Be_Used_After_Removing_Active_Theme()
		{
			var lightDictionary = new ResourceDictionary();
			lightDictionary.Add("TestKey", "TestValueFromLightDictionary");

			var dictionary = new ResourceDictionary();
			dictionary.ThemeDictionaries.Add("Light", lightDictionary);
			Assert.IsTrue(dictionary.TryGetValue("TestKey", out var testValue));
			Assert.AreEqual("TestValueFromLightDictionary", testValue);

			var defaultDictionary = new ResourceDictionary();
			defaultDictionary.Add("TestKey", "TestValueFromDefaultDictionary");
			dictionary.ThemeDictionaries.Add("Default", defaultDictionary);
			dictionary.ThemeDictionaries.Remove("Light");
			Assert.IsTrue(dictionary.TryGetValue("TestKey", out testValue));
			Assert.AreEqual("TestValueFromDefaultDictionary", testValue);

			dictionary.ThemeDictionaries.Remove("Default");
			Assert.IsFalse(dictionary.TryGetValue("TestKey", out _));
		}

		[TestMethod]
		public void When_Lazy()
		{
			var dictionary = new ResourceDictionary();
			var brush = new SolidColorBrush { Color = Colors.Red };
			dictionary.TryGetValue("TestKey", out _);

			var app = UnitTestsApp.App.EnsureApplication();

			dictionary.ThemeDictionaries.Add("Light", new WeakResourceInitializer(app, o =>
				new ResourceDictionary
				{
					["TestKey"] = new WeakResourceInitializer(o, _ => brush)
				}));


			// Make sure the cache is updated.
			Assert.IsTrue(dictionary.TryGetValue("TestKey", out var testValue));
			Assert.AreEqual(brush, testValue);

		}

		[TestMethod]
		public void When_Resource_NotImplemented()
		{
			var initialCreationCount = SomeNotImplType.CreationAttempts;
			var page = new When_Resource_NotImplemented_Page();
			var resources = page.Resources;
			AssertEx.AssertContainsColorBrushResource(resources, "LarcenousColorBrush", Colors.PaleVioletRed);
			Assert.AreEqual(initialCreationCount, SomeNotImplType.CreationAttempts);
		}

		[TestMethod]
		public void ThemeResource_Named_ResourceDictionary_Override()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var SUT = new ThemeResource_Named_ResourceDictionary_Override();
			SUT.ForceLoaded();

			Assert.IsInstanceOfType(SUT.border01.Background, typeof(SolidColorBrush));
			Assert.IsInstanceOfType(SUT.border02.Background, typeof(SolidColorBrush));
			Assert.IsInstanceOfType(SUT.border03.Background, typeof(SolidColorBrush));
			Assert.AreEqual(Colors.Red, ((SolidColorBrush)SUT.border01.Background).Color);
			Assert.AreEqual(Colors.Blue, ((SolidColorBrush)SUT.border02.Background).Color);
			Assert.AreEqual(Colors.Green, ((SolidColorBrush)SUT.border03.Background).Color);
		}
	}
}
