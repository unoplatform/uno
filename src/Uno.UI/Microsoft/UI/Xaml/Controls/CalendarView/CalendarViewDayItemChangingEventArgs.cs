// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.


using namespace DirectUI;
using namespace DirectUISynonyms;


CalendarViewDayItemChangingEventArgs()
    : m_pContainerBackPointer(null)
{
}


// reset the args in such a way that it is not holding on to any other structures
private void ResetLifetimeImpl()
{
    put_Callback(null);
    put_Item(null);
}

// Register this container for a callback on the next phase
private void RegisterUpdateCallbackImpl( wf.ITypedEventHandler<xaml_controls.CalendarView, xaml_controls.CalendarViewDayItemChangingEventArgs>* pCallback)
{
    return RegisterUpdateCallbackWithPhaseImpl(m_phase + 1, pCallback);
}

// Register this container for a callback on a specific phase
private void RegisterUpdateCallbackWithPhaseImpl( Uint callbackPhase,  wf.ITypedEventHandler<xaml_controls.CalendarView, xaml_controls.CalendarViewDayItemChangingEventArgs>* pCallback)
{
    m_phase = callbackPhase;
    put_Callback(pCallback);
    put_WantsCallBack(true);

}
