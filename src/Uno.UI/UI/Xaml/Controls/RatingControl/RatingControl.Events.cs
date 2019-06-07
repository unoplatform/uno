using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;

namespace Windows.UI.Xaml.Controls
{
	//[PortStatus(Complete = true)]
	public partial class RatingControl
	{
		[PortStatus("From Generated/3.x", Complete = true)]
		public event TypedEventHandler<RatingControl, object> ValueChanged;

		[PortStatus("Helper function for DRY prinicipal", Complete = true)]
		private void RaiseValueChanged()
		{
			ValueChanged?.Invoke(this, null);
		}
	}
}
