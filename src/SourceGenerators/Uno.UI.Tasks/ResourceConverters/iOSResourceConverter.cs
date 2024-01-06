#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Windows.ApplicationModel.Resources.Core
{
	/// <summary>
	/// Converts a resource candidate to an iOS resource path.
	/// </summary>
	internal static class iOSResourceConverter
	{
		public static string? Convert(ResourceCandidate resourceCandidate, string defaultLanguage)
		{
			try
			{
				ValidatePlatform(resourceCandidate);

				var language = GetLanguage(resourceCandidate);
				var directory = Path.GetDirectoryName(resourceCandidate.LogicalPath);
				var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(resourceCandidate.LogicalPath);
				var scale = GetScale(resourceCandidate);
				var extension = Path.GetExtension(resourceCandidate.LogicalPath).ToLowerInvariant();

				return Path.Combine(language, directory, $"{fileNameWithoutExtension}{scale}{extension}");
			}
			catch (Exception)
			{
				return null;
			}
		}

		private static void ValidatePlatform(ResourceCandidate resourceCandidate)
		{
			var custom = resourceCandidate.GetQualifierValue("custom");

			if (custom != null && custom != "ios")
			{
				throw new NotSupportedException($"Custom qualifier of value {custom} is not supported on iOS.");
			}
		}

		private static string GetLanguage(ResourceCandidate resourceCandidate)
		{
			var language = resourceCandidate.GetQualifierValue("language");

			if (language == null)
			{
				return "";
			}

			if (language.Contains("-"))
			{
				language = language.Replace("-", "_");
			}

			return $"{language}.lproj";
		}

		private static string GetScale(ResourceCandidate resourceCandidate)
		{
			var scale = resourceCandidate.GetQualifierValue("scale");

			switch (scale)
			{
				case null:
				case "100":
					return "";
				case "200":
					return "@2x";
				case "300":
					return "@3x";
				default:
					throw new NotSupportedException($"Scale qualifier of value {scale} is not supported on iOS.");
			}
		}
	}
}
