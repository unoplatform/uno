using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI
{
    internal static class AndroidResourceNameEncoder
    {
		private const string NumberPrefix = "__";
		// These characters are not supported on Android, but they're used by the attached property localization syntax.
		// Example: "MyUid.[using:Windows.UI.Xaml.Automation]AutomationProperties.Name"
		private static char[] UnsupportedCharacters = new char[] { '[', ']', ':', '-' };

		/// <summary>
		/// Encode a resource name to remove characters that are not supported on Android.
		/// </summary>
		/// <param name="key">The original resource name from the UWP Resources.resw file.</param>
		/// <returns>The encoded resource name for the Android Strings.xml file.</returns>
		public static string Encode(string key)
		{
			// Checks whether the key contains unsupported characters
			// to avoid the unecessary overhead of string.Replace in the majority of cases.
			if (key.IndexOfAny(UnsupportedCharacters) != -1)
			{
				foreach (var unsupportedCharacter in UnsupportedCharacters)
				{
					key = key.Replace(unsupportedCharacter, '_');
				}
			}

			//Checks if the keys are starting by a number because they are invalid in C#
			if (int.TryParse(key.Substring(0,1), out var number))
			{
				key = $"{NumberPrefix}{key}";
			}

			return key;
		}
	}
}
