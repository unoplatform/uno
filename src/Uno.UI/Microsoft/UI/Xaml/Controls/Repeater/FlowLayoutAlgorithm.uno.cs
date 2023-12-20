using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
	partial class FlowLayoutAlgorithm
	{
		internal int RealizedElementCount => m_elementManager.GetRealizedElementCount;
	}
}
