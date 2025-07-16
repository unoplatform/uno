﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls.Primitives;

public partial class Popup
{
	private DismissalTriggerFlags m_dismissalTriggerFlags;

	private bool m_fIsLightDismiss;

	internal bool IsFlyout => AssociatedFlyout is not null;

	internal override bool IsFocusable =>
		m_fIsLightDismiss &&
		IsVisible() &&
		IsEnabledInternal() &&
		AreAllAncestorsVisible();
}
