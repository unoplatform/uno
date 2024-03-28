using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Markup;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Windows.UI.Xaml.Media;
using Uno.UI;
using Uno;
using Uno.UI.Xaml;

namespace Windows.UI.Xaml.Controls;

public partial class NativePopupBase : Primitives.Popup
{
	private Dictionary<DependencyProperty, DependencyProperty> _forwardedProperties
		= new Dictionary<DependencyProperty, DependencyProperty>();

	public NativePopupBase()
	{
		RegisterDependencyPropertyForward(IsOpenProperty, Primitives.Popup.IsOpenProperty);
		RegisterDependencyPropertyForward(ChildProperty, Primitives.Popup.ChildProperty);
		RegisterDependencyPropertyForward(IsLightDismissEnabledProperty, Primitives.Popup.IsLightDismissEnabledProperty);
		RegisterDependencyPropertyForward(HorizontalOffsetProperty, Primitives.Popup.HorizontalOffsetProperty);
		RegisterDependencyPropertyForward(VerticalOffsetProperty, Primitives.Popup.VerticalOffsetProperty);
	}

	private protected void RegisterDependencyPropertyForward(DependencyProperty property, DependencyProperty baseProperty)
	{
		this.SetValue(property, GetValue(baseProperty), DependencyPropertyValuePrecedences.Local);
		this.RegisterPropertyChangedCallback(baseProperty, OnBasePropertyChanged);
		_forwardedProperties[baseProperty] = property;
	}

	private void OnBasePropertyChanged(DependencyObject sender, DependencyProperty dp)
	{
		if (_forwardedProperties.TryGetValue(dp, out var localProperty))
		{
			SetValue(localProperty, GetValue(dp));
		}
	}

	#region IsOpen Property
	public new bool IsOpen
	{
		get => GetIsOpenValue();
		set => SetIsOpenValue(value);
	}

	private new void OnIsOpenChanged(bool oldValue, bool newValue)
		=> base.IsOpen = newValue;

	[GeneratedDependencyProperty(DefaultValue = false, LocalCache = false, ChangedCallback = true)]
	public new static DependencyProperty IsOpenProperty { get; } = CreateIsOpenProperty();
	#endregion

	#region Child Property
	public new UIElement Child
	{
		get => GetChildValue();
		set => SetChildValue(value);
	}

	private new void OnChildChanged(UIElement oldValue, UIElement newValue)
		=> base.Child = newValue;

	[GeneratedDependencyProperty(DefaultValue = null, LocalCache = false, ChangedCallback = true)]
	public new static DependencyProperty ChildProperty { get; } = CreateChildProperty();
	#endregion

	#region IsLightDismissEnabled Property
	public new bool IsLightDismissEnabled
	{
		get => GetIsLightDismissEnabledValue();
		set => SetIsLightDismissEnabledValue(value);
	}

	private new void OnIsLightDismissEnabledChanged(bool oldValue, bool newValue)
		=> base.IsLightDismissEnabled = newValue;

	[GeneratedDependencyProperty(DefaultValue = false, LocalCache = false, ChangedCallback = true)]
	public new static DependencyProperty IsLightDismissEnabledProperty { get; } = CreateIsLightDismissEnabledProperty();
	#endregion

	#region HorizontalOffset Property
	public new double HorizontalOffset
	{
		get => GetHorizontalOffsetValue();
		set => SetHorizontalOffsetValue(value);
	}
	private void OnHorizontalOffsetChanged(double oldValue, double newValue)
		=> base.HorizontalOffset = newValue;

	[GeneratedDependencyProperty(DefaultValue = 0.0, LocalCache = false, ChangedCallback = true)]
	public new static DependencyProperty HorizontalOffsetProperty { get; } = CreateHorizontalOffsetProperty();
	#endregion

	#region VerticalOffset Property
	public new double VerticalOffset
	{
		get => GetVerticalOffsetValue();
		set => SetVerticalOffsetValue(value);
	}

	private void OnVerticalOffsetChanged(double oldValue, double newValue)
		=> base.VerticalOffset = newValue;

	[GeneratedDependencyProperty(DefaultValue = 0.0, LocalCache = false, ChangedCallback = true)]
	public new static DependencyProperty VerticalOffsetProperty { get; } = CreateVerticalOffsetProperty();
	#endregion

	public new event EventHandler<object> Closed
	{
		add => base.Closed += value;
		remove => base.Closed -= value;
	}

	public new event EventHandler<object> Opened
	{
		add => base.Opened += value;
		remove => base.Opened -= value;
	}
}
