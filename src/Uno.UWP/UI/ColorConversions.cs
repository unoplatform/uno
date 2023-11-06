// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// Partially ported
// MUX Reference ColorConversions.cpp, tag winui3/release/1.4.2

using System;
using System.Globalization;
namespace Windows.UI;

internal struct KnownColor
{
	string m_strColorStorage;
	KnownColors m_rgb;

	public KnownColor(string mStrColorStorage, KnownColors mRgb)
	{
		m_strColorStorage = mStrColorStorage;
		m_rgb = mRgb;
	}
}

internal class ColorConversions
{
	private static KnownColor[] sakc =
	{
		new KnownColor("AliceBlue", KnownColors.AliceBlue),
	    new KnownColor("AntiqueWhite", KnownColors.AntiqueWhite),
	    new KnownColor("Aqua", KnownColors.Aqua),
	    new KnownColor("Aquamarine", KnownColors.Aquamarine),
	    new KnownColor("Azure", KnownColors.Azure),
	    new KnownColor("Beige", KnownColors.Beige),
	    new KnownColor("Bisque", KnownColors.Bisque),
	    new KnownColor("Black", KnownColors.Black),
	    new KnownColor("BlanchedAlmond", KnownColors.BlanchedAlmond),
	    new KnownColor("Blue", KnownColors.Blue),
	    new KnownColor("BlueViolet", KnownColors.BlueViolet),
	    new KnownColor("Brown", KnownColors.Brown),
	    new KnownColor("BurlyWood", KnownColors.BurlyWood),
	    new KnownColor("CadetBlue", KnownColors.CadetBlue),
	    new KnownColor("Chartreuse", KnownColors.Chartreuse),
	    new KnownColor("Chocolate", KnownColors.Chocolate),
	    new KnownColor("Coral", KnownColors.Coral),
	    new KnownColor("CornflowerBlue", KnownColors.CornflowerBlue),
	    new KnownColor("Cornsilk", KnownColors.Cornsilk),
	    new KnownColor("Crimson", KnownColors.Crimson),
	    new KnownColor("Cyan", KnownColors.Cyan),
	    new KnownColor("DarkBlue", KnownColors.DarkBlue),
	    new KnownColor("DarkCyan", KnownColors.DarkCyan),
	    new KnownColor("DarkGoldenrod", KnownColors.DarkGoldenrod),
	    new KnownColor("DarkGray", KnownColors.DarkGray),
	    new KnownColor("DarkGreen", KnownColors.DarkGreen),
	    new KnownColor("DarkKhaki", KnownColors.DarkKhaki),
	    new KnownColor("DarkMagenta", KnownColors.DarkMagenta),
	    new KnownColor("DarkOliveGreen", KnownColors.DarkOliveGreen),
	    new KnownColor("DarkOrange", KnownColors.DarkOrange),
	    new KnownColor("DarkOrchid", KnownColors.DarkOrchid),
	    new KnownColor("DarkRed", KnownColors.DarkRed),
	    new KnownColor("DarkSalmon", KnownColors.DarkSalmon),
	    new KnownColor("DarkSeaGreen", KnownColors.DarkSeaGreen),
	    new KnownColor("DarkSlateBlue", KnownColors.DarkSlateBlue),
	    new KnownColor("DarkSlateGray", KnownColors.DarkSlateGray),
	    new KnownColor("DarkTurquoise", KnownColors.DarkTurquoise),
	    new KnownColor("DarkViolet", KnownColors.DarkViolet),
	    new KnownColor("DeepPink", KnownColors.DeepPink),
	    new KnownColor("DeepSkyBlue", KnownColors.DeepSkyBlue),
	    new KnownColor("DimGray", KnownColors.DimGray),
	    new KnownColor("DodgerBlue", KnownColors.DodgerBlue),
	    new KnownColor("Firebrick", KnownColors.Firebrick),
	    new KnownColor("FloralWhite", KnownColors.FloralWhite),
	    new KnownColor("ForestGreen", KnownColors.ForestGreen),
	    new KnownColor("Fuchsia", KnownColors.Fuchsia),
	    new KnownColor("Gainsboro", KnownColors.Gainsboro),
	    new KnownColor("GhostWhite", KnownColors.GhostWhite),
	    new KnownColor("Gold", KnownColors.Gold),
	    new KnownColor("Goldenrod", KnownColors.Goldenrod),
	    new KnownColor("Gray", KnownColors.Gray),
	    new KnownColor("Green", KnownColors.Green),
	    new KnownColor("GreenYellow", KnownColors.GreenYellow),
	    new KnownColor("Honeydew", KnownColors.Honeydew),
	    new KnownColor("HotPink", KnownColors.HotPink),
	    new KnownColor("IndianRed", KnownColors.IndianRed),
	    new KnownColor("Indigo", KnownColors.Indigo),
	    new KnownColor("Ivory", KnownColors.Ivory),
	    new KnownColor("Khaki", KnownColors.Khaki),
	    new KnownColor("Lavender", KnownColors.Lavender),
	    new KnownColor("LavenderBlush", KnownColors.LavenderBlush),
	    new KnownColor("LawnGreen", KnownColors.LawnGreen),
	    new KnownColor("LemonChiffon", KnownColors.LemonChiffon),
	    new KnownColor("LightBlue", KnownColors.LightBlue),
	    new KnownColor("LightCoral", KnownColors.LightCoral),
	    new KnownColor("LightCyan", KnownColors.LightCyan),
	    new KnownColor("LightGoldenrodYellow", KnownColors.LightGoldenrodYellow),
	    new KnownColor("LightGray", KnownColors.LightGray),
	    new KnownColor("LightGreen", KnownColors.LightGreen),
	    new KnownColor("LightPink", KnownColors.LightPink),
	    new KnownColor("LightSalmon", KnownColors.LightSalmon),
	    new KnownColor("LightSeaGreen", KnownColors.LightSeaGreen),
	    new KnownColor("LightSkyBlue", KnownColors.LightSkyBlue),
	    new KnownColor("LightSlateGray", KnownColors.LightSlateGray),
	    new KnownColor("LightSteelBlue", KnownColors.LightSteelBlue),
	    new KnownColor("LightYellow", KnownColors.LightYellow),
	    new KnownColor("Lime", KnownColors.Lime),
	    new KnownColor("LimeGreen", KnownColors.LimeGreen),
	    new KnownColor("Linen", KnownColors.Linen),
	    new KnownColor("Magenta", KnownColors.Magenta),
	    new KnownColor("Maroon", KnownColors.Maroon),
	    new KnownColor("MediumAquamarine", KnownColors.MediumAquamarine),
	    new KnownColor("MediumBlue", KnownColors.MediumBlue),
	    new KnownColor("MediumOrchid", KnownColors.MediumOrchid),
	    new KnownColor("MediumPurple", KnownColors.MediumPurple),
	    new KnownColor("MediumSeaGreen", KnownColors.MediumSeaGreen),
	    new KnownColor("MediumSlateBlue", KnownColors.MediumSlateBlue),
	    new KnownColor("MediumSpringGreen", KnownColors.MediumSpringGreen),
	    new KnownColor("MediumTurquoise", KnownColors.MediumTurquoise),
	    new KnownColor("MediumVioletRed", KnownColors.MediumVioletRed),
	    new KnownColor("MidnightBlue", KnownColors.MidnightBlue),
	    new KnownColor("MintCream", KnownColors.MintCream),
	    new KnownColor("MistyRose", KnownColors.MistyRose),
	    new KnownColor("Moccasin", KnownColors.Moccasin),
	    new KnownColor("NavajoWhite", KnownColors.NavajoWhite),
	    new KnownColor("Navy", KnownColors.Navy),
	    new KnownColor("OldLace", KnownColors.OldLace),
	    new KnownColor("Olive", KnownColors.Olive),
	    new KnownColor("OliveDrab", KnownColors.OliveDrab),
	    new KnownColor("Orange", KnownColors.Orange),
	    new KnownColor("OrangeRed", KnownColors.OrangeRed),
	    new KnownColor("Orchid", KnownColors.Orchid),
	    new KnownColor("PaleGoldenrod", KnownColors.PaleGoldenrod),
	    new KnownColor("PaleGreen", KnownColors.PaleGreen),
	    new KnownColor("PaleTurquoise", KnownColors.PaleTurquoise),
	    new KnownColor("PaleVioletRed", KnownColors.PaleVioletRed),
	    new KnownColor("PapayaWhip", KnownColors.PapayaWhip),
	    new KnownColor("PeachPuff", KnownColors.PeachPuff),
	    new KnownColor("Peru", KnownColors.Peru),
	    new KnownColor("Pink", KnownColors.Pink),
	    new KnownColor("Plum", KnownColors.Plum),
	    new KnownColor("PowderBlue", KnownColors.PowderBlue),
	    new KnownColor("Purple", KnownColors.Purple),
	    new KnownColor("Red", KnownColors.Red),
	    new KnownColor("RosyBrown", KnownColors.RosyBrown),
	    new KnownColor("RoyalBlue", KnownColors.RoyalBlue),
	    new KnownColor("SaddleBrown", KnownColors.SaddleBrown),
	    new KnownColor("Salmon", KnownColors.Salmon),
	    new KnownColor("SandyBrown", KnownColors.SandyBrown),
	    new KnownColor("SeaGreen", KnownColors.SeaGreen),
	    new KnownColor("SeaShell", KnownColors.SeaShell),
	    new KnownColor("Sienna", KnownColors.Sienna),
	    new KnownColor("Silver", KnownColors.Silver),
	    new KnownColor("SkyBlue", KnownColors.SkyBlue),
	    new KnownColor("SlateBlue", KnownColors.SlateBlue),
	    new KnownColor("SlateGray", KnownColors.SlateGray),
	    new KnownColor("Snow", KnownColors.Snow),
	    new KnownColor("SpringGreen", KnownColors.SpringGreen),
	    new KnownColor("SteelBlue", KnownColors.SteelBlue),
	    new KnownColor("Tan", KnownColors.Tan),
	    new KnownColor("Teal", KnownColors.Teal),
	    new KnownColor("Thistle", KnownColors.Thistle),
	    new KnownColor("Tomato", KnownColors.Tomato),
	    new KnownColor("Transparent", KnownColors.Transparent),
	    new KnownColor("Turquoise", KnownColors.Turquoise),
	    new KnownColor("Violet", KnownColors.Violet),
	    new KnownColor("Wheat", KnownColors.Wheat),
	    new KnownColor("White", KnownColors.White),
	    new KnownColor("WhiteSmoke", KnownColors.WhiteSmoke),
	    new KnownColor("Yellow", KnownColors.Yellow),
	    new KnownColor("YellowGreen", KnownColors.YellowGreen)
	};

