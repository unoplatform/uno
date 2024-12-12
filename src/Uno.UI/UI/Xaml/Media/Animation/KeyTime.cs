using Uno.UI.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Windows.UI.Xaml.Media.Animation
{
	public partial struct KeyTime : IEquatable<KeyTime>, IComparable<KeyTime>
	{
		public TimeSpan TimeSpan;

		public static KeyTime FromTimeSpan(TimeSpan timeSpan)
			=> new KeyTime() { TimeSpan = timeSpan };

		public static implicit operator KeyTime(string timeSpan)
			=> FromTimeSpan(TimeSpan.Parse(timeSpan, CultureInfo.InvariantCulture));

		public static implicit operator KeyTime(TimeSpan timeSpan)
			=> FromTimeSpan(timeSpan);

		#region Equality
		public override int GetHashCode()
			=> TimeSpan.GetHashCode();

		public override bool Equals(object obj)
			=> obj is KeyTime other && Equals(this, other);

		public bool Equals(KeyTime other)
			=> Equals(this, other);

		public static bool Equals(KeyTime first, KeyTime second)
			=> first.TimeSpan.Equals(second.TimeSpan);

		public static bool operator ==(KeyTime t1, KeyTime t2)
			=> Equals(t1, t2);

		public static bool operator !=(KeyTime t1, KeyTime t2)
			=> !Equals(t1, t2);
		#endregion

		#region Comparision
		int IComparable<KeyTime>.CompareTo(KeyTime other)
			=> TimeSpan.CompareTo(other.TimeSpan);

		public static bool operator <(KeyTime keytime1, KeyTime keytime2)
			=> keytime1.TimeSpan < keytime2.TimeSpan;

		public static bool operator >(KeyTime keytime1, KeyTime keytime2)
			=> keytime1.TimeSpan > keytime2.TimeSpan;

		public static bool operator <=(KeyTime keytime1, KeyTime keytime2)
			=> keytime1.TimeSpan <= keytime2.TimeSpan;

		public static bool operator >=(KeyTime keytime1, KeyTime keytime2)
			=> keytime1.TimeSpan >= keytime2.TimeSpan;
		#endregion

		public override string ToString()
			=> TimeSpan.ToXamlString(CultureInfo.InvariantCulture);
	}
}
