#nullable disable

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference InfoBar.idl, commit 3125489

namespace Microsoft.UI.Xaml.Controls
{
	/// <summary>
	/// Provides data for the InfoBar.Closed event.
	/// </summary>
	public partial class InfoBarClosedEventArgs
    {
		internal InfoBarClosedEventArgs(InfoBarCloseReason reason) =>
			Reason = reason;

		/// <summary>
		/// Gets a constant that specifies whether the cause of the Closed
		/// event was due to user interaction (Close button click) or programmatic closure.
		/// </summary>
		public InfoBarCloseReason Reason { get; }
	}
}