	// Uno Doc: partially ported
	// public static void ColorFromString(
	// string str,
	// out UInt32 prgbResult
	// )
	// {
	//     //  Implementation details:
	//     //      Colors can be listed in several forms.
	//     //      1) Hex value (e.g. #e0708853)
	//     //      2) scRGB floats (e.g. sc#0.5,0.75,0.0)
	//     //      3) Known color names (e.g. Red, Olive, MediumBlue)
	//     //      4) Context color (e.g. ContextColor foo,bar,huh)
	//     //
	//     //  Currently we don't deal with case 4.
	//
	// 	// Uno Doc: this implementation diverges a bit from WinUI due to the lack of pointer arithmetic and
	// 	// "strings as a character pointer/array" in c#. In our implementation, we use a ReadOnlySpan instead
	// 	// of manipulating a pointer to the first character (pString).
	//
	//     // HRESULT hr = E_UNEXPECTED;
	//     UInt32 rgb = 0;
	//     // UINT32 cString;
	//     // const WCHAR* pString = str.GetBufferAndCount(&cString);
	// 	int cString = str.Length;
	// 	ReadOnlySpan<char> pString = str;
	//
	//     // Skip leading spaces
	// 	// TrimWhitespace(cString, pString, &cString, &pString);
	// 	pString = pString.TrimStart();
	// 	cString = pString.Length;
	//
	//     // Check for hex formatted colors
	//
	// 	// if (cString && (L'#' == *pString))
	// 	if (str.Length > 0 && ('#' == pString[0]))
	//     {
	//         pString = pString[1..];
	//         cString--;
	//
	//         UInt32 hex;
	//         UInt32 tmp;
	//
	//         int cPrevious = cString;
	//
	// 		// IFC(UnsignedFromHexString(cString, pString, &cString, &pString, &hex));
	// 		StringConversions.UnsignedFromHexString(cString, pString, out cString, out pString, out hex);
	//
	//         // There are only a limited number of valid hex formats.  One digit each
	//         // for red, green, and blue with one digit of optional alpha or two digits
	//         // each for the colors and the optional alpha.  Ignoring the hash mark this
	//         // gives only four valid lengths to consider (3, 4, 6, and 8).
	//
	//         switch (cPrevious - cString)
	//         {
	//         case 3: // rgb
	//             rgb = 0xff000000;                  // Make fully opaque
	//             tmp = (hex & 0x0f00);              // Mask out red value
	//             tmp *= 17;                          // Scale by 0x11
	//             rgb |= (tmp << 8);                  // Store it
	//             tmp = (hex & 0x00f0);              // Mask out green value
	//             tmp *= 17;                          // Scale by 0x11
	//             rgb |= (tmp << 4);                  // Store it
	//             tmp = (hex & 0x000f);              // Mask out blue value
	//             tmp *= 17;                          // Scale by 0x11
	//             rgb |= tmp;                         // Store it
	//             break;
	//
	//         case 4: // argb
	//             tmp = (hex & 0xf000);              // Mask out alpha value
	//             tmp *= 17;                          // Scale by 0x11
	//             rgb = (tmp << 12);                 // Store it
	//             tmp = (hex & 0x0f00);              // Mask out red value
	//             tmp *= 17;                          // Scale by 0x11
	//             rgb |= (tmp << 8);                  // Store it
	//             tmp = (hex & 0x00f0);              // Mask out green value
	//             tmp *= 17;                          // Scale by 0x11
	//             rgb |= (tmp << 4);                  // Store it
	//             tmp = (hex & 0x000f);              // Mask out blue value
	//             tmp *= 17;                          // Scale by 0x11
	//             rgb |= tmp;                         // Store it
	//             break;
	//
	//         case 6: // rrggbb
	//             rgb = 0xff000000 | hex;             // Make fully opaque
	//             break;
	//
	//         case 8: // aarrggbb
	//             rgb = hex;
	//             break;
	//
	//         default:
	// 			throw new ArgumentException("Couldn't convert Color to String"); // return E_UNEXPECTED;
	//         }
	//     }
	//
	//     // Check for scRGB values
	//
	//     else if ((cString > 2) && ('s' == pString[0]) && ('c' == pString[1]) && ('#' == pString[2]))
	//     {
	//         float[] ae = new float[4];
	//         UInt32 ce;
	//         Color eRGB;
	//
	//         pString = pString[3..];
	//         cString -= 3;
	//         ce = 0;
	//
	//         // TrimWhitespace(cString, pString, &cString, &pString);
	// 		pString = pString.TrimStart();
	// 		cString = pString.Length;
	//
	//         do
	//         {
	// 			// IFC(FloatFromString(cString, pString, &cString, &pString, &ae[ce]));
	// 			FloatFromString(cString, pString, out cString, out pString, ref ae[ce]);
	//
	//             ce++;
	//
	//             // Clear debris from parse string including white spaces and comma.
	//
	//             // TrimWhitespace(cString, pString, &cString, &pString);
	// 			pString = pString.TrimStart();
	// 			cString = pString.Length;
	//
	//             if (cString != 0 && ',' == pString[0])
	// 			{
	// 				pString = pString[1..];
	//                 cString--;
	//
	//                 // TrimWhitespace(cString, pString, &cString, &pString);
	// 				pString = pString.TrimStart();
	// 				cString = pString.Length;
	//             }
	//         } while (cString != 0 && (ce < 4));
	//
	//         // We must get either three or four values to be valid.
	//
	//         switch (ce)
	//         {
	//         case 3:
	//             eRGB.a = 1.0f;
	//             eRGB.r = ae[0];
	//             eRGB.g = ae[1];
	//             eRGB.b = ae[2];
	//             break;
	//
	//         case 4:
	//             eRGB.a = ae[0];
	//             eRGB.r = ae[1];
	//             eRGB.g = ae[2];
	//             eRGB.b = ae[3];
	//             break;
	//
	//         default:
	//             return E_UNEXPECTED;
	//         }
	//
	//         rgb = Inline_Convert_MILColorF_scRGB_To_MILColor_sRGB(&eRGB);
	//     }
	//
	//     // try to convert the string to an integer
	//     else if (cString > 0 && Ctypes.xisdigit(pString[0]) != 0)
	//     {
	//         // hr = UnsignedFromDecimalString(cString, pString, &cString, &pString, &rgb);
	// 		rgb = uint.Parse()
	//         if (SUCCEEDED(hr))
	//         {
	//             // we don't support the use of straight integers
	//             IFC(E_UNEXPECTED);
	//         }
	//         if (FAILED(hr))
	//         {
	//             // If we don't have a decimal number then bail
	//             rgb = XUINT32(0);
	//             hr = S_OK;
	//         }
	//     }
	//
	//     // Check for context colors
	//
	//     else if ((cString > 12) && !wcsncmp(L"ContextColor", pString, 12))
	//     {
	//         rgb = XUINT32(0);
	//     }
	//
	//     // Check for known color values
	//
	//     else
	//     {
	//         // trim trailing whitespace
	//         while (cString > 0 && iswspace(pString[cString - 1]))
	//         {
	//             cString--;
	//         }
	//
	//         const size_t colorCount = std::extent<decltype(sakc)>::value;
	//         const KnownColor* sakcEnd = sakc + colorCount;
	//
	// #ifdef DBG
	//         // Validate that the colors are sorted at least once
	//         static bool validatedColors = false;
	//         if (!validatedColors)
	//         {
	//             auto sortedUntil = std::is_sorted_until(sakc, sakcEnd,
	//                 [](const KnownColor& lhs, const KnownColor& rhs)
	//             {
	//                 return _wcsicmp(lhs.m_strColorStorage, rhs.m_strColorStorage) < 0;
	//             });
	//             ASSERT(sortedUntil == sakcEnd);
	//             validatedColors = true;
	//         }
	// #endif
	//
	//
	//
	//         // Binary search doing a string-insensitive search
	//         auto pos = std::lower_bound(sakc, sakcEnd, pString,
	//             [cString](const KnownColor& color, const WCHAR* pString)
	//         {
	//             // custom comparator for checking a trimmed string
	//             // first: compare the min of the trimmed string with the KnownColor
	//             // if they match, then compare the lengths to break the tie
	//             auto knownColorLength = wcslen(color.m_strColorStorage);
	//             auto minLength = std::min(static_cast<size_t>(knownColorLength), static_cast<size_t>(cString));
	//             auto trimmedCompareValue = _wcsnicmp(color.m_strColorStorage, pString, minLength);
	//             if (trimmedCompareValue == 0)
	//             {
	//                 return knownColorLength < cString;
	//             }
	//             return trimmedCompareValue < 0;
	//         });
	//
	//         if (pos != sakcEnd &&
	//             wcslen(pos->m_strColorStorage) == cString &&
	//             _wcsnicmp(pos->m_strColorStorage, pString, cString) == 0)
	//         {
	//             rgb = static_cast<UINT32>(pos->m_rgb);
	//         }
	//
	//         // If we do not find the color in the list of known color values, throw
	//         if (rgb == 0)
	//         {
	//             hr = E_UNEXPECTED;
	//         }
	//         else
	//         {
	//             hr = S_OK;
	//         }
	//     }
	//
	//     // Return the value to the caller
	//     *prgbResult = rgb;
	//
	// Cleanup:
	//     RRETURN(hr);
	// }

	// Uno Doc: this diverges from the FloatFromString in StringConversions.cpp. This one takes advantage
	// of the built-in string parsing capabilities in C#
	// private static void FloatFromString(int cString, ReadOnlySpan<char> pString, out int pcSuffix, out ReadOnlySpan<char> ppSuffix, ref float peValue)
	// {
	// 	for (var i = pString.Length; i > 0; i++)
	// 	{
	// 		if (float.TryParse(pString[..i], out var result))
	// 		{
	// 			ppSuffix = pString[i..];
	// 			pcSuffix = ppSuffix.Length;
	// 			peValue = result;
	// 		}
	// 	}
	//
	// 	throw new ArgumentException("Cannot convert any prefix of this string a float.");
	// }
}
