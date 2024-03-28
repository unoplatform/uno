// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.


using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
	internal partial class LayoutPanelLayoutContext : NonVirtualizingLayoutContext
	{
		private LayoutPanel m_owner;

		internal LayoutPanelLayoutContext(LayoutPanel owner)
		{
			m_owner = owner;
		}

		public override IReadOnlyList<UIElement> ChildrenCore => m_owner.Children.ToArray();

		protected internal override object LayoutStateCore
		{
			get => m_owner.LayoutState;
			set => m_owner.LayoutState = value;
		}
	}
}
