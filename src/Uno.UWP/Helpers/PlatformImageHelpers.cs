using Windows.Graphics.Display;

namespace Uno.Helpers;

internal static partial class PlatformImageHelpers
{
	private static readonly int[] KnownScales =
	{
		(int)ResolutionScale.Scale100Percent,
		(int)ResolutionScale.Scale120Percent,
		(int)ResolutionScale.Scale125Percent,
		(int)ResolutionScale.Scale140Percent,
		(int)ResolutionScale.Scale150Percent,
		(int)ResolutionScale.Scale160Percent,
		(int)ResolutionScale.Scale175Percent,
		(int)ResolutionScale.Scale180Percent,
		(int)ResolutionScale.Scale200Percent,
		(int)ResolutionScale.Scale225Percent,
		(int)ResolutionScale.Scale250Percent,
		(int)ResolutionScale.Scale300Percent,
		(int)ResolutionScale.Scale350Percent,
		(int)ResolutionScale.Scale400Percent,
		(int)ResolutionScale.Scale450Percent,
		(int)ResolutionScale.Scale500Percent
	};

}
