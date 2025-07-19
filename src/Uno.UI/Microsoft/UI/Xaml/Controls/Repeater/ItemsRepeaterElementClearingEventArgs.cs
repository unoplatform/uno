﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ItemsRepeaterElementClearingEventArgs
	{
		internal ItemsRepeaterElementClearingEventArgs(UIElement element)
		{
			Update(element);
		}

		public UIElement Element { get; private set; }

		public void Update(UIElement element)
		{
			Element = element;
		}
	}
}
