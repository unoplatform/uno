#if HAS_UNO
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Uno.UI.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

[TestClass]
[RunsOnUIThread]
public class Given_DependencyProperty_PropMethodCall
{
	[TestMethod]
	public void When_SetValue_GetValue_Roundtrip()
	{
		var control = new PropMethodCallTestControl();
		Assert.IsFalse(control.TestBool); // default is false

		control.TestBool = true;
		Assert.IsTrue(control.TestBool);
		Assert.IsTrue((bool)control.GetValue(PropMethodCallTestControl.TestBoolProperty));

		control.TestBool = false;
		Assert.IsFalse(control.TestBool);
		Assert.IsFalse((bool)control.GetValue(PropMethodCallTestControl.TestBoolProperty));
	}

	[TestMethod]
	public void When_ClearValue_Restores_Default()
	{
		var control = new PropMethodCallTestControl();
		control.TestBool = true;
		Assert.IsTrue(control.TestBool);

		control.ClearValue(PropMethodCallTestControl.TestBoolProperty);
		Assert.IsFalse(control.TestBool); // default is false
	}

	[TestMethod]
	public void When_Style_Value_Fallback()
	{
		var style = new Style(typeof(PropMethodCallTestControl));
		style.Setters.Add(new Setter(PropMethodCallTestControl.TestBoolProperty, true));

		var control = new PropMethodCallTestControl();
		control.Style = style;
		Assert.IsTrue(control.TestBool); // style value

		// Local overrides style
		control.TestBool = false;
		Assert.IsFalse(control.TestBool);

		// Clearing local restores style value
		control.ClearValue(PropMethodCallTestControl.TestBoolProperty);
		Assert.IsTrue(control.TestBool); // style value restored
	}

	[TestMethod]
	public void When_PropertyChanged_Fires_With_Correct_Values()
	{
		var control = new PropMethodCallTestControl();
		control.PropertyChangedCount = 0;
		control.LastChange = null;

		control.TestBool = true;
		Assert.AreEqual(1, control.PropertyChangedCount);
		Assert.IsNotNull(control.LastChange);
		Assert.AreEqual(false, control.LastChange.Value.OldValue);
		Assert.AreEqual(true, control.LastChange.Value.NewValue);

		control.TestBool = false;
		Assert.AreEqual(2, control.PropertyChangedCount);
		Assert.AreEqual(true, control.LastChange.Value.OldValue);
		Assert.AreEqual(false, control.LastChange.Value.NewValue);
	}

	[TestMethod]
	public void When_ReadLocalValue()
	{
		var control = new PropMethodCallTestControl();
		Assert.AreEqual(DependencyProperty.UnsetValue, control.ReadLocalValue(PropMethodCallTestControl.TestBoolProperty));

		control.TestBool = true;
		Assert.AreEqual(true, control.ReadLocalValue(PropMethodCallTestControl.TestBoolProperty));

		control.ClearValue(PropMethodCallTestControl.TestBoolProperty);
		Assert.AreEqual(DependencyProperty.UnsetValue, control.ReadLocalValue(PropMethodCallTestControl.TestBoolProperty));
	}

	[TestMethod]
	public void When_Binding_Propagates_Value()
	{
		var source = new BindingSource { BoolValue = true };
		var control = new PropMethodCallTestControl();

		control.SetBinding(PropMethodCallTestControl.TestBoolProperty, new Binding
		{
			Source = source,
			Path = new PropertyPath(nameof(BindingSource.BoolValue)),
			Mode = BindingMode.OneWay,
		});

		Assert.IsTrue(control.TestBool);

		source.BoolValue = false;
		Assert.IsFalse(control.TestBool);
	}

	[TestMethod]
	public void When_Coercion_Clamps_Value()
	{
		var control = new PropMethodCallTestControl();

		// CoerceTestCoerced clamps to [0, 100]
		control.TestCoerced = 50;
		Assert.AreEqual(50, control.TestCoerced);

		control.TestCoerced = 200;
		Assert.AreEqual(100, control.TestCoerced); // clamped to max

		control.TestCoerced = -10;
		Assert.AreEqual(0, control.TestCoerced); // clamped to min
	}

	[TestMethod]
	public void When_Coercion_Then_ClearValue()
	{
		var control = new PropMethodCallTestControl();

		control.TestCoerced = 200;
		Assert.AreEqual(100, control.TestCoerced); // clamped

		control.ClearValue(PropMethodCallTestControl.TestCoercedProperty);
		Assert.AreEqual(0, control.TestCoerced); // default restored
	}

	[TestMethod]
	public void When_Multiple_Precedence_Transitions()
	{
		var style = new Style(typeof(PropMethodCallTestControl));
		style.Setters.Add(new Setter(PropMethodCallTestControl.TestIntProperty, 10));

		var control = new PropMethodCallTestControl();
		Assert.AreEqual(0, control.TestInt); // default

		control.Style = style;
		Assert.AreEqual(10, control.TestInt); // style

		control.TestInt = 42;
		Assert.AreEqual(42, control.TestInt); // local

		control.ClearValue(PropMethodCallTestControl.TestIntProperty);
		Assert.AreEqual(10, control.TestInt); // back to style

		control.Style = null;
		Assert.AreEqual(0, control.TestInt); // back to default
	}

