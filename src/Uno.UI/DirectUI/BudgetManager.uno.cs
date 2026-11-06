using System;
using System.Diagnostics;

namespace DirectUI
{
	partial class BudgetManager
	{
		private long _endFrameTimestamp = Stopwatch.GetTimestamp();

		// TODO Uno: WinUI stamps the end of the UI tick. Uno has no such tick, so BuildTreeService stamps the start of each pass instead.
		internal void StoreFrameTime() => _endFrameTimestamp = Stopwatch.GetTimestamp();

		public int GetElapsedMilliSecondsSinceLastUITick()
			=> (int)Stopwatch.GetElapsedTime(_endFrameTimestamp).TotalMilliseconds;
	}
}
