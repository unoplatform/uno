// UNO PREAMBLE START

// Note that this license is inherited from the original Hunspell project
// 	as this is derived from that work. Original license:
//
// Version: MPL 1.1/GPL 2.0/LGPL 2.1
//
// The contents of this file are subject to the Mozilla Public License Version
// 1.1 (the "License"); you may not use this file except in compliance with
// the License. You may obtain a copy of the License at
// http://www.mozilla.org/MPL/
//
// Software distributed under the License is distributed on an "AS IS" basis,
// WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License
// for the specific language governing rights and limitations under the
// License.
//
// 	The Original Code is Hunspell, based on MySpell.
//
// 	The Initial Developers of the Original Code are
// Kevin Hendricks (MySpell) and NÃ©meth LÃ¡szlÃ³ (Hunspell).
// 	Portions created by the Initial Developers are Copyright (C) 2002-2005
// the Initial Developers. All Rights Reserved.
//
// 	Contributor(s):
// David Einstein 
// Davide Prina
// Giuseppe Modugno 
// Gianluca Turconi
// Simon Brouwer
// Noll JÃ¡nos
// BÃ­rÃ³ ÃrpÃ¡d
// Goldman EleonÃ³ra
// SarlÃ³s TamÃ¡s
// BencsÃ¡th BoldizsÃ¡r
// HalÃ¡csy PÃ©ter
// Dvornik LÃ¡szlÃ³
// Gefferth AndrÃ¡s
// Nagy Viktor
// Varga DÃ¡niel
// Chris Halls
// Rene Engelhard
// Bram Moolenaar
// Dafydd Jones
// Harri PitkÃ¤nen
//
// 	Alternatively, the contents of this file may be used under the terms of
// 	either the GNU General Public License Version 2 or later (the "GPL"), or
// 	the GNU Lesser General Public License Version 2.1 or later (the "LGPL"),
// 	in which case the provisions of the GPL or the LGPL are applicable instead
// of those above. If you wish to allow use of your version of this file only
// under the terms of either the GPL or the LGPL, and not to allow others to
// use your version of this file under the terms of the MPL, indicate your
// decision by deleting the provisions above and replace them with the notice
// and other provisions required by the GPL or the LGPL. If you do not delete
// the provisions above, a recipient may use your version of this file under
// the terms of any one of the MPL, the GPL or the LGPL.

// https://github.com/aarondandy/WeCantSpell.Hunspell

#pragma warning disable IDE0055, CA1805, CA1815, CA1310,

// UNO PREAMBLE END

using System;
using System.Text;

namespace WeCantSpell.Hunspell;

internal static class EncodingEx
{
    public static Encoding? GetEncodingByName(string encodingName) =>
        GetUtf8EncodingOrDefault(encodingName) ?? GetEncodingFromDatabase(encodingName);

    public static Encoding? GetEncodingByName(ReadOnlySpan<char> encodingName) =>
        GetUtf8EncodingOrDefault(encodingName) ?? GetEncodingFromDatabase(encodingName.ToString());

    private static Encoding? GetUtf8EncodingOrDefault(ReadOnlySpan<char> encodingName)
    {
	    // UNO EDIT START : this causes a crash in ILC, https://github.com/dotnet/runtime/issues/123833
        // if (encodingName.Equals("UTF8", StringComparison.OrdinalIgnoreCase) || encodingName.Equals("UTF-8", StringComparison.OrdinalIgnoreCase))
        // {
        //     return Encoding.UTF8;
        // }
        //
        // return null;
        var lowercaseString = new string(encodingName).ToLowerInvariant();
        if (lowercaseString is "utf8" or "utf-8")
        {
	        return Encoding.UTF8;
        }

        return null;
        // UNO EDIT END
    }

    private static Encoding? GetEncodingFromDatabase(string encodingName)
    {
        try
        {
            return Encoding.GetEncoding(encodingName);
        }
        catch (ArgumentException)
        {
            return getEncodingByAlternateNames(encodingName);
        }

        static Encoding? getEncodingByAlternateNames(string encodingName)
        {
            var spaceIndex = encodingName.IndexOf(' ');
            if (spaceIndex > 0)
            {
                return GetEncodingByName(encodingName.AsSpan(0, spaceIndex));
            }

            if (encodingName.Length >= 4 && encodingName.StartsWith("ISO") && encodingName[3] != '-')
            {
                return GetEncodingByName(encodingName.Insert(3, "-"));
            }

            return null;
        }
    }

#if NO_SPAN_DECODE
    public static void Convert(this Decoder decoder, ReadOnlySpan<byte> bytes, Span<char> chars, bool flush, out int bytesUsed, out int charsUsed, out bool completed)
    {
        unsafe
        {
            fixed (byte* bytesPointer = &System.Runtime.InteropServices.MemoryMarshal.GetReference(bytes))
            fixed (char* charsPointer = &System.Runtime.InteropServices.MemoryMarshal.GetReference(chars))
            {
                decoder.Convert(
                    bytesPointer,
                    bytes.Length,
                    charsPointer,
                    chars.Length,
                    flush: false,
                    out bytesUsed,
                    out charsUsed,
                    out completed);
            }
        }
    }
#endif

}

