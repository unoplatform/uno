// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\NavigationHistory.cpp, tag winui3/release/1.5.5, commit fd8e26f1d

//  Abstract:
//      Manages the sequence of navigated content and the navigations
//      between them. Serializes and deserializes the navigation entries
//      using Windows storage.
//  Notes:
//      Navigations are split into requests and commits because they can
//      be canceled synchronously, but are carried out asynchronously
//      because components are loaded asynchronously. Commits are atomic;
//      any intermediate error rolls back all changes.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace DirectUI;

internal partial class NavigationHistory
{
	static uint c_versionNumber = 1;

	public NavigationHistory()
	{
	}

	// TODO:MZ: Destructor ok?
	~NavigationHistory()
	{
		DeInit();
		m_pIFrame = null;
	}

	void DeInit()
	{
		m_isNavigationPending = false;
		m_isSetNavigationStatePending = false;
		m_navigationMode = NavigationMode.New;
	}

	private void ClearNavigationHistory()
	{


		// Clear the frame pointer on entries that are being removed from the
		// PageStack (BackStack or ForwardStack)
		ResetPageStackEntries(true /* isBackStack */);
		ResetPageStackEntries(false /* isBackStack */);

		m_tpBackStack.ClearInternal();
		m_tpForwardStack.ClearInternal();

		m_tpCurrentPageStackEntry = null;
		m_tpPendingPageStackEntry = null;

		DeInit();
	}


	internal static NavigationHistory Create(Frame frame)
	{
		NavigationHistory pNavigationHistory = new();

		pNavigationHistory.m_pIFrame = frame;

		PageStackEntryTrackerCollection spBackStack = new();

		spBackStack.Init(pNavigationHistory, true /* isBackStack */);
		pNavigationHistory.m_tpBackStack = spBackStack;

		PageStackEntryTrackerCollection spForwardStack = new();

		spForwardStack.Init(pNavigationHistory, false /* isBackStack */);
		pNavigationHistory.m_tpForwardStack = spForwardStack;

		return pNavigationHistory;
	}

	internal void NavigatePrevious()
	{
		var nCount = m_tpBackStack.Count;
		if (nCount < 1)
		{
			throw new InvalidOperationException("No entries in the back stack.");
		}

		m_isNavigationPending = true;
		m_navigationMode = NavigationMode.Back;

		var pIEntry = m_tpBackStack.GetAtEnd();
		m_tpPendingPageStackEntry = pIEntry;
	}

	internal void NavigateNext()
	{
		var nCount = m_tpForwardStack.Count;
		if (nCount < 1)
		{
			throw new InvalidOperationException("No entries in the back stack.");
		}

		m_isNavigationPending = true;
		m_navigationMode = NavigationMode.Forward;

		var pIEntry = m_tpForwardStack.GetAtEnd();
		m_tpPendingPageStackEntry = pIEntry;
	}

	internal void NavigateNew(
		 string newDescriptor,
		 object pParameter,
		 NavigationTransitionInfo pTransitionInfo)
	{
		m_navigationMode = NavigationMode.New;

		m_tpPendingPageStackEntry = null;

		var pEntry = PageStackEntry.Create(m_pIFrame, newDescriptor, pParameter, pTransitionInfo);
		m_tpPendingPageStackEntry = pEntry;

		m_isNavigationPending = true;
	}

	internal IList<PageStackEntry> GetBackStack() => m_tpBackStack;

	internal IList<PageStackEntry> GetForwardStack() => m_tpForwardStack;

	internal PageStackEntry GetCurrentPageStackEntry() => m_tpCurrentPageStackEntry;

	internal PageStackEntry GetPendingPageStackEntry()
	{
		if (!m_isNavigationPending)
		{
			throw new InvalidOperationException("Navigation is not pending.");
		}

		if (m_tpPendingPageStackEntry is null)
		{
			throw new InvalidOperationException("Pending page stack entry is null.");
		}

		return (PageStackEntry)(m_tpPendingPageStackEntry);
	}

