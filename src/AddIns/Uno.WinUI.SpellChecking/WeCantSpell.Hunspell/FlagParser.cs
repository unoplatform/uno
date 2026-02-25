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
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace WeCantSpell.Hunspell;

[DebuggerDisplay("Mode = {Mode}, Encoding = {Encoding}")]
internal readonly struct FlagParser
{
    internal FlagParser(AffixConfig affix)
        : this(affix.FlagMode, affix.Encoding)
    {
    }

    public FlagParser(FlagParsingMode mode, Encoding encoding)
    {
        Encoding = encoding;
        Mode = mode;
        _flagSetCache = [];

        switch (mode)
        {
            case FlagParsingMode.Char:
                TryParseFlag = FlagValue.TryParseAsChar;
                ParseFlagsInOrder = FlagValue.ParseAsChars;
                ParseFlagSet = FlagSetParseAsChars;
                break;
            case FlagParsingMode.Long:
                TryParseFlag = FlagValue.TryParseAsLong;
                ParseFlagsInOrder = FlagValue.ParseAsLongs;
                ParseFlagSet = FlagSetParseAsLongs;
                break;
            case FlagParsingMode.Num:
                TryParseFlag = FlagValue.TryParseAsNumber;
                ParseFlagsInOrder = FlagValue.ParseAsNumbers;
                ParseFlagSet = FlagSetParseAsNumbers;
                break;
            case FlagParsingMode.Uni:
                if (Encoding.UTF8.Equals(Encoding))
                {
                    goto case FlagParsingMode.Char;
                }
                else
                {
                    TryParseFlag = TryParseFlagWithUnicodeReDecode;
                    ParseFlagsInOrder = ParseFlagsInOrderWithUnicodeReDecode;
                    ParseFlagSet = ParseFlagSetWithUnicodeReDecode;
                }

                break;
            default:
                throwNotSupportedFlagMode();
                TryParseFlag = null!;
                ParseFlagsInOrder = null!;
                ParseFlagSet = null!;
                break;
        }

#if !NO_EXPOSED_NULLANNOTATIONS
        [System.Diagnostics.CodeAnalysis.DoesNotReturn]
#endif
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void throwNotSupportedFlagMode() => throw new NotSupportedException("Flag mode is not supported");
    }

    public readonly TryParseFlagValueDelegate TryParseFlag;
    public readonly ParseFlagValuesDelegate ParseFlagsInOrder;
    public readonly ParseFlagSetDelegate ParseFlagSet;
    public readonly Encoding Encoding;
    public readonly FlagParsingMode Mode;
    private readonly TextDictionary<FlagSet> _flagSetCache;

    public FlagParser WithEncoding(Encoding encoding) =>
        Encoding.Equals(encoding) ? this : new(Mode, encoding);

    public FlagParser WithMode(FlagParsingMode mode) =>
        Mode == mode ? this : new(mode, Encoding);

    public FlagValue ParseFlagOrDefault(ReadOnlySpan<char> text)
    {
        _ = TryParseFlag(text, out var result);
        return result;
    }

    private readonly bool TryParseFlagWithUnicodeReDecode(ReadOnlySpan<char> text, out FlagValue value) =>
        FlagValue.TryParseAsChar(ReDecodeConvertedStringAsUtf8(text, Encoding), out value);

    private readonly FlagValue[] ParseFlagsInOrderWithUnicodeReDecode(ReadOnlySpan<char> text) =>
        FlagValue.ParseAsChars(ReDecodeConvertedStringAsUtf8(text, Encoding));

    private FlagSet FlagSetParseAsChars(ReadOnlySpan<char> text)
    {
        FlagSet set;
        if (text.Length > 0)
        {
            if (!_flagSetCache.TryGetValue(text, out set))
            {
                var textString = text.ToString();
                set = FlagSet.ParseAsChars(textString);
                _flagSetCache.Add(textString, set);
            }
        }
        else
        {
            set = FlagSet.Empty;
        }

        return set;
    }

    private FlagSet FlagSetParseAsChars(string text)
    {
        FlagSet set;
        if (text.Length > 0)
        {
            if (!_flagSetCache.TryGetValue(text, out set))
            {
                set = FlagSet.ParseAsChars(text);
                _flagSetCache.Add(text, set);
            }
        }
        else
        {
            set = FlagSet.Empty;
        }

        return set;
    }

    private FlagSet FlagSetParseAsLongs(ReadOnlySpan<char> text)
    {
        FlagSet set;
        if (text.Length > 0)
        {
            if (!_flagSetCache.TryGetValue(text, out set))
            {
                set = FlagSet.ParseAsLongs(text);
                _flagSetCache.Add(text.ToString(), set);
            }
        }
        else
        {
            set = FlagSet.Empty;
        }

        return set;
    }

    private FlagSet FlagSetParseAsNumbers(ReadOnlySpan<char> text)
    {
        FlagSet set;
        if (text.Length > 0)
        {
            if (!_flagSetCache.TryGetValue(text, out set))
            {
                set = FlagSet.ParseAsNumbers(text);
                _flagSetCache.Add(text.ToString(), set);
            }
        }
        else
        {
            set = FlagSet.Empty;
        }

        return set;
    }

    private readonly FlagSet ParseFlagSetWithUnicodeReDecode(ReadOnlySpan<char> text) =>
        FlagSetParseAsChars(ReDecodeConvertedStringAsUtf8(text, Encoding));

    private static string ReDecodeConvertedStringAsUtf8(ReadOnlySpan<char> decoded, Encoding encoding)
    {
#if NO_ENCODING_SPANS

        byte[] encodedBytes;
        int encodedBytesCount;

        unsafe
        {
            fixed (char* decodedPointer = &System.Runtime.InteropServices.MemoryMarshal.GetReference(decoded))
            {
                encodedBytes = new byte[Encoding.UTF8.GetByteCount(decodedPointer, decoded.Length)];
                fixed (byte* encodedBytesPointer = &encodedBytes[0])
                {
                    encodedBytesCount = encoding.GetBytes(decodedPointer, decoded.Length, encodedBytesPointer, encodedBytes.Length);
                }
            }
        }

        return Encoding.UTF8.GetString(encodedBytes, 0, encodedBytesCount);
#else
        var buffer = new ArrayBufferWriter<byte>();
        _ = encoding.GetBytes(decoded, buffer);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
#endif
    }
}

