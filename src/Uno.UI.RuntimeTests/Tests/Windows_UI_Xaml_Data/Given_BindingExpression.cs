using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Private.Infrastructure;
namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Data;

[TestClass]
public class Given_BindingExpression
{
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Binding_With_Converter_DataContext_Set_To_Null()
	{
		var SUT = new TextBlock();
		var converter = new BoolToVisibilityConverter();
		SUT.SetBinding(TextBlock.TextProperty, new Binding("Outer.Inner", converter));

		TestServices.WindowHelper.WindowContent = SUT;
		await TestServices.WindowHelper.WaitForIdle();

		SUT.DataContext = new TestItem();
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsFalse(converter.ReceivedNull);
		Assert.AreEqual("default", SUT.Text);

		SUT.DataContext = null;
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsFalse(converter.ReceivedNull);
		Assert.AreEqual("", SUT.Text);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Binding_With_Converter_DataContext_Property_Not_Final_Segment_Set_To_Null()
	{
		var SUT = new TextBlock();
		var converter = new BoolToVisibilityConverter();
		SUT.SetBinding(TextBlock.TextProperty, new Binding("Outer.Inner", converter));

		TestServices.WindowHelper.WindowContent = SUT;
		await TestServices.WindowHelper.WaitForIdle();

		SUT.DataContext = new TestItem();
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsFalse(converter.ReceivedNull);
		Assert.AreEqual("default", SUT.Text);

		(SUT.DataContext as TestItem).Outer = null;
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsFalse(converter.ReceivedNull);
		Assert.AreEqual("", SUT.Text);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Binding_With_Converter_DataContext_Property_Final_Segment_Set_To_Null()
	{
		var SUT = new TextBlock();
		var converter = new BoolToVisibilityConverter();
		SUT.SetBinding(TextBlock.TextProperty, new Binding("Outer.Inner", converter));

		TestServices.WindowHelper.WindowContent = SUT;
		await TestServices.WindowHelper.WaitForIdle();

		SUT.DataContext = new TestItem();
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsFalse(converter.ReceivedNull);
		Assert.AreEqual("default", SUT.Text);

		(SUT.DataContext as TestItem).Outer.Inner = null;
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(converter.ReceivedNull);
		Assert.AreEqual("", SUT.Text);
	}
}

public class BoolToVisibilityConverter : IValueConverter
{
	public bool ReceivedNull { get; private set; }
	public object Convert(object value, Type targetType, object parameter, string language)
	{
		ReceivedNull |= value is null;
		return value;
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

public class TestItem : INotifyPropertyChanged
{
	private NestedItem _outer = new NestedItem();
	public NestedItem Outer
	{
		get => _outer;
		set
		{
			_outer = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Outer)));
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;
}

public class NestedItem : INotifyPropertyChanged
{
	private string _inner = "default";
	public string Inner
	{
		get => _inner;
		set
		{
			_inner = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Inner)));
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;
}
