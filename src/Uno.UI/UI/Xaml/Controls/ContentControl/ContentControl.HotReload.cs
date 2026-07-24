#if HAS_UNO
#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using Microsoft.UI.Xaml.Media;
using Uno.Foundation.Logging;
using Uno.UI.Helpers;

[assembly: ElementMetadataUpdateHandlerAttribute(typeof(Microsoft.UI.Xaml.Controls.ContentControl), typeof(Microsoft.UI.Xaml.Controls.ContentControlElementMetadataUpdateHandler))]

namespace Microsoft.UI.Xaml.Controls;

internal static partial class ContentControlElementMetadataUpdateHandler
{
	public static void ElementUpdate(FrameworkElement element, Type[] updatedTypes)
	{
		// Frame's own handler patches its content and syncs the navigation history;
		// skipping it here prevents the two handlers from double-replacing.
		if (element is not ContentControl contentControl || contentControl is Frame)
		{
			return;
		}

		if (CreateReplacementForStrandedContent(contentControl, updatedTypes) is { } newContent)
		{
			contentControl.Content = newContent;
		}
	}

	/// <summary>
	/// Re-creates content whose type was hot-reloaded while the content was never
	/// materialized in the visual tree (host not laid out, template never applied) —
	/// the HR visual-tree walk only enumerates materialized children and cannot
	/// replace such an instance itself. Returns null when there is nothing to patch.
	/// </summary>
	[UnconditionalSuppressMessage("Trimming", "IL2072")]
	// Hot reload requires trimming disabled; TypeMappings preserves replacement ctors.
	internal static FrameworkElement? CreateReplacementForStrandedContent(ContentControl host, Type[] updatedTypes)
	{
		// Data-item content rendered through a ContentTemplate is never re-created.
		if (host.Content is not FrameworkElement currentContent)
		{
			return null;
		}

		// Materialized content is replaced by the visual-tree walk itself.
		if (VisualTreeHelper.GetParent(currentContent) is not null)
		{
			return null;
		}

		var liveType = currentContent.GetType();
		var expectedType = liveType.GetReplacementType();

		// Same triggers as the walk: a CNOMU replacement type or an in-place (EnC) update.
		var isUpdated = expectedType != liveType || Array.IndexOf(updatedTypes, liveType) >= 0;
		if (!isUpdated)
		{
			return null;
		}

		if (typeof(ContentControlElementMetadataUpdateHandler).Log().IsEnabled(LogLevel.Debug))
		{
			typeof(ContentControlElementMetadataUpdateHandler).Log().Debug(
				$"Re-creating stranded (unmaterialized) content {liveType.Name} of {host.GetType().Name} as {expectedType.Name}");
		}

		FrameworkElement? newContent;
		try
		{
			// Centralized HR-aware creation (replacement-type resolution + bindable
			// metadata provider), also used by NavigationCache and PagePool.
			newContent = Frame.CreatePageInstance(liveType) as FrameworkElement;
		}
		catch (Exception e)
		{
			if (typeof(ContentControlElementMetadataUpdateHandler).Log().IsEnabled(LogLevel.Warning))
			{
				typeof(ContentControlElementMetadataUpdateHandler).Log().Warn(
					$"Could not re-create stranded content {liveType.Name} as {expectedType.Name}; keeping the stale instance.", e);
			}

			return null;
		}

		if (newContent is null)
		{
			if (typeof(ContentControlElementMetadataUpdateHandler).Log().IsEnabled(LogLevel.Warning))
			{
				typeof(ContentControlElementMetadataUpdateHandler).Log().Warn(
					$"Could not re-create stranded content {liveType.Name} as {expectedType.Name} (no instance produced); keeping the stale instance.");
			}

			return null;
		}

		// Copy the DataContext only when it was locally set; an inherited value must
		// keep flowing from the host instead of being pinned on the new instance.
		if (currentContent.ReadLocalValue(FrameworkElement.DataContextProperty) != DependencyProperty.UnsetValue)
		{
			newContent.DataContext = currentContent.DataContext;
		}

		return newContent;
	}
}
#endif
