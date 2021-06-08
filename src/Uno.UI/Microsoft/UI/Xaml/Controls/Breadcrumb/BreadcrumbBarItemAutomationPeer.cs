// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

using Microsoft.UI.Xaml.Controls;
using Uno.UI.Helpers.WinUI;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Automation.Provider;

namespace Microsoft.UI.Xaml.Automation.Peers
{
	public partial class BreadcrumbBarItemAutomationPeer : FrameworkElementAutomationPeer, IInvokeProvider
	{
		public BreadcrumbBarItemAutomationPeer(BreadcrumbBarItem owner) : base(owner)
		{
		}

		// IAutomationPeerOverrides
		protected override string GetLocalizedControlTypeCore() =>
			ResourceAccessor.GetLocalizedStringResource(
				ResourceAccessor.SR_BreadcrumbBarItemLocalizedControlType);

		protected override object GetPatternCore(PatternInterface patternInterface)
		{
			if (patternInterface == PatternInterface.Invoke)
			{
				return this;
			}

			return base.GetPatternCore(patternInterface);
		}

		protected override string GetClassNameCore() => nameof(BreadcrumbBarItem);

		protected override AutomationControlType GetAutomationControlTypeCore() =>
			AutomationControlType.Button;

		private BreadcrumbBarItem? GetImpl()
		{
			BreadcrumbBarItem? impl = null;

			if (Owner is BreadcrumbBarItem breadcrumbItem)
			{
				impl = breadcrumbItem;
			}

			return impl;
		}

		// IInvokeProvider
		public void Invoke()
		{
			if (GetImpl() is { } breadcrumbItem)
			{
				breadcrumbItem.OnClickEvent(null, null);
			}
		}
	}
}
