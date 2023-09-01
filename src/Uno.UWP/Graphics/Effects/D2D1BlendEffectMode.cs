using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.Graphics.Effects
{
	/// <summary>
	/// This enum specifies Direct2D blend modes supported by Composition APIs<br/>
	/// Note that Color and Luminosity values are switched to follow the current behavior (which is a bug) on Windows<br/><br/>
	/// <remarks>
	/// References:<br/>
	///		1- <see href="https://microsoft.github.io/Win2D/WinUI2/html/T_Microsoft_Graphics_Canvas_Effects_BlendEffectMode.htm"/><br/>
	///		2- wuceffects.dll!Windows::UI::Composition::g_pszModeNames
	///	</remarks>
	/// </summary>
	internal enum D2D1BlendEffectMode
	{
		Multiply = 0,
		Screen = 1,
		Darken = 2,
		Lighten = 3,
		//Dissolve = 4, // Note: Composition doesn't support Dissolve yet (as of 10.0.25941.1000)
		ColorBurn = 5,
		LinearBurn = 6,
		DarkerColor = 7,
		LighterColor = 8,
		ColorDodge = 9,
		LinearDodge = 10,
		Overlay = 11,
		SoftLight = 12,
		HardLight = 13,
		VividLight = 14,
		LinearLight = 15,
		PinLight = 16,
		HardMix = 17,
		Difference = 18,
		Exclusion = 19,
		Hue = 20, // Note: Composition supports Hue since 19H1, docs are outdated
		Saturation = 21, // Note: Composition supports Saturation since 19H1, docs are outdated
		Luminosity = 22, // Note: Composition supports Luminosity since 19H1, docs are outdated
		Color = 23, // Note: Composition supports Color since 19H1, docs are outdated
		Subtract = 24,
		Division = 25
	}
}
