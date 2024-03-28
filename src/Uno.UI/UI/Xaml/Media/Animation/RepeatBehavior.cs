using Uno.UI.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Windows.UI.Xaml.Media.Animation
{
	public partial struct RepeatBehavior : IEquatable<RepeatBehavior>
	{
		public static RepeatBehavior Forever => new RepeatBehavior() { Type = RepeatBehaviorType.Forever };

		private const string ForeverLiteral = "Forever";

		public RepeatBehavior(double count)
		{
			if (double.IsInfinity(count) ||
				double.IsNaN(count) ||
				count < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(count), count, "The count must be a positive number, and not infinity or NaN.");
			}

			Type = RepeatBehaviorType.Count;
			Duration = default;
			Count = count;
		}

		public RepeatBehavior(TimeSpan duration)
		{
			if (duration < TimeSpan.Zero)
			{
				throw new ArgumentOutOfRangeException(nameof(duration), duration, "The duration must be positive.");
			}

			Type = RepeatBehaviorType.Duration;
			Duration = duration;
			Count = 0;
		}

		public double Count;

		public TimeSpan Duration;

		public RepeatBehaviorType Type;

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
				RepeatBehaviorType.Count => Count.ToString(provider) + "x",
				RepeatBehaviorType.Duration => Duration.ToXamlString(provider),
				RepeatBehaviorType.Forever => ForeverLiteral,

				_ => throw new NotSupportedException("This RepeatBehavior type is not supported.")
			};

		public static implicit operator RepeatBehavior(string str)
		{
			// TODO: move this part from runtime, to compile time in XamlCodeGenerator

			// valid syntax: // the 'Forever' and 'x' are case insensitive here
			// <object property="Forever"/>
			// <object property="iterationsx"/>
			// <object property="[days.]hours:minutes:seconds[.fractionalSeconds]"/>

			if (str is null || str.Length == 0)
			{
			}
			else if (string.Equals(str, ForeverLiteral, StringComparison.InvariantCultureIgnoreCase))
			{
				return Forever;
			}
			else if (str.EndsWith('x') || str.EndsWith('X'))
			{
				if (double.TryParse(str.AsSpan().Slice(0, str.Length - 1), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var count))
				{
					return new RepeatBehavior(count);
				}
			}
			else if (TimeSpan.TryParse(str, CultureInfo.InvariantCulture, out var duration)
				// uwp: negative time is acceptable at compilation, but will crash on Storyboard::Begin().
				// lets throw it early here?
				&& duration > TimeSpan.Zero)
			{
				return new RepeatBehavior(duration);
			}

			throw new FormatException($"Failed to create a '{typeof(RepeatBehavior).FullName}' from the text '{str}'.");
		}
	}
}
