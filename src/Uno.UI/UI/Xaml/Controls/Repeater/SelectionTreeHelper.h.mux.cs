// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference SelectionTreeHelper.h, commit 4b206bce3

namespace Microsoft.UI.Xaml.Controls;

partial class SelectionTreeHelper
{
	internal struct TreeWalkNodeInfo
	{
		internal TreeWalkNodeInfo(SelectionNode node, IndexPath indexPath, SelectionNode parent)
		{
			Node = node;
			Path = indexPath;
			ParentNode = parent;
		}

		internal TreeWalkNodeInfo(SelectionNode node, IndexPath indexPath)
		{
			Node = node;
			Path = indexPath;
			ParentNode = null;
		}

		internal SelectionNode Node;
		internal IndexPath Path;
		internal SelectionNode ParentNode;
	}
}
