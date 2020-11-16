using System;
using System.Linq;

namespace Microsoft.UI.Xaml.Controls
{
	public partial struct FlowLayoutAnchorInfo
	{
		internal FlowLayoutAnchorInfo(in int index, in double offset)
		{
			Index = index;
			Offset = offset;
		}

		public int Index;

		public double Offset;
	};
}
