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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;

namespace Windows.Win32;

internal static unsafe partial class ComHelpers
{
	// Note that ComScope<T> needs to be the return value to facilitate using in a `using`.
	//
	//  using var stream = GetComScope<IStream>(obj, out bool success);

	/// <summary>
	///  Returns <see langword="true"/> if built-in COM interop is supported. When using AOT or trimming this will
	///  return <see langword="false"/>.
	/// </summary>
	internal static bool BuiltInComSupported { get; }
		// Presume it is supported if we can't get the switch.
		= !AppContext.TryGetSwitch("System.Runtime.InteropServices.BuiltInComInterop.IsSupported", out bool supported)
			|| supported;

	/// <summary>
	///  Gets a pointer for the specified <typeparamref name="T"/> for the given <paramref name="object"/>. Throws if
	///  the desired pointer can not be obtained.
	/// </summary>
	internal static ComScope<T> GetComScope<T>(object? @object) where T : unmanaged, IComIID =>
		new(GetComPointer<T>(@object));

	/// <summary>
	///  Attempts to get a pointer for the specified <typeparamref name="T"/> for the given <paramref name="object"/>.
	/// </summary>
	internal static ComScope<T> TryGetComScope<T>(object? @object) where T : unmanaged, IComIID =>
		TryGetComScope<T>(@object, out _);

	/// <summary>
	///  Attempts to get a pointer for the specified <typeparamref name="T"/> for the given <paramref name="object"/>.
	/// </summary>
	internal static ComScope<T> TryGetComScope<T>(object? @object, out HRESULT hr) where T : unmanaged, IComIID =>
		new(TryGetComPointer<T>(@object, out hr));

	/// <summary>
	///  Gets the specified <typeparamref name="T"/> interface for the given <paramref name="object"/>. Throws if
	///  the desired pointer can not be obtained.
	/// </summary>
	internal static T* GetComPointer<T>(object? @object) where T : unmanaged, IComIID
	{
		T* result = TryGetComPointer<T>(@object, out HRESULT hr);
		hr.ThrowOnFailure();
		return result;
	}

	/// <summary>
	///  Attempts to get the specified <typeparamref name="T"/> interface for the given <paramref name="object"/>.
	/// </summary>
	/// <returns>The requested pointer or <see langword="null"/> if unsuccessful.</returns>
	internal static T* TryGetComPointer<T>(object? @object) where T : unmanaged, IComIID =>
		TryGetComPointer<T>(@object, out _);

	/// <summary>
	///  Queries for the given interface and releases it.
	///  Note that this method should only be used for the purposes of checking if the object supports a given interface.
	///  If that interface is needed, it is best try to get the ComScope directly to avoid querying twice.
	/// </summary>
	internal static bool SupportsInterface<T>(object? @object) where T : unmanaged, IComIID
	{
		using var scope = TryGetComScope<T>(@object, out HRESULT hr);
		return hr.Succeeded;
	}

	/// <summary>
	///  Attempts to get the specified <typeparamref name="T"/> interface for the given <paramref name="object"/>.
	/// </summary>
	/// <param name="result">
	///  Typically either <see cref="HRESULT.S_OK"/> or <see cref="HRESULT.E_POINTER"/>. Check for success, not
	///  specific results.
	/// </param>
	/// <returns>The requested pointer or <see langword="null"/> if unsuccessful.</returns>
	internal static T* TryGetComPointer<T>(object? @object, out HRESULT result) where T : unmanaged, IComIID
	{
		if (@object is null)
		{
			result = HRESULT.E_POINTER;
			return null;
		}

		IUnknown* ccw = null;
		if (@object is IManagedWrapper)
		{
			// One of our classes that we can generate a CCW for.
			ccw = (IUnknown*)WinFormsComWrappers.Instance.GetOrCreateComInterfaceForObject(@object, CreateComInterfaceFlags.None);
		}
		else if (ComWrappers.TryGetComInstance(@object, out nint unknown))
		{
			// A ComWrappers generated RCW.
			ccw = (IUnknown*)unknown;
		}
		else
		{
			// Fall back to COM interop if possible. Note that this will use the globally registered ComWrappers
			// if that exists (so it won't always fall into legacy COM interop).
			try
			{
				ccw = (IUnknown*)Marshal.GetIUnknownForObject(@object);
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Did not find IUnknown for {@object.GetType().Name}. {ex.Message}");
			}
		}

		if (ccw is null)
		{
			result = HRESULT.E_NOINTERFACE;
			return null;
		}

		if (typeof(T) == typeof(IUnknown))
		{
			// No need to query if we wanted IUnknown.
			result = HRESULT.S_OK;
			return (T*)ccw;
		}

		// Now query out the requested interface
		result = ccw->QueryInterface(IID.GetRef<T>(), out void* ppvObject);
		ccw->Release();
		return (T*)ppvObject;
	}

