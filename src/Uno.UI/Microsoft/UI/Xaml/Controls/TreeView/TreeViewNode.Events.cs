// MUX Reference TreeViewNode.cpp, commit de78834

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class TreeViewNode
	{
		internal event TypedEventHandler<TreeViewNode, IVectorChangedEventArgs> ChildrenChanged;

		internal event TypedEventHandler<TreeViewNode, DependencyPropertyChangedEventArgs> ExpandedChanged;
	}
}
