// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// Partially ported
// MUX Reference StringConversions.cpp, tag winui3/release/1.4.2

using System;
namespace Windows.UI;

internal class StringConversions
{
	public static void
		UnsignedFromHexString(
			int cString,
			ReadOnlySpan<char> pString,
			out int pcSuffix,
			out ReadOnlySpan<char> ppSuffix,
			out UInt32 pnValue
		)
	{
		UInt32 nValue = 0;

		// Deal with erroneous input

		if (cString == 0)
			throw new ArgumentException("Zero-length string"); // return E_UNEXPECTED;

		// Consume all the hexadecimal digits

		while (cString != 0 && Ctypes.xisxdigit(pString[0]) != 0)
		{
			// Return failure on overflow

			if ((nValue & 0xF0000000) != 0)
				throw new OverflowException("Overflow during conversion"); // return E_UNEXPECTED;

			// Adjust the value for the next digit

			nValue *= 16;

			if (Ctypes.xisdigit(pString[0]) != 0)
				nValue += (UInt32)pString[0] - '0';
			else if (Ctypes.xisupper(pString[0]) != 0)
				nValue += (UInt32)pString[0] - 'A' + 10;
			else
				nValue += (UInt32)pString[0] - 'a' + 10;

			pString = pString[1..];
			cString--;
		}

		// Return the values to the caller

		pnValue = nValue;
		pcSuffix = cString;
		ppSuffix = pString;
	}
}
