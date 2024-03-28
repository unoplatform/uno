using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Uno.Extensions;

namespace Windows.UI.Xaml
{
	[DebuggerDisplay("{DebugDisplay,nq}")]
	public partial struct GridLength : IEquatable<GridLength>
	{
		private static readonly char[] _commaArray = new[] { ',' };
		public static GridLength Auto => GridLengthHelper.Auto;

		public GridUnitType GridUnitType;
		public double Value;

		public bool IsAbsolute { get { return GridUnitType == Xaml.GridUnitType.Pixel; } }

		public bool IsAuto { get { return GridUnitType == Xaml.GridUnitType.Auto; } }

		public bool IsStar { get { return GridUnitType == Xaml.GridUnitType.Star; } }

		public static implicit operator GridLength(string value)
			=> FromString(value);

		public static implicit operator GridLength(double value)
			=> new GridLength(value);

		public GridLength(double pixels) : this(pixels, GridUnitType.Pixel)
		{
		}

		public GridLength(double value, GridUnitType gridUnitType)
		{
			if (double.IsNaN(value) || double.IsInfinity(value) || value < 0.0 ||
				(gridUnitType != GridUnitType.Auto &&
				 gridUnitType != GridUnitType.Pixel &&
				 gridUnitType != GridUnitType.Star))
			{
				throw new ArgumentException($"Invalid GridLength {value}{gridUnitType}.", nameof(value));
			}

			Value = (gridUnitType == GridUnitType.Auto) ? 1.0 : value;
			GridUnitType = gridUnitType;
		}

		public static GridLength FromString(string s)
		{
			var trimmed = s.Trim();

			if (trimmed == "*")
			{
				return new GridLength(1, GridUnitType.Star);
			}
			else if (trimmed.Equals("auto", StringComparison.OrdinalIgnoreCase))
			{
				return new GridLength(0, GridUnitType.Auto);
			}
#if NETSTANDARD
			else if (trimmed.EndsWith("*", StringComparison.Ordinal))
#else
			else if (trimmed.EndsWith('*'))
#endif
			{
				var stringValue = trimmed.Substring(0, trimmed.Length - 1);

				if (double.TryParse(stringValue, NumberStyles.Any & ~NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var value))
				{
					return new GridLength(value, GridUnitType.Star);
				}
				else
				{
					throw new InvalidOperationException($"The value [{trimmed}] is not a valid GridLength");
				}
			}
			else
			{
				if (trimmed.EndsWith("px", StringComparison.OrdinalIgnoreCase) ||
					trimmed.EndsWith("cm", StringComparison.OrdinalIgnoreCase) ||
					trimmed.EndsWith("in", StringComparison.OrdinalIgnoreCase) ||
					trimmed.EndsWith("pt", StringComparison.OrdinalIgnoreCase))
				{
					trimmed = trimmed.Substring(0, trimmed.Length - 2);
				}

				if (double.TryParse(trimmed, NumberStyles.Any & ~NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var value))
				{
					return new GridLength(value, GridUnitType.Pixel);
				}
				else
				{
					throw new InvalidOperationException($"The value [{trimmed}] is not a valid GridLength");
				}
			}
		}

		public static GridLength[] ParseGridLength(string s)
		{
			var parts = s.Split(_commaArray);

			var result = new GridLength[parts.Length];

			for (int i = 0; i < parts.Length; i++)
			{
				var part = parts[i];
				if (string.IsNullOrEmpty(part))
				{
					result[i] = new GridLength(0, GridUnitType.Auto);
					continue;
				}

				result[i] = FromString(part);
			}

			return result;
		}

		public bool Equals(GridLength other)
		{
			if (other.GridUnitType == GridUnitType)
			{
				if (GridUnitType == GridUnitType.Auto)
				{
					return true;
				}
				else
				{
					return other.Value == Value;
				}
			}
			else
			{
				return false;
			}
		}

		public override bool Equals(object obj) => obj is GridLength other ? Equals(other) : false;

		public override int GetHashCode() => GridUnitType.GetHashCode() ^ Value.GetHashCode();

		public static bool operator ==(GridLength gl1, GridLength gl2) => gl1.Equals(gl2);
		public static bool operator !=(GridLength gl1, GridLength gl2) => !gl1.Equals(gl2);

		private string DebugDisplay => ToDisplayString();

		internal readonly string ToDisplayString() => $"GridLength({this})";

		public override string ToString() =>
			GridUnitType switch
			{
				GridUnitType.Auto => "Auto",
				GridUnitType.Pixel => $"{Value:f1}px",
				GridUnitType.Star when Value == 1.0 => "*",
				GridUnitType.Star => $"{Value:f1}*",
				_ => "invalid"
			};
	}
}
