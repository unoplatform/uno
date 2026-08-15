// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference winrtgeneratedclasses/ScrollViewerViewChangingEventArgs.g.h, commit dc46907e92

namespace Microsoft.UI.Xaml.Controls
{
	public sealed partial class ScrollViewerViewChangingEventArgs
	{
#if __SKIA__ || __WASM__ || __NETSTD_REFERENCE__
		internal ScrollViewerViewChangingEventArgs() { }
#endif

#if __SKIA__
		public ScrollViewerView NextView { get; internal set; }

		public ScrollViewerView FinalView { get; internal set; }

		public bool IsInertial { get; internal set; }
#endif
	}
}
