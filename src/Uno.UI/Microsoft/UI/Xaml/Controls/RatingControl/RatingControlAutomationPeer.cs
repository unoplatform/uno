// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Uno.UI.Helpers.WinUI;
using Windows.Globalization.NumberFormatting;
using Windows.UI.Xaml.Automation.Peers;
using RatingControl = Microsoft.UI.Xaml.Controls.RatingControl;

namespace Microsoft.UI.Xaml.Automation.Peers
{
	public class RatingControlAutomationPeer : FrameworkElementAutomationPeer
	{
		public RatingControlAutomationPeer(RatingControl owner) : base(owner)
		{
		}

		protected override string GetLocalizedControlTypeCore()
		{
			return ResourceAccessor.GetLocalizedStringResource(SR_RatingLocalizedControlType);
		}

		// Properties.
		private bool IsReadOnly()
		{
			return GetRatingControl().IsReadOnly;
		}

		string RatingControlAutomationPeer.IValueProvider_Value()
		{
			double ratingValue = GetRatingControl().Value();
			string valueString;

			string ratingString;

			if (ratingValue == -1)
			{
				double placeholderValue = GetRatingControl().PlaceholderValue();
				if (placeholderValue == -1)
				{
					valueString = ResourceAccessor.GetLocalizedStringResource(SR_RatingUnset);
				}
				else
				{
					valueString = GenerateValue_ValueString(ResourceAccessor.GetLocalizedStringResource(SR_CommunityRatingString), placeholderValue);
				}
			}
			else
			{
				valueString = GenerateValue_ValueString(ResourceAccessor.GetLocalizedStringResource(SR_BasicRatingString), ratingValue);
			}

			return valueString;
		}

		void SetValue(string & value)
		{
			DecimalFormatter formatter;
			var potentialRating = formatter.ParseDouble(value);
			if (potentialRating)
			{
				GetRatingControl().Value(potentialRating.Value());
			}
		}


		// IRangeValueProvider overrides
		double SmallChange()
		{
			return 1.0;
		}

		double LargeChange()
		{
			return 1.0;
		}

		double Maximum()
		{
			return GetRatingControl().MaxRating();
		}

		double Minimum()
		{
			return 0;
		}

		double Value()
		{
			// Change this to provide a placeholder value too.
			double value = GetRatingControl().Value();
			if (value == -1)
			{
				return 0;
			}
			else
			{
				return value;
			}
		}

		void SetValue(double value)
		{
			GetRatingControl().Value(value);
		}

		//IAutomationPeerOverrides

		DependencyObject GetPatternCore(PatternInterface & patternInterface)
		{
			if (patternInterface == PatternInterface.Value || patternInterface == PatternInterface.RangeValue)
			{
				return this;
			}

			return __super.GetPatternCore(patternInterface);
		}

		AutomationControlType GetAutomationControlTypeCore()
		{
			return AutomationControlType.Slider;
		}

		// Protected methods
		void RaisePropertyChangedEvent(double newValue)
		{
			// UIA doesn't tolerate a null doubles, so we convert them to zeroes.
			double oldValue = GetRatingControl().Value();
			var oldValueProp = PropertyValue.CreateDouble(oldValue);

			if (newValue == -1)
			{
				var newValueProp = PropertyValue.CreateDouble(0.0);
				__super.RaisePropertyChangedEvent(ValuePatternIdentifiers.ValueProperty(), oldValueProp, newValueProp);
				__super.RaisePropertyChangedEvent(RangeValuePatternIdentifiers.ValueProperty(), oldValueProp, newValueProp);
			}
			else
			{
				var newValueProp = PropertyValue.CreateDouble(newValue);
				__super.RaisePropertyChangedEvent(ValuePatternIdentifiers.ValueProperty(), oldValueProp, newValueProp); // make these strings
				__super.RaisePropertyChangedEvent(RangeValuePatternIdentifiers.ValueProperty(), oldValueProp, newValueProp);
			}
		}

		// private methods

		private RatingControl GetRatingControl()
		{
			UIElement owner = Owner();
			return owner.as< RatingControl > ();
		}

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

			string maxRatingString = GetRatingControl().MaxRating;

			int fractionDigits = DetermineFractionDigits(ratingValue);
			int sigDigits = DetermineSignificantDigits(ratingValue, fractionDigits);
			formatter.FractionDigits = fractionDigits;
			rounder.SignificantDigits = (uint)sigDigits;
			string ratingString = formatter.Format(ratingValue);

			return StringUtil.FormatString(resourceString, ratingString, maxRatingString);
		}
	}
}






