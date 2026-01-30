using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions;
using Uno.UI.Tests.App.Views;
using Uno.UI.Tests.App.Xaml;
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Tests.ViewLibrary;
using Uno.UI.Tests.Helpers;

namespace Uno.UI.Tests.Windows_UI_Xaml
{
	[TestClass]
	public class Given_Implicit_Style
	{
		private Thickness DefaultButtonPadding => Given_Explicit_Style.DefaultButtonPadding;

		[TestInitialize]
		public void Init()
		{
			UnitTestsApp.App.EnsureApplication();
		}

		[TestMethod]
		public void When_Implicit_Style_In_Application_Merged()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var control = new Test_Control();
			app.HostView.Children.Add(control);

			Assert.AreEqual(Microsoft.UI.Colors.LightGoldenrodYellow, (control.TestCheckBox.Foreground as SolidColorBrush).Color);
		}

		[TestMethod]
		public void When_Implicit_Style_In_Visual_Tree_Local_Type()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var control = new Test_Control();

			var strBefore = control.InlineTemplateControl.MyStringDP;

			app.HostView.Children.Add(control);

			var strAfter = control.InlineTemplateControl.MyStringDP;

			Assert.AreEqual("AppLevelImplicit", strBefore);
			Assert.AreEqual("InnerTreeImplicit", strAfter);
		}

		[TestMethod]
		public void When_Implicit_Style_In_Visual_Tree_Framework_Type()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var control = new Test_Control();

			var tagBefore = control.TestRadioButton.Tag;

			app.HostView.Children.Add(control);

			var tagAfter = control.TestRadioButton.Tag;

			Assert.AreEqual("AppLevelImplicit", tagBefore);
			Assert.AreEqual("InnerTreeImplicit", tagAfter);
		}

		[TestMethod]
		public void When_Newly_Created_Custom_Type()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var styledControl = new StylesTestControl();

			var styledButton = new StylesTestButton();

			Assert.AreEqual("FromDefault", styledControl.Rugosity);
			Assert.AreEqual(new Thickness(), styledButton.Padding);

			app.HostView.Children.Add(styledControl);
			app.HostView.Children.Add(styledButton);

			Assert.AreEqual("TopImplicitStyleValue", styledControl.Rugosity);
			Assert.AreEqual(DefaultButtonPadding, styledButton.Padding);
		}

		[TestMethod]
		public void When_Framework_Type_Default_Style()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var button = new Button();

			app.HostView.Children.Add(button);

			Assert.IsNull(button.Style); //Default style is not actually set to the Style property
			Assert.AreEqual(DefaultButtonPadding, button.Padding);
		}

		[TestMethod]
		public void When_Newly_Created_Framework_Type_Set_Style()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var button = new Button();

			var style = app.Resources["ExplicitButtonStyle"] as Style;
			button.Style = style;
			Assert.AreEqual(new Thickness(), button.Padding); //Default style not applied yet, we haven't been added to the visual tree.
		}

		[TestMethod]
		public void When_Newly_Created_Framework_Type_ApplyTemplate()
		{
			var button = new Button() { /*No Content - note that if we had content, a default template would be created*/ };

			button.ApplyTemplate();

			Assert.AreEqual(new Thickness(), button.Padding);
			Assert.IsNull(button.Template);
		}

		[TestMethod]
		public void When_Implicit_Style_And_Programmatic_Explicit_Style()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var testControl = new Test_Control();

			Assert.AreEqual("AppLevelImplicit", testControl.TestRadioButton.Tag);

			var style = app.Resources["ExplicitRadioButtonStyle1"] as Style;
			Assert.IsNotNull(style);
			testControl.TestRadioButton.Style = style;
			Assert.AreEqual(Colors.Moccasin, (testControl.TestRadioButton.Background as SolidColorBrush).Color);
			Assert.IsNull(testControl.TestRadioButton.Tag);
		}

		[TestMethod]
		public void When_Inheriting_From_Type_With_Implicit_Style()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var testControl = new Test_Control();

			app.HostView.Children.Add(testControl);

			Assert.AreEqual("InnerTreeImplicit", testControl.TestRadioButton.Tag);
			Assert.AreEqual(typeof(RadioButton), testControl.StylesTestRadioButton.ExposedKey);
			Assert.IsNull(testControl.StylesTestRadioButton.Tag); //Inherited type doesn't use implicit style for base type
		}

		[TestMethod]
		public void When_From_External_Library_And_CodeBehind()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var extControl = new MyExtControl();

			app.HostView.Children.Add(extControl);

			Assert.AreEqual("FromDefaultStyle", extControl.MyTag);
		}

		[TestMethod]
		public void When_From_External_Library_And_Xaml()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var testPage = new Test_Page();

			app.HostView.Children.Add(testPage);

			Assert.AreEqual("FromDefaultStyle", testPage.MyExtControl.MyTag);
			Assert.AreEqual("FromLocalImplicit", testPage.MyExtControl.MyTag2);
		}

		[TestMethod]
		public void When_Inherited_Property()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var testPage = new Test_Page();

			app.HostView.Children.Add(testPage);

			testPage.Measure(new Size(1000, 1000));

			var accentColor = (Color)app.Resources["AccentColorAlias"];
			AssertEx.AssertHasColor(testPage.OuterHyperlinkButton.Foreground, accentColor);
			AssertEx.AssertHasColor(testPage.InnerHyperlinkButton.Foreground, accentColor);
		}

		[TestMethod]
		public void Verify_Framework_DefaultStyleKeys()
		{
			var keyProp = typeof(Control)
#if NETFX_CORE
				.GetTypeInfo()
				.GetDeclaredProperty("DefaultStyleKey");
#else
				.GetProperty("DefaultStyleKey", BindingFlags.NonPublic | BindingFlags.Instance);
#endif

			AssertDefaultStyleKey<Microsoft.UI.Xaml.Controls.ContentDialog>();
#if !NETFX_CORE
			AssertDefaultStyleKey<Microsoft.UI.Xaml.Controls.DatePickerSelector>();
			AssertDefaultStyleKey<Microsoft.UI.Xaml.Controls.TimePickerSelector>();
#endif
			AssertDefaultStyleKey<Microsoft.UI.Xaml.Controls.FlyoutPresenter>();
			AssertDefaultStyleKey<Microsoft.UI.Xaml.Controls.ContentControl>();
			AssertDefaultStyleKey<Microsoft.UI.Xaml.Controls.HyperlinkButton>();
			AssertDefaultStyleKey<Microsoft.UI.Xaml.Controls.ComboBox>();
			AssertDefaultStyleKey<Microsoft.UI.Xaml.Controls.ComboBoxItem>();
			AssertDefaultStyleKey<Microsoft.UI.Xaml.Controls.ListViewItem>();
			AssertDefaultStyleKey<Microsoft.UI.Xaml.Controls.GridViewItem>();
			AssertDefaultStyleKey<Microsoft.UI.Xaml.Controls.TimePicker>();
			AssertDefaultStyleKey<Microsoft.UI.Xaml.Controls.SplitView>();
			AssertDefaultStyleKey<Microsoft.UI.Xaml.Controls.ListView>();
			AssertDefaultStyleKey<Microsoft.UI.Xaml.Controls.GridView>();
			AssertDefaultStyleKey<Microsoft.UI.Xaml.Controls.ScrollViewer>();
			AssertDefaultStyleKey<Microsoft.UI.Xaml.Controls.AppBarToggleButton>();
			AssertDefaultStyleKey<Microsoft.UI.Xaml.Controls.AppBarSeparator>();
			AssertDefaultStyleKey<Microsoft.UI.Xaml.Controls.ListViewHeaderItem>();
			AssertDefaultStyleKey<Microsoft.UI.Xaml.Controls.GridViewHeaderItem>();
			AssertDefaultStyleKey<Microsoft.UI.Xaml.Controls.FlipViewItem>();
			AssertDefaultStyleKey<Microsoft.UI.Xaml.Controls.PivotItem>(isString: true);
#if !NETFX_CORE //Calling new PivotHeaderItem() on Windows throws an exception(!)
			AssertDefaultStyleKey<Microsoft.UI.Xaml.Controls.Primitives.PivotHeaderItem>();
#endif
			AssertDefaultStyleKey<Microsoft.UI.Xaml.Controls.AutoSuggestBox>();
#if !HAS_UNO_WINUI
			AssertDefaultStyleKey<Microsoft.UI.Xaml.Controls.MediaPlayerElement>();
			AssertDefaultStyleKey<Microsoft.UI.Xaml.Controls.MediaTransportControls>();
#endif
			AssertDefaultStyleKey<Microsoft.UI.Xaml.Controls.Button>();
			AssertDefaultStyleKey<Microsoft.UI.Xaml.Controls.CheckBox>();
			AssertDefaultStyleKey<Microsoft.UI.Xaml.Controls.ToggleSwitch>();
			AssertDefaultStyleKey<Microsoft.UI.Xaml.Controls.Slider>();
			AssertDefaultStyleKey<Microsoft.UI.Xaml.Controls.ProgressBar>();
			AssertDefaultStyleKey<Microsoft.UI.Xaml.Controls.TextBox>();
			AssertDefaultStyleKey<Microsoft.UI.Xaml.Controls.Pivot>(isString: true);
			AssertDefaultStyleKey<Microsoft.UI.Xaml.Controls.CommandBar>();
			AssertDefaultStyleKey<Microsoft.UI.Xaml.Controls.AppBarButton>();
			AssertDefaultStyleKey<Microsoft.UI.Xaml.Controls.Frame>();
			AssertDefaultStyleKey<Microsoft.UI.Xaml.Controls.MenuBarItem>(isString: true);
			AssertDefaultStyleKey<Microsoft.UI.Xaml.Controls.MenuFlyoutPresenter>();
			AssertDefaultStyleKey<Microsoft.UI.Xaml.Controls.MenuFlyoutItem>();
			AssertDefaultStyleKey<Microsoft.UI.Xaml.Controls.MenuFlyoutSubItem>();
			AssertDefaultStyleKey<Microsoft.UI.Xaml.Controls.NavigationView>(isString: true);
			AssertDefaultStyleKey<Microsoft.UI.Xaml.Controls.NavigationViewItem>(isString: true);
			AssertDefaultStyleKey<Microsoft.UI.Xaml.Controls.Primitives.NavigationViewItemPresenter>(isString: true);
			AssertDefaultStyleKey<Microsoft.UI.Xaml.Controls.NavigationViewItemHeader>(isString: true);
			AssertDefaultStyleKey<Microsoft.UI.Xaml.Controls.NavigationViewItemSeparator>(isString: true);

			AssertDefaultStyleKey<ItemsControl>();
			AssertDefaultStyleKey<Microsoft.UI.Xaml.Controls.Primitives.Thumb>();
			AssertDefaultStyleKey<Microsoft.UI.Xaml.Controls.Primitives.ToggleButton>();
			AssertDefaultStyleKey<RadioButton>();

			AssertDefaultStyleKeyFactory<Control>(factory: () => new SubControl());
			AssertDefaultStyleKeyFactory<Microsoft.UI.Xaml.Controls.Primitives.ButtonBase>(factory: () => new SubButtonBase());
			AssertDefaultStyleKeyFactory<Microsoft.UI.Xaml.Controls.Primitives.RangeBase>(factory: () => new SubRangeBase());
			AssertDefaultStyleKeyFactory<NavigationViewItem>(isString: true, factory: () => new SubNavigationViewItem());

			void AssertDefaultStyleKey<TControl>(bool isString = false) where TControl : Control, new()
			{
				var t = new TControl();
				var key = keyProp.GetValue(t);
				object expected = typeof(TControl);
#if NETFX_CORE // On Windows a few controls use a string instead of the Type type as the key. Uno currently doesn't replicate this.
				if (isString)
				{
					expected = expected.ToString();
				}
#endif
				Assert.AreEqual(expected, key);
			}

			void AssertDefaultStyleKeyFactory<TControl>(Func<TControl> factory, bool isString = false) where TControl : Control
			{
				var t = factory?.Invoke();
				var key = keyProp.GetValue(t);
				object expected = typeof(TControl);
#if NETFX_CORE
				if (isString)
				{
					expected = expected.ToString();
				}
#endif
				Assert.AreEqual(expected, key);
			}
		}

		public partial class SubControl : Control { }
		public partial class SubButtonBase : Microsoft.UI.Xaml.Controls.Primitives.ButtonBase { }
		public partial class SubRangeBase : Microsoft.UI.Xaml.Controls.Primitives.RangeBase { }
		public partial class SubNavigationViewItem : NavigationViewItem { }

		#region Generic.xaml Tests (Issue #4424)

		[TestMethod]
		public void When_Custom_Control_Style_In_Generic_Xaml()
		{
			// Test that a custom control defined in the app assembly gets its default style from Themes/Generic.xaml
			var app = UnitTestsApp.App.EnsureApplication();

			var control = new GenericXamlTestControl();
			Assert.AreEqual("NotApplied", control.TestTag); // Before being added to visual tree

			app.HostView.Children.Add(control);

			Assert.AreEqual("FromGenericXaml", control.TestTag);
			Assert.AreEqual("FromGenericXaml", control.TestTag2);
		}

		[TestMethod]
		public void When_Style_In_MergedDictionary_Of_Generic_Xaml()
		{
			// Test that styles in MergedDictionaries of Generic.xaml are also found
			var app = UnitTestsApp.App.EnsureApplication();

			var control = new GenericXamlMergedControl();
			Assert.AreEqual("NotApplied", control.TestTag); // Before being added to visual tree

			app.HostView.Children.Add(control);

			Assert.AreEqual("FromMergedDictionary", control.TestTag);
		}

		[TestMethod]
		public void When_External_Library_Generic_Xaml_Still_Works_After_App_Generic_Xaml()
		{
			// Regression test: ensure external library Generic.xaml still works with the new app Generic.xaml support
			var app = UnitTestsApp.App.EnsureApplication();

			var extControl = new MyExtControl();

			app.HostView.Children.Add(extControl);

			Assert.AreEqual("FromDefaultStyle", extControl.MyTag);
		}

		[TestMethod]
		public void When_Explicit_Style_Replaces_Implicit_Merges_Default()
		{
			// Test that explicit style replaces implicit but merges with default style from Generic.xaml
			var app = UnitTestsApp.App.EnsureApplication();

			var control = new GenericXamlTestControl();
			// Set an explicit style that only sets TestTag2
			control.Style = new Style(typeof(GenericXamlTestControl))
			{
				Setters = { new Setter(GenericXamlTestControl.TestTag2Property, "FromExplicitStyle") }
			};

			app.HostView.Children.Add(control);

			// TestTag should come from default style (Generic.xaml) since explicit style doesn't set it
			Assert.AreEqual("FromGenericXaml", control.TestTag);
			// TestTag2 should come from explicit style
			Assert.AreEqual("FromExplicitStyle", control.TestTag2);
		}

		#endregion

	}
}
