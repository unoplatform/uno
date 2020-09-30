// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls
{
	internal partial class TreeViewViewModel
    {
		internal event TypedEventHandler<TreeViewNode, object> NodeExpanding;

		internal event TypedEventHandler<TreeViewNode, object> NodeCollapsed;
	}
}
