// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference winrtgeneratedclasses/ScrollViewerViewChangingEventArgs.g.h, commit 5f9e85113

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ScrollViewerViewChangingEventArgs
	{
#if __SKIA__
		// On Skia we provide a real implementation. The generated NotImplemented-stub
		// remains for the other platforms.
		internal ScrollViewerViewChangingEventArgs() { }

		public ScrollViewerView NextView { get; internal set; }

		public ScrollViewerView FinalView { get; internal set; }

		public bool IsInertial { get; internal set; }
#endif
	}
}
