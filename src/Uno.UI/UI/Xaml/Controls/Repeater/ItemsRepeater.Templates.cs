using System;
using System.Collections.Specialized;
using Uno.Disposables;
using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls;

// **************************************************************************
// PARTIAL FILE: ItemsRepeater.Templates.cs
// 
// PURPOSE: Contains Uno-specific dynamic template update functionality for ItemsRepeater.
// This file implements runtime DataTemplate updates while maintaining layout safety.
//
// ARCHITECTURE DECISION: Partial file approach
// - Keeps WinUI-imported code (ItemsRepeater.cs) minimal and unchanged (4-5 lines)
// - Isolates all Uno-specific functionality in this separate file
// - Facilitates easier upstream merging and maintenance
//
// CODE DUPLICATION NOTE:
// This file contains intentional duplication of wrapper update logic from OnItemTemplateChanged.
// This is architecturally necessary to bypass layout constraints while maintaining ViewManager
// compatibility. See UpdateItemTemplateWrapper() documentation for detailed justification.
//
// RELATED GITHUB PR: https://github.com/unoplatform/uno/pull/[PR_NUMBER]
// The working GitHub PR modifies OnItemTemplateChanged directly. Our partial file approach
// achieves the same functionality through controlled duplication.
// **************************************************************************

// Partial class file containing dynamic template update functionality for ItemsRepeater.
partial class ItemsRepeater
{
	// =====================
	// DATA TEMPLATE UPDATES
	// =====================
	// 
	// -- FLOW --
	//
	// Template Change Request (from Uno.UI.TemplateManager helper)
	//     ↓
	// HandleDynamicTemplateUpdate()
	//     ├─→ Always: UpdateItemTemplateWrapper() (ViewManager requirement) ✅
	//     ├─→ Layout Active? YES: Defer reset with DispatcherQueue ⏱️
	//     └─→ Layout Active? NO: Proceed with dynamic reset ✅
	//         ↓
	//     Callback Triggered (RefreshAllItemsForTemplateUpdate)
	//         ↓
	//     m_isLayoutInProgress Check
	//         ├─→ YES: Defer callback execution ⏱️
	//         └─→ NO: Execute reset ✅
	//             ↓
	//         SafeTemplateReset()
	//             ↓
	//         Final Layout Check
	//             ├─→ YES: Defer reset operation ⏱️
	//             └─→ NO: Execute TriggerTemplateReset() safely ✅


	/// <summary>
	/// Flag to prevent reentrancy during template reset operations.
	/// When true, all template change operations are blocked to prevent
	/// cascading updates that could violate layout constraints.
	/// </summary>
	private bool m_isResettingTemplate;

	/// <summary>
	/// Handles dynamic template updates when enabled.
	/// This consolidates only the NEW behavior logic for dynamic template updates.
	/// </summary>
	/// <returns>True if dynamic update was handled, false if original behavior should execute</returns>
	private bool HandleDynamicTemplateUpdate(object oldValue, object newValue)
	{
		// CRITICAL: Block all template changes during reset operations
		// This prevents cascading updates from OnItemsChangedCore
		if (m_isResettingTemplate)
		{
			return false; // Let original behavior handle it (will likely queue for later)
		}

		// If layout is in progress, we need special handling
		// The ViewManager might try to set a default template, which would trigger the constraint violation
		if (m_isLayoutInProgress)
		{
			// During layout, we only set up subscriptions but prevent the original behavior
			// to avoid the "ItemTemplate cannot be changed during layout" exception

			if (Uno.UI.TemplateManager.IsDataTemplateDynamicUpdateEnabled)
			{
				// Subscribe to template updates for the new template (if applicable)
				if (newValue is DataTemplate layoutDataTemplate)
				{
					// Only subscribe if the dynamic template update feature is enabled
					Uno.UI.TemplateUpdateSubscription.Attach(this, layoutDataTemplate, RefreshAllItemsForTemplateUpdate);
				}
			}

			// CRITICAL: Update m_itemTemplateWrapper even during layout
			// ViewManager REQUIRES this to be valid for element creation
			UpdateItemTemplateWrapper(newValue);

			// CRITICAL: Return TRUE to prevent original behavior during layout
			// This bypasses the "ItemTemplate cannot be changed during layout" exception
			// The template assignment will be deferred until layout is complete
			_ = DispatcherQueue.TryEnqueue(() =>
			{
				if (!m_isLayoutInProgress)
				{
					// Retry the template assignment when layout is complete
					ItemTemplate = newValue;
				}
			});

			return true; // Block original behavior to prevent constraint violation
		}

		var templateCanBeUpdated = false;

		// Subscribe to template updates for the new template
		if (newValue is DataTemplate newDataTemplate)
		{
			// Only subscribe if the dynamic template update feature is enabled
			templateCanBeUpdated = Uno.UI.TemplateUpdateSubscription.Attach(this, newDataTemplate, RefreshAllItemsForTemplateUpdate);
		}

		// Execute dynamic template reset only if feature is enabled
		if (templateCanBeUpdated)
		{
			// Update wrapper first before resetting
			UpdateItemTemplateWrapper(newValue);
			// Safe to reset immediately since we verified layout is not in progress
			SafeTemplateReset(oldValue ?? newValue);
			return true; // Indicate that dynamic update was handled
		}

		return false; // Indicate that original behavior should execute
	}

