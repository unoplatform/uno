#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI.DataBinding;

namespace Windows.UI.Xaml;

#if ENABLE_LEGACY_TEMPLATED_PARENT_SUPPORT
internal static class TemplatedParentScope
{
	private static readonly Stack<MaterializingTemplateInfo> MaterializingTemplateStack = new();

	/// <summary>Set the templated-parent for the dependency-object based on the currently materializing template.</summary>
	/// <param name="do"></param>
	/// <param name="reapplyTemplateBindings">Should be true, if not called from ctor.</param>
	/// <param name="store"></param>
	internal static void UpdateTemplatedParentIfNeeded(DependencyObject? @do, bool reapplyTemplateBindings = false, DependencyObjectStore? store = null)
	{
		// note: `store` instance is used within DOStore.ctor to avoid stack overflow.

		if (@do is null) return;
		if (GetCurrentTemplate() is { IsLegacyTemplate: true, TemplatedParent: { } tp })
		{
			UpdateTemplatedParent(@do, tp, reapplyTemplateBindings, store);
		}
	}

	internal static void UpdateTemplatedParent(DependencyObject? @do, DependencyObject tp, bool reapplyTemplateBindings = true, DependencyObjectStore? store = null)
	{
		if (@do is IDependencyObjectStoreProvider provider)
		{
			(store ?? provider.Store).SetTemplatedParent2(tp);

			// note: This can be safely removed, once moving away from legacy impl.
			// In the new impl, the templated-parent would be immediately available
			// before any binding is applied, so there is no need to force update.
			if (reapplyTemplateBindings)
			{
				(store ?? provider.Store).ApplyTemplateBindings();
			}
		}
	}

	internal static MaterializingTemplateInfo? GetCurrentTemplate()
	{
		return MaterializingTemplateStack.TryPeek(out var result) ? result : null;
	}

	internal static void PushScope(DependencyObject? templatedParent, bool isLegacyTemplate) =>
		MaterializingTemplateStack.Push(new(templatedParent, isLegacyTemplate));

	internal static void PopScope() => MaterializingTemplateStack.Pop();

	internal record MaterializingTemplateInfo(DependencyObject? TemplatedParent, bool IsLegacyTemplate);
}
#endif
