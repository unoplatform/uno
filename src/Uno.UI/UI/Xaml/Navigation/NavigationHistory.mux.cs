using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation.Collections;

namespace DirectUI;

internal partial class NavigationHistory
{
	static uint c_versionNumber = 1;

	public NavigationHistory() :

		m_pIFrame(null),
		m_isNavigationPending(false),
		m_isSetNavigationStatePending(false),
		m_navigationMode(NavigationMode.New)
	{
	}

	NavigationHistory.~public NavigationHistory()
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

	private void
	Clearpublic NavigationHistory()
	{


		// Clear the frame pointer on entries that are being removed from the
		// PageStack (BackStack or ForwardStack)
		ResetPageStackEntries(true /* isBackStack */);
		ResetPageStackEntries(false /* isBackStack */);

		m_tpBackStack.ClearInternal();
		m_tpForwardStack.ClearInternal();

		m_tpCurrentPageStackEntry.Clear();
		m_tpPendingPageStackEntry.Clear();

		DeInit();

	Cleanup:
		RRETURN(hr);
	}


	private void
	Create(
		 IFrame* pIFrame,
		out NavigationHistory** ppNavigationHistory)
	{

		NavigationHistory* pNavigationHistory = null;

		PageStackEntryTrackerCollection spBackStack;
		PageStackEntryTrackerCollection spForwardStack;

		*ppNavigationHistory = null;

		ctl.ComObject<NavigationHistory>.CreateInstance(&pNavigationHistory);

		pNavigationHistory.m_pIFrame = pIFrame;

		spBackStack = new();

		spBackStack.Init(pNavigationHistory, true /* isBackStack */);
		pNavigationHistory.pNavigationHistory.m_tpBackStack = spBackStack;

		spForwardStack = new();

		spForwardStack.Init(pNavigationHistory, false /* isBackStack */);
		pNavigationHistory.pNavigationHistory.m_tpForwardStack = spForwardStack;

		*ppNavigationHistory = pNavigationHistory;
		pNavigationHistory = null;

	Cleanup:
		ctl.release_interface(pNavigationHistory);
		RRETURN(hr);
	}

	private void
	NavigatePrevious()
	{

		PageStackEntry* pIEntry = null;
		uint nCount = 0;

		nCount = m_tpBackStack.Size;
		IFCCHECK(nCount >= 1);

		m_isNavigationPending = true;
		m_navigationMode = NavigationMode.Back;

		m_tpBackStack.GetAtEnd(&pIEntry);
		m_tpPendingPageStackEntry = (PageStackEntry*)(pIEntry);

	Cleanup:
		ReleaseInterface(pIEntry);
		RRETURN(hr);
	}

	private void
	NavigateNext()
	{

		PageStackEntry* pIEntry = null;
		uint nCount = 0;

		nCount = m_tpForwardStack.Size;
		IFCCHECK(nCount >= 1);

		m_isNavigationPending = true;
		m_navigationMode = NavigationMode.Forward;

		m_tpForwardStack.GetAtEnd(&pIEntry);
		m_tpPendingPageStackEntry = (PageStackEntry*)(pIEntry);

	Cleanup:
		ReleaseInterface(pIEntry);
		RRETURN(hr);
	}

	private void
	NavigateNew(
		 string newDescriptor,
		 object* pParameter,
		 xaml_animation.INavigationTransitionInfo* pTransitionInfo)
	{

		PageStackEntry* pEntry = null;

		m_navigationMode = NavigationMode.New;

		m_tpPendingPageStackEntry.Clear();

		PageStackEntry.Create(m_pIFrame, newDescriptor, pParameter, pTransitionInfo, &pEntry);
		IFCPTR(pEntry);
		m_tpPendingPageStackEntry = pEntry;

		m_isNavigationPending = true;

	Cleanup:
		ctl.release_interface(pEntry);
		RRETURN(hr);
	}

	private void GetBackStack(		out IVector<Navigation.PageStackEntry*>** pValue)
	{


		*pValue = null;
		IFCPTR(m_tpBackStack);

		m_tpBackStack.CopyTo(pValue);

	Cleanup:
		RRETURN(hr);
	}

