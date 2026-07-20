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
		// Frame has its own element-update handler which additionally keeps the navigation
		// history in sync with the re-created page — let it own its content patching
		// entirely so the two handlers never double-replace the same instance.
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
	/// Creates a replacement for the host's current content when that content's type was
	/// hot-reloaded but the content was never materialized in the visual tree — e.g. the
	/// host lives under an ancestor that has not had a layout pass, so its template was
	/// never applied and the content exists only as <see cref="ContentControl.Content"/>.
	/// The hot-reload visual-tree walk enumerates materialized children only, so it cannot
	/// replace such an instance itself; without this, the stale pre-update content is what
	/// gets displayed once the subtree is finally materialized.
	/// Returns <see langword="null"/> when there is nothing to patch (data-item content,
	/// materialized content, type not updated, or no parameterless constructor).
	/// </summary>
	[UnconditionalSuppressMessage("Trimming", "IL2072")]
	// Hot reload is only expected to work when trimming is disabled; TypeMappings
	// preserves the replacement types' constructors.
	internal static FrameworkElement? CreateReplacementForStrandedContent(ContentControl host, Type[] updatedTypes)
	{
		// Content can be a plain data item rendered through a ContentTemplate — only view
		// instances can be re-created here.
		if (host.Content is not FrameworkElement currentContent)
		{
			return null;
		}

		// Materialized content is enumerated (and replaced) by the hot-reload visual-tree
		// walk itself — patching it here as well would double-replace it.
		if (VisualTreeHelper.GetParent(currentContent) is not null)
		{
			return null;
		}

		var liveType = currentContent.GetType();
		var expectedType = liveType.GetReplacementType();

		// CNOMU update: the live type maps to a replacement type.
		// In-place (EnC) update: same type identity, but present in the updated list.
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
			// Despite its name, Frame.CreatePageInstance is the centralized, type-agnostic
			// hot-reload-aware creation path (replacement-type resolution + bindable metadata
			// provider support) also used by NavigationCache and PagePool. No constructor
			// pre-check here: a bindable metadata activator may construct types a reflection
			// probe would reject, so creation failure is handled instead of predicted.
			newContent = Frame.CreatePageInstance(liveType) as FrameworkElement;
		}
		catch (Exception e)
		{
			// The replacement type cannot be constructed (e.g. no parameterless constructor
			// and no bindable activator) — keep the stale content rather than failing the
			// hot-reload pass; the visual-tree walk applies the same tolerance.
			if (typeof(ContentControlElementMetadataUpdateHandler).Log().IsEnabled(LogLevel.Debug))
			{
				typeof(ContentControlElementMetadataUpdateHandler).Log().Debug(
					$"Could not re-create stranded content {liveType.Name} as {expectedType.Name}: {e.Message}");
			}

			return null;
		}

		if (newContent is null)
		{
			return null;
		}

		// Carry the DataContext over only when the old content had its own value —
		// an inherited DataContext must keep flowing from the host rather than being
		// pinned onto the new instance.
		if (!ReferenceEquals(currentContent.DataContext, host.DataContext))
		{
			newContent.DataContext = currentContent.DataContext;
		}

		return newContent;
	}
}
#endif
