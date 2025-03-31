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
using System.Threading;
using Windows.Win32.System.Com;

namespace Windows.Win32.Foundation;

internal static unsafe partial class GlobalInterfaceTable
{
	/// <summary>
	///  Strategy for <see cref="StrategyBasedComWrappers"/> that uses the <see cref="GlobalInterfaceTable"/>.
	/// </summary>
	private unsafe class UnknownStrategy : IIUnknownStrategy
	{
		private uint _cookie;

		void* IIUnknownStrategy.CreateInstancePointer(void* unknown)
		{
			Debug.Assert(_cookie == 0, "A cookie has already been generated for this instance.");
			_cookie = RegisterInterface((IUnknown*)unknown);
			return unknown;
		}

		int IIUnknownStrategy.QueryInterface(void* instancePtr, in Guid iid, out void* ppObj)
			=> s_globalInterfaceTable->GetInterfaceFromGlobal(_cookie, iid, out ppObj);

		int IIUnknownStrategy.Release(void* instancePtr)
		{
			uint cookie = Interlocked.Exchange(ref _cookie, 0);
			return cookie != 0 ? (int)HRESULT.S_OK : (int)RevokeInterface(_cookie);
		}
	}
}
