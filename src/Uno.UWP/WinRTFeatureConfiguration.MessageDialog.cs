using System.ComponentModel;

namespace Uno;

partial class WinRTFeatureConfiguration
{
	public static class MessageDialog
	{
		/// <summary>
		/// Set this flag to true to use native OS dialogs when displaying MessageDialog.
		/// Note the native dialogs may not support all the features and they are also not
		/// supported on Skia targets.
		/// </summary>
		public static bool UseNativeDialog { get; set; } = false;
	}
}
