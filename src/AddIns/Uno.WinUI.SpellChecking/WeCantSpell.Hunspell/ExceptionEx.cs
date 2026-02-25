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
using System.Runtime.CompilerServices;

namespace WeCantSpell.Hunspell;

internal static class ExceptionEx
{

#if !NO_EXPOSED_NULLANNOTATIONS
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
#endif
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowNotSupported() => throw new NotSupportedException();

#if !NO_EXPOSED_NULLANNOTATIONS
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
#endif
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static T ThrowNotSupported<T>() => throw new NotSupportedException();

#if !NO_EXPOSED_NULLANNOTATIONS
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
#endif
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowNotImplementedYet() => throw new NotImplementedException();

#if !NO_EXPOSED_NULLANNOTATIONS
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
#endif
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static T ThrowNotImplementedYet<T>() => throw new NotImplementedException();

#if !NO_EXPOSED_NULLANNOTATIONS
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
#endif
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowInvalidOperation(string message) => throw new InvalidOperationException(message);

#if !HAS_THROWOOR || DEBUG

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfArgumentEmpty(ReadOnlySpan<char> value, string paramName)
    {
        if (value.IsEmpty) ThrowArgumentOutOfRange(paramName);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfArgumentNull<T>(T value, string paramName) where T : class
    {
#if HAS_THROWNULL
        ArgumentNullException.ThrowIfNull(value, paramName);
#else
        if (value is null) ThrowArgumentNull(paramName);
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfArgumentEqual<T>(T value, T other, string paramName) where T : IEquatable<T>
    {
#if HAS_THROWOOR
        ArgumentOutOfRangeException.ThrowIfEqual(value, other, paramName);
#else
        if (other is null ? value is null : other.Equals(value)) ThrowArgumentOutOfRange(paramName);
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfArgumentNotEqual<T>(T value, T other, string paramName) where T : IEquatable<T>
    {
#if HAS_THROWOOR
        ArgumentOutOfRangeException.ThrowIfNotEqual(value, other, paramName);
#else
        if (other is null ? value is not null : !other.Equals(value)) ThrowArgumentOutOfRange(paramName);
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfArgumentLessThan<T>(T value, T other, string paramName) where T : IComparable<T>
    {
#if HAS_THROWOOR
        ArgumentOutOfRangeException.ThrowIfLessThan(value, other, paramName);
#else
        if (value.CompareTo(other) < 0) ThrowArgumentOutOfRange(paramName);
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfArgumentLessThanOrEqual<T>(T value, T other, string paramName) where T : IComparable<T>
    {
#if HAS_THROWOOR
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, other, paramName);
#else
        if (value.CompareTo(other) <= 0) ThrowArgumentOutOfRange(paramName);
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfArgumentGreaterThan<T>(T value, T other, string paramName) where T : IComparable<T>
    {
#if HAS_THROWOOR
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, other, paramName);
#else
        if (value.CompareTo(other) > 0) ThrowArgumentOutOfRange(paramName);
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfArgumentGreaterThanOrEqual<T>(T value, T other, string paramName) where T : IComparable<T>
    {
#if HAS_THROWOOR
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(value, other, paramName);
#else
        if (value.CompareTo(other) >= 0) ThrowArgumentOutOfRange(paramName);
#endif
    }

#if !NO_EXPOSED_NULLANNOTATIONS
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
#endif
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowArgumentNull(string paramName) => throw new ArgumentNullException(paramName);

#if !NO_EXPOSED_NULLANNOTATIONS
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
#endif
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowArgumentOutOfRange(string paramName) => throw new ArgumentOutOfRangeException(paramName);

#if !NO_EXPOSED_NULLANNOTATIONS
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
#endif
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowArgumentOutOfRange(string paramName, string message) => throw new ArgumentOutOfRangeException(paramName, message);

#if !NO_EXPOSED_NULLANNOTATIONS
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
#endif
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowInvalidOperation() => throw new InvalidOperationException();

#endif

}

