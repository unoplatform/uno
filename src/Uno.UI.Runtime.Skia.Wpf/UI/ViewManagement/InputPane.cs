// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using MS.Internal.PresentationCore.WindowsRuntime;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace MS.Internal.WindowsRuntime
{
	namespace Windows.UI.ViewManagement
	{
		/// <summary>
		/// DevDiv:1193138
		/// This class wraps the corresponding WinRT APIs for InputPane.  This is used to show the touch keyboard.
		/// Note that WinRT events belonging to this class are not included for simplicity.
		/// 
		/// This class uses RCWs for WinRT objects in order to properly cast from a WinRT IActivationFactory and
		/// therefore implements IDisposable in order to allow for fast RCW cleanup.
		/// </summary>
		internal class InputPane : IDisposable
		{
			#region Fields

			/// <summary>
			/// Bool to check if the WinRT input pane is supported
			/// </summary>
			private static readonly bool _isSupported;


			/// <summary>
			/// Activation factory to instantiate InputPane RCWs
			/// </summary>
			private static object _winRtActivationFactory;

			/// <summary>
			/// The appropriate RCW for calling TryShow/Hide
			/// </summary>
			private InputPaneRcw.IInputPane2 _inputPane;

			#endregion

			#region Constructors

			/// <summary>
			/// Acquires the InputPane type from the winmd
			/// </summary>
			[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
			static InputPane()
			{
				// We don't want to throw here - so wrap in try..catch
				try
				{
					// If we cannot get a new activation factory, then we cannot support
					// this platform.  As such, null out the type to guard instantiations.
					if (GetWinRtActivationFactory(forceInitialization: true) == null)
					{
						_isSupported = false;
					}
					_isSupported = true;
				}
				catch
				{
					_isSupported = false;
				}
			}

			/// <summary>
			/// Checks that the InputPane type was loaded and gets a new InputPane for the parent window.
			/// </summary>
			/// <exception cref="PlatformNotSupportedException"></exception>
			private InputPane(IntPtr? hwnd)
			{
				if (!_isSupported)
				{
					throw new PlatformNotSupportedException();
				}

				try
				{
					if (hwnd.HasValue)
					{
						InputPaneRcw.IInputPaneInterop inputPaneInterop;

						try
						{
							// Get the IActivationFactory and cast to IInputPaneInterop.  The WinRT pattern for
							// static factory types implements an activation factory that allows casting to one
							// or more COM interfaces containing "static" methods to use as initialization.  In 
							// the case of InputPane this is an interop class that contains an init function
							// designed to take an HWND.  This interface is cloaked and not part of the WinRT
							// projections and therefore cannot be seen by reflecting on the type.
							inputPaneInterop = GetWinRtActivationFactory() as InputPaneRcw.IInputPaneInterop;
						}
						catch (COMException)
						{
							// Do a fine grained catch here to detect the activation factory going stale.
							// If this happens, we retry the cast querying a new factory.  If this retry fails
							// something else is going wrong, allow the error to be handled by the outer block.
							inputPaneInterop = GetWinRtActivationFactory(forceInitialization: true) as InputPaneRcw.IInputPaneInterop;
						}

						_inputPane = inputPaneInterop?.GetForWindow(hwnd.Value, typeof(InputPaneRcw.IInputPane2).GUID);
					}
				}
				catch (COMException)
				{
					// Something went wrong in acquiring/using one of the COM RCWs above.
					// This is not a fatal error and just means that this particular attempt to initialize InputPane
					// has failed or the platform does not support this access (this can be caught at the Type init
					// but it is possible that is not the case).
				}

				if (_inputPane == null)
				{
					throw new PlatformNotSupportedException();
				}
			}

			#endregion

			#region Member Functions

			/// <summary>
			/// Wraps creation in a manner analagous to the WinRT interface to this class.
			/// </summary>
			/// <returns>A new InputPane</returns>
			/// <exception cref="PlatformNotSupportedException"></exception>
			internal static InputPane GetForWindow(HwndSource source)
			{
				return new InputPane(source?.CriticalHandle ?? null);
			}

			/// <summary>
			/// Attempts to show the touch keyboard
			/// </summary>
			/// <returns>True if successful, false otherwise</returns>
			internal bool TryShow()
			{
				bool result = false;

				try
				{
					result = _inputPane?.TryShow() ?? false;
				}
				catch (COMException)
				{
					// It's possible that the IInputPane2 has gone stale for some reason
					// in that case we should catch the exception here and simply return
					// false indicating that the KB did not show.
				}

				return result;
			}

			/// <summary>
			/// Attempts to hide the touch keyboard
			/// </summary>
			/// <returns>True if successful, false otherwise</returns>
			internal bool TryHide()
			{
				bool result = false;

				try
				{
					result = _inputPane?.TryHide() ?? false;
				}
				catch (COMException)
				{
					// It's possible that the IInputPane2 has gone stale for some reason
					// in that case we should catch the exception here and simply return
					// false indicating that the KB did not show.
				}

				return result;
			}

			/// <summary>
			/// Creates, caches, and returns a WinRT activation factory for use with the InputPane runtime type.
			/// </summary>
			/// <param name="forceInitialization">If true, will create a new IActivationFactory.  If false will
			/// only create a new IActivationFactory if there is no valid cached instance available.</param>
			/// <returns>An IActivationFactory of InputPane or null if it fails to instantiate.</returns>
			private static object GetWinRtActivationFactory(bool forceInitialization = false)
			{
				if (forceInitialization || _winRtActivationFactory == null)
				{
					try
					{
						_winRtActivationFactory = InputPaneRcw.GetInputPaneActivationFactory();
					}
					catch (Exception e) when (e is TypeLoadException
											 || e is FileNotFoundException
											 || e is EntryPointNotFoundException
											 || e is DllNotFoundException
											 || e.HResult == NativeMethods.E_NOINTERFACE
											 || e.HResult == NativeMethods.REGDB_E_CLASSNOTREG)
					{
						// Catch the set of exceptions that are considered activation exceptions,
						// as well as exception with HResults that can be returned from DllGetActivationFactory when it fails.
						// <see cref="https://msdn.microsoft.com/en-us/library/system.runtime.interopservices.windowsruntime.windowsruntimemarshal.getactivationfactory(v=vs.110).aspx"/>
						// On some Windows SKUs, notably ServerCore, a failing static dependency in InputPane seems to cause a
						// FileNotFoundException during acquisition of the activation factory. We explicitly catch this exception 
						// here to alleviate this issue.  This is not an ideal solution to the platform bug, but keeps WPF applications 
						// from being exposed to the issue.

						// We also catch an EntryPointNotFoundException and DllNotFoundExceptions for when WinRT isn't supported on the platform.
						_winRtActivationFactory = null;
					}
				}

				return _winRtActivationFactory;
			}

			#endregion

			#region IDisposable

			bool _disposed;

			~InputPane()
			{
				Dispose(false);
			}

			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			/// <summary>
			/// Releases the _inputPane RCW
			/// </summary>
			/// <param name="disposing">True if called from a Dispose() call, false when called from the finalizer</param>
			private void Dispose(bool disposing)
			{
				if (!_disposed)
				{
					if (_inputPane != null)
					{
						try
						{
							// Release the input pane here
							Marshal.ReleaseComObject(_inputPane);
						}
						catch
						{
							// Don't want to raise any exceptions in a finalizer, eat them here
						}

						_inputPane = null;
					}

					_disposed = true;
				}
			}

			#endregion
		}
	}
}
