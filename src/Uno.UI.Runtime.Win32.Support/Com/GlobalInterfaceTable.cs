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
using System.Diagnostics;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32.System.Com;

namespace Windows.Win32.Foundation;

/// <summary>
///  Wrapper for the COM global interface table.
/// </summary>
internal static unsafe partial class GlobalInterfaceTable
{
	private static readonly IGlobalInterfaceTable* s_globalInterfaceTable;

	static GlobalInterfaceTable()
	{
		Guid guid = CLSID.StdGlobalInterfaceTable;

		fixed (IGlobalInterfaceTable** git = &s_globalInterfaceTable)
		{
			PInvoke.CoCreateInstance(
				&guid,
				pUnkOuter: null,
				CLSCTX.CLSCTX_INPROC_SERVER,
				IID.Get<IGlobalInterfaceTable>(),
				(void**)git).ThrowOnFailure();
		}
	}

	/// <summary>
	///  Registers the given <paramref name="interface"/> in the global interface table. This decrements the
	///  ref count so that the entry in the table will "own" the interface (as it increments the ref count).
	/// </summary>
	/// <returns>The cookie used to refer to the interface in the table.</returns>
	public static uint RegisterInterface<TInterface>(TInterface* @interface)
		where TInterface : unmanaged, IComIID
	{
		uint cookie;
		HRESULT hr = s_globalInterfaceTable->RegisterInterfaceInGlobal(
			(IUnknown*)@interface,
			IID.Get<TInterface>(),
			&cookie);
		hr.ThrowOnFailure();
		return cookie;
	}

	/// <summary>
	///  Gets an agile interface for the <paramref name="cookie"/> that was given back by
	///  <see cref="RegisterInterface{TInterface}(TInterface*)"/>
	/// </summary>
	public static ComScope<TInterface> GetInterface<TInterface>(uint cookie, out HRESULT result)
		where TInterface : unmanaged, IComIID
	{
		ComScope<TInterface> @interface = new(null);
		result = s_globalInterfaceTable->GetInterfaceFromGlobal(cookie, IID.Get<TInterface>(), @interface);
		return @interface;
	}

	/// <summary>
	///  Revokes the interface registered with <see cref="RegisterInterface{TInterface}(TInterface*)"/>.
	///  This will decrement the ref count for the interface.
	/// </summary>
	public static HRESULT RevokeInterface(uint cookie)
	{
		HRESULT hr = s_globalInterfaceTable->RevokeInterfaceFromGlobal(cookie);
		Debug.WriteLineIf(hr.Failed, $"{nameof(GlobalInterfaceTable)}: Failed to revoke interface.");
		return hr;
	}

	/// <summary>
	///  Creates a new instance of an <see cref="IIUnknownStrategy"/> for <see cref="StrategyBasedComWrappers"/>
	///  that uses the Global Interface Table.
	/// </summary>
	/// <remarks>
	///  <para>
	///   The returned instance should not be cached.
	///  </para>
	/// </remarks>
	public static IIUnknownStrategy CreateUnknownStrategy() => new UnknownStrategy();
}
