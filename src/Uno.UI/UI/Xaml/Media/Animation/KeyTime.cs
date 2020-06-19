using Uno.UI.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Windows.UI.Xaml.Media.Animation
{
	public partial struct KeyTime : IEquatable<KeyTime>, IComparable<KeyTime>
	{
		public TimeSpan TimeSpan { get; private set; }

		public static KeyTime FromTimeSpan(TimeSpan timeSpan)
		{
			return new KeyTime() { TimeSpan = timeSpan };
		}

		public static implicit operator KeyTime(string timeSpan)
			=> FromTimeSpan(TimeSpan.Parse(timeSpan));

		public static bool operator ==(KeyTime t1, KeyTime t2)
		{
			return Equals(t1, t2);
		}

		public static bool operator !=(KeyTime t1, KeyTime t2)
		{
			return !Equals(t1, t2);
		}

		public static implicit operator KeyTime(TimeSpan timeSpan)
		{
			return FromTimeSpan(timeSpan);
		}

		#region Equality overrides

		public override int GetHashCode()
		{
			return this.TimeSpan.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj is KeyTime)
			{
				return this.Equals((KeyTime)obj);
			}

			return false;
		}

		public bool Equals(KeyTime other)
		{
			return KeyTime.Equals(this, other);
		}

		public static bool Equals(KeyTime first, KeyTime second)
		{
			return first.TimeSpan.Equals(second.TimeSpan);
		}

		#endregion

		public override string ToString()
		{
			return this.TimeSpan.ToXamlString(CultureInfo.InvariantCulture);
		}

		int IComparable<KeyTime>.CompareTo(KeyTime other) => TimeSpan.CompareTo(other.TimeSpan);

		public static bool operator <(KeyTime keytime1, KeyTime keytime2) => keytime1.TimeSpan < keytime2.TimeSpan;
		public static bool operator >(KeyTime keytime1, KeyTime keytime2) => keytime1.TimeSpan > keytime2.TimeSpan;
		public static bool operator <=(KeyTime keytime1, KeyTime keytime2) => keytime1.TimeSpan <= keytime2.TimeSpan;
		public static bool operator >=(KeyTime keytime1, KeyTime keytime2) => keytime1.TimeSpan >= keytime2.TimeSpan;
	}
}
