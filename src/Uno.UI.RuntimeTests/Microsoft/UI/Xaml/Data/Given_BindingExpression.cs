using System.Threading.Tasks;
using Combinatorial.MSTest;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using SamplesApp.UITests;
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
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/19420")]
	public async Task When_ElementName_Source_Added_In_Code_Before_Load()
	{
		var sut = new ElementName_CodeAddedSource(addSourceInLoaded: false);
		await UITestHelper.Load(sut, x => x.IsLoaded);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual("Hello, World!", sut.Bound.Text);
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/19420")]
	public async Task When_ElementName_Source_Added_In_Code_After_Load()
	{
		var sut = new ElementName_CodeAddedSource(addSourceInLoaded: true);
		await UITestHelper.Load(sut, x => x.IsLoaded);
		await TestServices.WindowHelper.WaitForIdle();

		// The name is looked up once, when the target loads, so a source added afterwards is not picked up (as on WinUI).
		Assert.AreEqual(2, sut.ParentSP.Children.Count);
		Assert.AreEqual("", sut.Bound.Text);
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/19420")]
	public async Task When_ElementName_Binding_Created_In_Code_Before_Load()
	{
		var target = new TextBlock();
		target.SetBinding(TextBlock.TextProperty, new Binding { ElementName = "CodeBindingSourceBeforeLoad", Path = new PropertyPath("Text") });
		var root = new StackPanel { Children = { new TextBlock { Name = "CodeBindingSourceBeforeLoad", Text = "source" }, target } };

		await UITestHelper.Load(root, x => x.IsLoaded);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual("source", target.Text);
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/19420")]
	public async Task When_ElementName_Binding_Created_In_Code_On_Live_Target()
	{
		var target = new TextBlock();
		var root = new StackPanel { Children = { new TextBlock { Name = "CodeBindingSourceLive", Text = "source" }, target } };
		await UITestHelper.Load(root, x => x.IsLoaded);

		target.SetBinding(TextBlock.TextProperty, new Binding { ElementName = "CodeBindingSourceLive", Path = new PropertyPath("Text") });
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual("source", target.Text);
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/19420")]
	public async Task When_ElementName_Binding_Created_In_Code_Source_Added_After_Load()
	{
		var target = new TextBlock();
		target.SetBinding(TextBlock.TextProperty, new Binding { ElementName = "CodeBindingSourceAfterLoad", Path = new PropertyPath("Text") });
		var root = new StackPanel { Children = { target } };
		await UITestHelper.Load(root, x => x.IsLoaded);

		root.Children.Add(new TextBlock { Name = "CodeBindingSourceAfterLoad", Text = "source" });
		await TestServices.WindowHelper.WaitForIdle();

		// Same one-shot lookup as for a XAML binding: the source wasn't there when the target loaded (as on WinUI).
		Assert.AreEqual("", target.Text);
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/19420")]
	public async Task When_ElementName_Source_Removed_And_Name_Reused()
	{
		var first = new TextBlock { Name = "ReusedCodeName", Text = "first" };
		var root = new StackPanel { Children = { first } };
		await UITestHelper.Load(root, x => x.IsLoaded);

		root.Children.Remove(first);
		root.Children.Add(new TextBlock { Name = "ReusedCodeName", Text = "second" });

		var target = new TextBlock();
		target.SetBinding(TextBlock.TextProperty, new Binding { ElementName = "ReusedCodeName", Path = new PropertyPath("Text") });
		root.Children.Add(target);
		await TestServices.WindowHelper.WaitFor(() => target.IsLoaded);

		Assert.AreEqual("second", target.Text);
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/19420")]
	public async Task When_ElementName_Code_Name_Duplicated_Last_Wins()
	{
		var first = new TextBlock { Name = "DuplicatedCodeName", Text = "first" };
		var second = new TextBlock { Name = "DuplicatedCodeName", Text = "second" };
		var root = new StackPanel { Children = { first } };
		await UITestHelper.Load(root, x => x.IsLoaded);
		root.Children.Add(second);

		var whileBothLive = await AddElementNameTarget(root, "DuplicatedCodeName");
		root.Children.Remove(first);
		var afterFirstRemoved = await AddElementNameTarget(root, "DuplicatedCodeName");

		Assert.AreEqual("second", whileBothLive.Text);
		Assert.AreEqual("second", afterFirstRemoved.Text);
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/19420")]
	public async Task When_ElementName_Code_Name_Duplicated_Later_Removed()
	{
		var first = new TextBlock { Name = "DuplicatedRemovedCodeName", Text = "first" };
		var second = new TextBlock { Name = "DuplicatedRemovedCodeName", Text = "second" };
		var root = new StackPanel { Children = { first } };
		await UITestHelper.Load(root, x => x.IsLoaded);
		root.Children.Add(second);

		root.Children.Remove(second);
		var target = await AddElementNameTarget(root, "DuplicatedRemovedCodeName");

		// Leaving clears the entry the later element overwrote, so the earlier one isn't found anymore (as on WinUI).
		Assert.AreEqual("", target.Text);
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/19420")]
	public async Task When_ElementName_Code_Name_Collides_With_Xaml_Name()
	{
		var sut = new ElementName_CodeNameCollision();
		await UITestHelper.Load(sut, x => x.IsLoaded);
		sut.Panel.Children.Add(new TextBlock { Name = "CollidingName", Text = "code" });

		var target = await AddElementNameTarget(sut.Panel, "CollidingName");

		Assert.AreEqual("code", target.Text);
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/19420")]
	public async Task When_ElementName_Code_Name_Is_ContentControl_Content()
	{
		var content = new TextBlock { Name = "ContentControlCodeName", Text = "content" };
		var root = new StackPanel { Children = { new ContentControl { Content = content } } };
		await UITestHelper.Load(root, x => x.IsLoaded);
		await TestServices.WindowHelper.WaitFor(() => content.IsLoaded);

		var target = await AddElementNameTarget(root, "ContentControlCodeName");

		Assert.AreEqual("content", target.Text);
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/19420")]
	public async Task When_ElementName_Code_Name_Is_ScrollViewer_Content()
	{
		var content = new TextBlock { Name = "ScrollViewerCodeName", Text = "content" };
		var root = new StackPanel { Children = { new ScrollViewer { Content = content } } };
		await UITestHelper.Load(root, x => x.IsLoaded);
		await TestServices.WindowHelper.WaitFor(() => content.IsLoaded);

		var target = await AddElementNameTarget(root, "ScrollViewerCodeName");

		Assert.AreEqual("content", target.Text);
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/19420")]
	public async Task When_ElementName_Code_Name_Changed_While_Live()
	{
		var renamed = new TextBlock { Name = "RenamedCodeName", Text = "renamed" };
		var namedLate = new TextBlock { Text = "named late" };
		var root = new StackPanel { Children = { renamed, namedLate } };
		await UITestHelper.Load(root, x => x.IsLoaded);

		renamed.Name = "NameAfterRename";
		namedLate.Name = "NameSetWhileLive";

		var newName = await AddElementNameTarget(root, "NameAfterRename");
		var setWhileLive = await AddElementNameTarget(root, "NameSetWhileLive");

		Assert.AreEqual("renamed", newName.Text);
		Assert.AreEqual("named late", setWhileLive.Text);
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/19420")]
	public async Task When_ElementName_Targets_A_Template_Part_From_Outside()
	{
		var button = new Button { Content = "button" };
		var root = new StackPanel { Children = { button } };
		await UITestHelper.Load(root, x => x.IsLoaded);

		var target = await AddElementNameTarget(root, "ContentPresenter", "Name");

		// Template parts live in their template's namescope only.
		Assert.AreEqual("", target.Text);
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/19420")]
	public async Task When_ElementName_Targets_XamlReader_Content_From_Outside()
	{
		const string Xmlns = "xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'";
		var fragment = (FrameworkElement)XamlReader.Load($"<StackPanel {Xmlns} x:Name='ReaderRootName'><TextBlock x:Name='ReaderInnerName' Text='inner' /></StackPanel>");
		var root = new StackPanel { Children = { fragment } };
		await UITestHelper.Load(root, x => x.IsLoaded);

		var inner = await AddElementNameTarget(root, "ReaderInnerName");
		var fragmentRoot = await AddElementNameTarget(root, "ReaderRootName", "Name");

		// Loaded content is a namescope of its own, root included (as on WinUI).
		Assert.AreEqual("", inner.Text);
		Assert.AreEqual("", fragmentRoot.Text);
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/19420")]
	public async Task When_ElementName_Source_In_Popup_Target_Outside()
	{
		var root = new StackPanel();
		await UITestHelper.Load(root, x => x.IsLoaded);

		var popupPanel = new StackPanel { Children = { new TextBlock { Name = "PopupInsideSource", Text = "inside" } } };
		var popup = new Popup { Child = popupPanel, XamlRoot = root.XamlRoot, IsOpen = true };
		try
		{
			await TestServices.WindowHelper.WaitFor(() => popupPanel.IsLoaded);
			var target = await AddElementNameTarget(root, "PopupInsideSource");

			// A popup that isn't in the tree doesn't share the page's namescope (as on WinUI).
			Assert.AreEqual("", target.Text);
		}
		finally
		{
			popup.IsOpen = false;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/19420")]
	public async Task When_ElementName_Target_In_Popup_Source_Outside()
	{
		var root = new StackPanel { Children = { new TextBlock { Name = "PopupOutsideSource", Text = "outside" } } };
		await UITestHelper.Load(root, x => x.IsLoaded);

		var popupPanel = new StackPanel();
		var popup = new Popup { Child = popupPanel, XamlRoot = root.XamlRoot, IsOpen = true };
		try
		{
			var target = await AddElementNameTarget(popupPanel, "PopupOutsideSource");

			Assert.AreEqual("", target.Text);
		}
		finally
		{
			popup.IsOpen = false;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/19420")]
	public async Task When_ElementName_Source_And_Target_In_Popup()
	{
		var root = new StackPanel();
		await UITestHelper.Load(root, x => x.IsLoaded);

		var popupPanel = new StackPanel { Children = { new TextBlock { Name = "PopupSiblingSource", Text = "sibling" } } };
		var popup = new Popup { Child = popupPanel, XamlRoot = root.XamlRoot, IsOpen = true };
		try
		{
			await TestServices.WindowHelper.WaitFor(() => popupPanel.IsLoaded);
			var target = await AddElementNameTarget(popupPanel, "PopupSiblingSource");

			Assert.AreEqual("sibling", target.Text);
		}
		finally
		{
			popup.IsOpen = false;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/19420")]
	public async Task When_ElementName_Binding_Created_In_Code_With_Source()
	{
		var root = new StackPanel { Children = { new TextBlock { Name = "ShadowedBySource", Text = "element" } } };
		await UITestHelper.Load(root, x => x.IsLoaded);

		var target = new TextBlock();
		target.SetBinding(TextBlock.TextProperty, new Binding { Source = new TextBlock { Text = "source" }, ElementName = "ShadowedBySource", Path = new PropertyPath("Text") });
		root.Children.Add(target);
		await TestServices.WindowHelper.WaitFor(() => target.IsLoaded);

		// The explicit Source wins over the ElementName (as on WinUI).
		Assert.AreEqual("source", target.Text);
	}

	private static async Task<TextBlock> AddElementNameTarget(Panel panel, string elementName, string path = "Text")
	{
		var target = new TextBlock();
		target.SetBinding(TextBlock.TextProperty, new Binding { ElementName = elementName, Path = new PropertyPath(path) });
		panel.Children.Add(target);
		await TestServices.WindowHelper.WaitFor(() => target.IsLoaded);

		return target;
	}
}
