// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#if false

namespace DirectUI
{
	partial class BudgetManager
	{
		private void Initialize()
		{
			//bool success = false;

			//success = QueryPerformanceFrequency(&m_freq);

			//return;
		}

		private void StoreFrameTime(bool isBeginOfTick)
		{
			//bool success = false;

			//if (isBeginOfTick)
			//{
			//	m_startFrameTime = success = QueryPerformanceCounter();
			//}
			//else
			//{
			//	m_endFrameTime = success = QueryPerformanceCounter();
			//}

			//// does all ARM hardware support this
			//global::System.Diagnostics.Debug.Assert(success);
		}


		private void GetElapsedMilliSecondsSinceLastUITickImpl(out int returnValue)
		{
			//long lTimeCurrent = default;
			//bool success = false;

			//// todo: switch between end and start time based on whether we trust the tick to be accurate

			//success = QueryPerformanceCounter(lTimeCurrent);

			//if (success)
			//{
			//	double elapsedSeconds = (double)(lTimeCurrent.QuadPart - m_endFrameTime.QuadPart) / (double)(m_freq.QuadPart);

			//	returnValue = (int)(elapsedSeconds1000);
			//}
			//else
			//{
			//	// hardware that doesn't understand QPC?? Very unlikely, but in this case, let's not crash
			//	// and just return that no time has elapsed. This will trigger people relying on budget to see that they
			//	// have a lot of budget and at least not have them encounter a situation where they do not get to perform
			//	// certain work.
			//	returnValue = 0;
			//}

			returnValue = default;
		}
	}
}

#endif
