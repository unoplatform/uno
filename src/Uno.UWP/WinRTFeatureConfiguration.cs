#nullable disable

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
			/// <see cref="Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride"/> takes effect (by changing current culture) during its setter execution.
			/// On Windows, changing this property *may* take effect only after restarting the application.
			/// Set this property to false to require an app restart for the change to take effect. The default of the property is true.
			/// </summary>
			public static bool UseLegacyPrimaryLanguageOverride { get; set; } = true;
		}
	}
}