	/// <summary>
	/// Triggers a reset of all items when template changes.
	/// </summary>
	/// <param name="templateForLayoutNotification">The template to pass to layout for reset notification (oldTemplate for template change, current template for dynamic update)</param>
	/// <param name="clearRecyclePool">Whether to clear the recycle pool after reset</param>
	private void TriggerTemplateReset(object templateForLayoutNotification, bool clearRecyclePool = false)
	{
		// Additional safety: Don't reset if ItemTemplate is null or if we don't have a valid template to notify
		if (ItemTemplate == null)
		{
			return;
		}

		// Use current template if templateForLayoutNotification is null
		templateForLayoutNotification ??= ItemTemplate;

		// Since the ItemTemplate has changed, we need to re-evaluate all the items that
		// have already been created and are now in the tree. The easiest way to do that
		// would be to do a reset.. Note that this has to be done before we change the template
		// so that the cleared elements go back into the old template.
		var layout = Layout;
		if (layout != null)
		{
			var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
			using var processingChange = Disposable.Create(() => m_processingItemsSourceChange = null);
			m_processingItemsSourceChange = args;

			if (layout is VirtualizingLayout virtualLayout)
			{
				virtualLayout.OnItemsChangedCore(GetLayoutContext(), templateForLayoutNotification, args);
			}
			else if (layout is NonVirtualizingLayout nonVirtualLayout)
			{
				// Walk through all the elements and make sure they are cleared for
				// non-virtualizing layouts.
				foreach (var element in Children)
				{
					if (GetVirtualizationInfo(element).IsRealized)
					{
						ClearElementImpl(element);
					}
				}
			}
		}

		// Clear the RecyclePool after layout has processed the reset (for dynamic updates)
		if (clearRecyclePool && ItemTemplate is DataTemplate template)
		{
			var recyclePool = RecyclePool.GetPoolInstance(template);
			recyclePool?.Clear();
		}

		InvalidateMeasure();
	}

	/// <summary>
	/// Called when a template is dynamically updated. Handles layout reentrancy safely.
	/// This method is only called when dynamic template updates are enabled.
	/// </summary>
	private void RefreshAllItemsForTemplateUpdate()
	{
		// CRITICAL: Block if already in reset operation to prevent cascading updates
		if (m_isResettingTemplate)
		{
			return; // Ignore cascading update requests
		}

		// CRITICAL: Never trigger any template changes during layout
		// This would violate the original constraint and cause crashes
		if (m_isLayoutInProgress)
		{
			// Defer the refresh until layout is completely finished
			_ = DispatcherQueue.TryEnqueue(() =>
			{
				if (!m_isLayoutInProgress)
				{
					RefreshAllItemsForTemplateUpdate();
				}
			});
			return;
		}

		// Only proceed if we're definitely not in a layout operation
		SafeTemplateReset(ItemTemplate, clearRecyclePool: true);
	}

