// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\inc\RuntimeClassHelpers.h, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using System;
using System.Runtime.InteropServices;

namespace Uno.UI.Helpers.WinUI;

// TODO Uno: the thread-affinity half of the C++ ReferenceTracker<D, ImplT, I...> base. C++/WinRT calls
// abi_enter() on every projected call; C# has no ABI boundary, so a ported ReferenceTracker type owns one of
// these (created in its constructor) and calls CheckThread() at the top of each public member.
internal readonly struct ReferenceTrackerThreadAffinity
{
	private const int RPC_E_WRONG_THREAD = unchecked((int)0x8001010E);

	private readonly int m_owningThreadId;

	private ReferenceTrackerThreadAffinity(int owningThreadId)
	{
		m_owningThreadId = owningThreadId;
	}

	// m_owningThreadId = ::GetCurrentThreadId();
	internal static ReferenceTrackerThreadAffinity ForCurrentThread() => new(Environment.CurrentManagedThreadId);

	internal bool IsOnThread()
	{
		return Environment.CurrentManagedThreadId == m_owningThreadId;
	}

	internal void CheckThread()
	{
		if (!IsOnThread())
		{
			// TODO Uno: winrt::hresult_wrong_thread()
			throw new COMException("The application called an interface that was marshalled for a different thread.", RPC_E_WRONG_THREAD);
		}
	}
}
