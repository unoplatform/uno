using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Uno.Extensions;

namespace Windows.UI.Xaml
{
    public partial struct GridLength : IEquatable<GridLength>
    {
        public static GridLength Auto => GridLengthHelper.Auto;

        public GridUnitType GridUnitType { get; private set; }

        public bool IsAbsolute { get { return GridUnitType == Xaml.GridUnitType.Pixel; } }

        public bool IsAuto { get { return GridUnitType == Xaml.GridUnitType.Auto; } }

        public bool IsStar { get { return GridUnitType == Xaml.GridUnitType.Star; } }

        public double Value { get; private set; }

        public static implicit operator GridLength(string value)
        {
            var result = GridLength.ParseGridLength(value);

            if (result.Length == 0)
            {
                throw new InvalidOperationException("Cannot create GridLength from invalid string [{0}]".InvariantCultureFormat(value));
            }

            return result[0];
        }

        public GridLength(double pixels) : this(pixels, GridUnitType.Pixel)
        {
        }

        public GridLength(double value, GridUnitType gridUnitType)
		{
			Value = value;
			GridUnitType = gridUnitType;
		}
		
		private static readonly Regex GridLengthParsingRegex =
			new Regex(
				@"^(?:(?<stars>\d*(?:.\d*))\*)|(?<abs>\d+(?:[.\d$]*))|(?<auto>Auto)|(?<star>\*)$",
#if (NETFX_CORE || XAMARIN) || (SILVERLIGHT && !WINDOWS_PHONE)
				RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline);
#else
				RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.Compiled);
#endif


		public static GridLength[] ParseGridLength(string s)
		{
			var parts = s.Split(',');

			var result = new List<GridLength>(parts.Length);

			foreach (var part in parts)
			{
				if (string.IsNullOrEmpty(part))
				{
					result.Add(new GridLength(0, GridUnitType.Auto));
					continue;
				}

				var match = GridLengthParsingRegex.Match(part);
				if (!match.Success)
				{
					throw new InvalidOperationException("Invalid value '" + part + "', unable to parse.");
				}

				var autoGroup = match.Groups["auto"];
				if (autoGroup.Success)
				{
					result.Add(new GridLength(0, GridUnitType.Auto));
					continue;
				}

				var starsGroup = match.Groups["stars"];
				if (starsGroup.Success)
				{
					var value =
						!string.IsNullOrWhiteSpace(starsGroup.Value)
							? double.Parse(starsGroup.Value, CultureInfo.InvariantCulture)
							: 1;
					result.Add(new GridLength(value, GridUnitType.Star));
					continue;
				}

				var starGroup = match.Groups["star"];
				if (starGroup.Success)
				{
					result.Add(new GridLength(1, GridUnitType.Star));
					continue;
				}

				var absGroup = match.Groups["abs"];
				if (absGroup.Success)
				{
					var value = double.Parse(absGroup.Value, CultureInfo.InvariantCulture);
					result.Add(new GridLength(value, GridUnitType.Pixel));
					continue;
				}

				throw new Exception("Unknown parsing error");
			}

			return result.ToArray();
		}

		public bool Equals(GridLength other) => 
			other.Value == Value && other.GridUnitType == GridUnitType;
	}
}