	/// <summary>
	/// Safely triggers a template reset with reentrancy protection.
	/// </summary>
	/// <param name="templateForLayoutNotification">The template to pass to layout for reset notification</param>
	/// <param name="clearRecyclePool">Whether to clear the recycle pool after reset</param>
	private void SafeTemplateReset(object templateForLayoutNotification, bool clearRecyclePool = false)
	{
		// CRITICAL: Prevent reentrancy - block cascading resets
		if (m_isResettingTemplate)
		{
			return; // Already resetting, ignore this request
		}

		// CRITICAL: Never perform any reset operations during layout
		// This is a hard constraint that must be respected to avoid crashes
		if (m_isLayoutInProgress)
		{
			// Defer the reset until layout is completely finished
			_ = DispatcherQueue.TryEnqueue(() =>
			{
				if (!m_isLayoutInProgress)
				{
					SafeTemplateReset(templateForLayoutNotification, clearRecyclePool);
				}
			});
			return;
		}

		// Set flag to block any template changes during reset operation
		m_isResettingTemplate = true;
		try
		{
			TriggerTemplateReset(templateForLayoutNotification, clearRecyclePool);
		}
		finally
		{
			m_isResettingTemplate = false;
		}
	}

	/// <summary>
	/// Updates the ItemTemplateWrapper which is critical for ViewManager element creation.
	/// This must be kept in sync even during layout operations.
	/// 
	/// IMPORTANT: This method duplicates logic from OnItemTemplateChanged (lines 795-825 in the original WinUI code).
	/// This duplication is ARCHITECTURALLY NECESSARY because:
	/// 
	/// 1. PARTIAL FILE ARCHITECTURE: We use partial files to separate Uno-specific functionality
	///    from the original WinUI code, maintaining clean separation and easier upstream merging.
	/// 
	/// 2. LAYOUT CONSTRAINT BYPASS: The original OnItemTemplateChanged throws during layout.
	///    We need the wrapper update logic WITHOUT the layout constraint check.
	/// 
	/// 3. VIEWMANAGER DEPENDENCY: ViewManager.GetElement() requires m_itemTemplateWrapper to be
	///    valid immediately, even during layout operations, or it crashes with NullReferenceException.
	/// 
	/// 4. GITHUB PR COMPARISON: The working GitHub PR modifies OnItemTemplateChanged directly.
	///    Our partial file approach requires this controlled duplication to achieve the same result.
	/// 
	/// FUTURE: When this functionality is integrated upstream, this duplication will be eliminated
	/// by merging both approaches into the main OnItemTemplateChanged method.
	/// </summary>
	/// <param name="newValue">The new template value</param>
	private void UpdateItemTemplateWrapper(object newValue)
	{
		// Clear flag for bug #776
		m_isItemTemplateEmpty = false;
		m_itemTemplateWrapper = newValue as IElementFactoryShim;
		if (m_itemTemplateWrapper == null)
		{
			// ItemTemplate set does not implement IElementFactoryShim. We also 
			// want to support DataTemplate and DataTemplateSelectors automagically.
			if (newValue is DataTemplate dataTemplate) // Implements IElementFactory but not IElementFactoryShim
			{
				m_itemTemplateWrapper = new ItemTemplateWrapper(dataTemplate);
				if (dataTemplate.LoadContent() is FrameworkElement content)
				{
					// Due to bug https://github.com/microsoft/microsoft-ui-xaml/issues/3057, we need to get the framework
					// to take ownership of the extra implicit ref that was returned by LoadContent. The simplest way to do
					// this is to add it to a Children collection and immediately remove it.						
					var children = Children;
					children.Add(content);
					children.RemoveAt(children.Count - 1);
				}
				else
				{
					// We have a DataTemplate which is empty, so we need to set it to true
					m_isItemTemplateEmpty = true;
				}
			}
			else if (newValue is DataTemplateSelector selector) // Implements IElementFactory but not IElementFactoryShim
			{
				m_itemTemplateWrapper = new ItemTemplateWrapper(selector);
			}
			else if (newValue != null)
			{
				throw new ArgumentException("ItemTemplate", "ItemTemplate");
			}
		}

		// Bug in framework's reference tracking causes crash during
		// UIAffinityQueue cleanup. To avoid that bug, take a strong ref
		m_itemTemplate = newValue as IElementFactory; // DataTemplate of DataTemplateSelector
	}
}
