// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using Windows.UI.Xaml.Controls;

namespace Microsoft.UI.Private.Controls;

internal partial class ScrollViewTestHooks
{
	public static ScrollViewTestHooks GetGlobalTestHooks()
	{
		return s_testHooks;
	}

	private Dictionary<IScrollView, bool?> m_autoHideScrollControllersMap = new();
}
