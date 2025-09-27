// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// Imported in uno on 2021/03/21 from commit 307bd99682cccaa128483036b764c0b7c862d666
// https://github.com/microsoft/microsoft-ui-xaml/blob/307bd99682cccaa128483036b764c0b7c862d666/dev/SwipeControl/SwipeItemInvokedEventArgs.cpp

namespace Microsoft.UI.Xaml.Controls
{
	public partial class SwipeItemInvokedEventArgs
	{
		public SwipeControl SwipeControl { get; }

		internal SwipeItemInvokedEventArgs(SwipeControl swipeControl)
		{
			SwipeControl = swipeControl;
		}
	}
}
