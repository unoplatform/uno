// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#include "precomp.h"
#include "CalendarViewDayItemChangingEventArgs.g.h"

using namespace DirectUI;
using namespace DirectUISynonyms;


CalendarViewDayItemChangingEventArgs.CalendarViewDayItemChangingEventArgs()
    : m_pContainerBackPointer(null)
{
}


// reset the args in such a way that it is not holding on to any other structures
_Check_return_ HRESULT CalendarViewDayItemChangingEventArgs.ResetLifetimeImpl()
{
    HRESULT hr = S_OK;

    IFC(put_Callback(null));
    IFC(put_Item(null));
Cleanup:
    return hr;
}

// Register this container for a callback on the next phase
_Check_return_ HRESULT CalendarViewDayItemChangingEventArgs.RegisterUpdateCallbackImpl( wf.ITypedEventHandler<xaml_controls.CalendarView*, xaml_controls.CalendarViewDayItemChangingEventArgs*>* pCallback)
{
    return RegisterUpdateCallbackWithPhaseImpl(m_phase + 1, pCallback);
}

// Register this container for a callback on a specific phase
_Check_return_ HRESULT CalendarViewDayItemChangingEventArgs.RegisterUpdateCallbackWithPhaseImpl( UINT callbackPhase,  wf.ITypedEventHandler<xaml_controls.CalendarView*, xaml_controls.CalendarViewDayItemChangingEventArgs*>* pCallback)
{
    HRESULT hr = S_OK;

    m_phase = callbackPhase;
    IFC(put_Callback(pCallback));
    IFC(put_WantsCallBack(TRUE));

Cleanup:
    return hr;
}
