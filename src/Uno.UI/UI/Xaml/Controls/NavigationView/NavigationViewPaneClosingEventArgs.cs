// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

//
// This file is a C# translation of the NavigationViewPaneClosingEventArgs.cpp file from WinUI controls.
//

#if HAS_UNO_WINUI
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
#endif

namespace Windows.UI.Xaml.Controls
{
	public partial class NavigationViewPaneClosingEventArgs
	{
		private bool _cancel;

		public bool Cancel
		{
			get => _cancel;
			set
			{
				_cancel = value;

				if (SplitViewClosingArgs != null)
				{
					SplitViewClosingArgs.Cancel = value;
				}
			}
		}

		internal SplitViewPaneClosingEventArgs SplitViewClosingArgs { get; set; }
	}
}