	internal NavigationMode GetPendingNavigationMode()
	{
		if (!m_isNavigationPending)
		{
			throw new InvalidOperationException("Navigation is not pending.");
		}

		return m_navigationMode;
	}

	internal NavigationMode GetCurrentNavigationMode() => m_navigationMode;

	[UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "Types manipulated here have been marked earlier")]
	internal void CommitNavigation()
	{
		int nCount = 0;
		bool wasChanged = false;
		bool newCanGoBack = false;
		bool newCanGoForward = false;

		if (m_pIFrame is null)
		{
			throw new InvalidOperationException("Frame is null.");
		}
		if (!m_isNavigationPending)
		{
			throw new InvalidOperationException("Navigation is not pending.");
		}
		if (m_tpPendingPageStackEntry is null)
		{
			throw new InvalidOperationException("Pending page stack entry is null.");
		}

		var pFrame = (Frame)(m_pIFrame);
		var oldCanGoBack = pFrame.CanGoBack;
		var oldCanGoForward = pFrame.CanGoForward;
		var oldSourcePageType = pFrame.CurrentSourcePageType;

		try
		{
			// TODO:MZ: Check if this is needed
			//if (oldSourcePageType is not null)
			//{
			//	CClassInfo* pType = null;

			//	// NOTE: The call seems to serve as a type name check only.
			//	MetadataAPI.GetClassInfoByTypeName(oldSourcePageType, &pType);
			//}

			switch (m_navigationMode)
			{
				case NavigationMode.New:
					newCanGoBack = m_tpCurrentPageStackEntry != null;
					newCanGoForward = false;
					break;

				case NavigationMode.Back:
					nCount = m_tpBackStack.Count;
					if (nCount < 1)
					{
						throw new InvalidOperationException("No entries in the back stack.");
					}

					newCanGoBack = (nCount - 1) >= 1;
					newCanGoForward = true;
					break;

				case NavigationMode.Forward:
					nCount = m_tpForwardStack.Count;
					if (nCount < 1)
					{
						throw new InvalidOperationException("No entries in the forward stack.");
					}

					newCanGoBack = true;
					newCanGoForward = (nCount - 1) >= 1;
					break;

				default:
					throw new InvalidOperationException("Invalid navigation mode.");
			}

			var strNewDescriptor = m_tpPendingPageStackEntry.GetDescriptor();

			wasChanged = true;

			pFrame.CanGoBack = newCanGoBack;
			pFrame.CanGoForward = newCanGoForward;
			var newSourcePageType = Type.GetType(strNewDescriptor);
			pFrame.SourcePageType = newSourcePageType;
			pFrame.CurrentSourcePageType = newSourcePageType;

			switch (m_navigationMode)
			{
				case NavigationMode.New:
					if (m_tpCurrentPageStackEntry is not null)
					{
						m_tpBackStack.AddInternal((PageStackEntry)(m_tpCurrentPageStackEntry));
					}

					m_isNavigationPending = false;
					m_tpForwardStack.Clear();
					break;

				case NavigationMode.Back:
					if (m_tpCurrentPageStackEntry is not null)
					{
						m_tpForwardStack.AddInternal((PageStackEntry)(m_tpCurrentPageStackEntry));
					}

					m_tpBackStack.RemoveAtEndInternal();
					break;

				case NavigationMode.Forward:
					if (m_tpCurrentPageStackEntry is not null)
					{
						m_tpBackStack.AddInternal((PageStackEntry)(m_tpCurrentPageStackEntry));
					}

					m_tpForwardStack.RemoveAtEndInternal();
					break;
			}

			nCount = 0;
			nCount = m_tpBackStack.Count;
			pFrame.BackStackDepth = nCount;

			m_tpCurrentPageStackEntry = m_tpPendingPageStackEntry;

		}
		catch
		{
			if (wasChanged)
			{
				pFrame.CanGoBack = oldCanGoBack;
				pFrame.CanGoForward = oldCanGoForward;
				pFrame.SourcePageType = oldSourcePageType;
				pFrame.CurrentSourcePageType = oldSourcePageType;

				m_tpPendingPageStackEntry = null;
			}

			throw;
		}
		finally
		{
			m_isNavigationPending = false;
			m_tpPendingPageStackEntry = null;
		}
	}

	internal void CommitSetNavigationState(NavigationCache pNavigationCache)
	{
		bool newCanGoBack = false;
		bool newCanGoForward = false;

		if (!m_isSetNavigationStatePending)
		{
			throw new InvalidOperationException("Set navigation state is not pending.");
		}

		var pFrame = (Frame)(m_pIFrame);

		// Enable/Disable GoBack & GoForward
		var nBackStackCount = m_tpBackStack.Count;
		newCanGoBack = (nBackStackCount >= 1);
		var nForwardStackCount = m_tpForwardStack.Count;
		newCanGoForward = (nForwardStackCount >= 1);
		pFrame.CanGoBack = newCanGoBack;
		pFrame.CanGoForward = newCanGoForward;

		// See source type in IFrame
		if (m_tpCurrentPageStackEntry is not null)
		{
			var strDescriptior = m_tpCurrentPageStackEntry.GetDescriptor();
			var sourcePageType = Type.GetType(strDescriptior);
			pFrame.SourcePageType = sourcePageType;
			pFrame.CurrentSourcePageType = sourcePageType;
		}

		m_isSetNavigationStatePending = false;
	}

	private void ValidateCanChangePageStack()
	{
		// Make sure we are not in the middle of a navigation.
		if (m_isNavigationPending)
		{
			throw new InvalidOperationException("Frame is navigating.");
		}
	}

	private void ValidateCanInsertEntry(PageStackEntry pEntry)
	{
		// Make sure the entry being inserted is not null.
		if (pEntry is null)
		{
			throw new ArgumentNullException(nameof(pEntry));
		}

		// Make sure this PageStackEntry isn't already owned by another frame.
		var canAdd = pEntry.CanBeAddedToFrame(m_pIFrame);
		if (!canAdd)
		{
			throw new InvalidOperationException("PageStackEntry is already owned by another frame.");
		}
	}

	private void ValidateCanClearPageStack()
	{
		// Make sure we are not in the middle of a navigation.
		if (m_isNavigationPending)
		{
			throw new InvalidOperationException("Frame is navigating.");
		}
	}

	//------------------------------------------------------------------------
	//
	//  Method: ResetPageStackEntries
	//
	//  Synopsis:
	//     Clear the frame pointer on entries that are being removed from the
	//     PageStack (BackStack or ForwardStack)
	//
	//------------------------------------------------------------------------

	private void ResetPageStackEntries(bool isBackStack)
	{
		var iterator = isBackStack ? m_tpBackStack : m_tpForwardStack;
		foreach (var spEntry in iterator)
		{
			spEntry.SetFrame(null);
		}
	}


	//------------------------------------------------------------------------
	//
	//  Method: OnPageStackChanging
	//
	//  Synopsis:
	//     Do the necessary validations and clear the frame pointer
	//     on entries that are being removed from the
	//     PageStack (BackStack or ForwardStack)
	//
	//------------------------------------------------------------------------

	internal void OnPageStackChanging(
				 bool isBackStack,
				 CollectionChange action,
				 int index,
				 PageStackEntry pEntry)
	{

		int nCount = 0;
		Frame pFrame = null;
		PageStackEntry pIEntry = null;
		IList<PageStackEntry> spPageStack;

		if (m_pIFrame is null)
		{
			throw new InvalidOperationException("Frame is null.");
		}

		pFrame = (Frame)(m_pIFrame);

		if (isBackStack)
		{
			spPageStack = pFrame.BackStack;
		}
		else
		{
			spPageStack = pFrame.ForwardStack;
		}

		nCount = spPageStack.Count;

		// Do the necessary validations and clear the frame pointer on entries that are being removed.
		switch (action)
		{
			case CollectionChange.ItemChanged:
				{
					ValidateCanChangePageStack();
					ValidateCanInsertEntry((PageStackEntry)(pEntry));
					pIEntry = spPageStack[index];
					PageStackEntry pPageStackEntry = (PageStackEntry)(pIEntry);
					pPageStackEntry.SetFrame(null);
					break;
				}
			case CollectionChange.ItemInserted:
				{
					ValidateCanChangePageStack();
					ValidateCanInsertEntry((PageStackEntry)(pEntry));
					break;
				}
			case CollectionChange.ItemRemoved:
				{
					ValidateCanChangePageStack();
					pIEntry = spPageStack[index];
					PageStackEntry pPageStackEntry = (PageStackEntry)(pIEntry);
					pPageStackEntry.SetFrame(null);
					break;
				}
			case CollectionChange.Reset:
				{
					ValidateCanClearPageStack();
					ResetPageStackEntries(isBackStack);
					break;
				}
			default:
				throw new InvalidOperationException("Invalid collection change");
		}
	}

	//------------------------------------------------------------------------
	//
	//  Method: OnPageStackChanged
	//
	//  Synopsis:
	//     Update the frame pointer on entries that were added to the
	//     PageStack (BackStack or ForwardStack)and update the
	//     CanGoBack, CanGoForward and BackStackDepth properties.
	//
	//------------------------------------------------------------------------

	internal void OnPageStackChanged(
		 bool isBackStack,
		 CollectionChange action,
		 int index)
	{
		Frame pFrame = null;
		IList<PageStackEntry> spPageStack;

		if (m_pIFrame is null)
		{
			throw new InvalidOperationException("Frame is null.");
		}

		pFrame = (Frame)(m_pIFrame);

		if (isBackStack)
		{
			spPageStack = pFrame.BackStack;
		}
		else
		{
			spPageStack = pFrame.ForwardStack;
		}

		var nCount = spPageStack.Count;

		// Update the frame pointer on entries that were added and update the CanGoBack, CanGoForward and BackStackDepth properties.
		switch (action)
		{
			case CollectionChange.ItemInserted:
				{
					if (nCount <= index)
					{
						throw new ArgumentOutOfRangeException(nameof(index));
					}
					var pIEntry = spPageStack[index];
					PageStackEntry pPageStackEntry = (PageStackEntry)(pIEntry);
					pPageStackEntry.SetFrame(m_pIFrame);
					if (isBackStack)
					{
						pFrame.CanGoBack = true;
						pFrame.BackStackDepth = nCount;
					}
					else
					{
						pFrame.CanGoForward = true;
					}
					break;
				}
			case CollectionChange.ItemRemoved:
				{
					if (isBackStack)
					{
						pFrame.CanGoBack = ((nCount > 0 ? true : false));
						pFrame.BackStackDepth = nCount;
					}
					else
					{
						pFrame.CanGoForward = ((nCount > 0 ? true : false));
					}
					break;
				}
			case CollectionChange.Reset:
				{
					if (isBackStack)
					{
						pFrame.CanGoBack = false;
						pFrame.BackStackDepth = nCount;
					}
					else
					{
						pFrame.CanGoForward = false;
					}
					break;
				}
			default:
				throw new InvalidOperationException("Invalid collection change");
		}
	}

	//------------------------------------------------------------------------
	//
	//  Method: NavigationHistory.GetNavigationState
	//
	//  Synopsis:
	//     Serialize navigation history into an string
	//
	//     Format:
	//  <version number>,<number of pages>,<current page index>,
	//     <page1's type Name length>,<page1 type name>,<page1's parameter type (wf.PropertyType)>,
	//              <length of page1's serialized parameter>,<page1's serialized parameter>,
	//     :
	//     <pageN type name length>,<pageN type name>,<pageN's parameter type (wf.PropertyType)>,
	//              <length of pageN's serialized parameter>,<pageN's serialized parameter>,
	//
	//    If a page does not have a parameter, the format is:
	//     <pageN type name length>,<pageN type name>,<PropertyType.Empty>,
	//
	//    Supported parameter types are string, char, numeric and guid.
	//
	//------------------------------------------------------------------------

	internal string GetNavigationState()
	{
		StringBuilder buffer = new();

		// Write version number, to handle format changes
		NavigationHelpers.WriteUINT32ToString(c_versionNumber, buffer);

		// Supported parameter types
		//
		// None of the following values should ever change because they are
		// public. If any of these values ever changes, the version number
		// of the string returned by GetNavigationState will need to be changed and
		// back compat handled.
		MUX_ASSERT((int)PropertyType.Empty == 0);
		MUX_ASSERT((int)PropertyType.UInt8 == 1);
		MUX_ASSERT((int)PropertyType.Int16 == 2);
		MUX_ASSERT((int)PropertyType.UInt16 == 3);
		MUX_ASSERT((int)PropertyType.Int32 == 4);
		MUX_ASSERT((int)PropertyType.UInt32 == 5);
		MUX_ASSERT((int)PropertyType.Int64 == 6);
		MUX_ASSERT((int)PropertyType.UInt64 == 7);
		MUX_ASSERT((int)PropertyType.Single == 8);
		MUX_ASSERT((int)PropertyType.Double == 9);
		MUX_ASSERT((int)PropertyType.Char16 == 10);
		MUX_ASSERT((int)PropertyType.Boolean == 11);
		MUX_ASSERT((int)PropertyType.String == 12);
		MUX_ASSERT((int)PropertyType.Guid == 16);

		int nextSize = 0;
		int previousSize = 0;
		int totalSize = 0;

		// Get size of entries before and after the current entry, and the total size
		previousSize = m_tpBackStack.Count;
		nextSize = m_tpForwardStack.Count;

		if (m_tpCurrentPageStackEntry is not null)
		{
			// Previous entries + Current entry + Next entries
			totalSize = previousSize + 1 + nextSize;
		}
		else if (previousSize > 0)
		{
			totalSize = previousSize + nextSize;
		}
		else
		{
			totalSize = 0;
		}

		// Write number of entries in history.
		NavigationHelpers.WriteUINT32ToString((uint)totalSize, buffer);

		if (totalSize > 0)
		{
			PageStackEntry pIEntry;
			PageStackEntry tempCurrentIEntry = null;

			// If the current page is null consider the top element in BackStack as current and don't add it to the serialized BackStack.
			if (m_tpCurrentPageStackEntry is null)
			{
				MUX_ASSERT(previousSize > 0);

				tempCurrentIEntry = m_tpBackStack[previousSize - 1];
				previousSize--;
			}

			// Write index of current entry
			NavigationHelpers.WriteUINT32ToString((uint)previousSize, buffer);

			// Write previous entries
			for (int i = 0; i < previousSize; ++i)
			{
				pIEntry = m_tpBackStack[i];
				WritePageStackEntryToString(pIEntry, buffer);
			}

			// Write current entry
			if (tempCurrentIEntry is not null)
			{
				WritePageStackEntryToString(tempCurrentIEntry, buffer);
			}
			else
			{
				WritePageStackEntryToString(m_tpCurrentPageStackEntry, buffer);
			}

			// Write subsequent entries
			for (int i = 0; i < nextSize; ++i)
			{
				pIEntry = m_tpForwardStack[i];
				WritePageStackEntryToString(pIEntry, buffer);
			}
		}

		// Remove last ',' delimiter
		if (buffer[buffer.Length - 1] == ',')
		{
			buffer.Remove(buffer.Length - 1, 1);
		}

		return buffer.ToString();
	}

	//------------------------------------------------------------------------
	//
	//  Method: NavigationHistory.SetNavigationState
	//
	//  Synopsis:
	//     Read navigation history from an string. Caller needs to
	//  to then create the current page and call CommitSetNavigationState
	//  to complete this operation.
	//
	//------------------------------------------------------------------------

	internal void SetNavigationState(string navigationState, bool suppressNavigate)
	{
		int currentPosition = 0;
		int nextPosition = 0;
		string buffer = navigationState;

		// Read version number
		NavigationHelpers.ReadUINT32FromString(buffer, currentPosition, out var versionNumber, out nextPosition);
		currentPosition = nextPosition;
		if (versionNumber != c_versionNumber)
		{
			throw new InvalidOperationException("Navigation state format version mismatch.");
		}

		// Read number of entries in history. Previous entries + Current entry + Next entries
		NavigationHelpers.ReadUINT32FromString(buffer, currentPosition, out var contentCount, out nextPosition);
		currentPosition = nextPosition;

		// Clear Navigation history, because new history is going to be read
		ClearNavigationHistory();

		if (contentCount > 0)
		{
			uint nextSize = 0;
			uint previousSize = 0;
			uint contentIndex = 0;
			PageStackEntry pPageStackEntry;

			// Read index of current entry
			NavigationHelpers.ReadUINT32FromString(buffer, currentPosition, out contentIndex, out nextPosition);
			currentPosition = nextPosition;
			if (contentIndex >= contentCount)
			{
				throw new InvalidOperationException("Invalid current page index.");
			}

			previousSize = contentIndex;
			nextSize = contentCount - previousSize - 1;

			// Read previous entries
			for (int i = 0; i < previousSize; ++i)
			{
				ReadPageStackEntryFromString(buffer, currentPosition, out pPageStackEntry, out nextPosition);
				currentPosition = nextPosition;
				m_tpBackStack.AddInternal(pPageStackEntry);
			}

			// Read current entry
			ReadPageStackEntryFromString(buffer, currentPosition, out pPageStackEntry, out nextPosition);
			currentPosition = nextPosition;

			if (suppressNavigate)
			{
				m_tpBackStack.AddInternal(pPageStackEntry);
				m_tpCurrentPageStackEntry = null;
			}
			else
			{
				m_tpCurrentPageStackEntry = pPageStackEntry;
			}

			// Read next entries
			for (int i = 0; i < nextSize; ++i)
			{
				ReadPageStackEntryFromString(buffer, currentPosition, out pPageStackEntry, out nextPosition);
				currentPosition = nextPosition;
				m_tpForwardStack.AddInternal(pPageStackEntry);
			}
		}

		int nCount = 0;

		if (m_pIFrame is null)
		{
			throw new InvalidOperationException("Frame is null.");
		}

		// Navigation can be set without navigating to the current page so we need to update BackStackDepth here because CommitNavigation could not be called.
		nCount = m_tpBackStack.Count;
		((Frame)m_pIFrame).BackStackDepth = nCount;

		m_isSetNavigationStatePending = !suppressNavigate;
	}

	//------------------------------------------------------------------------
	//
	//  Method: NavigationHistory.WritePageStackEntryToString
	//
	//  Synopsis:
	//     Write a PageStackEntry to a string.
	//     Format:
	//      <page type name length>,<page type name>,<page's parameter type (wf.PropertyType)>,
	//              <length of page's serialized parameter>,<page's serialized parameter>,
	//
	//------------------------------------------------------------------------

	private void WritePageStackEntryToString(PageStackEntry pPageStackEntry, StringBuilder buffer)
	{
		string strTransitionInfo;
		object pParameterobject = null;
		bool isParameterTypeSupported = false;

		// Write descriptor
		var strDescriptor = pPageStackEntry.GetDescriptor();
		if (strDescriptor is null)
		{
			throw new InvalidOperationException("Descriptor should not be null");
		}

		NavigationHelpers.WriteHSTRINGToString(strDescriptor, buffer);

		// Write parameter
		pParameterobject = pPageStackEntry.Parameter;
		NavigationHelpers.WriteNavigationParameterToString(pParameterobject, buffer, out isParameterTypeSupported);

		// Get the NavigationTransitionInfo.
		var spTransitionInfoAsI = pPageStackEntry.NavigationTransitionInfo;
		var spTransitionInfo = spTransitionInfoAsI;

		if (spTransitionInfo is not null)
		{
			// Write NavigationTransitionInfo type.
			NavigationHelpers.WriteHSTRINGToString(spTransitionInfo.GetType().AssemblyQualifiedName, buffer);

			// Write NavigationTransitionInfo.
			strTransitionInfo = spTransitionInfo.GetNavigationStateCoreInternal();
			NavigationHelpers.WriteHSTRINGToString(strTransitionInfo, buffer);
		}
		else
		{
			// Placeholder for type.
			NavigationHelpers.WriteHSTRINGToString(null, buffer);

			// Only write the serialization of the type if we need to.
		}

		if (!isParameterTypeSupported)
		{
			// Throw exception saying that a parameter type is not supported for
			// serialization
			throw new InvalidOperationException("Unsupported parameter type for serialization.");
		}
	}

	//------------------------------------------------------------------------
	//
	//  Method: NavigationHistory.ReadPageStackEntryFromString
	//
	//  Synopsis:
	//     Read a PageStackEntry from a string.
	//
	//------------------------------------------------------------------------

	private void ReadPageStackEntryFromString(
		string buffer,
		int currentPosition,
		out PageStackEntry ppPageStackEntry,
		out int pNextPosition)
	{

		string strDescriptor;
		string strTransitionInfoType = null;
		string strTransitionInfo = null;
		object spParameterobject;
		NavigationTransitionInfo spTransitionInfo = null;

		// Read descriptor
		NavigationHelpers.ReadHSTRINGFromString(
			buffer,
			currentPosition,
			out strDescriptor,
			out pNextPosition);
		currentPosition = pNextPosition;
		if (strDescriptor == null)
		{
			throw new InvalidOperationException("Descriptor should not be null");
		}

		// Read parameter
		NavigationHelpers.ReadNavigationParameterFromString(buffer, currentPosition, out spParameterobject, out pNextPosition);
		currentPosition = pNextPosition;

		// Create NavigationTransitionInfo
		var succeeded = true;
		try
		{
			NavigationHelpers.ReadHSTRINGFromString(
				buffer,
				currentPosition,
				out strTransitionInfoType,
				out pNextPosition);
		}
		catch
		{
			succeeded = false;
		}

		if (succeeded)
		{
			currentPosition = pNextPosition;

			if (strTransitionInfoType is not null)
			{
				var pTransitionInfoTypeInfo = Type.GetType(strTransitionInfoType);
				spTransitionInfo = (NavigationTransitionInfo)Activator.CreateInstance(pTransitionInfoTypeInfo);

				// Read NavigationTransitionInfo.
				NavigationHelpers.ReadHSTRINGFromString(
					buffer,
					currentPosition,
					out strTransitionInfo,
					out pNextPosition);
				currentPosition = pNextPosition;

				if (strTransitionInfo != null)
				{
					spTransitionInfo.SetNavigationStateCoreInternal(strTransitionInfo);
				}
			}
		}
		else
		{
			// Swallowing the failure, as some apps override the navigation state manually;
			// therefore, we cannot expect this value to be present in Pre-Blue apps.

#if DBG
        char szTrace[256];
        IFCEXPECT(swprintf_s(szTrace, 256, "==== NavigationTransitionInfo not present while parsing navigation state.") >= 0.0;
        Trace(szTrace);
#endif
		}

		// Create PageStackEntry
		ppPageStackEntry = PageStackEntry.Create(
			m_pIFrame,
			strDescriptor,
			spParameterobject,
			spTransitionInfo);
	}
}
