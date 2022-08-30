using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml;

public partial class FontFamilyHelper
{
	/// <summary>
	/// Pre-loads a font to minimize loading time and prevent potential text re-layouts.
	/// </summary>
	public static void Preload(FontFamily family)
		=> FontFamily.Preload(family);

	/// <summary>
	/// Pre-loads a font to minimize loading time and prevent potential text re-layouts.
	/// </summary>
	public static void Preload(string familyName)
		=> Preload(new FontFamily(familyName));
}
