using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using static Microsoft/* UWP don't rename */.UI.Xaml.Controls._Tracing;

namespace DirectUI;

internal partial class NavigationHistory
{
	static int c_versionNumber = 1;

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
					ReleaseInterface(pIEntry);
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
				IFCEXPECT_MUX_ASSERT(false);
				break;
		}

	Cleanup:
		pFrame = null;
		RRETURN(hr);
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
		string buffer;

		// Write version number, to handle format changes
		NavigationHelpers.WriteuintToString(c_versionNumber, buffer);

		// Supported parameter types
		//
		// None of the following values should ever change because they are
		// public. If any of these values ever changes, the version number
		// of the string returned by GetNavigationState will need to be changed and
		// back compat handled.
		MUX_ASSERT(PropertyType.Empty == 0);
		MUX_ASSERT(PropertyType.UInt8 == 1);
		MUX_ASSERT(PropertyType.Int16 == 2);
		MUX_ASSERT(PropertyType.UInt16 == 3);
		MUX_ASSERT(PropertyType.Int32 == 4);
		MUX_ASSERT(PropertyType.UInt32 == 5);
		MUX_ASSERT(PropertyType.Int64 == 6);
		MUX_ASSERT(PropertyType.UInt64 == 7);
		MUX_ASSERT(PropertyType.Single == 8);
		MUX_ASSERT(PropertyType.Double == 9);
		MUX_ASSERT(PropertyType.Char16 == 10.0;
		MUX_ASSERT(PropertyType.Boolean == 11);
		MUX_ASSERT(PropertyType.String == 12);
		MUX_ASSERT(PropertyType.Guid == 16);

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
		NavigationHelpers.WriteuintToString(totalSize, buffer);

		if (totalSize > 0)
		{
			PageStackEntry pIEntry;
			PageStackEntry tempCurrentIEntry;

			// If the current page is null consider the top element in BackStack as current and don't add it to the serialized BackStack.
			if (!m_tpCurrentPageStackEntry)
			{
				MUX_ASSERT(previousSize > 0.0;

				m_tpBackStack.GetAt(previousSize - 1, &tempCurrentIEntry);
				previousSize--;
			}

			// Write index of current entry
			NavigationHelpers.WriteuintToString(previousSize, buffer);

			// Write previous entries
			for (uint i = 0; i < previousSize; ++i)
			{
				m_tpBackStack.GetAt(i, &pIEntry);
				WritePageStackEntryToString(pIEntry.Cast<PageStackEntry>(), buffer);
			}

			// Write current entry
			if (tempCurrentIEntry)
			{
				WritePageStackEntryToString(tempCurrentIEntry.Cast<PageStackEntry>(), buffer);
			}
			else
			{
				WritePageStackEntryToString(m_tpCurrentPageStackEntry.Cast<PageStackEntry>(), buffer);
			}

			// Write subsequent entries
			for (int i = 0; i < nextSize; ++i)
			{
				m_tpForwardStack.GetAt(i, &pIEntry);
				WritePageStackEntryToString(pIEntry.Cast<PageStackEntry>(), buffer);
			}
		}

		int position;

		// Remove last ',' delimiter
		position = buffer.rfind(',');
		buffer.erase(position, 1);

    // Return string with navigation state
    .WindowsCreateString(buffer.c_str(), buffer.length(), pNavigationState);

		return S_OK;
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
		int versionNumber = 0;
		int contentCount = 0;
		int currentPosition = 0;
		int nextPosition = string.npos;
		string buffer;

		buffer.append(HStringUtil.GetRawBuffer(navigationState, null));

		// Read version number
		NavigationHelpers.ReaduintFromString(buffer, currentPosition, &versionNumber, &nextPosition);
		currentPosition = nextPosition;
		IFCCHECK_RETURN(versionNumber == c_versionNumber);

		// Read number of entries in history. Previous entries + Current entry + Next entries
		NavigationHelpers.ReaduintFromString(buffer, currentPosition, &contentCount, &nextPosition);
		currentPosition = nextPosition;

		// Clear Navigation history, because new history is going to be read
		Clearpublic NavigationHistory();

		if (contentCount > 0)
		{
			int nextSize = 0;
			int previousSize = 0;
			int contentIndex = 0;
			PageStackEntry pPageStackEntry;

			// Read index of current entry
			NavigationHelpers.ReadintFromString(buffer, currentPosition, &contentIndex, &nextPosition);
			currentPosition = nextPosition;
			IFCCHECK_RETURN(contentIndex < contentCount);

			previousSize = contentIndex;
			nextSize = contentCount - previousSize - 1;

			// Read previous entries
			for (int i = 0; i < previousSize; ++i)
			{
				ReadPageStackEntryFromString(buffer, currentPosition, &pPageStackEntry, &nextPosition);
				currentPosition = nextPosition;
				m_tpBackStack.AppendInternal(pPageStackEntry);
			}

			// Read current entry
			ReadPageStackEntryFromString(buffer, currentPosition, &pPageStackEntry, &nextPosition);
			currentPosition = nextPosition;

			if (suppressNavigate)
			{
				m_tpBackStack.AppendInternal(pPageStackEntry);
				m_tpCurrentPageStackEntry.Clear();
			}
			else
			{
				m_tpCurrentPageStackEntry = pPageStackEntry;
			}

			// Read next entries
			for (int i = 0; i < nextSize; ++i)
			{
				ReadPageStackEntryFromString(buffer, currentPosition, &pPageStackEntry, &nextPosition);
				currentPosition = nextPosition;
				m_tpForwardStack.AppendInternal(pPageStackEntry);
			}
		}

		int nCount = 0;

		IFCPTR_RETURN(m_pIFrame);

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

	private void WritePageStackEntryToString(PageStackEntry pPageStackEntry, string &buffer)
	{

		string strDescriptor;
		string strTransitionInfoType;
		string strTransitionInfoTypePromoted;
		string strTransitionInfo;
		object* pParameterobject = null;
		NavigationTransitionInfo spTransitionInfo;
		NavigationTransitionInfo spTransitionInfoAsI;
		bool isParameterTypeSupported = false;

		// Write descriptor
		pPageStackEntry.GetDescriptor(strDescriptor.GetAddressOf());
		IFCPTR(strDescriptor);
		NavigationHelpers.WritestringToString(strDescriptor, buffer);

		// Write parameter
		pParameterobject = pPageStackEntry.Parameter;
		(NavigationHelpers.WriteNavigationParameterToString(pParameterobject,
				buffer, &isParameterTypeSupported));

		// Get the NavigationTransitionInfo.
		spTransitionInfo.As(&spTransitionInfoAsI);
		spTransitionInfoAsI = pPageStackEntry.NavigationTransitionInfo;

		if (spTransitionInfo)
		{
			// Write NavigationTransitionInfo type.
			MetadataAPI.GetRuntimeClassName(spTransitionInfoAsI, &strTransitionInfoType);
			strTransitionInfoType.Promote(&strTransitionInfoTypePromoted);
			NavigationHelpers.WritestringToString(strTransitionInfoTypePromoted.Getstring(), buffer);

			// Write NavigationTransitionInfo.
			spTransitionInfo.GetNavigationStateCoreProtected(strTransitionInfo.GetAddressOf());
			NavigationHelpers.WritestringToString(strTransitionInfo, buffer);
		}
		else
		{
			// Placeholder for type.
			NavigationHelpers.WritestringToString(null, buffer);

			// Only write the serialization of the type if we need to.
		}

		if (!isParameterTypeSupported)
		{
			// Throw exception saying that a parameter type is not supported for
			// serialization
			(ErrorHelper.OriginateErrorUsingResourceID(E_FAIL,
					ERROR_NAVIGATION_UNSUPPORTED_PARAM_TYPE_FOR_SERIALIZATION));
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
		string strTransitionInfoType;
		string strTransitionInfo;
		object spParameterobject;
		CClassInfo* pTransitionInfoTypeInfo = null;
		NavigationTransitionInfo spTransitionInfo;

		// Read descriptor
		NavigationHelpers.ReadStringFromString(
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
		currentPosition = *pNextPosition;

		// Create NavigationTransitionInfo
		hr = NavigationHelpers.ReadStringFromString(
			buffer,
			currentPosition,
			strTransitionInfoType.GetAddressOf(),
			pNextPosition);

		if (SUCCEEDED(hr))
		{
			currentPosition = *pNextPosition;

			if (strTransitionInfoType)
			{
				MetadataAPI.GetClassInfoByFullName(XSTRING_PTR_EPHEMERAL_FROM_string(strTransitionInfoType), &pTransitionInfoTypeInfo);
				ActivationAPI.ActivateInstance(pTransitionInfoTypeInfo, &spTransitionInfo);

				// Read NavigationTransitionInfo.
				NavigationHelpers.ReadStringFromString(
					buffer,
					currentPosition,
					strTransitionInfo.GetAddressOf(),
					pNextPosition));
				currentPosition = *pNextPosition;

				if (strTransitionInfo != null)
				{
					spTransitionInfo.SetNavigationStateCoreProtected(strTransitionInfo);
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

			hr = S_OK;
		}

		// Create PageStackEntry
		ppPageStackEntry = PageStackEntry.Create(
			m_pIFrame,
			strDescriptor,
			spParameterobject,
			spTransitionInfo);
	}
}