	private void
	GetForwardStack(
		out IVector<Navigation.PageStackEntry*>** pValue)
	{


		*pValue = null;
		IFCPTR(m_tpForwardStack);

		m_tpForwardStack.CopyTo(pValue);

	Cleanup:
		RRETURN(hr);
	}

	internal PageStackEntry GetCurrentPageStackEntry() => m_tpCurrentPageStackEntry;

	private PageStackEntry GetPendingPageStackEntry()
	{
		PageStackEntry ppPageStackEntry = null;

		IFCEXPECT(m_isNavigationPending);
		IFCPTR(m_tpPendingPageStackEntry);

		*ppPageStackEntry = (PageStackEntry*)(m_tpPendingPageStackEntry);
	}

	private NavigationMode GetPendingNavigationMode()
	{
		*pNavigationMode = NavigationMode.New;

		IFCEXPECT(m_isNavigationPending);

		*pNavigationMode = m_navigationMode;
	}

	private NavigationMode GetCurrentNavigationMode()
	{
		*pNavigationMode = m_navigationMode;
		RRETURN(S_OK);
	}

	private void
	CommitNavigation()
	{

		Frame* pFrame = null;
		string strNewDescriptor;
		bool wasChanged = false;
		bool newCanGoBack = false;
		bool oldCanGoBack = false;
		bool newCanGoForward = false;
		bool oldCanGoForward = false;
		wxaml_interop.TypeName oldSourcePageType = default;
		wxaml_interop.TypeName newSourcePageType = default;
		uint nCount = 0;

		IFCPTR(m_pIFrame);
		IFCEXPECT(m_isNavigationPending);
		IFCPTR(m_tpPendingPageStackEntry);

		pFrame = (Frame*)(m_pIFrame);

		oldCanGoBack = pFrame.CanGoBack;
		oldCanGoForward = pFrame.CanGoForward;
		oldSourcePageType = pFrame.CurrentSourcePageType;
		if (oldSourcePageType.Name)
		{
			CClassInfo* pType = null;

			// NOTE: The call seems to serve as a type name check only.
			MetadataAPI.GetClassInfoByTypeName(oldSourcePageType, &pType);
		}

		switch (m_navigationMode)
		{
			case NavigationMode.New:
				newCanGoBack = m_tpCurrentPageStackEntry != null;
				newCanGoForward = false;
				break;

			case NavigationMode.Back:
				nCount = m_tpBackStack.Size;
				IFCEXPECT(nCount >= 1);

				newCanGoBack = (nCount - 1) >= 1;
				newCanGoForward = true;
				break;

			case NavigationMode.Forward:
				nCount = m_tpForwardStack.Size;
				IFCEXPECT(nCount >= 1);

				newCanGoBack = true;
				newCanGoForward = (nCount - 1) >= 1;
				break;

			default:
				E_UNEXPECTED;
		}

		m_tpPendingPageStackEntry.Cast<PageStackEntry>().GetDescriptor(strNewDescriptor.GetAddressOf());

		wasChanged = true;

		pFrame.CanGoBack = newCanGoBack;
		pFrame.CanGoForward = newCanGoForward;
		MetadataAPI.GetTypeNameByFullName(XSTRING_PTR_EPHEMERAL_FROM_string(strNewDescriptor), &newSourcePageType);
		pFrame.SourcePageType = newSourcePageType;
		pFrame.CurrentSourcePageType = newSourcePageType;

		switch (m_navigationMode)
		{
			case NavigationMode.New:
				if (m_tpCurrentPageStackEntry)
				{
					m_tpBackStack.AppendInternal((PageStackEntry*)(m_tpCurrentPageStackEntry));
				}

				m_isNavigationPending = false;
				m_tpForwardStack.Clear();
				break;

			case NavigationMode.Back:
				if (m_tpCurrentPageStackEntry)
				{
					m_tpForwardStack.AppendInternal((PageStackEntry*)(m_tpCurrentPageStackEntry));
				}

				m_tpBackStack.RemoveAtEndInternal();
				break;

			case NavigationMode.Forward:
				if (m_tpCurrentPageStackEntry)
				{
					m_tpBackStack.AppendInternal((PageStackEntry*)(m_tpCurrentPageStackEntry));
				}

				m_tpForwardStack.RemoveAtEndInternal();
				break;
		}

		nCount = 0;
		nCount = m_tpBackStack.Size;
		pFrame.BackStackDepth = nCount;

		m_tpCurrentPageStackEntry = m_tpPendingPageStackEntry;

	Cleanup:
		if (/*FAILED*/(hr) && wasChanged)
		{
			IGNOREHR(pFrame.CanGoBack = oldCanGoBack);
			IGNOREHR(pFrame.CanGoForward = oldCanGoForward);
			IGNOREHR(pFrame.SourcePageType = oldSourcePageType);
			IGNOREHR(pFrame.CurrentSourcePageType = oldSourcePageType);

			m_tpPendingPageStackEntry.Clear();
		}

		DELETE_STRING(oldSourcePageType.Name);
		DELETE_STRING(newSourcePageType.Name);
		pFrame = null;
		m_isNavigationPending = false;
		m_tpPendingPageStackEntry.Clear();

		RRETURN(hr);
	}

