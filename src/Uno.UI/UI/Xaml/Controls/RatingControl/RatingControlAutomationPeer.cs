// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RatingControlAutomationPeer.cpp, tag winui3/release/1.8.4

using System.Globalization;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using Windows.Globalization.NumberFormatting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using RatingControl = Microsoft.UI.Xaml.Controls.RatingControl;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes RatingControl types to Microsoft UI Automation.
/// </summary>
public partial class RatingControlAutomationPeer : FrameworkElementAutomationPeer, IRangeValueProvider, IValueProvider
{
	/// <summary>
	/// Initializes a new instance of the RatingControlAutomationPeer class.
	/// </summary>
	/// <param name="owner">Rating control.</param>
	public RatingControlAutomationPeer(RatingControl owner) : base(owner)
	{
	}

	protected override string GetLocalizedControlTypeCore() =>
		ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_RatingLocalizedControlType);

	// Properties.
	bool IValueProvider.IsReadOnly => GetRatingControl().IsReadOnly;

	string IValueProvider.Value
	{
		get
		{
			double ratingValue = GetRatingControl().Value;
			string valueString;

			//string ratingString;

			if (ratingValue == -1)
			{
				double placeholderValue = GetRatingControl().PlaceholderValue;
				if (placeholderValue == -1)
				{
					valueString = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_RatingUnset);
				}
				else
				{
					valueString = GenerateValue_ValueString(ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_CommunityRatingString), placeholderValue);
				}
			}
			else
			{
				valueString = GenerateValue_ValueString(ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_BasicRatingString), ratingValue);
			}

			return valueString;
		}
	}

	void IValueProvider.SetValue(string value)
	{
		DecimalFormatter formatter = new DecimalFormatter();
		var potentialRating = formatter.ParseDouble(value);
		if (potentialRating is not null)
		{
			GetRatingControl().Value = potentialRating.Value;
		}
	}

	// IRangeValueProvider overrides
	double IRangeValueProvider.SmallChange => 1.0;

	double IRangeValueProvider.LargeChange => 1.0;

	double IRangeValueProvider.Maximum => GetRatingControl().MaxRating;

	double IRangeValueProvider.Minimum => 0;

	double IRangeValueProvider.Value
	{
		get
		{
			// Change this to provide a placeholder value too.
			double value = GetRatingControl().Value;
			if (value == -1)
			{
				return 0;
			}
			else
			{
				return value;
			}
		}
	}

	void IRangeValueProvider.SetValue(double value) => GetRatingControl().Value = value;

	bool IRangeValueProvider.IsReadOnly => GetRatingControl().IsReadOnly;

	//IAutomationPeerOverrides

	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.Value ||
			patternInterface == PatternInterface.RangeValue)
		{
			return this;
		}

		return base.GetPatternCore(patternInterface);
	}

	protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Slider;

	// Protected methods
	internal void RaisePropertyChangedEvent(double newValue)
	{
		// UIA doesn't tolerate a null doubles, so we convert them to zeroes.
		double oldValue = GetRatingControl().Value;
		var oldValueProp = PropertyValue.CreateDouble(oldValue);

		if (newValue == -1)
		{
			var newValueProp = PropertyValue.CreateDouble(0.0);
			base.RaisePropertyChangedEvent(ValuePatternIdentifiers.ValueProperty, oldValueProp, newValueProp);
			base.RaisePropertyChangedEvent(RangeValuePatternIdentifiers.ValueProperty, oldValueProp, newValueProp);
		}
		else
		{
			var newValueProp = PropertyValue.CreateDouble(newValue);
			base.RaisePropertyChangedEvent(ValuePatternIdentifiers.ValueProperty, oldValueProp, newValueProp); // make these strings
			base.RaisePropertyChangedEvent(RangeValuePatternIdentifiers.ValueProperty, oldValueProp, newValueProp);
		}
	}

	// private methods

	private RatingControl GetRatingControl() => (RatingControl)Owner;

	private int DetermineFractionDigits(double value)
	{
		value = value * 100;
		int intValue = (int)value;

		// When reading out the Value_Value, we want clients to read out the least number of digits
		// possible. We don't want a 3 (represented as a double) to be read out as 3.00...
		// Here we determine the number of digits past the decimal point we care about,
		// and this number is used by the caller to truncate the Value_Value string.

		if (intValue % 100 == 0)
		{
			return 0;
		}
		else if (intValue % 10 == 0)
		{
			return 1;
		}
		else
		{
			return 2;
		}
	}

	private int DetermineSignificantDigits(double value, int fractionDigits)
	{
		int sigFigsInt = (int)value;
		int length = 0;

		while (sigFigsInt > 0)
		{
			sigFigsInt /= 10;
			length++;
		}

		return length + fractionDigits;
	}

	private string GenerateValue_ValueString(string resourceString, double ratingValue)
	{
		DecimalFormatter formatter = new DecimalFormatter();
		SignificantDigitsNumberRounder rounder = new SignificantDigitsNumberRounder();
		formatter.NumberRounder = rounder;

		string maxRatingString = GetRatingControl().MaxRating.ToString(CultureInfo.CurrentCulture);

		int fractionDigits = DetermineFractionDigits(ratingValue);
		int sigDigits = DetermineSignificantDigits(ratingValue, fractionDigits);
		formatter.FractionDigits = fractionDigits;
		rounder.SignificantDigits = (uint)sigDigits;
		string ratingString = formatter.Format(ratingValue);

		return StringUtil.FormatString(resourceString, ratingString, maxRatingString);
	}
}
