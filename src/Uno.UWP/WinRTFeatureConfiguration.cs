using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Uno
{
	public static partial class WinRTFeatureConfiguration
	{
		/// <summary>
		/// Used by tests cleanup to restore the default configuration for other tests!
		/// </summary>
		internal static void RestoreDefaults()
		{
			GestureRecognizer.RestoreDefaults();
		}

		public static class ApplicationLanguages
		{
			/// <summary>
			/// <see cref="Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride"/> used to take effect (by changing current culture) during its setter.
			/// On Windows, changing this property only takes effect after restarting the application. We changed the default behavior as a breaking change to match Windows.
			/// Set this property to true to get the old behavior.
			/// </summary>
			public static bool UseLegacyPrimaryLanguageOverride { get; set; }
		}
	}
}