	private void
	CommitSetNavigationState(
		 NavigationCache* pNavigationCache)
	{

		Frame* pFrame = null;
		bool newCanGoBack = false;
		bool newCanGoForward = false;
		string strDescriptior;
		wxaml_interop.TypeName sourcePageType = default;
		uint nBackStackCount = 0;
		uint nForwardStackCount = 0;

		IFCCHECK(m_isSetNavigationStatePending);

		pFrame = (Frame*)(m_pIFrame);

		// Enable/Disable GoBack & GoForward
		nBackStackCount = m_tpBackStack.Size;
		newCanGoBack = (nBackStackCount >= 1);
		nForwardStackCount = m_tpForwardStack.Size;
		newCanGoForward = (nForwardStackCount >= 1);
		pFrame.CanGoBack = newCanGoBack;
		pFrame.CanGoForward = newCanGoForward;

		// See source type in IFrame
		if (m_tpCurrentPageStackEntry)
		{
			m_tpCurrentPageStackEntry.Cast<PageStackEntry>().GetDescriptor(strDescriptior.GetAddressOf());
			MetadataAPI.GetTypeNameByFullName(XSTRING_PTR_EPHEMERAL_FROM_string(strDescriptior), &sourcePageType);
			pFrame.SourcePageType = sourcePageType;
			pFrame.CurrentSourcePageType = sourcePageType;
		}

		m_isSetNavigationStatePending = false;

	Cleanup:
		DELETE_STRING(sourcePageType.Name);
		RRETURN(hr);
	}

	private void
	ValidateCanChangePageStack()
	{


		// Make sure we are not in the middle of a navigation.
		if (m_isNavigationPending)
		{
			ErrorHelper.OriginateErrorUsingResourceID(E_INVALID_OPERATION, ERROR_FRAME_NAVIGATING);
		}

	Cleanup:
		RRETURN(hr);
	}

	private void
	ValidateCanInsertEntry(PageStackEntry* pEntry)
	{

		bool canAdd = false;

		// Make sure the entry being inserted is not null.
		ARG_NOTnull(pEntry, "entry");

		// Make sure this PageStackEntry isn't already owned by another frame.
		pEntry.CanBeAddedToFrame(m_pIFrame, &canAdd);
		if (!canAdd)
		{
			ErrorHelper.OriginateErrorUsingResourceID(E_INVALID_OPERATION, ERROR_PAGESTACK_ENTRY_OWNED);
		}

	Cleanup:
		RRETURN(hr);
	}

