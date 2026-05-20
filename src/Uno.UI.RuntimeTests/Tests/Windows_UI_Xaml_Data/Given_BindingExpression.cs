using System.ComponentModel;
using System.Threading.Tasks;
using Combinatorial.MSTest;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.Disposables;
using Uno.UI.Extensions;
using Uno.UI.Helpers;
using Uno.UI.RuntimeTests.Helpers;
namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Data;

[TestClass]
public class Given_BindingExpression
{
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Binding_Should_Have_Correct_Default()
	{
		var binding = new Binding();
		Assert.AreEqual(BindingMode.OneWay, binding.Mode);
		Assert.AreEqual("", binding.ConverterLanguage);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Binding_Outer_Inner_DC_Null()
	{
		BoolToVisibilityConverter.ReceivedNull = false;
		using var _ = Disposable.Create(() =>
		{
			BoolToVisibilityConverter.ReceivedNull = false;
			BoolToVisibilityConverter.CanReturnNull = false;
		});

		var root = new BindingExpression_With_Converter();

		TestServices.WindowHelper.WindowContent = root;
		await TestServices.WindowHelper.WaitForIdle();

		var SUT = (MyElement)root.FindName("eBinding1");

		SUT.DataContext = new TestItem();
		await TestServices.WindowHelper.WaitForIdle();
		await Task.Delay(100); // WaitForIdle is not enough on WinUI

		Assert.IsFalse(BoolToVisibilityConverter.ReceivedNull);
		Assert.AreEqual("default", SUT.Prop);

		SUT.DataContext = null;
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsFalse(BoolToVisibilityConverter.ReceivedNull);
		Assert.AreEqual("fallback", SUT.Prop);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Binding_Outer_Inner_DC_Not_Null_Outer_Null()
	{
		BoolToVisibilityConverter.ReceivedNull = false;
		using var _ = Disposable.Create(() =>
		{
			BoolToVisibilityConverter.ReceivedNull = false;
			BoolToVisibilityConverter.CanReturnNull = false;
		});

		var root = new BindingExpression_With_Converter();

		TestServices.WindowHelper.WindowContent = root;
		await TestServices.WindowHelper.WaitForIdle();

		var SUT = (MyElement)root.FindName("eBinding1");

		SUT.DataContext = new TestItem();
		await TestServices.WindowHelper.WaitForIdle();
		await Task.Delay(100); // WaitForIdle is not enough on WinUI

		Assert.IsFalse(BoolToVisibilityConverter.ReceivedNull);
		Assert.AreEqual("default", SUT.Prop);

		(SUT.DataContext as TestItem).Outer = null;
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsFalse(BoolToVisibilityConverter.ReceivedNull);
		Assert.AreEqual("fallback", SUT.Prop);
	}

	[TestMethod]
	[RunsOnUIThread]
	[CombinatorialData]
	public async Task When_Binding_Outer_Inner_DC_Not_Null_Outer_Not_Null_Inner_Null(bool canReturnNull)
	{
		BoolToVisibilityConverter.ReceivedNull = false;
		BoolToVisibilityConverter.CanReturnNull = canReturnNull;
		using var _ = Disposable.Create(() =>
		{
			BoolToVisibilityConverter.ReceivedNull = false;
			BoolToVisibilityConverter.CanReturnNull = false;
		});

		var root = new BindingExpression_With_Converter();

		TestServices.WindowHelper.WindowContent = root;
		await TestServices.WindowHelper.WaitForIdle();

		var SUT = (MyElement)root.FindName("eBinding1");

		SUT.DataContext = new TestItem();
		await TestServices.WindowHelper.WaitForIdle();
		await Task.Delay(100); // WaitForIdle is not enough on WinUI

		Assert.IsFalse(BoolToVisibilityConverter.ReceivedNull);
		Assert.AreEqual("default", SUT.Prop);

		(SUT.DataContext as TestItem).Outer.Inner = null;
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(BoolToVisibilityConverter.ReceivedNull);
		Assert.AreEqual(canReturnNull ? "targetnullvalue" : "convertervalue", SUT.Prop);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Binding_Outer_DC_Null()
	{
		BoolToVisibilityConverter.ReceivedNull = false;
		using var _ = Disposable.Create(() =>
		{
			BoolToVisibilityConverter.ReceivedNull = false;
			BoolToVisibilityConverter.CanReturnNull = false;
		});

		var root = new BindingExpression_With_Converter();

		TestServices.WindowHelper.WindowContent = root;
		await TestServices.WindowHelper.WaitForIdle();

		var SUT = (MyElement)root.FindName("eBinding2");

		SUT.DataContext = new TestItem();
		await TestServices.WindowHelper.WaitForIdle();
		await Task.Delay(100); // WaitForIdle is not enough on WinUI

		Assert.IsFalse(BoolToVisibilityConverter.ReceivedNull);
		Assert.AreEqual("Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Data.NestedItem", SUT.Prop);

		SUT.DataContext = null;
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsFalse(BoolToVisibilityConverter.ReceivedNull);
		Assert.AreEqual("fallback", SUT.Prop);
	}

	[TestMethod]
	[RunsOnUIThread]
	[CombinatorialData]
	public async Task When_Binding_Outer_DC_Not_Null_Outer_Null(bool canReturnNull)
	{
		BoolToVisibilityConverter.ReceivedNull = false;
		BoolToVisibilityConverter.CanReturnNull = canReturnNull;
		using var _ = Disposable.Create(() =>
		{
			BoolToVisibilityConverter.ReceivedNull = false;
			BoolToVisibilityConverter.CanReturnNull = false;
		});

		var root = new BindingExpression_With_Converter();

		TestServices.WindowHelper.WindowContent = root;
		await TestServices.WindowHelper.WaitForIdle();

		var SUT = (MyElement)root.FindName("eBinding2");

		SUT.DataContext = new TestItem();
		await TestServices.WindowHelper.WaitForIdle();
		await Task.Delay(100); // WaitForIdle is not enough on WinUI

		Assert.IsFalse(BoolToVisibilityConverter.ReceivedNull);
		Assert.AreEqual("Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Data.NestedItem", SUT.Prop);

		(SUT.DataContext as TestItem).Outer = null;
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(BoolToVisibilityConverter.ReceivedNull);
		Assert.AreEqual(canReturnNull ? "targetnullvalue" : "convertervalue", SUT.Prop);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Binding_EmptyPath_DC_Null()
	{
		BoolToVisibilityConverter.ReceivedNull = false;
		using var _ = Disposable.Create(() =>
		{
			BoolToVisibilityConverter.ReceivedNull = false;
			BoolToVisibilityConverter.CanReturnNull = false;
		});

		var root = new BindingExpression_With_Converter();

		TestServices.WindowHelper.WindowContent = root;
		await TestServices.WindowHelper.WaitForIdle();

		var SUT = (MyElement)root.FindName("eBinding3");

		SUT.DataContext = new TestItem();
		await TestServices.WindowHelper.WaitForIdle();
		await Task.Delay(100); // WaitForIdle is not enough on WinUI

		Assert.IsFalse(BoolToVisibilityConverter.ReceivedNull);
		Assert.AreEqual("Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Data.TestItem", SUT.Prop);

		SUT.DataContext = null;
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsFalse(BoolToVisibilityConverter.ReceivedNull);
		Assert.AreEqual("fallback", SUT.Prop);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_xBindNoDataTemplate_Outer_Inner_DC_Null()
	{
		BoolToVisibilityConverter.ReceivedNull = false;
		using var _ = Disposable.Create(() =>
		{
			BoolToVisibilityConverter.ReceivedNull = false;
			BoolToVisibilityConverter.CanReturnNull = false;
		});

		var root = new BindingExpression_With_Converter();

		TestServices.WindowHelper.WindowContent = root;
		await TestServices.WindowHelper.WaitForIdle();

		var SUT = (MyElement)root.FindName("eXBindNoTemplate1");

		root.Item1 = new TestItem();
		await TestServices.WindowHelper.WaitForIdle();
		await Task.Delay(100); // WaitForIdle is not enough on WinUI

		Assert.IsFalse(BoolToVisibilityConverter.ReceivedNull);
		Assert.AreEqual("default", SUT.Prop);

		root.Item1 = null;
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsFalse(BoolToVisibilityConverter.ReceivedNull);
		Assert.AreEqual("fallback", SUT.Prop);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_xBindNoDataTemplate_Outer_Inner_DC_Not_Null_Outer_Null()
	{
		BoolToVisibilityConverter.ReceivedNull = false;
		using var _ = Disposable.Create(() =>
		{
			BoolToVisibilityConverter.ReceivedNull = false;
			BoolToVisibilityConverter.CanReturnNull = false;
		});

		var root = new BindingExpression_With_Converter();

		TestServices.WindowHelper.WindowContent = root;
		await TestServices.WindowHelper.WaitForIdle();

		var SUT = (MyElement)root.FindName("eXBindNoTemplate1");

		root.Item1 = new TestItem();
		await TestServices.WindowHelper.WaitForIdle();
		await Task.Delay(100); // WaitForIdle is not enough on WinUI

		Assert.IsFalse(BoolToVisibilityConverter.ReceivedNull);
		Assert.AreEqual("default", SUT.Prop);

		root.Item1.Outer = null;
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsFalse(BoolToVisibilityConverter.ReceivedNull);
		Assert.AreEqual("fallback", SUT.Prop);
	}

	[TestMethod]
	[RunsOnUIThread]
	[CombinatorialData]
	public async Task When_xBindNoDataTemplate_Outer_Inner_DC_Not_Null_Outer_Not_Null_Inner_Null(bool canReturnNull)
	{
		BoolToVisibilityConverter.ReceivedNull = false;
		BoolToVisibilityConverter.CanReturnNull = canReturnNull;
		using var _ = Disposable.Create(() =>
		{
			BoolToVisibilityConverter.ReceivedNull = false;
			BoolToVisibilityConverter.CanReturnNull = false;
		});

		var root = new BindingExpression_With_Converter();

		TestServices.WindowHelper.WindowContent = root;
		await TestServices.WindowHelper.WaitForIdle();

		var SUT = (MyElement)root.FindName("eXBindNoTemplate1");

		root.Item1 = new TestItem();
		await TestServices.WindowHelper.WaitForIdle();
		await Task.Delay(100); // WaitForIdle is not enough on WinUI

		Assert.IsFalse(BoolToVisibilityConverter.ReceivedNull);
		Assert.AreEqual("default", SUT.Prop);

		root.Item1.Outer.Inner = null;
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(BoolToVisibilityConverter.ReceivedNull);
		Assert.AreEqual(canReturnNull ? "targetnullvalue" : "convertervalue", SUT.Prop);
	}

	[TestMethod]
	[RunsOnUIThread]
	[CombinatorialData]
	public async Task When_xBindNoDataTemplate_Outer_DC_Not_Null_Outer_Null(bool canReturnNull)
	{
		BoolToVisibilityConverter.ReceivedNull = false;
		BoolToVisibilityConverter.CanReturnNull = canReturnNull;
		using var _ = Disposable.Create(() =>
		{
			BoolToVisibilityConverter.ReceivedNull = false;
			BoolToVisibilityConverter.CanReturnNull = false;
		});

		var root = new BindingExpression_With_Converter();

		TestServices.WindowHelper.WindowContent = root;
		await TestServices.WindowHelper.WaitForIdle();

		var SUT = (MyElement)root.FindName("eXBindNoTemplate2");

		root.Item2 = new TestItem();
		await TestServices.WindowHelper.WaitForIdle();
		await Task.Delay(100); // WaitForIdle is not enough on WinUI

		Assert.IsFalse(BoolToVisibilityConverter.ReceivedNull);
		Assert.AreEqual("Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Data.NestedItem", SUT.Prop);

		root.Item2.Outer = null;
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(BoolToVisibilityConverter.ReceivedNull);
		Assert.AreEqual(canReturnNull ? "targetnullvalue" : "convertervalue", SUT.Prop);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_xBindNoDataTemplate_Outer_DC_Null()
	{
		BoolToVisibilityConverter.ReceivedNull = false;
		using var _ = Disposable.Create(() =>
		{
			BoolToVisibilityConverter.ReceivedNull = false;
			BoolToVisibilityConverter.CanReturnNull = false;
		});

		var root = new BindingExpression_With_Converter();

		TestServices.WindowHelper.WindowContent = root;
		await TestServices.WindowHelper.WaitForIdle();

		var SUT = (MyElement)root.FindName("eXBindNoTemplate2");

		root.Item2 = new TestItem();
		await TestServices.WindowHelper.WaitForIdle();
		await Task.Delay(100); // WaitForIdle is not enough on WinUI

		Assert.IsFalse(BoolToVisibilityConverter.ReceivedNull);
		Assert.AreEqual("Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Data.NestedItem", SUT.Prop);

		root.Item2 = null;
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsFalse(BoolToVisibilityConverter.ReceivedNull);
		Assert.AreEqual("fallback", SUT.Prop);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_xBindInDataTemplate_Outer_Inner_DC_Null()
	{
		BoolToVisibilityConverter.ReceivedNull = false;
		using var _ = Disposable.Create(() =>
		{
			BoolToVisibilityConverter.ReceivedNull = false;
			BoolToVisibilityConverter.CanReturnNull = false;
		});

		var root = new BindingExpression_With_Converter();

		TestServices.WindowHelper.WindowContent = root;
		await TestServices.WindowHelper.WaitForIdle();

		var cp = (ContentPresenter)root.FindName("cp1");

		cp.Content = new TestItem();
		await TestServices.WindowHelper.WaitForIdle();
		await Task.Delay(100); // WaitForIdle is not enough on WinUI

		var SUT = VisualTreeHelper.GetChild(cp, 0) as MyElement;

		Assert.IsFalse(BoolToVisibilityConverter.ReceivedNull);
		Assert.AreEqual("default", SUT.Prop);

		cp.Content = null;
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsFalse(BoolToVisibilityConverter.ReceivedNull);
		Assert.AreEqual("default", SUT.Prop);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_xBindInDataTemplate_Outer_DC_Null()
	{
		BoolToVisibilityConverter.ReceivedNull = false;
		using var _ = Disposable.Create(() =>
		{
			BoolToVisibilityConverter.ReceivedNull = false;
			BoolToVisibilityConverter.CanReturnNull = false;
		});

		var root = new BindingExpression_With_Converter();

		TestServices.WindowHelper.WindowContent = root;
		await TestServices.WindowHelper.WaitForIdle();

		var cp = (ContentPresenter)root.FindName("cp2");

		cp.Content = new TestItem();
		await TestServices.WindowHelper.WaitForIdle();
		await Task.Delay(100); // WaitForIdle is not enough on WinUI

		var SUT = VisualTreeHelper.GetChild(cp, 0) as MyElement;

		Assert.IsFalse(BoolToVisibilityConverter.ReceivedNull);
		Assert.AreEqual("Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Data.NestedItem", SUT.Prop);

		cp.Content = null;
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsFalse(BoolToVisibilityConverter.ReceivedNull);
		Assert.AreEqual("Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Data.NestedItem", SUT.Prop);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_xBindInDataTemplate_EmptyPath_DC_Null()
	{
		BoolToVisibilityConverter.ReceivedNull = false;
		using var _ = Disposable.Create(() =>
		{
			BoolToVisibilityConverter.ReceivedNull = false;
			BoolToVisibilityConverter.CanReturnNull = false;
		});

		var root = new BindingExpression_With_Converter();

		TestServices.WindowHelper.WindowContent = root;
		await TestServices.WindowHelper.WaitForIdle();

		var cp = (ContentPresenter)root.FindName("cp3");

		cp.Content = new TestItem();
		await TestServices.WindowHelper.WaitForIdle();
		await Task.Delay(100); // WaitForIdle is not enough on WinUI

		var SUT = VisualTreeHelper.GetChild(cp, 0) as MyElement;

		Assert.IsFalse(BoolToVisibilityConverter.ReceivedNull);
		Assert.AreEqual("Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Data.TestItem", SUT.Prop);

		cp.Content = null;
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsFalse(BoolToVisibilityConverter.ReceivedNull);
		Assert.AreEqual("Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Data.TestItem", SUT.Prop);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_xBindInDataTemplate_Outer_Inner_DC_Not_Null_Outer_Null()
	{
		BoolToVisibilityConverter.ReceivedNull = false;
		using var _ = Disposable.Create(() =>
		{
			BoolToVisibilityConverter.ReceivedNull = false;
			BoolToVisibilityConverter.CanReturnNull = false;
		});

		var root = new BindingExpression_With_Converter();

		TestServices.WindowHelper.WindowContent = root;
		await TestServices.WindowHelper.WaitForIdle();

		var cp = (ContentPresenter)root.FindName("cp1");

		cp.Content = new TestItem();
		await TestServices.WindowHelper.WaitForIdle();
		await Task.Delay(100); // WaitForIdle is not enough on WinUI

		var SUT = VisualTreeHelper.GetChild(cp, 0) as MyElement;

		Assert.IsFalse(BoolToVisibilityConverter.ReceivedNull);
		Assert.AreEqual("default", SUT.Prop);

		(cp.Content as TestItem).Outer = null;
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsFalse(BoolToVisibilityConverter.ReceivedNull);
		Assert.AreEqual("fallback", SUT.Prop);
	}

	[TestMethod]
	[RunsOnUIThread]
	[CombinatorialData]
	public async Task When_xBindInDataTemplate_Outer_Inner_DC_Not_Null_Outer_Not_Null_Inner_Null(bool canReturnNull)
	{
		BoolToVisibilityConverter.ReceivedNull = false;
		BoolToVisibilityConverter.CanReturnNull = canReturnNull;

		using var _ = Disposable.Create(() =>
		{
			BoolToVisibilityConverter.ReceivedNull = false;
			BoolToVisibilityConverter.CanReturnNull = false;
		});

		var root = new BindingExpression_With_Converter();

		TestServices.WindowHelper.WindowContent = root;
		await TestServices.WindowHelper.WaitForIdle();

		var cp = (ContentPresenter)root.FindName("cp1");

		cp.Content = new TestItem();
		await TestServices.WindowHelper.WaitForIdle();
		await Task.Delay(100); // WaitForIdle is not enough on WinUI

		var SUT = VisualTreeHelper.GetChild(cp, 0) as MyElement;

		Assert.IsFalse(BoolToVisibilityConverter.ReceivedNull);
		Assert.AreEqual("default", SUT.Prop);

		(cp.Content as TestItem).Outer.Inner = null;
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(BoolToVisibilityConverter.ReceivedNull);
		Assert.AreEqual(BoolToVisibilityConverter.CanReturnNull ? "targetnullvalue" : "convertervalue", SUT.Prop);
	}

	[TestMethod]
	[RunsOnUIThread]
	[CombinatorialData]
	public async Task When_xBindInDataTemplate_Outer_DC_Not_Null_Outer_Null(bool canReturnNull)
	{
		BoolToVisibilityConverter.ReceivedNull = false;
		BoolToVisibilityConverter.CanReturnNull = canReturnNull;
		using var _ = Disposable.Create(() =>
		{
			BoolToVisibilityConverter.ReceivedNull = false;
			BoolToVisibilityConverter.CanReturnNull = false;
		});

		var root = new BindingExpression_With_Converter();

		TestServices.WindowHelper.WindowContent = root;
		await TestServices.WindowHelper.WaitForIdle();

		var cp = (ContentPresenter)root.FindName("cp2");

		cp.Content = new TestItem();
		await TestServices.WindowHelper.WaitForIdle();
		await Task.Delay(100); // WaitForIdle is not enough on WinUI

		var SUT = VisualTreeHelper.GetChild(cp, 0) as MyElement;

		Assert.IsFalse(BoolToVisibilityConverter.ReceivedNull);
		Assert.AreEqual("Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Data.NestedItem", SUT.Prop);

		(cp.Content as TestItem).Outer = null;
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(BoolToVisibilityConverter.ReceivedNull);
		Assert.AreEqual(BoolToVisibilityConverter.CanReturnNull ? "targetnullvalue" : "convertervalue", SUT.Prop);
	}

	[TestMethod]
	[RunsOnUIThread]
	[DataRow("SUT_S")]
	[DataRow("SUT_RS")]
	[DataRow("SUT_S_RS")]
	public async Task When_Binding_Sources(string sutName)
	{
		var host = new When_Binding_Sources_Setup();
		//	<Button x:Name="HostButton" Content="ButtonContent" Tag="ButtonTag">
		//		<Button.Resources>
		//			<x:String x:Key="LocalResStringA">LocalResStringA</x:String>
		//		</Button.Resources>
		//		<Button.Template>
		//			<ControlTemplate TargetType="Button">
		//				<StackPanel>
		//					<TextBlock x:Name="SUT_S" Tag="{Binding Source={StaticResource LocalResStringA}}" Text="string:LocalResStringA" />
		//					<TextBlock x:Name="SUT_RS" Tag="{Binding RelativeSource={RelativeSource Mode=TemplatedParent}}" Text="Button#HostButton" />
		//					<TextBlock x:Name="SUT_S_RS" Tag="{Binding Source={StaticResource LocalResStringA}, RelativeSource={RelativeSource Mode=TemplatedParent}}" Text="string:LocalResStringA" />
		//				</StackPanel>
		//			</ControlTemplate>
		//		</Button.Template>
		//	</Button>
		await UITestHelper.Load(host);

		var sut = host.FindFirstDescendantOrThrow<TextBlock>(sutName);
		if (sut.Text == "string:LocalResStringA")
		{
			Assert.IsInstanceOfType<string>(sut.Tag, $"[{sut.Name}]Expecting binding result to be 'LocalResStringA'");
			Assert.AreEqual("LocalResStringA", sut.Tag as string, $"[{sut.Name}]Expecting binding result to be 'LocalResStringA'");
		}
		else if (sut.Text == "Button#HostButton")
		{
			Assert.AreEqual(host, sut.Tag, $"[{sut.Name}]Expecting the binding result to be the templated-parent(Button#HostButton)");
		}
		else
		{
			Assert.Fail($"Invalid expectation: [{sut.Name}]{sut.Text}");
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/7174")]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/21402")]
	public async Task When_XBind_TwoWay_Enum_Converter_ConvertBack_Receives_Correct_TargetType()
	{
		var root = new When_XBind_TwoWay_Enum_Converter();
		await UITestHelper.Load(root);

		// Verify initial forward conversion: enum -> string
		Assert.AreEqual("Hello", root.tbEnum.Text, "Forward conversion should produce enum name string");

		// x:Bind TwoWay on TextBox.Text commits the source on LostFocus, and compiled x:Bind
		// bindings are not exposed via GetBindingExpression on WinUI (it returns null there).
		// Drive ConvertBack through a real focus change instead, which behaves identically on
		// Uno and native WinUI (same pattern as Given_TextBox.When_TwoWay_Text_Binding).
		root.tbEnum.Focus(FocusState.Programmatic);
		root.tbEnum.Text = "World";
		root.dummyButton.Focus(FocusState.Programmatic);
		await TestServices.WindowHelper.WaitForIdle();

		// ConvertBack must receive the enum source type as targetType (issues #7174 / #21402).
		// On WinUI the x:Bind compiler passes PathStep.ValueType; on Uno the fix passes XBindSourceType.
		Assert.AreEqual(typeof(XBindTestEnum), root.Converter.LastConvertBackTargetType,
			"ConvertBack should receive the enum type as targetType");
		Assert.AreEqual(XBindTestEnum.World, root.EnumValue,
			"Backward conversion should correctly parse the string to the enum value");
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/7174")]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/21402")]
	public async Task When_XamlReader_Binding_TwoWay_Enum_Converter_RoundTrips()
	{
		// Regression guard for the {Binding} workaround documented in #21402: XamlReader-loaded
		// XAML with a TwoWay {Binding} + Converter on an enum source must round-trip correctly.
		// ConvertBack's targetType differs by platform here - native WinUI's classic {Binding} is
		// late-bound and passes Object, while Uno passes the reflected enum type - so this test
		// asserts the functional outcome (the workaround's guarantee), not the targetType.
		var xaml = """
			<Page xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
				  xmlns:local="using:Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Data">
				<Page.Resources>
					<local:XBindEnumToStringConverter x:Key="EnumConverter" />
				</Page.Resources>
				<TextBox x:Name="tbEnum"
						 Text="{Binding EnumValue, Mode=TwoWay, Converter={StaticResource EnumConverter}}" />
			</Page>
			""";

		var page = (Page)XamlReader.Load(xaml);
		var vm = new XamlReaderEnumBindingTestViewModel();
		page.DataContext = vm;

		TestServices.WindowHelper.WindowContent = page;
		await TestServices.WindowHelper.WaitForIdle();

		var tbEnum = (TextBox)page.FindName("tbEnum");

		Assert.AreEqual("Hello", tbEnum.Text, "Forward conversion should produce enum name string");

		// Classic {Binding} exposes a BindingExpression on both Uno and WinUI, so push the new
		// value through ConvertBack deterministically via UpdateSource.
		tbEnum.Text = "World";
		await TestServices.WindowHelper.WaitForIdle();
		tbEnum.GetBindingExpression(TextBox.TextProperty).UpdateSource();
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(XBindTestEnum.World, vm.EnumValue,
			"Backward conversion should correctly parse the string to the enum value");
	}
}

// Plain INotifyPropertyChanged view-model, representative of the {Binding} workaround for the
// x:Bind enum issues. Note the platform difference exercised by the test below: native WinUI's
// classic {Binding} is late-bound and reports ConvertBack's targetType as Object, whereas Uno
// resolves the reflected enum type. The converter copes with both, so the test asserts the
// functional round-trip (the workaround's actual guarantee).
public sealed class XamlReaderEnumBindingTestViewModel : INotifyPropertyChanged
{
	private XBindTestEnum _enumValue = XBindTestEnum.Hello;

	public XBindTestEnum EnumValue
	{
		get => _enumValue;
		set
		{
			if (_enumValue != value)
			{
				_enumValue = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EnumValue)));
			}
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;
}
