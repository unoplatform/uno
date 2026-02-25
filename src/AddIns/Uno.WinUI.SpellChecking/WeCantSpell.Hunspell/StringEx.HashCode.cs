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
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WeCantSpell.Hunspell;

internal static partial class StringEx
{
    private const uint Hash1Start = (5381 << 16) + 5381;
    private const uint Hash1StartRotated = ((Hash1Start << 5) | (Hash1Start >> 27)) + Hash1Start;
    private const uint Factor = 1_566_083_941;

#if NO_NUMERICS_BITOPERATIONS

    public static uint GetStableOrdinalHashCode(string value) => value is null ? 0 : GetStableOrdinalHashCode(value.AsSpan());

    public static uint GetStableOrdinalHashCode(ReadOnlySpan<char> value)
    {
        // This is mostly sourced from System.Collections.Frozen.Hashing
        // TODO: replace with non-randomized hashing when that is available
        uint hash1, hash2;

        switch (value.Length)
        {
            case 0:
                return unchecked(Hash1Start * Factor) + Hash1Start;

            case 1:
                return ((value[0] ^ Hash1StartRotated) * Factor) + Hash1Start;

            case 2:
                hash2 = value[0] ^ Hash1StartRotated;
                return (((rotateLeft(hash2) + hash2) ^ value[1]) * Factor) + Hash1Start;

            case 3:
                hash2 = value[0] ^ Hash1StartRotated;
                hash2 = (rotateLeft(hash2) + hash2) ^ value[1];
                return (((rotateLeft(hash2) + hash2) ^ value[2]) * Factor) + Hash1Start;

            default:
                var valueInts = MemoryMarshal.Cast<char, uint>(value);

                switch (value.Length)
                {
                    case 4:
                        hash1 = valueInts[0] ^ Hash1StartRotated;
                        hash2 = valueInts[1] ^ Hash1StartRotated;
                        break;

                    case 5:
                        hash1 = valueInts[0] ^ Hash1StartRotated;
                        hash2 = valueInts[1] ^ Hash1StartRotated;
                        hash2 = (rotateLeft(hash2) + hash2) ^ value[4];
                        break;

                    case 6:
                        hash1 = valueInts[0] ^ Hash1StartRotated;
                        hash2 = valueInts[1] ^ Hash1StartRotated;
                        hash1 = (rotateLeft(hash1) + hash1) ^ valueInts[2];
                        break;

                    case 7:
                        hash1 = valueInts[0] ^ Hash1StartRotated;
                        hash2 = valueInts[1] ^ Hash1StartRotated;
                        hash1 = (rotateLeft(hash1) + hash1) ^ valueInts[2];
                        hash2 = (rotateLeft(hash2) + hash2) ^ value[6];
                        break;

                    case 8:
                        hash1 = valueInts[0] ^ Hash1StartRotated;
                        hash2 = valueInts[1] ^ Hash1StartRotated;
                        hash1 = (rotateLeft(hash1) + hash1) ^ valueInts[2];
                        hash2 = (rotateLeft(hash2) + hash2) ^ valueInts[3];
                        break;

                    case 9:
                        hash1 = valueInts[0] ^ Hash1StartRotated;
                        hash2 = valueInts[1] ^ Hash1StartRotated;
                        hash1 = (rotateLeft(hash1) + hash1) ^ valueInts[2];
                        hash2 = (rotateLeft(hash2) + hash2) ^ valueInts[3];
                        hash2 = (rotateLeft(hash2) + hash2) ^ value[8];
                        break;

                    case 10:
                        hash1 = valueInts[0] ^ Hash1StartRotated;
                        hash2 = valueInts[1] ^ Hash1StartRotated;
                        hash1 = (rotateLeft(hash1) + hash1) ^ valueInts[2];
                        hash2 = (rotateLeft(hash2) + hash2) ^ valueInts[3];
                        hash1 = (rotateLeft(hash1) + hash1) ^ valueInts[4];
                        break;

                    case 11:
                        hash1 = valueInts[0] ^ Hash1StartRotated;
                        hash2 = valueInts[1] ^ Hash1StartRotated;
                        hash1 = (rotateLeft(hash1) + hash1) ^ valueInts[2];
                        hash2 = (rotateLeft(hash2) + hash2) ^ valueInts[3];
                        hash1 = (rotateLeft(hash1) + hash1) ^ valueInts[4];
                        hash2 = (rotateLeft(hash2) + hash2) ^ value[10];
                        break;

                    default:

                        int i;

                        hash1 = valueInts[0] ^ Hash1StartRotated;
                        hash2 = valueInts[1] ^ Hash1StartRotated;

                        for (i = 2; (i + 1) < valueInts.Length; i += 2)
                        {
                            hash1 = (rotateLeft(hash1) + hash1) ^ valueInts[i];
                            hash2 = (rotateLeft(hash2) + hash2) ^ valueInts[i + 1];
                        }

                        if (i < valueInts.Length)
                        {
                            hash1 = (rotateLeft(hash1) + hash1) ^ valueInts[i];
                        }

                        if ((value.Length & 0x01) != 0)
                        {
                            hash2 = (rotateLeft(hash2) + hash2) ^ value[value.Length - 1];
                        }

                        break;
                }

                return (hash2 * Factor) + hash1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint rotateLeft(uint value) => (value << 5) | (value >> 27);
    }

#else

    public static uint GetStableOrdinalHashCode(string value) => value is null ? 0 : GetStableOrdinalHashCode(value.AsSpan());

    public static uint GetStableOrdinalHashCode(ReadOnlySpan<char> value)
    {
        // This is mostly sourced from System.Collections.Frozen.Hashing
        // TODO: replace with non-randomized hashing when that is available

        uint hash1, hash2;

        switch (value.Length)
        {
            case 0:
                return unchecked(Hash1Start * Factor) + Hash1Start;

            case 1:
                return ((value[0] ^ Hash1StartRotated) * Factor) + Hash1Start;

            case 2:
                hash2 = value[0] ^ Hash1StartRotated;
                return (((BitOperations.RotateLeft(hash2, 5) + hash2) ^ value[1]) * Factor) + Hash1Start;

            case 3:
                hash2 = value[0] ^ Hash1StartRotated;
                hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ value[1];
                return (((BitOperations.RotateLeft(hash2, 5) + hash2) ^ value[2]) * Factor) + Hash1Start;

            default:
                var valueInts = MemoryMarshal.Cast<char, uint>(value);

                switch (value.Length)
                {
                    case 4:
                        hash1 = valueInts[0] ^ Hash1StartRotated;
                        hash2 = valueInts[1] ^ Hash1StartRotated;
                        break;

                    case 5:
                        hash1 = valueInts[0] ^ Hash1StartRotated;
                        hash2 = valueInts[1] ^ Hash1StartRotated;
                        hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ value[4];
                        break;

                    case 6:
                        hash1 = valueInts[0] ^ Hash1StartRotated;
                        hash2 = valueInts[1] ^ Hash1StartRotated;
                        hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ valueInts[2];
                        break;

                    case 7:
                        hash1 = valueInts[0] ^ Hash1StartRotated;
                        hash2 = valueInts[1] ^ Hash1StartRotated;
                        hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ valueInts[2];
                        hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ value[6];
                        break;

                    case 8:
                        hash1 = valueInts[0] ^ Hash1StartRotated;
                        hash2 = valueInts[1] ^ Hash1StartRotated;
                        hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ valueInts[2];
                        hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ valueInts[3];
                        break;

                    case 9:
                        hash1 = valueInts[0] ^ Hash1StartRotated;
                        hash2 = valueInts[1] ^ Hash1StartRotated;
                        hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ valueInts[2];
                        hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ valueInts[3];
                        hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ value[8];
                        break;

                    case 10:
                        hash1 = valueInts[0] ^ Hash1StartRotated;
                        hash2 = valueInts[1] ^ Hash1StartRotated;
                        hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ valueInts[2];
                        hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ valueInts[3];
                        hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ valueInts[4];
                        break;

                    case 11:
                        hash1 = valueInts[0] ^ Hash1StartRotated;
                        hash2 = valueInts[1] ^ Hash1StartRotated;
                        hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ valueInts[2];
                        hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ valueInts[3];
                        hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ valueInts[4];
                        hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ value[10];
                        break;

                    default:

                        int i;

                        hash1 = valueInts[0] ^ Hash1StartRotated;
                        hash2 = valueInts[1] ^ Hash1StartRotated;

                        for (i = 2; (i + 1) < valueInts.Length; i += 2)
                        {
                            hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ valueInts[i];
                            hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ valueInts[i + 1];
                        }

                        if (i < valueInts.Length)
                        {
                            hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ valueInts[i];
                        }

                        if ((value.Length & 0x01) != 0)
                        {
                            hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ value[value.Length - 1];
                        }

                        break;
                }

                return (hash2 * Factor) + hash1;
        }
    }

#endif

}

