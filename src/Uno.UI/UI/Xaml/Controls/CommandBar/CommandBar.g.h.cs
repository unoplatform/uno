// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	partial class CommandBar
	{
		public event TypedEventHandler<CommandBar, DynamicOverflowItemsChangingEventArgs>? DynamicOverflowItemsChanging;

		ItemsControl? m_tpPrimaryItemsControlPart;
		ItemsControl? m_tpSecondaryItemsControlPart;
	}
}
