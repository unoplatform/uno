using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Microsoft.UI.Xaml.Media;
using Uno.UI;
using Uno;
using Uno.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls;

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
	[GeneratedDependencyProperty(DefaultValue = false, LocalCache = false, ChangedCallback = true)]
	public new partial bool IsOpen { get; set; }

	private new void OnIsOpenChanged(bool oldValue, bool newValue)
		=> base.IsOpen = newValue;
	#endregion

	#region Child Property
	[GeneratedDependencyProperty(DefaultValue = null, LocalCache = false, ChangedCallback = true)]
	public new partial UIElement Child { get; set; }

	private new void OnChildChanged(UIElement oldValue, UIElement newValue)
		=> base.Child = newValue;
	#endregion

	#region IsLightDismissEnabled Property
	[GeneratedDependencyProperty(DefaultValue = false, LocalCache = false, ChangedCallback = true)]
	public new partial bool IsLightDismissEnabled { get; set; }

	private new void OnIsLightDismissEnabledChanged(bool oldValue, bool newValue)
		=> base.IsLightDismissEnabled = newValue;
	#endregion

	#region HorizontalOffset Property
	[GeneratedDependencyProperty(DefaultValue = 0.0, LocalCache = false, ChangedCallback = true)]
	public new partial double HorizontalOffset { get; set; }
	private void OnHorizontalOffsetChanged(double oldValue, double newValue)
		=> base.HorizontalOffset = newValue;
	#endregion

	#region VerticalOffset Property
	[GeneratedDependencyProperty(DefaultValue = 0.0, LocalCache = false, ChangedCallback = true)]
	public new partial double VerticalOffset { get; set; }

	private void OnVerticalOffsetChanged(double oldValue, double newValue)
		=> base.VerticalOffset = newValue;
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
