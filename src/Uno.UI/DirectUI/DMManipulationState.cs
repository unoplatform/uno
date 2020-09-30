using System;
using System.Collections.Generic;
using System.Text;

namespace DirectUI
{
	internal enum DMManipulationState
	{
		DMManipulationStarting = 1,
		DMManipulationStarted = 2,
		DMManipulationDelta = 3,
		DMManipulationLastDelta = 4,
		DMManipulationCompleted = 5,
		ConstantVelocityScrollStarted = 6,
		ConstantVelocityScrollStopped = 7
	}
}
