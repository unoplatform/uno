#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Helpers;

[assembly: ElementMetadataUpdateHandlerAttribute(typeof(Uno.UI.Xaml.Controls.AlcContentHost), typeof(Uno.UI.Xaml.Controls.AlcContentHostMetadataUpdateHandler))]

namespace Uno.UI.Xaml.Controls;

internal static class AlcContentHostMetadataUpdateHandler
{
	public static void ElementUpdate(FrameworkElement element, Type[] updatedTypes)
	{
		if (element is AlcContentHost alcHost)
		{
			alcHost.ForceContentRefresh(updatedTypes);
		}
	}
}

public sealed partial class AlcContentHost
{
	/// <summary>
	/// Forces recreation of updated content elements in the hosted visual tree.
	/// The standard HR visual tree update may not correctly swap views when content
	/// is wrapped in intermediary hosts (e.g. HotDesignClientHost) across the ALC boundary.
	/// This method walks the tree to find and replace the specific updated elements.
	/// </summary>
	/// <remarks>
	/// Currently limited by a Roslyn E&amp;C constraint: hot reload deltas update the
	/// EmbeddedXamlSourcesProvider (raw XAML strings) but may not include the updated
	/// compiled InitializeComponent() method from source generators. New instances
	/// created here will use the updated C# code but may still render the original XAML
	/// until the source generator output is included in deltas.
	/// </remarks>
	internal void ForceContentRefresh(Type[] updatedTypes)
	{
		if (Content is not UIElement rootContent)
		{
			return;
		}

		ReplaceUpdatedElements(rootContent, updatedTypes);
	}

	[UnconditionalSuppressMessage("Trimming", "IL2072")]
	private static void ReplaceUpdatedElements(UIElement root, Type[] updatedTypes)
	{
		var childCount = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(root);
		for (var i = 0; i < childCount; i++)
		{
			var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(root, i);

			if (child is FrameworkElement fe)
			{
				var feType = fe.GetType();
				// Check both direct match (in-place modification) and replacement type match
				// (new type created via MetadataUpdateOriginalTypeAttribute).
				var replacementType = feType.GetReplacementType();
				var isUpdated = Array.Exists(updatedTypes, t => t == feType) || replacementType != feType;

				if (isUpdated)
				{
					var targetType = replacementType != feType ? replacementType : feType;
					try
					{
						if (Activator.CreateInstance(targetType) is FrameworkElement newInstance)
						{
							var parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(fe);
							if (parent is ContentControl cc)
							{
								cc.Content = newInstance;
								return;
							}
							if (parent is ContentPresenter cp)
							{
								var ownerCc = FindParentContentControl(cp);
								if (ownerCc is not null)
								{
									ownerCc.Content = newInstance;
									return;
								}
							}
						}
					}
					catch
					{
						// Best effort
					}
				}
			}

			if (child is UIElement childElement)
			{
				ReplaceUpdatedElements(childElement, updatedTypes);
			}
		}
	}

	private static ContentControl? FindParentContentControl(DependencyObject element)
	{
		var parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(element);
		while (parent is not null)
		{
			if (parent is ContentControl cc)
			{
				return cc;
			}
			parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(parent);
		}
		return null;
	}
}
