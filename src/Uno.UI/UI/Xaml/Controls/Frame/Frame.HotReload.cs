#if HAS_UNO // We don't have access to the instance of the page entry in the backstack on Windows

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
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
		/// Re-creates the frame's current page when it was hot-reloaded while never
		/// materialized (see <see cref="ContentControlElementMetadataUpdateHandler.CreateReplacementForStrandedContent"/>),
		/// then keeps the navigation history pointing at the new instance.
		/// </summary>
		private static void PatchStrandedContent(Frame frame, Type[] updatedTypes)
		{
			if (ContentControlElementMetadataUpdateHandler.CreateReplacementForStrandedContent(frame, updatedTypes) is not Page newPage)
			{
				return;
			}

			newPage.Frame = frame;
			frame.SetContent(newPage);

			// Legacy mode syncs CurrentEntry in OnContentChanged; the WinUI-behavior
			// history entry must be patched explicitly.
			if (frame._useWinUIBehavior && frame.GetCurrentPageStackEntry() is { } entry)
			{
				entry.Instance = newPage;
				SetSourcePageType(entry, newPage.GetType());
			}

			[UnconditionalSuppressMessage("Trimming", "IL2067")]
			// Replacement types come from TypeMappings, which preserves their constructors.
			static void SetSourcePageType(Navigation.PageStackEntry entry, Type type)
			{
				entry.SourcePageType = type;
			}
		}
	}
}
#endif
