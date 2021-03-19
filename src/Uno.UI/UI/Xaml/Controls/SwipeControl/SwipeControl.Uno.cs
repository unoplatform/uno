using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Windows.UI.Xaml.Controls
{
	public partial class SwipeControl
	{
		[Conditional("DEBUG")]
		private static void SWIPECONTROL_TRACE_INFO(SwipeControl that, [CallerLineNumber] int TRACE_MSG_METH = -1, [CallerMemberName] string METH_NAME = null, SwipeControl _ = null)
		{

		}

		[Conditional("DEBUG")]
		private static void SWIPECONTROL_TRACE_VERBOSE(SwipeControl that, [CallerLineNumber] int TRACE_MSG_METH = -1, [CallerMemberName] string METH_NAME = null, SwipeControl _ = null)
		{

		}

		// TODO Uno - Interactions
		private void InitializeInteractionTracker() {  }

		private void ConfigurePositionInertiaRestingValues() { }

		private void IdleStateEntered(object @null, object @also_null) { }
	}
}
