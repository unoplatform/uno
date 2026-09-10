// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference BuildTreeScheduler.cpp, commit 4b206bce3

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.UI.Private.Controls;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls;

partial class BuildTreeScheduler
{
	public static void RegisterWork(int priority, Action workFunc)
	{
		MUX_ASSERT(priority >= 0);
		MUX_ASSERT(workFunc != null);

		QueueTick();

		// [ThreadStatic] fields cannot have inline initializers, so lazy-init on first use per thread.
		if (m_pendingWork == null)
		{
			m_pendingWork = new List<WorkInfo>();
			m_timer = new Stopwatch();
			m_timer.Start();
		}

		m_pendingWork.Add(new WorkInfo(priority, workFunc));
	}

	public static bool ShouldYield()
	{
		return m_timer.ElapsedMilliseconds > m_budgetInMs;
	}

	private static void OnRendering(object sender, object args)
	{
		bool budgetReached = ShouldYield();
		if (!budgetReached && m_pendingWork.Count > 0)
		{
			// Sort in descending order of priority and work from the end of the list to avoid moving around during erase.
			m_pendingWork.Sort((lhs, rhs) => lhs.Priority - rhs.Priority);
			int currentIndex = m_pendingWork.Count - 1;

			do
			{
				m_pendingWork[currentIndex].InvokeWorkFunc();
				m_pendingWork.RemoveAt(currentIndex);
			} while (--currentIndex >= 0 && !ShouldYield());
		}

		if (m_pendingWork.Count == 0)
		{
			// TraceLoggingProviderWrite(
			//     XamlTelemetryLogging, "BuildTreeScheduler_OutOfWork",
			//     TraceLoggingLevel(WINEVENT_LEVEL_VERBOSE));

			// No more pending work, unhook from rendering event since being hooked up will case wux to try to
			// call the event at 60 frames per second
			Microsoft.UI.Xaml.Media.CompositionTarget.Rendering -= OnRendering;
			m_renderingToken = false;
			RepeaterTestHooks.NotifyBuildTreeCompleted();
		}

		// Reset the timer so it snaps the time just before rendering.
		// Stopwatch.Reset() also stops the timer, so Restart() is used to keep it measuring from now.
		m_timer.Restart();
	}

	private static void QueueTick()
	{
		if (!m_renderingToken)
		{
			Microsoft.UI.Xaml.Media.CompositionTarget.Rendering += OnRendering;
			m_renderingToken = true;
		}
	}
}
