using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.UI;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ContentControl
#if !__CROSSRUNTIME__ && !IS_UNIT_TESTS
		: ICustomClippingElement
#endif
	{
#if !__CROSSRUNTIME__ && !IS_UNIT_TESTS
		bool ICustomClippingElement.AllowClippingToLayoutSlot
		{
			get
			{
				if (Content is UIElement ue && ue.RenderTransform != null)
				{
					// If the Content is a UIElement and defines a RenderTransform,
					// no clipping should apply.

					return false;
				}
				if (TemplatedRoot is UIElement tr && tr.RenderTransform != null)
				{
					// Same for TemplatedRoot

					return false;
				}

				return true; // Clipping allowed
			}
		}

		bool ICustomClippingElement.ForceClippingToLayoutSlot => false;
#endif
	}
}
