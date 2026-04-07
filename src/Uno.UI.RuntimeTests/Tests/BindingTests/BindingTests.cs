using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using SamplesApp.UITests;
using Uno.UI.Helpers;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.Extensions;

namespace Uno.UI.RuntimeTests.Tests;

[TestClass]
[RunsOnUIThread]
public partial class BindingTests
{
	[TestMethod]
	public async Task When_Binding_Setter_Value_In_Style()
	{
		var SUT = new BindingToSetterValuePage();
		await UITestHelper.Load(SUT);

		assertBorder(SUT.borderXBind, "Hello");
		assertBorder(SUT.borderBinding, null);

		void assertBorder(Border border, string expectedSetterValue)
		{
			var styleXBind = border.Style;
			var setter = (Setter)styleXBind.Setters.Single();
			Assert.AreEqual(AutomationProperties.AutomationIdProperty, setter.Property);
			Assert.AreEqual(expectedSetterValue, setter.Value);
		}
	}

	[TestMethod]
	public async Task When_BindingShouldBeAppliedOnPropertyChangedEvent()
	{
		var SUT = new BindingShouldBeAppliedOnPropertyChangedEvent();
		await UITestHelper.Load(SUT);

		var dc = (BindingShouldBeAppliedOnPropertyChangedEventVM)SUT.DataContext;
		var converter = (BindingShouldBeAppliedOnPropertyChangedEventConverter)SUT.Resources["MyConverter"];

		Assert.AreEqual(1, converter.ConvertCount);
		Assert.AreEqual("0", SUT.myTb.Text);

		dc.Increment();

		Assert.AreEqual(2, converter.ConvertCount);
		Assert.AreEqual("1", SUT.myTb.Text);
	}

#if __SKIA__ && HAS_UNO_WINUI
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/16520")]
	public async Task When_XBind_In_Window()
	{
		if (!Uno.UI.Xaml.Controls.NativeWindowFactory.SupportsMultipleWindows)
		{
			Assert.Inconclusive("This test can only run in an environment with multiwindow support");
		}

		var activated = false;
		var SUT = new XBindInWindow();
		SUT.Activated += (s, e) => activated = true;
		SUT.Activate();

		await TestServices.WindowHelper.WaitFor(() => activated);
		try
		{
			Assert.AreEqual(0, SUT.ClickCount);
			SUT.MyButton.AutomationPeerClick();
			Assert.AreEqual(1, SUT.ClickCount);
		}
		finally
		{
			SUT.Close();
		}
	}
#endif

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_TargetNullValueThemeResource()
	{
		var SUT = new TargetNullValueThemeResource();
		await UITestHelper.Load(SUT);

		var myBtn = SUT.myBtn;
		Assert.AreEqual(Microsoft.UI.Colors.Red, ((SolidColorBrush)myBtn.Foreground).Color);

		using (ThemeHelper.UseDarkTheme())
		{
			Assert.AreEqual(Microsoft.UI.Colors.Green, ((SolidColorBrush)myBtn.Foreground).Color);
		}

		Assert.AreEqual(Microsoft.UI.Colors.Red, ((SolidColorBrush)myBtn.Foreground).Color);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_FallbackValueThemeResource_NoDataContext()
	{
		var SUT = new FallbackValueThemeResource();
		await UITestHelper.Load(SUT);

		var myBtn = SUT.myBtn;
		Assert.AreEqual(Microsoft.UI.Colors.Red, ((SolidColorBrush)myBtn.Foreground).Color);

		using (ThemeHelper.UseDarkTheme())
		{
#if WINAPPSDK
			Assert.AreEqual(Microsoft.UI.Colors.Green, ((SolidColorBrush)myBtn.Foreground).Color);
#else
			// WRONG behavior!
			Assert.AreEqual(Microsoft.UI.Colors.Red, ((SolidColorBrush)myBtn.Foreground).Color);
#endif
		}

		Assert.AreEqual(Microsoft.UI.Colors.Red, ((SolidColorBrush)myBtn.Foreground).Color);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_FallbackValueThemeResource_WithDataContext()
	{
		var SUT = new FallbackValueThemeResource();
		await UITestHelper.Load(SUT);

		var myBtn = SUT.myBtn;
		myBtn.DataContext = "Hello";
		Assert.AreEqual(Microsoft.UI.Colors.Red, ((SolidColorBrush)myBtn.Foreground).Color);

		using (ThemeHelper.UseDarkTheme())
		{
			Assert.AreEqual(Microsoft.UI.Colors.Green, ((SolidColorBrush)myBtn.Foreground).Color);
		}

		Assert.AreEqual(Microsoft.UI.Colors.Red, ((SolidColorBrush)myBtn.Foreground).Color);
	}

	[TestMethod]
	public async Task When_XBind_To_Const_Page()
	{
		var SUT = new XBindConstPage();
		await UITestHelper.Load(SUT);

		Assert.AreEqual(200, SUT.XBoundBorder.ActualWidth);
		Assert.AreEqual(200, SUT.XBoundBorder.ActualHeight);
	}

	[TestMethod]
	public async Task When_XBind_To_Const_Control_Template()
	{
		var SUT = new XBindConstControl();
		await UITestHelper.Load(SUT);

		Assert.AreEqual(200, SUT.XBoundBorder.ActualWidth);
		Assert.AreEqual(200, SUT.XBoundBorder.ActualHeight);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)] // https://github.com/unoplatform/uno/issues/22862
	public async Task When_XBind_Teardown_19641()
	{
		// This is a simplified repro of #19641.
		// XBindTeardown_Setup contains an element with x:Bind to its VM.Text
		// VM is a get-only property for: get => (Data)((Data_Wrapper)this.DataContext).Data;
		// While the setup is unloaded, update to the "Data.Text" should not trigger the xBind to update from INotifyPropertyChanged,
		// because that will cause a NullReferenceException that should not happen: ((Data_Wrapper)null).Data

		var template = XamlHelper.LoadXaml<DataTemplate>("""
			<DataTemplate xmlns:local="using:Uno.UI.RuntimeTests.Tests">
				<local:XBindTeardown_Setup />
			</DataTemplate>
		""");
		var host = new ContentControl()
		{
			ContentTemplateSelector = new LambdaDataTemplateSelector(
				x => x is XBindTeardown_Setup.XBindTeardown_Setup_Data_Wrapper ? template : null
			),
		};

		await UITestHelper.Load(host, x => x.IsLoaded);

		var wrapper = new XBindTeardown_Setup.XBindTeardown_Setup_Data_Wrapper(new() { Text = "qwe" });

		// cause a Setup instance to materialized, and connects its xbind
		host.Content = wrapper;
		await UITestHelper.WaitForIdle();

		// the Setup will now be unloaded, and its xbind should no longer trigger, until reloaded
		host.Content = null;
		await UITestHelper.WaitForIdle();

		// track VM access
		var failed = false;
		XBindTeardown_Setup.VMAccessDetected += (s, e) =>
		{
			if (s is XBindTeardown_Setup { DataContext: null })
			{
				failed = true;
			}
		};

		// update the xbind binding source, and see if the xbind updates (note: it shouldn't)
		wrapper.Data.Text = "asd";
		Assert.IsFalse(failed);
	}

	[TestMethod]
	public async Task When_XBind_Resurrection_20625()
	{
		// This is a repro of #20625.
		// x:Bind once suspended and then restored/resumed would not work.
		// This was due the inner subscription SerialDisposable was disposed on suspension,
		// and any new disposable assigned to it, on resuming, would be disposed immediately...

		var setup = new XBind_Resurrection();
		var sut = setup.TestBlock;
		var vm = setup.ViewModel;

		// load the test setup
		await UITestHelper.Load(setup, x => x.IsLoaded);

		// quick sanity check to dicsern xbind failure from not working outright or from reloading it
		vm.MyValue = 1;
		Assert.AreEqual(vm.MyValue, sut.Tag, "XBind is just not working...");

		// unload the view
		var unloaded = new TaskCompletionSource();
		setup.Unloaded += (s, e) => unloaded.TrySetResult();
		await UITestHelper.Load(new Border(), x => x.IsLoaded);
		await unloaded.Task;

		// reload the view
		await UITestHelper.Load(setup, x => x.IsLoaded);

		// check the xbind again
		vm.MyValue = 2;
		Assert.AreEqual(vm.MyValue, sut.Tag, "XBind is no longer working after unloading and reloading the root control...");
	}
}
partial class BindingTests
{
	public class LambdaDataTemplateSelector : DataTemplateSelector
	{
		private readonly Func<object, DataTemplate> _impl;

