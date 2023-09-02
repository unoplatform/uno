using System;
using Windows.UI.Xaml.Automation.Peers;

namespace Windows.UI.Xaml.Controls.Primitives;

/// <summary>
/// Represents an element that has a value within a specific range,
/// such as the ProgressBar, ScrollBar, and Slider controls.
/// </summary>
public partial class RangeBase : Control
{
	/// <summary>
	/// Called when the Minimum property changes.
	/// </summary>
	/// <param name="oldMinimum">Old minimum.</param>
	/// <param name="newMinimum">New minimum.</param>
	protected virtual void OnMinimumChanged(double oldMinimum, double newMinimum)
	{
	}

	/// <summary>
	/// Called when the Maximum property changes.
	/// </summary>
	/// <param name="oldMaximum">Old maximum.</param>
	/// <param name="newMaximum">New maximum.</param>
	protected virtual void OnMaximumChanged(double oldMaximum, double newMaximum)
	{
	}

	internal override bool GetDefaultValue2(
		DependencyProperty pDP,
		out object pValue)
	{
		pValue = null;
		if (pDP == LargeChangeProperty ||
			pDP == MaximumProperty)
		{
			pValue = 1.0;
		}
		else if (pDP == SmallChangeProperty)
		{
			pValue = 0.1;
		}
		else
		{
			base.GetDefaultValue2(pDP, out pValue);
		}

		return pValue != null;
	}

	internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
	{
		if (args.Property == MinimumProperty)
		{
			HandlePropertyChanged(
			args,
			(automationPeer, oldValue, newValue) => automationPeer.RaiseMinimumPropertyChangedEvent(oldValue, newValue),
			(rangeBase, oldValue, newValue) => rangeBase.OnMinimumChanged(oldValue, newValue));
		}
		else if (args.Property == MaximumProperty)
		{
			HandlePropertyChanged(
			args,
			(automationPeer, oldValue, newValue) => automationPeer.RaiseMaximumPropertyChangedEvent(oldValue, newValue),
			(rangeBase, oldValue, newValue) => rangeBase.OnMaximumChanged(oldValue, newValue));
		}
		else if (args.Property == ValueProperty)
		{
			HandlePropertyChanged(
			args,
			(automationPeer, oldValue, newValue) => automationPeer.RaiseValuePropertyChangedEvent(oldValue, newValue),
			(rangeBase, oldValue, newValue) => rangeBase.OnValueChanged(oldValue, newValue));
		}
	}

	private void HandlePropertyChanged(
		DependencyPropertyChangedEventArgs args,
		Action<RangeBaseAutomationPeer, double, double> onChanged,
		Action<RangeBase, double, double> onChangedProtected)
	{
		double oldValue = 0.0;
		double newValue = 0.0;
		oldValue = (double)args.OldValue;
		newValue = (double)args.NewValue;

		var bAutomationListener = AutomationPeer.ListenerExistsHelper(AutomationEvents.PropertyChanged);

		if (bAutomationListener)
		{
			var automationPeer = GetOrCreateAutomationPeer();

			if (automationPeer != null)
			{
				onChanged(automationPeer as RangeBaseAutomationPeer, oldValue, newValue);
			}
		}

		onChangedProtected(this, oldValue, newValue);
	}

	/// <summary>
	/// Raises the ValueChanged routed event.
	/// </summary>
	/// <param name="oldValue">Old value.</param>
	/// <param name="newValue">New value.</param>
	protected virtual void OnValueChanged(double oldValue, double newValue)
	{
		if (ShouldRaiseEvent(ValueChanged))
		{
			// Create the args
			var args = new RangeBaseValueChangedEventArgs(this);

			args.OldValue = oldValue;
			args.NewValue = newValue;

			// Raise the event
			ValueChanged?.Invoke(this, args);
		}
	}

	protected override AutomationPeer OnCreateAutomationPeer() => new RangeBaseAutomationPeer(this);

	// Makes sure the DOUBLE value is not Double.NaN, Double.PositiveInfinity, or Double.NegativeInfinity.
	// If the value passed is one of these illegal values, throws an ArgumentException with a custom message.
	private void EnsureValidDoubleValue(double value)
	{
		if (double.IsNaN(value) || double.IsInfinity(value))
		{
			throw new ArgumentException("Invalid value supplied", nameof(value));
		}
	}
}
