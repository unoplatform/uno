// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TreeViewNode.cpp, tag winui3/release/1.4.2

using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

public partial class TreeViewNode
{
	internal event TypedEventHandler<TreeViewNode, IVectorChangedEventArgs> ChildrenChanged;

	internal event TypedEventHandler<TreeViewNode, DependencyPropertyChangedEventArgs> ExpandedChanged;
}
