#nullable enable

using Microsoft.UI.Xaml.Media.Animation;

namespace Microsoft.UI.Xaml.Controls.Primitives;

public abstract partial class ModernCollectionBasePanel
{
	// MUX Reference panel.cpp (CPanel::GetTransitionsForChildElementNoAddRef), tag winui3/release/2.5.1, commit ba3a8d59e6d
	internal override TransitionCollection? GetTransitionsForChildElementNoAddRef(UIElement pChild)
		=> ChildrenTransitions ?? (m_cacheManager.IsItemsHostRegistered() ? (m_cacheManager.GetItemsHost() as ItemsControl)?.ItemContainerTransitions : null);

	// CUIElementCollection and EnterImpl: only a child that can transition gets storage, so a panel without transitions pays nothing.
	internal override void OnChildEntered(UIElement child)
	{
		if (child.m_pLayoutTransitionStorage is not { } storage)
		{
			if (GetTransitionsForChildElementNoAddRef(child) is not { Count: > 0 })
			{
				return;
			}

			storage = Transition.EnsureLayoutTransitionStorage(child);
		}

		storage.m_enteredTreeCounter = Transition.EnteredInThisTick;
	}

	// Called when transitions get set on a host whose panel already has children (panel.cpp walks them the same way).
	internal void EnsureChildrenLayoutTransitionStorage()
	{
		foreach (var child in Children)
		{
			if (GetTransitionsForChildElementNoAddRef(child) is { Count: > 0 })
			{
				Transition.EnsureLayoutTransitionStorage(child);
			}
		}
	}
}
