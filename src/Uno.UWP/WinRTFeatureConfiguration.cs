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
	}
}
