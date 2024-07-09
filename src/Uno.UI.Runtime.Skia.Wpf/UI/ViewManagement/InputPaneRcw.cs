// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace MS.Internal.WindowsRuntime
{
	namespace Windows.UI.ViewManagement
	{
		/// <summary>
		/// Contains internal RCWs for invoking the InputPane (tiptsf touch keyboard)
		/// </summary>
		internal static class InputPaneRcw
		{
			private static readonly Guid IID_IActivationFactory = Guid.Parse("00000035-0000-0000-C000-000000000046");

			public static object GetInputPaneActivationFactory()
			{
				const string typeName = "Windows.UI.ViewManagement.InputPane";
				IntPtr hstring = IntPtr.Zero;
				Marshal.ThrowExceptionForHR(NativeMethods.WindowsCreateString(typeName, typeName.Length, out hstring));
				try
				{
					Guid iid = IID_IActivationFactory;
					Marshal.ThrowExceptionForHR(NativeMethods.RoGetActivationFactory(hstring, ref iid, out object factory));
					return factory;
				}
				finally
				{
					Marshal.ThrowExceptionForHR(NativeMethods.WindowsDeleteString(hstring));
				}
			}

			internal enum TrustLevel
			{
				BaseTrust,
				PartialTrust,
				FullTrust
			}

			[Guid("75CF2C57-9195-4931-8332-F0B409E916AF"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
			[ComImport]
			internal interface IInputPaneInterop
			{
				[MethodImpl(MethodImplOptions.InternalCall)]
				void GetIids(out uint iidCount, [MarshalAs(UnmanagedType.LPStruct)] out Guid iids);

				[MethodImpl(MethodImplOptions.InternalCall)]
				void GetRuntimeClassName([MarshalAs(UnmanagedType.BStr)] out string className);

				[MethodImpl(MethodImplOptions.InternalCall)]
				void GetTrustLevel(out TrustLevel TrustLevel);

				[MethodImpl(MethodImplOptions.InternalCall)]
				IInputPane2 GetForWindow([In] IntPtr appWindow, [In] ref Guid riid);
			}

			[Guid("8A6B3F26-7090-4793-944C-C3F2CDE26276"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
			[ComImport]
			internal interface IInputPane2
			{
				[MethodImpl(MethodImplOptions.InternalCall)]
				void GetIids(out uint iidCount, [MarshalAs(UnmanagedType.LPStruct)] out Guid iids);

				[MethodImpl(MethodImplOptions.InternalCall)]
				void GetRuntimeClassName([MarshalAs(UnmanagedType.BStr)] out string className);

				[MethodImpl(MethodImplOptions.InternalCall)]
				void GetTrustLevel(out TrustLevel TrustLevel);

				[MethodImpl(MethodImplOptions.InternalCall)]
				bool TryShow();

				[MethodImpl(MethodImplOptions.InternalCall)]
				bool TryHide();
			}
		}
	}
}
