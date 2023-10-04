using Uno.UI.Extensions;
using System;
using System.Globalization;

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

		public DurationType Type;
		public TimeSpan TimeSpan;

		public static implicit operator Duration(string timeSpan)
			=> timeSpan != null ? new Duration(TimeSpan.Parse(timeSpan, CultureInfo.InvariantCulture)) : new Duration(TimeSpan.Zero);

		public bool HasTimeSpan => Type == DurationType.TimeSpan;

		public static Duration Forever => new Duration() { Type = DurationType.Forever };

		public static Duration Automatic => new Duration() { Type = DurationType.Automatic };

		public Duration Add(Duration duration)
		{
			// We have 9 cases:
			// (1) TimeSpan + TimeSpan
			// (2) TimeSpan + Automatic
			// (3) TimeSpan + Forever
			// (4) Automatic + TimeSpan
			// (5) Automatic + Automatic
			// (6) Automatic + Forever
			// (7) Forever + TimeSpan
			// (8) Forever + Automatic
			// (9) Forever + Forever

			// Case (1)
			if (this.Type == DurationType.TimeSpan && duration.Type == DurationType.TimeSpan)
			{
				return new Duration(this.TimeSpan.Add(duration.TimeSpan));
			}

			// Case (2), (4), (5), (6), (8)
			if (this.Type == DurationType.Automatic || duration.Type == DurationType.Automatic)
			{
				return Automatic;
			}

			// Case (3), (7), (9)
			return Forever;
		}

		public Duration Subtract(Duration duration)
		{
			// We have 9 cases:
			// (1) TimeSpan - TimeSpan     ===> Subtract spans
			// (2) TimeSpan - Automatic    ===> Automatic
			// (3) TimeSpan - Forever	   ===> Automatic
			// (4) Automatic - TimeSpan    ===> Automatic
			// (5) Automatic - Automatic   ===> Automatic
			// (6) Automatic - Forever	   ===> Automatic
			// (7) Forever - TimeSpan      ===> Forever
			// (8) Forever - Automatic     ===> Automatic
			// (9) Forever - Forever       ===> Automatic

			// Case (1)
			if (this.Type == DurationType.TimeSpan && duration.Type == DurationType.TimeSpan)
			{
				return new Duration(this.TimeSpan.Subtract(duration.TimeSpan));
			}

			// Case (7)
			if (this.Type == DurationType.Forever && duration.Type == DurationType.TimeSpan)
			{
				return Forever;
			}

			return Automatic;
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
			if (t1.HasTimeSpan && t2.HasTimeSpan)
			{
				return t1.TimeSpan > t2.TimeSpan;
			}

			if (t1.HasTimeSpan && t2.Type == DurationType.Forever)
			{
				return false;
			}

			if (t1.Type == DurationType.Forever && t2.HasTimeSpan)
			{
				return true;
			}

			return false;
		}

		public static bool operator >=(Duration t1, Duration t2)
		{
			if (t1.Type == DurationType.Automatic && t2.Type == DurationType.Automatic)
			{
				return true;
			}

			if (t1.Type == DurationType.Automatic || t2.Type == DurationType.Automatic)
			{
				return false;
			}

			return !(t1 < t2);
		}

		public static bool operator <(Duration t1, Duration t2)
		{
			if (t1.HasTimeSpan && t2.HasTimeSpan)
			{
				return t1.TimeSpan < t2.TimeSpan;
			}

			if (t1.HasTimeSpan && t2.Type == DurationType.Forever)
			{
				return true;
			}

			return false;
		}

		public static bool operator <=(Duration t1, Duration t2)
		{
			if (t1.Type == DurationType.Automatic && t2.Type == DurationType.Automatic)
			{
				return true;
			}

			if (t1.Type == DurationType.Automatic || t2.Type == DurationType.Automatic)
			{
				return false;
			}

			return !(t1 > t2);
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

