// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference BuildTreeScheduler.h, tag winui3/release/1.4.2

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.UI.Private.Controls;

namespace Microsoft.UI.Xaml.Controls;

partial class BuildTreeScheduler
{
	private struct WorkInfo
	{
		private readonly Action _workFunc;

		public WorkInfo(int priority, Action workFunc)
		{
			Priority = priority;
			_workFunc = workFunc;
		}

		public int Priority { get; }

		public void InvokeWorkFunc() => _workFunc();
	};


	[ThreadStatic]
	private static Stopwatch? m_timer;

	[ThreadStatic]
	private static List<WorkInfo>? m_pendingWork;

	[ThreadStatic]
	private static bool m_renderingToken;
}
