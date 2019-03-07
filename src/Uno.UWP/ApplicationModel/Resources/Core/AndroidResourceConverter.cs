using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Uno.Extensions;
using Uno.Logging;

namespace Windows.ApplicationModel.Resources.Core
{
	/// <summary>
	/// Converts a resource candidate to an Android resource path.
	/// </summary>
	internal class AndroidResourceConverter
	{
		private static readonly Regex ValidResourceName = new Regex("^[_a-zA-Z][_a-zA-Z0-9]*$", RegexOptions.Compiled);

		public static string Convert(ResourceCandidate resourceCandidate, string defaultLanguage)
		{
			try
			{
				ValidatePlatform(resourceCandidate);
				ValidateResourceName(resourceCandidate);

				var language = GetLanguage(resourceCandidate.GetQualifierValue("language"), defaultLanguage);
				var dpi = GetDpi(resourceCandidate.GetQualifierValue("scale"));
				var fileName = Path.GetFileName(resourceCandidate.LogicalPath);
				
				return Path.Combine($"drawable{language}{dpi}", fileName);
			}
			catch (Exception ex)
			{
				ex.Log().Info($"Couldn't convert {resourceCandidate.ValueAsString} to an Android resource path.", ex);
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

		private static void ValidateResourceName(ResourceCandidate resourceCandidate)
		{
			var resourceName = Path.GetFileName(resourceCandidate.LogicalPath).Split(new char[] { '.' })[0];
			
			if (string.IsNullOrWhiteSpace(resourceName) || !ValidResourceName.IsMatch(resourceName))
			{
				throw new NotSupportedException($"Resource name {resourceName} is not supported on Android.");
			}
		}

		private static string GetLanguage(string language, string defaultLanguage)
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

		private static string GetDpi(string scale)
		{
			switch (scale)
			{
				case null:
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
	}
}
