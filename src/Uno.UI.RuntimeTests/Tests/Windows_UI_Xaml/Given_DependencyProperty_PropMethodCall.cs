using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

[TestClass]
[RunsOnUIThread]
public class Given_DependencyProperty_PropMethodCall
{
	[TestMethod]
	public void When_SetValue_GetValue_Roundtrip()
	{
		var element = new Border();
		Assert.IsTrue(element.IsHitTestVisible); // default is true

		element.IsHitTestVisible = false;
		Assert.IsFalse(element.IsHitTestVisible);
		Assert.IsFalse((bool)element.GetValue(UIElement.IsHitTestVisibleProperty));

		element.IsHitTestVisible = true;
		Assert.IsTrue(element.IsHitTestVisible);
		Assert.IsTrue((bool)element.GetValue(UIElement.IsHitTestVisibleProperty));
	}

	[TestMethod]
	public void When_ClearValue_Restores_Default()
	{
		var element = new Border();
		element.IsHitTestVisible = false;
		Assert.IsFalse(element.IsHitTestVisible);

		element.ClearValue(UIElement.IsHitTestVisibleProperty);
		Assert.IsTrue(element.IsHitTestVisible); // default is true
	}

	[TestMethod]
	public void When_Style_Fallback()
	{
		var style = new Style(typeof(Border));
		style.Setters.Add(new Setter(UIElement.IsHitTestVisibleProperty, false));

		var element = new Border();
		element.Style = style;
		Assert.IsFalse(element.IsHitTestVisible); // style value

		// Local overrides style
		element.IsHitTestVisible = true;
		Assert.IsTrue(element.IsHitTestVisible);

		// Clearing local restores style value
		element.ClearValue(UIElement.IsHitTestVisibleProperty);
		Assert.IsFalse(element.IsHitTestVisible); // style value restored
	}

	[TestMethod]
	public void When_PropertyChanged_Callback_Fires()
	{
		var element = new Border();
		object? oldVal = null;
		object? newVal = null;
		int callCount = 0;

		var token = element.RegisterPropertyChangedCallback(UIElement.IsHitTestVisibleProperty, (sender, dp) =>
		{
			callCount++;
			oldVal = null; // RegisterPropertyChangedCallback doesn't provide old/new values directly
			newVal = ((UIElement)sender).IsHitTestVisible;
		});

		element.IsHitTestVisible = false;
		Assert.AreEqual(1, callCount);
		Assert.AreEqual(false, newVal);

		element.IsHitTestVisible = true;
		Assert.AreEqual(2, callCount);
		Assert.AreEqual(true, newVal);

		element.UnregisterPropertyChangedCallback(UIElement.IsHitTestVisibleProperty, token);

		element.IsHitTestVisible = false;
		Assert.AreEqual(2, callCount); // no more callbacks
	}

	[TestMethod]
	public void When_ReadLocalValue()
	{
		var element = new Border();
		Assert.AreEqual(DependencyProperty.UnsetValue, element.ReadLocalValue(UIElement.IsHitTestVisibleProperty));

		element.IsHitTestVisible = false;
		Assert.AreEqual(false, element.ReadLocalValue(UIElement.IsHitTestVisibleProperty));

		element.ClearValue(UIElement.IsHitTestVisibleProperty);
		Assert.AreEqual(DependencyProperty.UnsetValue, element.ReadLocalValue(UIElement.IsHitTestVisibleProperty));
	}

	[TestMethod]
	public void When_Binding_Propagates_Value()
	{
		var source = new BindingSource { Value = false };
		var element = new Border();

		element.SetBinding(UIElement.IsHitTestVisibleProperty, new Binding
		{
			Source = source,
			Path = new PropertyPath(nameof(BindingSource.Value)),
			Mode = BindingMode.OneWay,
		});

		Assert.IsFalse(element.IsHitTestVisible);

		source.Value = true;
		Assert.IsTrue(element.IsHitTestVisible);
	}

	private class BindingSource : INotifyPropertyChanged
	{
		private bool _value = true;

		public bool Value
		{
			get => _value;
			set
			{
				if (_value != value)
				{
					_value = value;
					OnPropertyChanged();
				}
			}
		}

		public event PropertyChangedEventHandler? PropertyChanged;

		private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
