using System;
using Windows.Foundation.Metadata;

namespace Windows.UI.Xaml.Navigation
{
	public sealed partial class NavigationFailedEventArgs
	{
		internal NavigationFailedEventArgs(Type sourcePageType, Exception exception)
		{
			SourcePageType = sourcePageType;
			Exception = exception;
		}

		//
		// Summary:
		//     Gets the result code for the exception that is associated with the failed navigation.
		//
		// Returns:
		//     A system exception result code.
		public Exception Exception { get; }
		//
		// Summary:
		//     Gets or sets a value that indicates whether the failure event has been handled.
		//
		// Returns:
		//     true if the failure event is handled; false if the failure event is not yet handled.
		public bool Handled { get; set; }
		//
		// Summary:
		//     Gets the data type of the target page.
		//
		// Returns:
		//     The data type of the target page, represented as namespace.type or simply type.
		public Type SourcePageType { get; }
	}
}
