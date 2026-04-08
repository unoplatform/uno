// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// Uno-specific additions to Layout:
//
// The standard C# event pattern (strong reference) causes memory leaks when Layout
// instances are stored in long-lived dictionaries (e.g., ItemsView.xaml default styles).
// WeakEventHelper provides weak-reference subscriptions to avoid this.
// See: https://github.com/unoplatform/uno/blob/c992ed058d1479cce8e6bca58acbf82cc54ce938/src/Uno.UI/UI/Xaml/Controls/ItemsView/ItemsView.xaml#L12-L16

using System;
using Windows.Foundation;
using Windows.UI.Core;

namespace Microsoft.UI.Xaml.Controls;

partial class Layout
{
	private WeakEventHelper.WeakEventCollection _measureInvalidatedHandlers;
	private WeakEventHelper.WeakEventCollection _arrangeInvalidatedHandlers;

	internal IDisposable RegisterMeasureInvalidated(TypedEventHandler<Layout, object> handler)
		=> WeakEventHelper.RegisterEvent(
			_measureInvalidatedHandlers ??= new(),
			handler,
			(h, s, e) => (h as TypedEventHandler<Layout, object>)?.Invoke((Layout)s, e)
		);

	internal IDisposable RegisterArrangeInvalidated(TypedEventHandler<Layout, object> handler)
		=> WeakEventHelper.RegisterEvent(
			_arrangeInvalidatedHandlers ??= new(),
			handler,
			(h, s, e) => (h as TypedEventHandler<Layout, object>)?.Invoke((Layout)s, e)
		);
}
