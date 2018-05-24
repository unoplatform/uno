using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls
{
	public partial class GroupStyle
	{
		public DataTemplate HeaderTemplate { get; set; }
        public Style HeaderContainerStyle { get; set; }
        public bool HidesIfEmpty { get; set; }
	}
}
