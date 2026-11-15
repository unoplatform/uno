// The MIT License (MIT)

// Copyright (c) .NET Foundation and Contributors
//
// All rights reserved.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Windows.Win32.Foundation;

internal static unsafe class IID
{
	private static ref readonly Guid IID_NULL
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			ReadOnlySpan<byte> data =
			[
				0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
			];

			return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
		}
	}

	// We cast away the "readonly" here as there is no way to communicate that through a pointer and
	// Marshal APIs take the Guid as ref. Even though none of our usages actually change the state.

	/// <summary>
	///  Gets a pointer to the IID <see cref="Guid"/> for the given <typeparamref name="T"/>.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Guid* Get<T>() where T : unmanaged, IComIID
		=> (Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in T.Guid));

	/// <summary>
	///  Gets a reference to the IID <see cref="Guid"/> for the given <typeparamref name="T"/>.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ref Guid GetRef<T>() where T : unmanaged, IComIID
		=> ref Unsafe.AsRef(in T.Guid);

	/// <summary>
	///  Empty <see cref="Guid"/> (GUID_NULL in docs).
	/// </summary>
	public static Guid* NULL() => (Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in IID_NULL));
}