	/// <summary>
	///  Attempts to unwrap a ComWrapper CCW as a particular managed object.
	/// </summary>
	private static bool TryUnwrapComWrapperCCW<TWrapper>(
		IUnknown* unknown,
		[NotNullWhen(true)] out TWrapper? @interface) where TWrapper : class
	{
		if (ComWrappers.TryGetObject((nint)unknown, out object? obj))
		{
			if (obj is TWrapper desired)
			{
				@interface = desired;
				return true;
			}
			else
			{
				Debug.WriteLine($"{nameof(TryGetObjectForIUnknown)}: Found a manual CCW, but couldn't unwrap to {typeof(TWrapper).Name}");
			}
		}

		@interface = default;
		return false;
	}

	/// <inheritdoc cref="TryGetObjectForIUnknown{TObject}(IUnknown*, bool, out TObject)"/>
	internal static bool TryGetObjectForIUnknown<TObject, TInterface>(
		ComScope<TInterface> comScope,
		[NotNullWhen(true)] out TObject? @object)
		where TObject : class
		where TInterface : unmanaged, IComIID => TryGetObjectForIUnknown(comScope.Value, out @object);

	/// <inheritdoc cref="TryGetObjectForIUnknown{TObject}(IUnknown*, bool, out TObject)"/>
	internal static bool TryGetObjectForIUnknown<TObject, TInterface>(
		TInterface* comPointer,
		[NotNullWhen(true)] out TObject? @object)
		where TObject : class
		where TInterface : unmanaged, IComIID
	{
		if (comPointer is null)
		{
			@object = null;
			return false;
		}

		IUnknown* unknown = (IUnknown*)comPointer;
		if (typeof(TInterface) == typeof(IUnknown))
		{
			return TryGetObjectForIUnknown(unknown, out @object);
		}

		HRESULT hr = unknown->QueryInterface(IID.Get<IUnknown>(), (void**)&unknown);
		if (hr.Failed)
		{
			Debug.Fail("How did we fail to query for IUnknown?");
			@object = null;
			return false;
		}

		return TryGetObjectForIUnknown(unknown, out @object);
	}

	/// <inheritdoc cref="TryGetObjectForIUnknown{TObject}(IUnknown*, bool, out TObject)"/>
	internal static bool TryGetObjectForIUnknown<TObject>(
		IUnknown* unknown,
		[NotNullWhen(true)] out TObject? @object) where TObject : class =>
		TryGetObjectForIUnknown(unknown, takeOwnership: false, out @object);

