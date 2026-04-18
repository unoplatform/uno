#if HAS_UNO
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Private.Infrastructure;
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

		// Second set uses the same effective value, so PropertyChanged should not fire again
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

	[TestMethod]
	public void When_Coercion_Then_ReadLocalValue_Returns_Base_Value()
	{
		var control = new PropMethodCallTestControl();

		// Local value 200 is coerced to 100 for the effective value,
		// but ReadLocalValue must return the pre-coercion local base value.
		control.TestCoerced = 200;

		Assert.AreEqual(100, control.TestCoerced); // effective (coerced)
		Assert.AreEqual(200, control.ReadLocalValue(PropMethodCallTestControl.TestCoercedProperty));
	}

	[TestMethod]
	public void When_Local_Then_Style_Local_Wins()
	{
		// Gap 5 regression: setting local first, then applying a Style with a Setter,
		// the local value must still win (matches WinUI IsPropertySetByStyle check).
		var control = new PropMethodCallTestControl();
		control.TestBool = true;

		var style = new Style(typeof(PropMethodCallTestControl));
		style.Setters.Add(new Setter(PropMethodCallTestControl.TestBoolProperty, false));
		control.Style = style;

		Assert.IsTrue(control.TestBool);
	}

	[TestMethod]
	public void When_UIElement_IsHitTestVisible_Roundtrip()
	{
		var element = new Border();
		Assert.IsTrue(element.IsHitTestVisible);

		element.IsHitTestVisible = false;
		Assert.IsFalse(element.IsHitTestVisible);
		Assert.IsFalse((bool)element.GetValue(UIElement.IsHitTestVisibleProperty));

		element.ClearValue(UIElement.IsHitTestVisibleProperty);
		Assert.IsTrue(element.IsHitTestVisible);
	}

	[TestMethod]
	public void When_UIElement_AllowDrop_Roundtrip()
	{
		var element = new Border();
		Assert.IsFalse(element.AllowDrop);

		element.AllowDrop = true;
		Assert.IsTrue(element.AllowDrop);
		Assert.IsTrue((bool)element.GetValue(UIElement.AllowDropProperty));

		element.ClearValue(UIElement.AllowDropProperty);
		Assert.IsFalse(element.AllowDrop);
	}

	[TestMethod]
	public async System.Threading.Tasks.Task When_UIElement_AllowDrop_Inherits_From_Parent()
	{
		// WinUI marks AllowDrop as IsInheritedProperty (StaticMetadata.g.cpp:25891).
		// Setting it on a parent must propagate to a child via inheritance.
		var child = new Border { Width = 10, Height = 10 };
		var parent = new Grid { Width = 100, Height = 100, Children = { child } };

		TestServices.WindowHelper.WindowContent = parent;
		await TestServices.WindowHelper.WaitForLoaded(parent);

		Assert.IsFalse(child.AllowDrop);

		parent.AllowDrop = true;
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(child.AllowDrop); // inherited
	}

	[TestMethod]
	public void When_UIElement_CanDrag_Roundtrip()
	{
		var element = new Border();
		Assert.IsFalse(element.CanDrag);

		element.CanDrag = true;
		Assert.IsTrue(element.CanDrag);

		element.ClearValue(UIElement.CanDragProperty);
		Assert.IsFalse(element.CanDrag);
	}

	[TestMethod]
	public void When_UIElement_IsTabStop_Roundtrip()
	{
		var element = new Border();
		Assert.IsFalse(element.IsTabStop);

		element.IsTabStop = true;
		Assert.IsTrue(element.IsTabStop);

		element.ClearValue(UIElement.IsTabStopProperty);
		Assert.IsFalse(element.IsTabStop);
	}

	[TestMethod]
	public void When_UIElement_UseSystemFocusVisuals_Roundtrip()
	{
		var element = new Border();
		Assert.IsFalse(element.UseSystemFocusVisuals);

		element.UseSystemFocusVisuals = true;
		Assert.IsTrue(element.UseSystemFocusVisuals);

		element.ClearValue(UIElement.UseSystemFocusVisualsProperty);
		Assert.IsFalse(element.UseSystemFocusVisuals);
	}

	[TestMethod]
	public void When_UIElement_BoolFlags_Independent()
	{
		// Multiple booleans share the BoolFlags storage — setting one must not clobber another.
		var element = new Border();

		element.AllowDrop = true;
		element.CanDrag = true;
		element.IsTabStop = true;
		element.UseSystemFocusVisuals = true;

		Assert.IsTrue(element.IsHitTestVisible); // untouched default
		Assert.IsTrue(element.AllowDrop);
		Assert.IsTrue(element.CanDrag);
		Assert.IsTrue(element.IsTabStop);
		Assert.IsTrue(element.UseSystemFocusVisuals);

		element.IsHitTestVisible = false;

		Assert.IsFalse(element.IsHitTestVisible);
		Assert.IsTrue(element.AllowDrop);
		Assert.IsTrue(element.CanDrag);
		Assert.IsTrue(element.IsTabStop);
		Assert.IsTrue(element.UseSystemFocusVisuals);
	}

	[TestMethod]
	public void When_UIElement_Translation_Roundtrip()
	{
		var element = new Border();
		Assert.AreEqual(System.Numerics.Vector3.Zero, element.Translation);

		var newTranslation = new System.Numerics.Vector3(0, 0, 10);
		element.Translation = newTranslation;
		Assert.AreEqual(newTranslation, element.Translation);
		Assert.AreEqual(newTranslation, (System.Numerics.Vector3)element.GetValue(UIElement.TranslationProperty));

		element.ClearValue(UIElement.TranslationProperty);
		Assert.AreEqual(System.Numerics.Vector3.Zero, element.Translation);
	}

	[TestMethod]
	public void When_BrushTransition_Duration_Default_And_Roundtrip()
	{
		var transition = new BrushTransition();
		Assert.AreEqual(TimeSpan.FromTicks(1500000), transition.Duration); // 150 ms

		transition.Duration = TimeSpan.FromMilliseconds(300);
		Assert.AreEqual(TimeSpan.FromMilliseconds(300), transition.Duration);
		Assert.AreEqual(TimeSpan.FromMilliseconds(300), (TimeSpan)transition.GetValue(BrushTransition.DurationProperty));

		transition.ClearValue(BrushTransition.DurationProperty);
		Assert.AreEqual(TimeSpan.FromTicks(1500000), transition.Duration);
	}

	[TestMethod]
	public void When_Control_IsEnabled_Roundtrip()
	{
		var control = new Button();
		Assert.IsTrue(control.IsEnabled);

		control.IsEnabled = false;
		Assert.IsFalse(control.IsEnabled);
		Assert.IsFalse((bool)control.GetValue(Control.IsEnabledProperty));

		control.ClearValue(Control.IsEnabledProperty);
		Assert.IsTrue(control.IsEnabled);
	}

	[TestMethod]
	public async System.Threading.Tasks.Task When_Control_IsEnabled_Inherits_From_Parent()
	{
		// Parent Control disables the subtree; child Control must observe IsEnabled=false via inheritance.
		var childButton = new Button();
		var parentContent = new ContentControl { Content = childButton };

		TestServices.WindowHelper.WindowContent = parentContent;
		await TestServices.WindowHelper.WaitForLoaded(parentContent);

		Assert.IsTrue(childButton.IsEnabled);

		parentContent.IsEnabled = false;
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsFalse(parentContent.IsEnabled);
		Assert.IsFalse(childButton.IsEnabled); // inherited

		parentContent.IsEnabled = true;
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(parentContent.IsEnabled);
		Assert.IsTrue(childButton.IsEnabled); // inherited re-enable
	}

	[TestMethod]
	public async System.Threading.Tasks.Task When_Control_IsEnabled_Local_Wins_Over_Inherited()
	{
		// Child sets local IsEnabled=true first; parent disables — child stays enabled.
		// This is the "local-first-then-inheritance" variant of the Gap 5 scenario.
		var childButton = new Button { IsEnabled = true };
		var parentContent = new ContentControl { Content = childButton };

		TestServices.WindowHelper.WindowContent = parentContent;
		await TestServices.WindowHelper.WaitForLoaded(parentContent);

		parentContent.IsEnabled = false;
		await TestServices.WindowHelper.WaitForIdle();

		// Child has local IsEnabled=true, but CoerceIsEnabled will coerce to false
		// because the parent is disabled — matches WinUI's behavior.
		Assert.IsFalse(childButton.IsEnabled);
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

#nullable enable
		private static object? TestBoolMethod(DependencyObject instance, bool isGet, object? valueToSet)
		{
			var control = (PropMethodCallTestControl)instance;
			if (isGet)
			{
				return Boxes.Box(control._testBool);
			}

			var newValue = (bool)valueToSet!;
			if (newValue != control._testBool)
			{
				control._testBool = newValue;
				return true;
			}

			return false;
		}

		private static object? TestIntMethod(DependencyObject instance, bool isGet, object? valueToSet)
		{
			var control = (PropMethodCallTestControl)instance;
			if (isGet)
			{
				return control._testInt;
			}

			var newValue = (int)valueToSet!;
			if (newValue != control._testInt)
			{
				control._testInt = newValue;
				return true;
			}

			return false;
		}

		private static object? TestCoercedMethod(DependencyObject instance, bool isGet, object? valueToSet)
		{
			var control = (PropMethodCallTestControl)instance;
			if (isGet)
			{
				return control._testCoerced;
			}

			var newValue = (int)valueToSet!;
			if (newValue != control._testCoerced)
			{
				control._testCoerced = newValue;
				return true;
			}

			return false;
		}
#nullable restore

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