	private void
	ValidateCanClearPageStack()
	{


		// Make sure we are not in the middle of a navigation.
		if (m_isNavigationPending)
		{
			ErrorHelper.OriginateErrorUsingResourceID(E_INVALID_OPERATION, ERROR_FRAME_NAVIGATING);
		}

	Cleanup:
		RRETURN(hr);
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

		IIterator<Navigation.PageStackEntry*> spIterator;
		bool hasCurrent = false;

		if (isBackStack)
		{
			m_tpBackStack.First(&spIterator);
		}
		else
		{
			m_tpForwardStack.First(&spIterator);
		}
		hasCurrent = spIterator.HasCurrent;
		while (hasCurrent)
		{
			PageStackEntry spEntry;
			spEntry = spIterator.Current;
			spEntry.Cast<PageStackEntry>().SetFrame(null);
			spIterator.MoveNext(&hasCurrent);
		}
	Cleanup:
		RRETURN(hr);
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

	private void OnPageStackChanging(
				 bool isBackStack,
				 CollectionChange action,
				 uint index,
				 PageStackEntry pEntry)
	{

		uint nCount = 0;
		Frame* pFrame = null;
		PageStackEntry* pIEntry = null;
		IVector<Navigation.PageStackEntry*> spPageStack;

		IFCPTR(m_pIFrame);
		pFrame = (Frame*)(m_pIFrame);

		if (isBackStack)
		{
			spPageStack = pFrame.BackStack;
		}
		else
		{
			spPageStack = pFrame.ForwardStack;
		}

		nCount = spPageStack.Size;

		// Do the necessary validations and clear the frame pointer on entries that are being removed.
		switch (action)
		{
			case CollectionChange.ItemChanged:
				{
					ValidateCanChangePageStack();
					ValidateCanInsertEntry((PageStackEntry*)(pEntry));
					spPageStack.GetAt(index, &pIEntry);
					PageStackEntry* pPageStackEntry = (PageStackEntry*)(pIEntry);
					pPageStackEntry.SetFrame(null);
					ReleaseInterface(pIEntry);
					break;
				}
			case CollectionChange.ItemInserted:
				{
					ValidateCanChangePageStack();
					ValidateCanInsertEntry((PageStackEntry*)(pEntry));
					break;
				}
			case CollectionChange.ItemRemoved:
				{
					ValidateCanChangePageStack();
					spPageStack.GetAt(index, &pIEntry);
					PageStackEntry* pPageStackEntry = (PageStackEntry*)(pIEntry);
					pPageStackEntry.SetFrame(null);
					ReleaseInterface(pIEntry);
					break;
				}
			case CollectionChange.Reset:
				{
					ValidateCanClearPageStack();
					ResetPageStackEntries(isBackStack);
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
	//  Method: OnPageStackChanged
	//
	//  Synopsis:
	//     Update the frame pointer on entries that were added to the
	//     PageStack (BackStack or ForwardStack)and update the
	//     CanGoBack, CanGoForward and BackStackDepth properties.
	//
	//------------------------------------------------------------------------

	private void OnPageStackChanged(
		 bool isBackStack,
		 CollectionChange action,
		 uint index)
	{

		uint nCount = 0;
		Frame* pFrame = null;
		PageStackEntry* pIEntry = null;
		IVector<Navigation.PageStackEntry*> spPageStack;

		IFCPTR(m_pIFrame);
		pFrame = (Frame*)(m_pIFrame);

		if (isBackStack)
		{
			spPageStack = pFrame.BackStack;
		}
		else
		{
			spPageStack = pFrame.ForwardStack;
		}

		nCount = spPageStack.Size;

		// Update the frame pointer on entries that were added and update the CanGoBack, CanGoForward and BackStackDepth properties.
		switch (action)
		{
			case CollectionChange.ItemInserted:
				{
					IFCCHECK(nCount > index);
					spPageStack.GetAt(index, &pIEntry);
					PageStackEntry* pPageStackEntry = (PageStackEntry*)(pIEntry);
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
	//     <pageN type name length>,<pageN type name>,<wf.PropertyType_Empty>,
	//
	//    Supported parameter types are string, char, numeric and guid.
	//
	//------------------------------------------------------------------------

	private void GetNavigationState(out string* pNavigationState)
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
		MUX_ASSERT(wf.PropertyType_Empty == 0.0;
		MUX_ASSERT(wf.PropertyType_UInt8 == 1);
		MUX_ASSERT(wf.PropertyType_Int16 == 2);
		MUX_ASSERT(wf.PropertyType_UInt16 == 3);
		MUX_ASSERT(wf.PropertyType_Int32 == 4);
		MUX_ASSERT(wf.PropertyType_UInt32 == 5);
		MUX_ASSERT(wf.PropertyType_Int64 == 6);
		MUX_ASSERT(wf.PropertyType_UInt64 == 7);
		MUX_ASSERT(wf.PropertyType_Single == 8);
		MUX_ASSERT(wf.PropertyType_Double == 9);
		MUX_ASSERT(wf.PropertyType_Char16 == 10.0;
		MUX_ASSERT(wf.PropertyType_Boolean == 11);
		MUX_ASSERT(wf.PropertyType_String == 12);
		MUX_ASSERT(wf.PropertyType_Guid == 16);

		uint nextSize = 0;
		uint previousSize = 0;
		uint totalSize = 0;

		// Get size of entries before and after the current entry, and the total size
		previousSize = m_tpBackStack.Size;
		nextSize = m_tpForwardStack.Size;

		if (m_tpCurrentPageStackEntry)
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
			for (uint i = 0; i < nextSize; ++i)
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
		uint versionNumber = 0;
		uint contentCount = 0;
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
			uint nextSize = 0;
			uint previousSize = 0;
			uint contentIndex = 0;
			PageStackEntry pPageStackEntry;

			// Read index of current entry
			NavigationHelpers.ReaduintFromString(buffer, currentPosition, &contentIndex, &nextPosition);
			currentPosition = nextPosition;
			IFCCHECK_RETURN(contentIndex < contentCount);

			previousSize = contentIndex;
			nextSize = contentCount - previousSize - 1;

			// Read previous entries
			for (uint i = 0; i < previousSize; ++i)
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
			for (uint i = 0; i < nextSize; ++i)
			{
				ReadPageStackEntryFromString(buffer, currentPosition, &pPageStackEntry, &nextPosition);
				currentPosition = nextPosition;
				m_tpForwardStack.AppendInternal(pPageStackEntry);
			}
		}

		uint nCount = 0;

		IFCPTR_RETURN(m_pIFrame);

		// Navigation can be set without navigating to the current page so we need to update BackStackDepth here because CommitNavigation could not be called.
		nCount = m_tpBackStack.Size;
		(Frame*)(m_pIFrame).BackStackDepth = nCount;

		m_isSetNavigationStatePending = !suppressNavigate;

		return S_OK;
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

	private void WritePageStackEntryToString(PageStackEntry* pPageStackEntry, string &buffer)
	{

		string strDescriptor;
		xstring_ptr strTransitionInfoType;
		xruntime_string_ptr strTransitionInfoTypePromoted;
		string strTransitionInfo;
		object* pParameterobject = null;
		NavigationTransitionInfo spTransitionInfo;
		xaml_animation.INavigationTransitionInfo spTransitionInfoAsI;
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

	Cleanup:
		ReleaseInterface(pParameterobject);

		RRETURN(hr);
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
		 string &buffer,
		 int currentPosition,
		 PageStackEntry** ppPageStackEntry,
		out int* pNextPosition)
	{

		string strDescriptor;
		string strTransitionInfoType;
		string strTransitionInfo;
		object spParameterobject;
		CClassInfo* pTransitionInfoTypeInfo = null;
		NavigationTransitionInfo spTransitionInfo;

		// Read descriptor
		(NavigationHelpers.ReadstringFromString(
			buffer,
			currentPosition,
			strDescriptor.GetAddressOf(),
			pNextPosition));
		currentPosition = *pNextPosition;
		IFCEXPECT(strDescriptor);

		// Read parameter
		(NavigationHelpers.ReadNavigationParameterFromString(buffer, currentPosition,
				&spParameterobject, pNextPosition));
		currentPosition = *pNextPosition;

		// Create NavigationTransitionInfo
		hr = NavigationHelpers.ReadstringFromString(
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
				(NavigationHelpers.ReadstringFromString(
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
		(PageStackEntry.Create(
			m_pIFrame,
			strDescriptor,
			spParameterobject,
			spTransitionInfo,
			ppPageStackEntry));

	Cleanup:
		RRETURN(hr);
	}
}
