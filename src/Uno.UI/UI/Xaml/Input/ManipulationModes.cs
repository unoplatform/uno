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
		TranslateX = 1U,
		/// <summary>Permit manipulation actions that translate the target on the Y axis.</summary>
		TranslateY = 2U,
		/// <summary>Permit manipulation actions that translate the target on the X axis but using a rails mode.</summary>
		TranslateRailsX = 4U,
		/// <summary>Permit manipulation actions that translate the target on the Y axis but using a rails mode.</summary>
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
}
