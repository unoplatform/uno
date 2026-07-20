// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference LayoutsTestHooksFactory.cpp, commit b8cfb8490

#nullable enable

namespace Microsoft.UI.Private.Controls;

partial class LayoutsTestHooks
{
	private static void EnsureHooks()
	{
		s_testHooks ??= new LayoutsTestHooks();
	}
}
