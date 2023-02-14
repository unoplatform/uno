using Uno.UI.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Microsoft.UI.Xaml.Media.Animation
{
	public partial struct RepeatBehavior : IEquatable<RepeatBehavior>
	{
		public static RepeatBehavior Forever => new RepeatBehavior() { Type = RepeatBehaviorType.Forever };

		private static readonly string __forever = "Forever";
		private static readonly string __count = "Count";

		public RepeatBehavior(double count)
		{
			if (count <= 0)
			{
				throw new ArgumentOutOfRangeException("count", "Count must be greater than zero.");
			}

			Type = RepeatBehaviorType.Count;
			Duration = default;
			Count = count;
		}

		public RepeatBehavior(TimeSpan duration)
		{
			Type = RepeatBehaviorType.Duration;
			Duration = duration;
			Count = 0;
		}

		public double Count { get; set; }

		public TimeSpan Duration { get; set; }

		public RepeatBehaviorType Type { get; set; }

		public bool HasCount => Type == RepeatBehaviorType.Count;

		public bool HasDuration => Type == RepeatBehaviorType.Duration;

		internal bool ShouldRepeat(TimeSpan elapsed, int count)
			=> Type switch
			{
				RepeatBehaviorType.Count when !double.IsNaN(Count) => Count > count, // will return true if NaN (which is weird but a value accepted by UWP for a RepeatBehavior)
				RepeatBehaviorType.Duration => Duration > elapsed,
				RepeatBehaviorType.Forever => true,
				_ => false
			};

		public override int GetHashCode()
			=> Count.GetHashCode()
				^ Duration.GetHashCode()
				^ Type.GetHashCode();

		public override bool Equals(object value)
			=> value is RepeatBehavior other && Equals(this, other);

		public bool Equals(RepeatBehavior other)
			=> Equals(this, other);

		public static bool operator ==(RepeatBehavior first, RepeatBehavior second)
			=> Equals(first, second);

		public static bool operator !=(RepeatBehavior first, RepeatBehavior second)
			=> !Equals(first, second);

		public static bool Equals(RepeatBehavior first, RepeatBehavior second)
			=> first.Type.Equals(second.Type)
				&& first.Count.Equals(second.Count)
				&& first.Duration.Equals(second.Duration);

		public override string ToString()
			=> ToString(CultureInfo.InvariantCulture);

		public string ToString(IFormatProvider provider)
			=> Type switch
			{
				RepeatBehaviorType.Count => Count.ToString(provider),
				RepeatBehaviorType.Duration => Duration.ToXamlString(provider),
				RepeatBehaviorType.Forever => __forever,
				_ => throw new NotSupportedException("this RepeatBehavior type is not supported.")
			};

		public static implicit operator RepeatBehavior(string str)
		{
			var type = RepeatBehaviorType.Duration;

			if (string.Equals(str, __forever, StringComparison.InvariantCultureIgnoreCase))
			{
				type = RepeatBehaviorType.Forever;
			}
			else if (string.Equals(str, __count, StringComparison.InvariantCultureIgnoreCase))
			{
				type = RepeatBehaviorType.Count;
			}
			return new RepeatBehavior { Type = type };
		}
	}
}
