using Uno.UI.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Microsoft.UI.Xaml.Media.Animation
{
	public partial struct KeyTime : IEquatable<KeyTime>, IComparable<KeyTime>
	{
		private KeyTime(TimeSpan timeSpan) => TimeSpan = timeSpan;

		public TimeSpan TimeSpan { get; }

		public static KeyTime FromTimeSpan(TimeSpan timeSpan)
			=> new KeyTime(timeSpan);

		public static implicit operator KeyTime(string timeSpan)
			=> FromTimeSpan(TimeSpan.Parse(timeSpan, CultureInfo.InvariantCulture));

		public static implicit operator KeyTime(TimeSpan timeSpan)
			=> FromTimeSpan(timeSpan);

		#region Equality
		public override int GetHashCode()
			=> TimeSpan.GetHashCode();

		public override bool Equals(object value)
			=> value is KeyTime other && Equals(this, other);

		public bool Equals(KeyTime value)
			=> Equals(this, value);

		public static bool Equals(KeyTime keyTime1, KeyTime keyTime2)
			=> keyTime1.TimeSpan.Equals(keyTime2.TimeSpan);

		public static bool operator ==(KeyTime keyTime1, KeyTime keyTime2)
			=> Equals(keyTime1, keyTime2);

		public static bool operator !=(KeyTime keyTime1, KeyTime keyTime2)
			=> !Equals(keyTime1, keyTime2);
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
