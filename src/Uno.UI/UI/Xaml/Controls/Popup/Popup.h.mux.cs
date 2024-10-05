using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.UI.Xaml.Controls.Primitives;

public partial class Popup
{
	private bool m_fIsLightDismiss;

	internal override bool IsFocusable =>
		m_fIsLightDismiss &&
		IsVisible() &&
		IsEnabledInternal() &&
		AreAllAncestorsVisible();
}