		public LambdaDataTemplateSelector(Func<object, DataTemplate> impl)
		{
			this._impl = impl;
		}

		protected override DataTemplate SelectTemplateCore(object item) => _impl(item);
		protected override DataTemplate SelectTemplateCore(object item, DependencyObject container) => _impl(item);
	}

}

// Repro tests for https://github.com/unoplatform/uno/issues/1516
[TestClass]
[RunsOnUIThread]
public class BindingTests_Issue1516
{
	private static T FindDescendant<T>(DependencyObject parent) where T : class
	{
		for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
		{
			var child = VisualTreeHelper.GetChild(parent, i);
			if (child is T t) return t;
			var result = FindDescendant<T>(child);
			if (result != null) return result;
		}
		return null;
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/1516")]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_TemplateBinding_Missing_Property_Does_Not_Break_DP_Inheritance()
	{
		// Issue: When a TemplateBinding references a missing/non-existent property,
		// the bound DependencyProperty is set to DependencyProperty.UnsetValue instead of
		// retaining its inherited/default value.
		// Expected: Invalid TemplateBinding should be silently ignored; the property should
		// retain its inherited or default value.

		var sut = (ContentControl)Microsoft.UI.Xaml.Markup.XamlReader.Load(
			@"<ContentControl xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
					xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
				<ContentControl.Template>
					<ControlTemplate TargetType='ContentControl'>
						<ScrollViewer x:Name='PART_ScrollViewer'
							HorizontalScrollBarVisibility='{TemplateBinding ARandomMissingProperty}' />
					</ControlTemplate>
				</ContentControl.Template>
			</ContentControl>");

		await UITestHelper.Load(sut);
		await UITestHelper.WaitForIdle();

		var scrollViewer = FindDescendant<ScrollViewer>(sut);
		Assert.IsNotNull(scrollViewer, "ScrollViewer should be found in the visual tree.");

		// HorizontalScrollBarVisibility should retain its default/inherited value (Disabled),
		// NOT be set to DependencyProperty.UnsetValue (which could cause exceptions or unexpected behavior).
		// The default for ScrollViewer.HorizontalScrollBarVisibility is Disabled.
		var hsv = scrollViewer.HorizontalScrollBarVisibility;
		Assert.AreNotEqual((ScrollBarVisibility)999, hsv,
			"HorizontalScrollBarVisibility should not be an invalid/unset value.");

		// The value should be a valid ScrollBarVisibility enum value
		Assert.IsTrue(
			hsv == ScrollBarVisibility.Auto ||
			hsv == ScrollBarVisibility.Disabled ||
			hsv == ScrollBarVisibility.Hidden ||
			hsv == ScrollBarVisibility.Visible,
			$"HorizontalScrollBarVisibility should be a valid enum value, but got {(int)hsv}. " +
			$"A failed TemplateBinding to a missing property is setting it to DependencyProperty.UnsetValue.");
	}
}
