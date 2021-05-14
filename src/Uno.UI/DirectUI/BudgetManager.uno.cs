using System;
using System.Linq;

namespace DirectUI
{
	partial class BudgetManager
	{
		public int GetElapsedMilliSecondsSinceLastUITick()
		{
			// This method is use to re-dispatch a work item to the next dispatcher loop if we are about to freeze UI for too long
			// (40 ms are allowed, cf CalendarViewGeneratorMonthViewHost.BUDGET_MANAGER_DEFAULT_LIMIT)

			return 1; 
		}
	}
}
