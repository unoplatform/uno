// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TabularShaping\ShapingRevoke.h, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using System;
using Uno.Disposables;

namespace Microsoft.UI.Xaml.Controls.Tabular;

internal static partial class ShapingHelpers
{
	// Revoke an event subscription without letting the revoke escape as an exception.
	//
	// A C++/WinRT revoker's destructor calls revoke(), which calls back across the ABI. When the
	// publisher is a managed object and the last reference is dropped on the CLR finalizer thread,
	// that call crosses apartments and fails; check_hresult then throws out of a destructor, which
	// is noexcept, and the process terminates via std::terminate. Teardown paths must therefore
	// swallow the failure -- the subscription dies with the publisher either way, so a failed
	// revoke has nothing left to leak.
	// TODO Uno: C++/WinRT revokers port as SerialDisposable; clearing Disposable is revoke().
	internal static void SafeRevoke(SerialDisposable revoker)
	{
		try
		{
			revoker.Disposable = null;
		}
		catch
		{
		}
	}

	// Same protection for a subscription held as a raw event token rather than a revoker, where
	// the caller has to name the event to revoke it.
	internal static void SafeRevokeWith(Action revoke)
	{
		try
		{
			revoke();
		}
		catch
		{
		}
	}
}
