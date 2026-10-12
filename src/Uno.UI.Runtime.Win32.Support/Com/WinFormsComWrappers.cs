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
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;

/// <summary>
///  The <see cref="ComWrappers"/> implementation for WinForm's COM interop usages.
/// </summary>
internal unsafe partial class WinFormsComWrappers : ComWrappers
{
	internal static WinFormsComWrappers Instance { get; } = new();

	private WinFormsComWrappers() { }

	internal static void PopulateIUnknownVTable(IUnknown.Vtbl* unknown)
	{
		GetIUnknownImpl(out nint fpQueryInterface, out nint fpAddRef, out nint fpRelease);
		unknown->QueryInterface_1 = (delegate* unmanaged[Stdcall]<IUnknown*, Guid*, void**, HRESULT>)fpQueryInterface;
		unknown->AddRef_2 = (delegate* unmanaged[Stdcall]<IUnknown*, uint>)fpAddRef;
		unknown->Release_3 = (delegate* unmanaged[Stdcall]<IUnknown*, uint>)fpRelease;
	}

	protected override unsafe ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
	{
		if (obj is not IManagedWrapper vtables)
		{
			Debug.Fail("object does not implement IManagedWrapper");
			count = 0;
			return null;
		}

		ComInterfaceTable table = vtables.GetComInterfaceTable();
		count = table.Count;

		return table.Entries;
	}

	protected override object CreateObject(IntPtr externalComObject, CreateObjectFlags flags)
	{
		throw new NotImplementedException();
	}

	protected override void ReleaseObjects(IEnumerable objects)
	{
		throw new NotImplementedException();
	}

	/// <summary>
	///  For the given <paramref name="this"/> pointer unwrap the associated managed object and use it to
	///  invoke <paramref name="func"/>.
	/// </summary>
	/// <remarks>
	///  <para>
	///   Handles exceptions and converts to <see cref="HRESULT"/>.
	///  </para>
	/// </remarks>
	internal static HRESULT UnwrapAndInvoke<TThis, TInterface>(TThis* @this, Func<TInterface, HRESULT> func)
		where TThis : unmanaged
		where TInterface : class
	{
		try
		{
			TInterface? @object = ComInterfaceDispatch.GetInstance<TInterface>((ComInterfaceDispatch*)@this);
			return @object is null ? /* HRESULT.COR_E_OBJECTDISPOSED */ (HRESULT)0x80131622 : func(@object);
		}
		catch (Exception ex)
		{
			return (HRESULT)ex.HResult;
		}
	}

	internal static TReturnType UnwrapAndInvoke<TThis, TInterface, TReturnType>(TThis* @this, Func<TInterface, TReturnType> func)
		where TThis : unmanaged
		where TInterface : class
		where TReturnType : unmanaged
	{
		try
		{
			TInterface? @object = ComInterfaceDispatch.GetInstance<TInterface>((ComInterfaceDispatch*)@this);
			return @object is null ? default : func(@object);
		}
		catch (Exception ex)
		{
			Debug.Fail($"Exception thrown in UnwrapAndInvoke {ex.Message}.");
			return default;
		}
	}

	/// <summary>
	///  For the given <paramref name="this"/> pointer unwrap the associated managed object and use it to
	///  invoke <paramref name="action"/>.
	/// </summary>
	/// <inheritdoc cref="UnwrapAndInvoke{TThis, TInterface}(TThis*, Func{TInterface, HRESULT})"/>
	internal static HRESULT UnwrapAndInvoke<TThis, TInterface>(TThis* @this, Action<TInterface> action)
		where TThis : unmanaged
		where TInterface : class
	{
		try
		{
			TInterface? @object = ComInterfaceDispatch.GetInstance<TInterface>((ComInterfaceDispatch*)@this);
			if (@object is null)
			{
				return /* HRESULT.COR_E_OBJECTDISPOSED */ (HRESULT)0x80131622;
			}

			action(@object);
			return HRESULT.S_OK;
		}
		catch (Exception ex)
		{
			return (HRESULT)ex.HResult;
		}
	}
}
