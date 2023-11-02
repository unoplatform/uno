// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference BuildTreeScheduler.cpp, tag winui3/release/1.4.2

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.UI.Private.Controls;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls;

internal partial class BuildTreeScheduler
{
	private const double m_budgetInMs = 40.0;

	public static void RegisterWork(int priority, Action workFunc)
	{
		MUX_ASSERT(priority >= 0);
		MUX_ASSERT(workFunc != null);

		QueueTick();

		if (m_pendingWork == null)
		{
			m_pendingWork = new List<WorkInfo>();
			m_timer = new Stopwatch();
			m_timer.Start();
		}
		m_pendingWork.Add(new WorkInfo(priority, workFunc));
	}

	public static bool ShouldYield() => m_timer.ElapsedMilliseconds > m_budgetInMs;

	public static void OnRendering(object sender, object args)
	{
		bool budgetReached = ShouldYield();
		if (!budgetReached && m_pendingWork.Count > 0)
		{
			// Sort in descending order of priority and work from the end of the list to avoid moving around during erase.
			m_pendingWork.Sort((lhs, rhs) => rhs.Priority.CompareTo(lhs.Priority));
			int currentIndex = m_pendingWork.Count - 1;

			do
			{
				m_pendingWork[currentIndex].InvokeWorkFunc();
				m_pendingWork.RemoveAt(currentIndex);
			} while (--currentIndex >= 0 && !ShouldYield());
		}

		if (m_pendingWork.Count == 0)
		{
			// No more pending work, unhook from rendering event since being hooked up will case wux to try to 
			// call the event at 60 frames per second
			m_renderingToken = false;
			Windows.UI.Xaml.Media.CompositionTarget.Rendering -= OnRendering;
			RepeaterTestHooks.NotifyBuildTreeCompleted();
		}

		// Reset the timer so it snaps the time just before rendering
		m_timer.Restart();
		// TODO:MZ: Should be Reset or Restart? Technically it seems QPCTimer does not stop ever, just snaps the baseline
		// Technically the rendering should be happening 60 times/s. The logic above seems to try to ensure that the logic
		// does not delay the rendering too much.
	}

	private static void QueueTick()
	{
		if (!m_renderingToken)
		{
			Windows.UI.Xaml.Media.CompositionTarget.Rendering += OnRendering;
			m_renderingToken = true;
		}
	}
}
