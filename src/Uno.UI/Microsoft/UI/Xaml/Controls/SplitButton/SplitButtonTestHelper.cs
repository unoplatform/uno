using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Documents;

namespace Microsoft.UI.Private.Controls
{
	internal class SplitButtonTestHelper
	{
		private bool m_simulateTouch = false;

		public static SplitButtonTestHelper Instance { get; } = new SplitButtonTestHelper();

		public static bool SimulateTouch
		{
			get => Instance.m_simulateTouch;
			set => Instance.m_simulateTouch = value;
		}
	}
}
