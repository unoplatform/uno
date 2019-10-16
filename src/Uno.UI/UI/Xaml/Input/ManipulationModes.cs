using System;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Microsoft.Extensions.Logging;
using Uno.Logging;

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
		private const ManipulationModes _unsupported =
			ManipulationModes.TranslateRailsX
			| ManipulationModes.TranslateRailsY
			| ManipulationModes.TranslateInertia
			| ManipulationModes.RotateInertia
			| ManipulationModes.ScaleInertia;

		public static bool IsSupported(this ManipulationModes mode)
			=> mode == ManipulationModes.All
			|| (mode & _unsupported) == 0;

		public static void LogIfNotSupported(this ManipulationModes mode, ILogger log)
		{
			if (!mode.IsSupported() && log.IsEnabled(LogLevel.Information))
			{
				log.Warn(
					$"The ManipulationMode '{mode}' is not supported by Uno. "
					+ "Only 'None', 'All', 'System', 'TranslateX', 'TranslateY', 'Rotate', and 'Scale' are supported. "
					+ "Any other mode will not cause any issue, but the corresponding but no manipulation event will be generated form them. "
					+ "Note that with Uno the 'All' and 'System' are handled the same way.");
			}
		}
	}
}
