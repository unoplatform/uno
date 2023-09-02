using System;

namespace Windows.UI.Xaml.Controls.Primitives;

public partial class RangeBase : Control
{
	private double m_uncoercedValue;

	// TODO Uno specific:
	// WinUI uses a custom SetValue "override" that controls the SetValue flow propagation itself.
	// This way it can ensure that the min <= value <= max invariant is always maintained precisely -
	// when attempting to break the invariant, the code elegantly sets the remaining boundaries first
	// before the actual value is set. We don't have this specific facility yet, so the invariant
	// may get broken temporarily.

	private double GetDoubleValueHelper(DependencyProperty property)
	{
		return (double)GetValue(property);
	}

	private double CoerceValueBetween(double m_uncoercedValue, double min, double max)
	{
		return Math.Max(Math.Min(m_uncoercedValue, max), min);
	}

	private void SetMaximum(double max)
	{
		SetValue(MaximumProperty, max);
	}

	private object SetRangeBaseValue(DependencyProperty property, object baseValue)
	{
		if (baseValue == null)
		{
			return null;
		}
		//bool wasHandled = false;

		if (property == ValueProperty)
		{
			m_uncoercedValue = (double)baseValue;

			double min = GetDoubleValueHelper(MinimumProperty);

			double max = GetDoubleValueHelper(MaximumProperty);

			var newValue = CoerceValueBetween(m_uncoercedValue, min, max);
			return newValue;

			//wasHandled = true;
		}
		else if (property == MinimumProperty)
		{
			double newMin = (double)baseValue;
			double oldMin = GetDoubleValueHelper(MinimumProperty);
			double max = GetDoubleValueHelper(MaximumProperty);

			if (newMin <= oldMin)
			{
				// expanding range (decreasing minimum)

				// set minimum
				// CControl::SetValue(args));

				// update value
				var newValue = CoerceValueBetween(m_uncoercedValue, newMin, max);
				if (newValue != Value)
				{
					SetValue(ValueProperty, newValue);
				}
			}
			else
			{
				// contracting range (increasing minimum)

				if (newMin > max)
				{
					// coerce and update maximum
					max = newMin;
					SetMaximum(max);
				}

				// set value
				var newValue = CoerceValueBetween(m_uncoercedValue, newMin, max);
				if (newValue != Value)
				{
					SetValue(ValueProperty, newValue);
				}

				// set minimum
				// CControl::SetValue(args));
			}

			return newMin;
			//wasHandled = true;
		}
		else if (property == MaximumProperty)
		{
			double newMax = (double)baseValue;
			double oldMax = GetDoubleValueHelper(MaximumProperty);
			double min = GetDoubleValueHelper(MinimumProperty);

			if (newMax >= oldMax)
			{
				// expanding range (increasing maximum)

				// set maximum
				// CControl::SetValue(args));

				// set value
				var newValue = CoerceValueBetween(m_uncoercedValue, min, newMax);
				if (newValue != Value)
				{
					SetValue(ValueProperty, newValue);
				}
			}
			else
			{
				// contracting range (decreasing maximum)

				// keep maximum higher than minimum
				newMax = Math.Max(newMax, min);

				// coerce and set value
				var newValue = CoerceValueBetween(m_uncoercedValue, min, newMax);
				if (newValue != Value)
				{
					SetValue(ValueProperty, newValue);
				}

				// set maximum
				// SetMaximum(newMax);
			}

			return newMax;
			//wasHandled = true;
		}

		return null;
		//if (!wasHandled)
		//{
		//	Control.SetValue(args);
		//}
	}
}
