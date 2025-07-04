// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using DirectUI;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Windows.UI.Xaml.Controls;

internal class ContentDialogMetadata
{
	private TrackerCollection<ContentDialog> m_openDialogs;

	internal void AddOpenDialog(ContentDialog dialog)
	{
#if DBG
    {
        var contentDialog = (ContentDialog*)(dialog);

        IDependencyObject parent;
        if (contentDialog.m_placementMode == ContentDialog.PlacementMode.InPlace)
        {
            parent = contentDialog.Parent;
        }

        bool hasOpenDialog = false;
        HasOpenDialog(parent, &hasOpenDialog);

        // Should not be adding a dialog if one is already open under a given parent.
        MUX_ASSERT(!hasOpenDialog);
    }
#endif

		if (m_openDialogs is null)
		{
			TrackerCollection<ContentDialog> openDialogs = new();
			m_openDialogs = openDialogs;
		}

		m_openDialogs.Add(dialog);
	}

	internal void RemoveOpenDialog(ContentDialog dialog)
	{
		MUX_ASSERT(m_openDialogs is not null);

		uint itemIndex = 0;
		bool itemFound = false;
		m_openDialogs.IndexOf(dialog, &itemIndex, &itemFound);

		if (itemFound)
		{
			m_openDialogs.RemoveAt(itemIndex);
		}
		else
		{
			// Calls to RemoveOpenDialog should be paired with a call to AddOpenDialog, so we expect it to be found.
			MUX_ASSERT(false);
		}
	}

	private bool IsOpen(ContentDialog dialog)
	{
		MUX_ASSERT(m_openDialogs);

		bool isOpenDialog = false;

		uint itemIndex = 0;
		bool itemFound = false;
		m_openDialogs.IndexOf(dialog, &itemIndex, &itemFound);

		isOpenDialog = !!itemFound;

		return isOpenDialog;
	}

	private bool HasOpenDialog(DependencyObject parent)
	{
		*hasOpenDialog = false;

		(ForEachOpenDialog([&parent, &hasOpenDialog](ContentDialog * openDialog, bool & stopIterating)


	{
			var openContentDialog = (ContentDialog*)(openDialog);

			// If parent is not null, then we're looking for any open in-place dialogs with that same parent.
			// If it is null, then we're looking for any open dialogs in the popup root.
			if (parent != null)
			{
				if (openContentDialog.m_placementMode == ContentDialog.PlacementMode.InPlace)
				{
					bool isAncestorOf = false;
					(DependencyObject*)(parent).IsAncestorOf(openContentDialog, &isAncestorOf);
					if (isAncestorOf)
					{
						*hasOpenDialog = true;
						stopIterating = true;
					}
				}
			}
			else if (openContentDialog.m_placementMode != ContentDialog.PlacementMode.InPlace)
			{
				*hasOpenDialog = true;
				stopIterating = true;
			}

			return S_OK;
		}));

		return S_OK;
	}

	private void ForEachOpenDialog(Func<ContentDialog, bool> action)
	{
		if (m_openDialogs is not null)
		{
			IIterator<ContentDialog*> iter;
			m_openDialogs.First(&iter);

			bool hasCurrent = false;
			hasCurrent = iter.HasCurrent;

			while (hasCurrent)
			{
				ContentDialog current;
				current = iter.Current;

				bool stopIterating = false;
				action(current, stopIterating);
				if (stopIterating)
				{
					break;
				}

				iter.MoveNext(&hasCurrent);
			}
		}

		return S_OK;
	}

}

