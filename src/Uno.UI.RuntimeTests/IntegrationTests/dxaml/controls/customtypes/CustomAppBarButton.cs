#if HAS_UNO
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Tests.Enterprise.CustomTypes
{
	public partial class CustomAppBarButton : Button, ICommandBarElement
	{
		private bool m_isCompact;
		private int m_dynamicOverflowOrder;
		private bool m_IsInOverflow;

		public bool IsCompact { get => m_isCompact; set => m_isCompact = value; }
		public int DynamicOverflowOrder { get => m_dynamicOverflowOrder; set => m_dynamicOverflowOrder = value; }
		public bool IsInOverflow { get => m_IsInOverflow; }

		public CustomAppBarButton()
		{
			m_isCompact = false;
			m_dynamicOverflowOrder = 0;
			m_IsInOverflow = false;
		}
	}
}
#endif
