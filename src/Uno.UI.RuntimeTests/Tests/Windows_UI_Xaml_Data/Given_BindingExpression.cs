using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.Disposables;
namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Data;

[TestClass]
public class Given_BindingExpression
{
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
	[DataRow(true)]
	[DataRow(false)]
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
	[DataRow(true)]
	[DataRow(false)]
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
	[DataRow(true)]
	[DataRow(false)]
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
	[DataRow(true)]
	[DataRow(false)]
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
	[DataRow(true)]
	[DataRow(false)]
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
	[DataRow(true)]
	[DataRow(false)]
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
}
