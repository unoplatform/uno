using Uno.UI.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Microsoft.UI.Xaml
{
	public partial struct Duration : IEquatable<Duration>, IComparable<Duration>
	{
		private static readonly string __automatic = "Automatic";
		private static readonly string __forever = "Forever";


		public Duration(TimeSpan timeSpan)
		{
			Type = DurationType.TimeSpan;
			TimeSpan = timeSpan;
		}

		public DurationType Type { get; private set; }

		public TimeSpan TimeSpan { get; private set; }

		public static implicit operator Duration(string timeSpan)
			=> timeSpan != null ? new Duration(TimeSpan.Parse(timeSpan, CultureInfo.InvariantCulture)) : new Duration(TimeSpan.Zero);

		public bool HasTimeSpan
		{
			get
			{
				return this.Type == DurationType.TimeSpan &&
					this.TimeSpan.CompareTo(TimeSpan.Zero) > 0;
			}
		}

		public static Duration Forever
		{
			get
			{
				return new Duration() { Type = DurationType.Forever };
			}
		}

		public static Duration Automatic
		{
			get
			{
				return new Duration() { Type = DurationType.Automatic };
			}
		}

		public Duration Add(Duration duration)
		{
			if (this.Type == DurationType.TimeSpan && duration.Type == DurationType.TimeSpan)
			{
				return new Duration(this.TimeSpan.Add(duration.TimeSpan));
			}

			return this;
		}

		public Duration Subtract(Duration duration)
		{
			if (this.Type == DurationType.TimeSpan && duration.Type == DurationType.TimeSpan)
			{
				return new Duration(this.TimeSpan.Subtract(duration.TimeSpan));
			}

			return this;
		}

		#region Operator overrides

		public static bool operator ==(Duration t1, Duration t2)
		{
			return t1.Equals(t2);
		}

		public static bool operator !=(Duration t1, Duration t2)
		{
			return !Equals(t1, t2);
		}

		public static bool operator >(Duration t1, Duration t2)
		{
			return Compare(t1, t2) > 0;
		}

		public static bool operator >=(Duration t1, Duration t2)
		{
			return Compare(t1, t2) >= 0;
		}

		public static bool operator <(Duration t1, Duration t2)
		{
			return Compare(t1, t2) < 0;
		}

		public static bool operator <=(Duration t1, Duration t2)
		{
			return Compare(t1, t2) <= 0;
		}

		public static Duration operator +(Duration t1, Duration t2)
		{
			return t1.Add(t2);
		}

		public static Duration operator -(Duration t1, Duration t2)
		{
			return t1.Subtract(t2);
		}

		public static implicit operator Duration(TimeSpan timeSpan)
		{
			return new Duration(timeSpan);
		}

		public static Duration operator +(Duration duration)
		{
			return new Duration() { Type = duration.Type, TimeSpan = duration.TimeSpan };
		}

		#endregion

		#region Equality and comparison overrides

		public override int GetHashCode()
		{
			var hashcode = this.Type.GetHashCode();

			if (this.Type == DurationType.TimeSpan)
			{
				hashcode ^= this.TimeSpan.GetHashCode();
			}

			return hashcode;
		}

		public override bool Equals(object value)
		{
			if (value == null)
			{
				return false;
			}
			else if (value is Duration)
			{
				return Equals((Duration)value);
			}
			return false;
		}

		public bool Equals(Duration duration)
		{
			if (HasTimeSpan)
			{
				if (duration.HasTimeSpan)
				{
					return TimeSpan == duration.TimeSpan;
				}
				return false;
			}
			else
			{
				return Type == duration.Type;
			}
		}

		public static bool Equals(Duration first, Duration second)
		{
			return first.Equals(second);

		}

		public int CompareTo(Duration other)
		{
			return Compare(this, other);
		}

		public static int Compare(Duration first, Duration second)
		{
			if (first.Type == second.Type)
			{
				// Both are TimeSpan
				if (first.Type == DurationType.TimeSpan)
				{
					return first.TimeSpan.CompareTo(second.TimeSpan);
				}

				// Both are Automatic or Forever
				return 0;
			}

			// First is Forever and second is not
			if (first.Type == DurationType.Forever)
			{
				return 1;
			}

			// First is Automatic and second is not
			if (first.Type == DurationType.Automatic)
			{
				return -1;
			}

			// First is Timespan and second is Automatic
			if (second.Type == DurationType.Automatic)
			{
				return 1;
			}

			// First is Timespan and second is Forever
			return -1;
		}

		#endregion

		public override string ToString()
		{
			switch (this.Type)
			{
				case DurationType.Automatic:
					return __automatic;
				case DurationType.Forever:
					return __forever;
				case DurationType.TimeSpan:
					return this.TimeSpan.ToXamlString(CultureInfo.InvariantCulture);
				default:
					// Should never happen
					throw new NotSupportedException("This Duration type is not supported.");
			}
		}
	}
}

