using System;
using Windows.Foundation;
using Windows.Foundation.Metadata;

using Uno.Foundation.Logging;

using Microsoft.UI.Input;

namespace Microsoft.UI.Xaml.Input
{
	[ContractVersion(typeof(UniversalApiContract), 65536U)]
	[Flags]
	[WebHostHidden]
	public enum ManipulationModes : uint
	{
		/// <summary>Do not present graphic interaction with manipulation events.</summary>
		None = 0U,
		/// <summary>Permit manipulation actions that translate the target on the X axis.</summary>
		TranslateX = 1U,
		/// <summary>Permit manipulation actions that translate the target on the Y axis.</summary>
		TranslateY = 2U,
		/// <summary>Permit manipulation actions that translate the target on the X axis but using a rails mode.</summary>
		[global::Uno.NotImplemented]
		TranslateRailsX = 4U,
		/// <summary>Permit manipulation actions that translate the target on the Y axis but using a rails mode.</summary>
		[global::Uno.NotImplemented]
		TranslateRailsY = 8U,
		/// <summary>Permit manipulation actions that rotate the target.</summary>
		Rotate = 16U,
		/// <summary>Permit manipulation actions that scale the target.</summary>
		Scale = 32U,
		/// <summary>Apply inertia to translate actions.</summary>
		TranslateInertia = 64U,
		/// <summary>Apply inertia to rotate actions.</summary>
		RotateInertia = 128U,
		/// <summary>Apply inertia to scale actions.</summary>
		ScaleInertia = 256U,
		/// <summary>Enable all manipulation interaction modes except those supported through Direct Manipulation</summary>
		All = 65535U,
		/// <summary>Enable system-driven touch interactions supported through Direct Manipulation.</summary>
		System = 65536U,
	}

	internal static class ManipulationModesExtensions
	{
		/// <summary>
		/// Converts the given <see cref="ManipulationModes"/> to a <see cref="GestureSettings"/>. cf. remarks.
		/// </summary>
		/// <remarks>
		/// The returned <see cref="GestureSettings"/> will contains only manipulation related flags
		/// as defined in <see cref="GestureSettingsHelper.Manipulations"/>
		/// </remarks>
		/// <param name="mode">The manipulation mode to convert</param>
		/// <returns>The gesture settings with the corresponding manipulation flags set.</returns>
		public static GestureSettings ToGestureSettings(this ManipulationModes mode)
		{
			var settings = default(GestureSettings);
			if (mode.HasFlag(ManipulationModes.TranslateX))
			{
				settings |= GestureSettings.ManipulationTranslateX;
			}
			if (mode.HasFlag(ManipulationModes.TranslateY))
			{
				settings |= GestureSettings.ManipulationTranslateY;
			}
			if (mode.HasFlag(ManipulationModes.TranslateRailsX))
			{
				settings |= GestureSettings.ManipulationTranslateRailsX;
			}
			if (mode.HasFlag(ManipulationModes.TranslateRailsY))
			{
				settings |= GestureSettings.ManipulationTranslateRailsY;
			}
			if (mode.HasFlag(ManipulationModes.TranslateInertia))
			{
				settings |= GestureSettings.ManipulationTranslateInertia;
			}
			if (mode.HasFlag(ManipulationModes.Rotate))
			{
				settings |= GestureSettings.ManipulationRotate;
			}
			if (mode.HasFlag(ManipulationModes.RotateInertia))
			{
				settings |= GestureSettings.ManipulationRotateInertia;
			}
			if (mode.HasFlag(ManipulationModes.Scale))
			{
				settings |= GestureSettings.ManipulationScale;
			}
			if (mode.HasFlag(ManipulationModes.ScaleInertia))
			{
				settings |= GestureSettings.ManipulationScaleInertia;
			}

			// Note: ManipulationMultipleFingerPanning is not supported by ManipulationModes enumeration

			return settings;
		}
	}
}
