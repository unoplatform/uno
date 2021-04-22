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
			/// Disables all composition supports in the application.
			/// </summary>
			Disabled = 0,

			/// <summary>
			/// [ANDROID ONLY] Use a dedicated background thread to render the views.
			/// </summary>
			UseCompositorThread = 1 << 1,

			/// <summary>
			/// [ANDROID ONLY] Use composition for render transforms.
			/// </summary>
			UseCompositionForTransforms = 1 << 2,

			/// <summary>
			/// [ANDROID ONLY] Use composition for independent animations.
			/// </summary>
			/// <remarks>This flag requires the UseCompositionForTransforms.</remarks>
			UseCompositionForAnimations = 1 << 3 | UseCompositionForTransforms,

			/// <summary>
			/// Enables all composition capabilities for the current platform.
			/// </summary>
			Enabled = UseCompositorThread | UseCompositionForTransforms | UseCompositionForAnimations,
		}


#if __ANDROID__
		private static bool _isCompositionSupported = ((int)Android.OS.Build.VERSION.SdkInt) >= 29; // Android 10, for RenderNode which is required for all composition operations!
#else
		private const bool _isCompositionSupported = true;
#endif

		/// <summary>
		/// Indicates if the app is using it own dedicated compositor thread
		/// </summary>
		internal static bool UseCompositorThread
			=> _isCompositionSupported && Configuration.HasFlag(Options.UseCompositorThread);

		/// <summary>
		/// Indicates if a Visual is being created for each UIElement
		/// </summary>
		internal static bool UseVisual
			=> _isCompositionSupported && Configuration.HasFlag(Options.UseCompositionForTransforms);

		internal static bool UseVisualForLayers => UseVisual;
	}
}
