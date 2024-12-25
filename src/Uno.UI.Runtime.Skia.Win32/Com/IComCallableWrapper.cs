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

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

namespace Windows.Win32.System.Com;

/// <inheritdoc cref="Interface"/>
internal unsafe struct IComCallableWrapper : IComIID, IVTable<IComCallableWrapper, IComCallableWrapper.Vtbl>
{
	private readonly void** _vtbl;

	// {73B17DAF-0480-4702-AF7C-AF3BD4715D71}
	public static Guid IID_Guid { get; } = new(0x73b17daf, 0x0480, 0x4702, 0xaf, 0x7c, 0xaf, 0x3b, 0xd4, 0x71, 0x5d, 0x71);

	static ref readonly Guid IComIID.Guid
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			ReadOnlySpan<byte> data =
			[
				// 0x73b17daf, 0x0480, 0x4702, 0xaf, 0x7c, 0xaf, 0x3b, 0xd4, 0x71, 0x5d, 0x71);
				0xaf, 0x7d, 0xb1, 0x73, 0x80, 0x04, 0x02, 0x47, 0xaf, 0x7c, 0xaf, 0x3b, 0xd4, 0x71, 0x5d, 0x71
			];

			return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
		}
	}

	public unsafe HRESULT QueryInterface(in Guid riid, out void* ppvObject)
	{
		fixed (void** ppvObjectLocal = &ppvObject)
		fixed (Guid* riidLocal = &riid)
		{
			return QueryInterface(riidLocal, ppvObjectLocal);
		}
	}

	public unsafe HRESULT QueryInterface(Guid* riid, void** ppvObject)
	{
		fixed (IComCallableWrapper* pThis = &this)
			return ((delegate* unmanaged[Stdcall]<IComCallableWrapper*, Guid*, void**, HRESULT>)_vtbl[0])(pThis, riid, ppvObject);
	}

	public uint AddRef()
	{
		fixed (IComCallableWrapper* pThis = &this)
			return ((delegate* unmanaged[Stdcall]<IComCallableWrapper*, uint>)_vtbl[1])(pThis);
	}

	public uint Release()
	{
		fixed (IComCallableWrapper* pThis = &this)
			return ((delegate* unmanaged[Stdcall]<IComCallableWrapper*, uint>)_vtbl[2])(pThis);
	}

	public struct Vtbl
	{
#pragma warning disable IDE1006 // Naming Styles - Matching CsWin32 patterns
		internal delegate* unmanaged[Stdcall]<IEnumUnknown*, Guid*, void**, HRESULT> QueryInterface_1;
		internal delegate* unmanaged[Stdcall]<IEnumUnknown*, uint> AddRef_2;
		internal delegate* unmanaged[Stdcall]<IEnumUnknown*, uint> Release_3;
#pragma warning restore IDE1006
	}

	/// <summary>
	///  Used to flag that the COM object is a <see cref="ComWrappers"/> generated object.
	/// </summary>
	[ComImport,
		InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
		Guid("73B17DAF-0480-4702-AF7C-AF3BD4715D71")]
	public interface Interface
	{
	}

	static void IVTable<IComCallableWrapper, Vtbl>.PopulateVTable(Vtbl* vtable) { }
}