	/// <summary>
	///  Attempts to get a managed wrapper of the specified type for the given COM interface.
	/// </summary>
	/// <param name="takeOwnership">
	///  When <see langword="true"/>, releases the original <paramref name="unknown"/> whether successful or not.
	/// </param>
	internal static bool TryGetObjectForIUnknown<TObject>(
		IUnknown* unknown,
		bool takeOwnership,
		[NotNullWhen(true)] out TObject? @object) where TObject : class
	{
		@object = null;
		if (unknown is null)
		{
			return false;
		}

		try
		{
			@object = (TObject)GetObjectForIUnknown(unknown);
			return true;
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"{nameof(TryGetObjectForIUnknown)}: Failed to get object for {typeof(TObject).Name}. {ex.Message}");
			return false;
		}
		finally
		{
			if (takeOwnership)
			{
				uint count = unknown->Release();
				Debug.WriteLineIf(count > 0, $"{nameof(TryGetObjectForIUnknown)}: Count for {typeof(TObject).Name} is {count} after release.");
			}
		}
	}

	/// <summary>
	///  Returns <see langword="true"/> if the given <paramref name="object"/>
	///  is projected as the given <paramref name="comPointer"/>.
	/// </summary>
	internal static bool WrapsManagedObject<T>(object @object, T* comPointer)
		where T : unmanaged, IComIID
	{
		if (comPointer is null)
		{
			return false;
		}

		using ComScope<IUnknown> unknown = new(null);
		((IUnknown*)comPointer)->QueryInterface(IID.Get<IUnknown>(), unknown).ThrowOnFailure();

		// If it is a ComWrappers object we need to simply pull out the original object to check.
		if (ComWrappers.TryGetObject((nint)unknown, out object? obj))
		{
			return @object == obj;
		}

		using ComScope<IUnknown> ccw = new((IUnknown*)(void*)Marshal.GetIUnknownForObject(@object));
		return ccw.Value == unknown;
	}

	/// <inheritdoc cref="GetObjectForIUnknown(IUnknown*)"/>
	internal static object GetObjectForIUnknown<TInterface>(TInterface* comPointer)
		where TInterface : unmanaged, IComIID
	{
		if (comPointer is null)
		{
			throw new ArgumentNullException(nameof(comPointer));
		}

		IUnknown* unknown = (IUnknown*)comPointer;

		if (typeof(TInterface) == typeof(IUnknown))
		{
			return GetObjectForIUnknown(unknown);
		}

		unknown->QueryInterface(IID.Get<IUnknown>(), (void**)&unknown).ThrowOnFailure();
		return GetObjectForIUnknown(unknown);
	}

	/// <inheritdoc cref="GetObjectForIUnknown(IUnknown*)"/>
	internal static object GetObjectForIUnknown<TInterface>(ComScope<TInterface> comScope)
		where TInterface : unmanaged, IComIID => GetObjectForIUnknown(comScope.Value);

	/// <summary>
	///  <see cref="ComWrappers"/> capable wrapper for <see cref="Marshal.GetObjectForIUnknown(nint)"/>.
	/// </summary>
	/// <exception cref="ArgumentNullException"><paramref name="unknown"/> is <see langword="null"/>.</exception>
	internal static object GetObjectForIUnknown(IUnknown* unknown)
	{
		if (unknown is null)
		{
			throw new ArgumentNullException(nameof(unknown));
		}

		// If it is a ComWrappers object we need to simply pull out the original object.
		if (ComWrappers.TryGetObject((nint)unknown, out object? obj))
		{
			return obj;
		}

		if (BuiltInComSupported)
		{
			return Marshal.GetObjectForIUnknown((nint)unknown);
		}
		else
		{
			// Analogous to ComInterfaceMarshaller<object>.ConvertToManaged(unknown), but we need our own strategy.
			return WinFormsComStrategy.Instance.GetOrCreateObjectForComInstance((nint)unknown, CreateObjectFlags.Unwrap);
		}
	}

	/// <summary>
	///  <see cref="IUnknown"/> vtable population hook for CsWin32's generated <see cref="IVTable"/> implementation.
	/// </summary>
	static partial void PopulateIUnknownImpl<TComInterface>(IUnknown.Vtbl* vtable)
		where TComInterface : unmanaged =>
		WinFormsComWrappers.PopulateIUnknownVTable(vtable);

	/// <summary>
	///  Find the given interface's <see cref="ITypeInfo"/> from the specified type library.
	/// </summary>
	public static ComScope<ITypeInfo> GetRegisteredTypeInfo(
		Guid typeLibrary,
		ushort majorVersion,
		ushort minorVersion,
		Guid interfaceId)
	{
		// Load the registered type library and get the relevant ITypeInfo for the specified interface.
		//
		// Note that the ITypeLib and ITypeInfo are free to be used on any thread. ITypeInfo add refs the
		// ITypeLib and keeps a reference to it.
		//
		// While type library loading is cached, that is only while it is still referenced (directly or via
		// an ITypeInfo reference) and there is still a fair amount of overhead to look up the right instance. The
		// caching is by the type library path, so the guid needs looked up again in the registry to figure out the
		// path again.
		using ComScope<ITypeLib> typelib = new(null);
		HRESULT hr = PInvoke.LoadRegTypeLib(typeLibrary, majorVersion, minorVersion, 0, typelib);
		hr.ThrowOnFailure();

		ComScope<ITypeInfo> typeInfo = new(null);
		typelib.Value->GetTypeInfoOfGuid(interfaceId, typeInfo).ThrowOnFailure();
		return typeInfo;
	}
}
