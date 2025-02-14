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
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
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

			Assert.AreEqual(Windows.UI.Colors.LightGoldenrodYellow, (control.TestCheckBox.Foreground as SolidColorBrush).Color);
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

			AssertDefaultStyleKey<Windows.UI.Xaml.Controls.ContentDialog>();
#if !NETFX_CORE
			AssertDefaultStyleKey<Windows.UI.Xaml.Controls.DatePickerSelector>();
			AssertDefaultStyleKey<Windows.UI.Xaml.Controls.TimePickerSelector>();
#endif
			AssertDefaultStyleKey<Windows.UI.Xaml.Controls.FlyoutPresenter>();
			AssertDefaultStyleKey<Windows.UI.Xaml.Controls.ContentControl>();
			AssertDefaultStyleKey<Windows.UI.Xaml.Controls.HyperlinkButton>();
			AssertDefaultStyleKey<Windows.UI.Xaml.Controls.ComboBox>();
			AssertDefaultStyleKey<Windows.UI.Xaml.Controls.ComboBoxItem>();
			AssertDefaultStyleKey<Windows.UI.Xaml.Controls.ListViewItem>();
			AssertDefaultStyleKey<Windows.UI.Xaml.Controls.GridViewItem>();
			AssertDefaultStyleKey<Windows.UI.Xaml.Controls.TimePicker>();
			AssertDefaultStyleKey<Windows.UI.Xaml.Controls.SplitView>();
			AssertDefaultStyleKey<Windows.UI.Xaml.Controls.ListView>();
			AssertDefaultStyleKey<Windows.UI.Xaml.Controls.GridView>();
			AssertDefaultStyleKey<Windows.UI.Xaml.Controls.ScrollViewer>();
			AssertDefaultStyleKey<Windows.UI.Xaml.Controls.AppBarToggleButton>();
			AssertDefaultStyleKey<Windows.UI.Xaml.Controls.AppBarSeparator>();
			AssertDefaultStyleKey<Windows.UI.Xaml.Controls.ListViewHeaderItem>();
			AssertDefaultStyleKey<Windows.UI.Xaml.Controls.GridViewHeaderItem>();
			AssertDefaultStyleKey<Windows.UI.Xaml.Controls.FlipViewItem>();
			AssertDefaultStyleKey<Windows.UI.Xaml.Controls.PivotItem>(isString: true);
#if !NETFX_CORE //Calling new PivotHeaderItem() on Windows throws an exception(!)
			AssertDefaultStyleKey<Windows.UI.Xaml.Controls.Primitives.PivotHeaderItem>();
#endif
			AssertDefaultStyleKey<Windows.UI.Xaml.Controls.AutoSuggestBox>();
#if !HAS_UNO_WINUI
			AssertDefaultStyleKey<Windows.UI.Xaml.Controls.MediaPlayerElement>();
			AssertDefaultStyleKey<Windows.UI.Xaml.Controls.MediaTransportControls>();
#endif
			AssertDefaultStyleKey<Windows.UI.Xaml.Controls.Button>();
			AssertDefaultStyleKey<Windows.UI.Xaml.Controls.CheckBox>();
			AssertDefaultStyleKey<Windows.UI.Xaml.Controls.ToggleSwitch>();
			AssertDefaultStyleKey<Windows.UI.Xaml.Controls.Slider>();
			AssertDefaultStyleKey<Windows.UI.Xaml.Controls.ProgressBar>();
			AssertDefaultStyleKey<Windows.UI.Xaml.Controls.TextBox>();
			AssertDefaultStyleKey<Windows.UI.Xaml.Controls.Pivot>(isString: true);
			AssertDefaultStyleKey<Windows.UI.Xaml.Controls.CommandBar>();
			AssertDefaultStyleKey<Windows.UI.Xaml.Controls.AppBarButton>();
			AssertDefaultStyleKey<Windows.UI.Xaml.Controls.Frame>();
			AssertDefaultStyleKey<Microsoft/* UWP don't rename */.UI.Xaml.Controls.MenuBarItem>(isString: true);
			AssertDefaultStyleKey<Windows.UI.Xaml.Controls.MenuFlyoutPresenter>();
			AssertDefaultStyleKey<Windows.UI.Xaml.Controls.MenuFlyoutItem>();
			AssertDefaultStyleKey<Windows.UI.Xaml.Controls.MenuFlyoutSubItem>();
			AssertDefaultStyleKey<Windows.UI.Xaml.Controls.NavigationView>(isString: true);
			AssertDefaultStyleKey<Windows.UI.Xaml.Controls.NavigationViewItem>(isString: true);
			AssertDefaultStyleKey<Windows.UI.Xaml.Controls.Primitives.NavigationViewItemPresenter>(isString: true);
			AssertDefaultStyleKey<Windows.UI.Xaml.Controls.NavigationViewItemHeader>(isString: true);
			AssertDefaultStyleKey<Windows.UI.Xaml.Controls.NavigationViewItemSeparator>(isString: true);

			AssertDefaultStyleKey<ItemsControl>();
			AssertDefaultStyleKey<Windows.UI.Xaml.Controls.Primitives.Thumb>();
			AssertDefaultStyleKey<Windows.UI.Xaml.Controls.Primitives.ToggleButton>();
			AssertDefaultStyleKey<RadioButton>();

			AssertDefaultStyleKeyFactory<Control>(factory: () => new SubControl());
			AssertDefaultStyleKeyFactory<Windows.UI.Xaml.Controls.Primitives.ButtonBase>(factory: () => new SubButtonBase());
			AssertDefaultStyleKeyFactory<Windows.UI.Xaml.Controls.Primitives.RangeBase>(factory: () => new SubRangeBase());
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
		public partial class SubButtonBase : Windows.UI.Xaml.Controls.Primitives.ButtonBase { }
		public partial class SubRangeBase : Windows.UI.Xaml.Controls.Primitives.RangeBase { }
		public partial class SubNavigationViewItem : NavigationViewItem { }

	}
}
