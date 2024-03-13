using System;
using System.Linq;

namespace Uno
{
	public static partial class CompositionConfiguration
	{
		/// <summary>
		/// Configures the composition options for the whole application.
		/// Refer to uno's documentation to get more info about capabilities and impacts of composition for each platform.
		/// </summary>
		/// <remarks>
		/// Due to the nature of the core feature of composition, this flag must be set as soon as possible in your application startup.
		/// Changing configuration while the application is already running will result to an undefined behavior.
		/// </remarks>
		public static Options Configuration { get; set; } = Options.Disabled;

		[Flags]
		public enum Options
		{
			/// <summary>
			/// Disables all composition capabilities in the application.
			/// </summary>
			Disabled = 0,

			/// <summary>
			/// [ANDROID ONLY] Use a dedicated background thread to render the views.
			/// </summary>
			UseCompositorThread = 0x1,

			/// <summary>
			/// [SKIA ONLY] Use antialiasing for drawing brushes.
			/// </summary>
			UseBrushAntialiasing = 0x2,

			/// <summary>
			/// Enables all composition capabilities for the current platform.
			/// </summary>
			Enabled = UseCompositorThread | UseBrushAntialiasing,
		}

		internal static bool UseCompositorThread
		{
			get
			{
				var value = Configuration.HasFlag(Options.UseCompositorThread);
#if __ANDROID__
				value &= ((int)Android.OS.Build.VERSION.SdkInt) >= 29; // Android 10, for RenderNode which is required for all composition operations!
#endif
				return value;
			}
		}

		internal static bool UseBrushAntialiasing => (Configuration & Options.UseBrushAntialiasing) != 0;
	}
}
