// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference BuildTreeScheduler.h, commit 4b206bce3

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.UI.Xaml.Controls;

internal struct WorkInfo
{
	private readonly Action _workFunc;

	public WorkInfo(int priority, Action workFunc)
	{
		Priority = priority;
		_workFunc = workFunc;
	}

	public int Priority { get; }

	public void InvokeWorkFunc() => _workFunc();
}

partial class BuildTreeScheduler
{
	private const double m_budgetInMs = 40.0;

	[ThreadStatic]
	private static Stopwatch m_timer;

	[ThreadStatic]
	private static List<WorkInfo> m_pendingWork;

	// C# event subscriptions don't return a token; a bool flag tracks whether we're hooked up.
	[ThreadStatic]
	private static bool m_renderingToken;
}
