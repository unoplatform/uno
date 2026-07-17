#if HAS_UNO // We don't have access to the instance of the page entry in the backstack on Windows

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using Microsoft.UI.Xaml.Media;
using Uno.Foundation.Logging;
using Uno.UI.Helpers;

[assembly: ElementMetadataUpdateHandlerAttribute(typeof(Microsoft.UI.Xaml.Controls.Frame), typeof(Microsoft.UI.Xaml.Controls.Frame.FrameElementMetadataUpdateHandler))]

namespace Microsoft.UI.Xaml.Controls;

partial class Frame
{
	internal static partial class FrameElementMetadataUpdateHandler
	{
		public static void ElementUpdate(FrameworkElement element, Type[] updatedTypes)
		{
			var frame = element as Frame;
			if (frame is null)
			{
				if (typeof(FrameElementMetadataUpdateHandler).Log().IsEnabled(LogLevel.Warning))
				{
					typeof(FrameElementMetadataUpdateHandler).Log().LogWarning($"AfterElementReplaced should only be called with Frame instances");
				}

				return;
			}

			if (frame._useWinUIBehavior)
			{
				foreach (var type in updatedTypes)
				{
					// Note: Does not support CNOMUA
					frame.RemovePageFromCache(type.FullName);
					frame.RemovePageFromCache(Navigation.PageStackEntry.BuildDescriptor(type));
				}

				PatchStrandedContent(frame, updatedTypes);
			}
			else // Uno's legacy implementation
			{
				foreach (var entry in frame.BackStack)
				{
					var expectedType = entry.SourcePageType.GetReplacementType();
					if (entry.Instance is not null &&
						entry.Instance.GetType() != expectedType)
					{
						if (typeof(FrameElementMetadataUpdateHandler).Log().IsEnabled(LogLevel.Trace))
						{
							typeof(FrameElementMetadataUpdateHandler).Log().Trace($"Backstack entry instance {entry.Instance.GetType().Name} replaced by instance of {expectedType.Name}");
						}

						var dc = entry.Instance.DataContext;
						entry.Instance = Activator.CreateInstance(expectedType) as Page;
						if (entry.Instance is not null)
						{
							entry.Instance.Frame = frame;
							entry.Instance.DataContext = dc;
						}
					}

					if (entry.SourcePageType is not null &&
						entry.SourcePageType != expectedType)
					{
						if (typeof(FrameElementMetadataUpdateHandler).Log().IsEnabled(LogLevel.Trace))
						{
							typeof(FrameElementMetadataUpdateHandler).Log().Trace($"Backstack entry SourcePageType changed from {entry.SourcePageType.Name} to {expectedType.Name}");
						}

						entry.SourcePageType = expectedType;
					}
				}

				PatchStrandedContent(frame, updatedTypes);
			}
		}

		/// <summary>
		/// Re-creates the frame's current page when its type was hot-reloaded but the page
		/// was never materialized in the visual tree — e.g. the frame lives under an ancestor
		/// that has not had a layout pass, so the page exists only as <see cref="ContentControl.Content"/>.
		/// The hot-reload visual-tree walk enumerates materialized children only, so it cannot
		/// replace such an instance itself; without this patch the stale pre-update page is shown
		/// once the subtree is finally materialized.
		/// </summary>
		[UnconditionalSuppressMessage("Trimming", "IL2072")]
		// Hot reload is only expected to work when trimming is disabled; TypeMappings
		// preserves the replacement types' constructors.
		private static void PatchStrandedContent(Frame frame, Type[] updatedTypes)
		{
			if (frame.Content is not Page currentPage)
			{
				return;
			}

			// A materialized page is enumerated (and replaced) by the hot-reload visual-tree
			// walk itself — patching it here as well would double-replace it.
			if (VisualTreeHelper.GetParent(currentPage) is not null)
			{
				return;
			}

			var liveType = currentPage.GetType();
			var expectedType = liveType.GetReplacementType();

			// CNOMU update: the live type maps to a replacement type.
			// In-place (EnC) update: same type identity, but present in the updated list.
			var isUpdated = expectedType != liveType || Array.IndexOf(updatedTypes, liveType) >= 0;
			if (!isUpdated || expectedType.GetConstructor(Type.EmptyTypes) is null)
			{
				return;
			}

			if (typeof(FrameElementMetadataUpdateHandler).Log().IsEnabled(LogLevel.Debug))
			{
				typeof(FrameElementMetadataUpdateHandler).Log().Debug(
					$"Re-creating stranded (unmaterialized) frame content {liveType.Name} as {expectedType.Name}");
			}

			if (Activator.CreateInstance(expectedType) is not Page newPage)
			{
				return;
			}

			newPage.Frame = frame;
			newPage.DataContext = currentPage.DataContext;

			// Keep the navigation history pointing at the new instance. In legacy mode
			// OnContentChanged syncs CurrentEntry automatically when Content changes; the
			// WinUI-behavior history entry must be patched explicitly.
			if (frame._useWinUIBehavior && frame.GetCurrentPageStackEntry() is { } entry)
			{
				entry.Instance = newPage;
				SetSourcePageType(entry, expectedType);
			}

			frame.SetContent(newPage);

			[UnconditionalSuppressMessage("Trimming", "IL2067")]
			// 'expectedType' comes from TypeMappings which preserves constructors for hot reload.
			static void SetSourcePageType(Navigation.PageStackEntry entry, Type type)
			{
				entry.SourcePageType = type;
			}
		}
	}
}
#endif
