using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Controls
{
	partial class FlowLayoutAlgorithm
	{
		internal int RealizedElementCount => m_elementManager.GetRealizedElementCount;
	}
}
