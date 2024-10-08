using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#if HAS_UNO
using Uno.UI.DataBinding;
#endif

namespace Uno.UI.RuntimeTests.Tests.TemplatedParent.Setup;

public sealed partial class BehaviorSetup : Page
{
	public BehaviorSetup()
	{
		this.InitializeComponent();
	}
}

public sealed class Interaction
{
	#region DependencyProperty: Behaviors

	public static DependencyProperty BehaviorsProperty { get; } = DependencyProperty.RegisterAttached(
		"Behaviors",
		typeof(BehaviorCollection),
		typeof(Interaction),
		new PropertyMetadata(null, OnBehaviorsChanged));

	public static BehaviorCollection GetBehaviors(DependencyObject obj) => GetBehaviorsOverride(obj);
	public static void SetBehaviors(DependencyObject obj, BehaviorCollection value) => obj.SetValue(BehaviorsProperty, value);

	#endregion

	private static BehaviorCollection GetBehaviorsOverride(DependencyObject obj)
	{
		var value = (BehaviorCollection)obj.GetValue(BehaviorsProperty);
		if (value is null)
		{
			obj.SetValue(BehaviorsProperty, value = new BehaviorCollection());
		}

		return value;
	}

	private static void OnBehaviorsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
		if (e.NewValue is BehaviorCollection collection)
		{
			collection.AssociatedObject = sender;
		}
	}
}
public sealed class BehaviorCollection : DependencyObjectCollection
{
	public DependencyObject AssociatedObject { get; set; }
}

public interface IBehavior { }

public partial class LegacyDOBehavior : DependencyObject, IBehavior
#if HAS_UNO
	, INotTemplatedParentProvider
#endif
{
	#region DependencyProperty: TestValue

	public static DependencyProperty TestValueProperty { get; } = DependencyProperty.Register(
		nameof(TestValue),
		typeof(object),
		typeof(LegacyDOBehavior),
		new PropertyMetadata(default(object), OnTestValueChanged));

	public object TestValue
	{
		get => (object)GetValue(TestValueProperty);
		set => SetValue(TestValueProperty, value);
	}

	#endregion

	private static void OnTestValueChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
	}
}
public partial class NonLegacyDOBehavior : DependencyObject, IBehavior
{
	#region DependencyProperty: TestValue

	public static DependencyProperty TestValueProperty { get; } = DependencyProperty.Register(
		nameof(TestValue),
		typeof(object),
		typeof(NonLegacyDOBehavior),
		new PropertyMetadata(default(object), OnTestValueChanged));

	public object TestValue
	{
		get => (object)GetValue(TestValueProperty);
		set => SetValue(TestValueProperty, value);
	}

	#endregion

	private static void OnTestValueChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
	}
}
