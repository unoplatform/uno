using Uno;

namespace Microsoft.Toolkit.Uwp.UI.Lottie
{
	[NotImplemented]
	public enum LottieVisualOptions
	{
		/// <summary>No options set.</summary>
		None = 0,

		/// <summary>
		/// Optimizes the translation of the Lottie so as to reduce resource
		/// usage during rendering. Note that this may slow down loading.
		/// </summary>
		[NotImplemented]
		Optimize = 1,

		/// <summary>
		/// Sets the AnimatedVisualPlayer.Diagnostics property with information
		/// about the Lottie.
		/// </summary>
		[NotImplemented]
		IncludeDiagnostics = 2,

		/// <summary>Enables all options.</summary>
		[NotImplemented]
		All = IncludeDiagnostics | Optimize,
	}
}
