// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using MS.Internal.PresentationCore;

namespace MS.Internal.WindowsRuntime
{
	namespace Windows.UI.ViewManagement
	{
		/// <summary>
		/// Contains internal RCWs for invoking the InputPane (tiptsf touch keyboard)
		/// </summary>
		internal static class NativeMethods
		{
			[DllImport(DllImport.ApiSetWinRTString, CallingConvention = CallingConvention.StdCall)]
			internal static extern unsafe int WindowsCreateString([MarshalAs(UnmanagedType.LPWStr)] string sourceString,
												  int length,
												  out IntPtr hstring);

			[DllImport(DllImport.ApiSetWinRTString, CallingConvention = CallingConvention.StdCall)]
			internal static extern int WindowsDeleteString(IntPtr hstring);

			[DllImport(DllImport.ApiSetWinRT, CallingConvention = CallingConvention.StdCall)]
			internal static extern unsafe int RoGetActivationFactory(IntPtr runtimeClassId, ref Guid iid, [MarshalAs(UnmanagedType.Interface)] out object factory);

			[DllImport(DllImport.ApiSetWinRT, CallingConvention = CallingConvention.StdCall)]
			internal static extern unsafe int RoActivateInstance(IntPtr runtimeClassId, [MarshalAs(UnmanagedType.Interface)] out object instance);

			internal const int E_NOINTERFACE = unchecked((int)0x80004002);

			internal const int REGDB_E_CLASSNOTREG = unchecked((int)0x80040154);
		}
	}
}
