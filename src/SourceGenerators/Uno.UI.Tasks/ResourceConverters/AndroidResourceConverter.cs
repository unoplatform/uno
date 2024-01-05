#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Uno;
using Uno.UI;

namespace Windows.ApplicationModel.Resources.Core
{
	/// <summary>
	/// Converts a resource candidate to an Android resource path.
	/// </summary>
	internal static class AndroidResourceConverter
	{
		public static string? Convert(ResourceCandidate resourceCandidate, string defaultLanguage)
		{
			try
			{
				ValidatePlatform(resourceCandidate);

				var language = GetLanguage(resourceCandidate.GetQualifierValue("language"), defaultLanguage);
				var dpi = GetDpi(resourceCandidate.GetQualifierValue("scale"));
				var theme = GetTheme(resourceCandidate.GetQualifierValue("theme"));
				var fileName = AndroidResourceNameEncoder.EncodeDrawablePath(resourceCandidate.LogicalPath);

				return Path.Combine($"drawable{language}{theme}{dpi}", fileName);
			}
			catch (Exception)
			{
				return null;
			}
		}

		private static void ValidatePlatform(ResourceCandidate resourceCandidate)
		{
			var custom = resourceCandidate.GetQualifierValue("custom");
			if (custom != null && custom != "android")
			{
				throw new NotSupportedException($"Custom qualifier of value {custom} is not supported on Android.");
			}
		}

		private static string GetLanguage(string? language, string defaultLanguage)
		{
			if (language == null || language == defaultLanguage)
			{
				return "";
			}

			if (language.Contains("-"))
			{
				language = language.Replace("-", "-r");
			}

			return $"-{language}";
		}

		private static string GetDpi(string? scale)
		{
			switch (scale)
			{
				case null:
					return "-nodpi";
				case "100":
					return "-mdpi";
				case "150":
					return "-hdpi";
				case "200":
					return "-xhdpi";
				case "300":
					return "-xxhdpi";
				case "400":
					return "-xxxhdpi";
				default: throw new NotSupportedException($"Scale {scale} is not supported on Android.");
			}
		}

		private static string GetTheme(string? theme)
		{
			switch (theme)
			{
				case null:
				case "light":
					return "";
				case "dark":
					return "-night";
				default: throw new NotSupportedException($"Theme {theme} is not supported on Android");

			}
		}
	}
}
