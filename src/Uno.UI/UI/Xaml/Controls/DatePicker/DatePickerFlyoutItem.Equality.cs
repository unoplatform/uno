using System;

namespace Windows.UI.Xaml.Controls
{
	partial class DatePickerFlyoutItem : IEquatable<DatePickerFlyoutItem>
	{
		public bool Equals(DatePickerFlyoutItem other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return Equals(PrimaryText, other.PrimaryText)
				&& Equals(PrimaryText, other.PrimaryText);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			return obj.GetType() == GetType() && Equals((DatePickerFlyoutItem)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var primaryText = PrimaryText;
				var secondaryText = SecondaryText;
				var hashCode = (primaryText != null ? primaryText.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (secondaryText != null ? secondaryText.GetHashCode() : 0);
				return hashCode;
			}
		}
	}
}