	[TestMethod]
	public void When_Non_Boolean_Type()
	{
		var control = new PropMethodCallTestControl();
		Assert.AreEqual(0, control.TestInt); // default

		control.TestInt = 99;
		Assert.AreEqual(99, control.TestInt);
		Assert.AreEqual(99, (int)control.GetValue(PropMethodCallTestControl.TestIntProperty));

		control.ClearValue(PropMethodCallTestControl.TestIntProperty);
		Assert.AreEqual(0, control.TestInt); // default
	}

	[TestMethod]
	public void When_Setting_Same_Value_Twice()
	{
		var control = new PropMethodCallTestControl();
		control.PropertyChangedCount = 0;

		control.TestBool = true;
		Assert.AreEqual(1, control.PropertyChangedCount);

		// Second set with same value — delegate returns false (no change), so PropertyChanged should not fire again
		control.TestBool = true;
		Assert.AreEqual(1, control.PropertyChangedCount);
	}

	[TestMethod]
	public void When_Style_Then_Coercion()
	{
		var style = new Style(typeof(PropMethodCallTestControl));
		style.Setters.Add(new Setter(PropMethodCallTestControl.TestCoercedProperty, 200));

		var control = new PropMethodCallTestControl();
		control.Style = style;

		// Style sets 200, coercion clamps to 100
		Assert.AreEqual(100, control.TestCoerced);
	}

	private partial class PropMethodCallTestControl : Control
	{
		private bool _testBool;
		private int _testInt;
		private int _testCoerced;
		internal int PropertyChangedCount;
		internal (object OldValue, object NewValue)? LastChange;

		public bool TestBool
		{
			get => (bool)GetValue(TestBoolProperty);
			set => SetValue(TestBoolProperty, value);
		}

		public static DependencyProperty TestBoolProperty { get; } = DependencyProperty.Register(
			nameof(TestBool),
			typeof(bool),
			typeof(PropMethodCallTestControl),
			new FrameworkPropertyMetadata(
				defaultValue: false,
				propertyChangedCallback: OnChanged)
			{
				PropMethodCall = TestBoolMethod,
			}
		);

		public int TestInt
		{
			get => (int)GetValue(TestIntProperty);
			set => SetValue(TestIntProperty, value);
		}

		public static DependencyProperty TestIntProperty { get; } = DependencyProperty.Register(
			nameof(TestInt),
			typeof(int),
			typeof(PropMethodCallTestControl),
			new FrameworkPropertyMetadata(
				defaultValue: 0,
				propertyChangedCallback: OnChanged)
			{
				PropMethodCall = TestIntMethod,
			}
		);

		public int TestCoerced
		{
			get => (int)GetValue(TestCoercedProperty);
			set => SetValue(TestCoercedProperty, value);
		}

		public static DependencyProperty TestCoercedProperty { get; } = DependencyProperty.Register(
			nameof(TestCoerced),
			typeof(int),
			typeof(PropMethodCallTestControl),
			new FrameworkPropertyMetadata(
				defaultValue: 0,
				propertyChangedCallback: OnChanged,
				coerceValueCallback: CoerceTestCoerced)
			{
				PropMethodCall = TestCoercedMethod,
			}
		);

		private static object TestBoolMethod(DependencyObject @do, bool isGet, object valueToSet)
		{
			var @this = (PropMethodCallTestControl)@do;
			if (isGet)
			{
				return Boxes.Box(@this._testBool);
			}
			else
			{
				var newValue = (bool)valueToSet;
				if (newValue != @this._testBool)
				{
					@this._testBool = newValue;
					return true;
				}

				return false;
			}
		}

		private static object TestIntMethod(DependencyObject @do, bool isGet, object valueToSet)
		{
			var @this = (PropMethodCallTestControl)@do;
			if (isGet)
			{
				return @this._testInt;
			}
			else
			{
				var newValue = (int)valueToSet;
				if (newValue != @this._testInt)
				{
					@this._testInt = newValue;
					return true;
				}

				return false;
			}
		}

		private static object TestCoercedMethod(DependencyObject @do, bool isGet, object valueToSet)
		{
			var @this = (PropMethodCallTestControl)@do;
			if (isGet)
			{
				return @this._testCoerced;
			}
			else
			{
				var newValue = (int)valueToSet;
				if (newValue != @this._testCoerced)
				{
					@this._testCoerced = newValue;
					return true;
				}

				return false;
			}
		}

		private static object CoerceTestCoerced(DependencyObject d, object baseValue, DependencyPropertyValuePrecedences precedence)
		{
			var value = (int)baseValue;
			return Math.Clamp(value, 0, 100);
		}

		private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var @this = (PropMethodCallTestControl)d;
			@this.PropertyChangedCount++;
			@this.LastChange = (e.OldValue, e.NewValue);
		}
	}

	private class BindingSource : INotifyPropertyChanged
	{
		private bool _boolValue;

		public bool BoolValue
		{
			get => _boolValue;
			set
			{
				if (_boolValue != value)
				{
					_boolValue = value;
					OnPropertyChanged();
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private void OnPropertyChanged([CallerMemberName] string propertyName = null)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
#endif
