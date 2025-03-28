#pragma warning disable 108 // new keyword hiding
using System;

namespace Windows.UI.Xaml;

/// <summary>
/// Provides data for the UnhandledException event.
/// </summary>
public partial class UnhandledExceptionEventArgs
{
	internal UnhandledExceptionEventArgs(Exception e, bool fatal)
	{
		Exception = e;
		Fatal = fatal;
		Message = e.Message;
	}

	/// <summary>
	/// Gets the HRESULT code associated with the unhandled exception.
	/// </summary>
	public Exception Exception { get; }

	/// <summary>
	/// Gets or sets a value that indicates whether the exception is handled.
	/// </summary>
	public bool Handled { get; set; }

	/// <summary>
	/// Gets the message string as passed by the originating unhandled exception.
	/// </summary>
	public string Message { get; }

	internal bool Fatal { get; }
}
