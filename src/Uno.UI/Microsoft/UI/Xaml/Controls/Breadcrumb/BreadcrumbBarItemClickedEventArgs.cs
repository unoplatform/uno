// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

namespace Microsoft.UI.Xaml.Controls
{
	public sealed partial class BreadcrumbBarItemClickedEventArgs
	{
		internal BreadcrumbBarItemClickedEventArgs(int index, object item)
		{
			Index = index;
			Item = item;
		}

		public object Item { get; }

		public int Index { get; }
	}
}
