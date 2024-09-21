using System.ComponentModel;

namespace Uno;

public static class FoundationFeatureConfiguration
{
	/// <summary>
	/// Used by tests cleanup to restore the default configuration for other tests!
	/// </summary>
	internal static void RestoreDefaults()
	{
		Rect.RestoreDefaults();
	}

	public static class Rect
	{
		internal static void RestoreDefaults()
		{
			AllowNegativeWidthHeight = _defaultAllowNegativeWidthHeight;
		}

		private const bool _defaultAllowNegativeWidthHeight = true;
		/// <summary>
		/// If this flag is set to true, the <see cref="Windows.Foundation.Rect"/> won't throw an exception
		/// if it's been created with a negative width / height.
		/// This should be kept to `true` until https://github.com/unoplatform/uno/issues/606 get fixed.
		/// </summary>
		/// <remarks>This hides some errors from invalid measure/arrange which have to be fixed!</remarks>
		[DefaultValue(_defaultAllowNegativeWidthHeight)]
		public static bool AllowNegativeWidthHeight { get; set; } = _defaultAllowNegativeWidthHeight;
	}

#if __WASM__
	public static class Runtime
	{
		/// <summary>
		/// [Deprecated] Indicates if exception thrown in javascript should be rethrown in managed code.
		/// </summary>
		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		public static bool RethrowNativeExceptions
		{
			// Obsolete, remove in next major
			get => true;
			set { }
		}
	}
#endif
}
