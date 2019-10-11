using System;
using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace Windows.UI.Xaml.Input
{
	[ContractVersion(typeof(UniversalApiContract), 65536U)]
	[Flags]
	[WebHostHidden]
	public enum ManipulationModes : uint
	{
		/// <summary>Do not present graphic interaction with manipulation events.</summary>
		None = 0U,
		/// <summary>Permit manipulation actions that translate the target on the X axis.</summary>
		[global::Uno.NotImplemented]
		TranslateX = 1U,
		/// <summary>Permit manipulation actions that translate the target on the Y axis.</summary>
		[global::Uno.NotImplemented]
		TranslateY = 2U,
		/// <summary>Permit manipulation actions that translate the target on the X axis but using a rails mode.</summary>
		[global::Uno.NotImplemented]
		TranslateRailsX = 4U,
		/// <summary>Permit manipulation actions that translate the target on the Y axis but using a rails mode.</summary>
		[global::Uno.NotImplemented]
		TranslateRailsY = 8U,
		/// <summary>Permit manipulation actions that rotate the target.</summary>
		[global::Uno.NotImplemented]
		Rotate = 16U,
		/// <summary>Permit manipulation actions that scale the target.</summary>
		[global::Uno.NotImplemented]
		Scale = 32U,
		/// <summary>Apply inertia to translate actions.</summary>
		[global::Uno.NotImplemented]
		TranslateInertia = 64U,
		/// <summary>Apply inertia to rotate actions.</summary>
		[global::Uno.NotImplemented]
		RotateInertia = 128U,
		/// <summary>Apply inertia to scale actions.</summary>
		[global::Uno.NotImplemented]
		ScaleInertia = 256U,
		/// <summary>Enable all manipulation interaction modes except those supported through Direct Manipulation</summary>
		All = 65535U,
		/// <summary>Enable system-driven touch interactions supported through Direct Manipulation.</summary>
		System = 65536U,
	}

	internal static class ManipulationModesExtensions
	{
		public static bool IsSupported(this ManipulationModes mode)
			=> mode == ManipulationModes.None
				|| mode >= ManipulationModes.All;
	}
}
