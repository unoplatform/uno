// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ScrollViewTestHooks.h, tag winui3/release/2.5.1, commit ba3a8d59e

using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Private.Controls;

internal partial class ScrollViewTestHooks
{
	public static ScrollViewTestHooks GetGlobalTestHooks()
	{
		return s_testHooks;
	}

	private Dictionary<IScrollView, bool?> m_autoHideScrollControllersMap = new();
}
