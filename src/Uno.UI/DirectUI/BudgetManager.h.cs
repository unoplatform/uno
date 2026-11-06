// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

//  Abstract:
//      Represents budgetmanager to keep the uitick times

namespace DirectUI
{
	internal partial class BudgetManager
	{
		// The default elapsed time before deferring
		//private const int BUDGET_MANAGER_DEFAULT_LIMIT = 40;

		internal bool UseUnbudgetedContainerBuild() => m_useUnbudgetedContainerBuild;

		internal void SetUseUnbudgetedContainerBuild(bool value) => m_useUnbudgetedContainerBuild = value;

		//long m_freq;
		//long m_startFrameTime;
		//long m_endFrameTime;

		// exceptionally set to True when (shift) tabbing into an unrealized item to give the BuildTreeService
		// enough time to prepare it to receive focus. This is specific to the Local/Cycle ListviewBase.TabNavigation.
		private bool m_useUnbudgetedContainerBuild;
	}
}
