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

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;

/// <summary>
///  Windows Forms <see cref="StrategyBasedComWrappers"/> implementation.
/// </summary>
/// <remarks>
///  <para>
///   Deriving from <see cref="StrategyBasedComWrappers"/> allows us to leverage the functionality the runtime
///   has implemented for source generated "RCW"s, including support for <see cref="ComImportAttribute"/> adaption
///   when built-in COM support is available (EnableGeneratedComInterfaceComImportInterop).
///  </para>
///  <para>
///   It isn't immediately clear how we could merge <see cref="WinFormsComWrappers"/> with this as there is no
///   strategy for <see cref="ComWrappers.ComputeVtables(object, CreateComInterfaceFlags, out int)"/>. We rely
///   on <see cref="IManagedWrapper"/> to apply the needed vtable functionality and it doesn't appear that we
///   can apply <see cref="IComExposedDetails"/> without manually implementing (or source generating)
///   <see cref="IComExposedDetails.GetComInterfaceEntries(out int)"/> on our exposed classes.
///  </para>
/// </remarks>
internal unsafe class WinFormsComStrategy : StrategyBasedComWrappers
{
	internal static WinFormsComStrategy Instance { get; } = new();

	protected override IIUnknownStrategy GetOrCreateIUnknownStrategy() => GlobalInterfaceTable.CreateUnknownStrategy();
}
