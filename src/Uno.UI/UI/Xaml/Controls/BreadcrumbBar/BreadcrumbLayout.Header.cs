// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference BreadcrumbLayout.h, branch main, commit e3c555a53

#nullable enable

using Uno.UI.DataBinding;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

internal partial class BreadcrumbLayout : NonVirtualizingLayout
{
	private UIElement? m_ellipsisButton = null;
	private ManagedWeakReference? m_breadcrumb = null;

	private bool m_ellipsisIsRendered;
	private uint m_firstRenderedItemIndexAfterEllipsis;
	private uint m_visibleItemsCount;
}
