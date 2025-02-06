// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ItemContainerAutomationPeer.cpp, tag winui3/release/1.5.0

using Windows.UI.Xaml.Automation.Provider;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Uno.UI.Helpers.WinUI;

namespace Windows.UI.Xaml.Automation.Peers;

partial class ItemContainerAutomationPeer : FrameworkElementAutomationPeer, ISelectionItemProvider, IInvokeProvider
{
	public ItemContainerAutomationPeer(ItemContainer owner)
		: base(owner)
	{
	}

	// IAutomationPeerOverrides
	protected override string GetNameCore()
	{
		string returnHString = base.GetNameCore();

		// If a name hasn't been provided by AutomationProperties.Name in markup:
		if (returnHString.Length == 0)
		{
			if (Owner is ItemContainer itemContainer)
			{
				returnHString = SharedHelpers.TryGetStringRepresentationFromObject(itemContainer.Child);
			}
		}

		if (returnHString.Length == 0)
		{
			returnHString = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_ItemContainerDefaultControlName);
		}

		return returnHString;
	}

	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		if (Owner is ItemContainer itemContainer)
		{
			if (patternInterface == PatternInterface.SelectionItem)
			{
#if MUX_PRERELEASE
				bool canUserSelect = (itemContainer.CanUserSelect() & (ItemContainerUserSelectMode.Auto | ItemContainerUserSelectMode.UserCanSelect)) != 0;
#else
				bool canUserSelect = (GetImpl().CanUserSelectInternal() & (ItemContainerUserSelectMode.Auto | ItemContainerUserSelectMode.UserCanSelect)) != 0;
#endif

				if (canUserSelect)
				{
					return this;
				}

			}
			else if (patternInterface == PatternInterface.Invoke)
			{
#if MUX_PRERELEASE
				bool canUserInvoke = (itemContainer.CanUserInvoke() & ItemContainerUserInvokeMode.UserCanInvoke) != 0;
#else
				bool canUserInvoke = (GetImpl().CanUserInvokeInternal() & ItemContainerUserInvokeMode.UserCanInvoke) != 0;
#endif

				if (canUserInvoke)
				{
					return this;
				}
			}
		}

		return base.GetPatternCore(patternInterface);
	}

	protected override AutomationControlType GetAutomationControlTypeCore()
	{
		return AutomationControlType.ListItem;
	}

	protected override string GetLocalizedControlTypeCore()
	{
		return ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_ItemContainerDefaultControlName);
	}

	protected override string GetClassNameCore()
	{
		return nameof(ItemContainer);
	}

	// IInvokeProvider
	public void Invoke()
	{
		if (GetImpl() is { } itemContainer)
		{
#if MUX_PRERELEASE
			bool canUserInvoke = (itemContainer.CanUserInvoke() & ItemContainerUserInvokeMode.UserCanInvoke) != 0;
#else
			bool canUserInvoke = (itemContainer.CanUserInvokeInternal() & ItemContainerUserInvokeMode.UserCanInvoke) != 0;
#endif

			if (canUserInvoke)
			{
				itemContainer.RaiseItemInvoked(ItemContainerInteractionTrigger.AutomationInvoke, null);
			}
		}
	}

	//ISelectionItemProvider
	public bool IsSelected
	{
		get
		{
			if (GetImpl() is { } itemContainer)
			{
				return itemContainer.IsSelected;
			}
			return false;
		}
	}

	public IRawElementProviderSimple SelectionContainer
	{
		get
		{
			if (GetImpl() is { } itemContainer)
			{
				UIElement GetSelectionContainer()
				{
					var parent = VisualTreeHelper.GetParent(itemContainer);

					while (parent is not null)
					{
						if (parent is UIElement parentAsUIElement)
						{
							if (FrameworkElementAutomationPeer.CreatePeerForElement(parentAsUIElement) is { } peer)
							{
								if (peer is ISelectionProvider selectionProvider)
								{
									return parentAsUIElement;
								}
							}
						}

						parent = VisualTreeHelper.GetParent(parent);
					}

					return parent as UIElement;
				};

				if (GetSelectionContainer() is { } selectionContainer)
				{
					if (FrameworkElementAutomationPeer.CreatePeerForElement(selectionContainer) is { } peer)
					{
						return ProviderFromPeer(peer);
					}
				}
			}

			return null;
		}
	}

	public void AddToSelection()
	{
		UpdateSelection(true);
	}

	public void RemoveFromSelection()
	{
		UpdateSelection(false);
	}

	public void Select()
	{
		UpdateSelection(true);
	}

	private ItemContainer GetImpl()
	{
		ItemContainer impl = null;

		if (Owner is ItemContainer itemContainer)
		{
			impl = itemContainer;
		}

		return impl;
	}

	private void UpdateSelection(bool isSelected)
	{
		if (GetImpl() is { } itemContainer)
		{
			if (isSelected)
			{
#if MUX_PRERELEASE
				bool canUserSelect = (itemContainer.CanUserSelect() & (ItemContainerUserSelectMode.Auto | ItemContainerUserSelectMode.UserCanSelect)) != 0;
#else
				bool canUserSelect = (itemContainer.CanUserSelectInternal() & (ItemContainerUserSelectMode.Auto | ItemContainerUserSelectMode.UserCanSelect)) != 0;
#endif

				if (canUserSelect)
				{
					itemContainer.IsSelected = true;
				}
			}
			else
			{
				itemContainer.IsSelected = false;
			}
		}
	}
}
