using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.Foundation;

using NativeMethods = __Windows.__System.Display.DisplayRequest.NativeMethods;

namespace Windows.System.Display
{
	public partial class DisplayRequest
	{
		partial void ActivateScreenLock()
		{
			NativeMethods.ActivateScreenLock();
		}

		partial void DeactivateScreenLock()
		{
			NativeMethods.DeactivateScreenLock();
		}
	}
}
