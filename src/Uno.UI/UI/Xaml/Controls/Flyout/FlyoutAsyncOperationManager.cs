using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.System;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Controls
{
	// --------------------------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Manages the creation and completion of async operations for picker flyouts, ensuring
	//      that operation state stays in sync with the state of the associated flyout.
	//
	//  Notes:
	//      This class expects not to outlive the associated flyout passed to the Initialize
	//      method. An instance may only be initialized once.
	//
	//      We handle the case where the consumer of the operation starts a new operation from the
	//      completion handler of the previous operation. This is important for support of chaining
	//      of async operations. The difficulty is that an operation may be completed before the
	//      flyout associated with that operation is closed. Calling ShowAt on the flyout in this
	//      case will no-op, and we'll be left with a dangling operation. To address this, we
	//      create a "deferred" operation in this case, which simply means that the call to ShowAt
	//      for the operation is deferred until the Closed event for the associated flyout is
	//      received. Because FlyoutBase ignores ShowAt calls until after the Closed event if both
	//      the flyout and placement target are the same as the closing flyout, we actually need
	//      to post the ShowAt to the dispatcher.
	//
	//      This added async-ness combined with cancellation handling makes the state transition
	//      logic for this manager quite complex.
	//      MODIFY WITH GREAT CARE!
	//
	// --------------------------------------------------------------------------------------------

	internal class FlyoutAsyncOperationManager<TResult>
	{
		private enum FlyoutState
		{
			Closed,
			Open,
			Closing,
		}

		public FlyoutAsyncOperationManager(FlyoutBase pAssociatedFlyout, Func<TResult> cancellationValueCallback)
		{
			Initialize(pAssociatedFlyout, cancellationValueCallback);
		}

		Func<TResult> m_cancellationValueCallback;
		FlyoutBase m_pAssociatedFlyoutNoRef;
		FrameworkElement m_tpTargetForDeferredShowAt;
		PickerFlyoutAsyncOperation<TResult> m_spCurrentOperation;
		bool m_isInitialized;
		bool m_isShowAtForCurrentOperationDeferred;
		FlyoutState m_flyoutState;
		DispatcherQueue m_spDispatcherQueue;

		private void Initialize(FlyoutBase pAssociatedFlyout, Func<TResult> cancellationValueCallback)
		{
			//HRESULT hr = S_OK;
			//EventRegistrationToken openingToken = { };
			//EventRegistrationToken closedToken = { };
			//wrl::ComPtr<wsy::IDispatcherQueueStatics> spDispatcherQueueStatics;

			//IFCEXPECT(!m_isInitialized);
			if (m_isInitialized)
			{
				throw new InvalidOperationException("Already initialized");
			}

			m_pAssociatedFlyoutNoRef = pAssociatedFlyout;
			m_cancellationValueCallback = cancellationValueCallback;

			//IFC(pAssociatedFlyout->add_Opening(
			//		wrl::Callback<wf::IEventHandler<IInspectable*>>
			//			(this, &FlyoutAsyncOperationManager < TResult, TTrackerRuntimeClass, OpName >::OnOpening).Get(),
			//	&openingToken));
			pAssociatedFlyout.Opening += OnOpening;

			//IFC(pAssociatedFlyout->add_Closed(
			//		wrl::Callback<wf::IEventHandler<IInspectable*>>
			//			(this, &FlyoutAsyncOperationManager < TResult, TTrackerRuntimeClass, OpName >::OnClosed).Get(),
			//	&closedToken));
			pAssociatedFlyout.Closed += OnClosed;

			//IFC(wf::GetActivationFactory(wrl_wrappers::HStringReference(RuntimeClass_Windows_System_DispatcherQueue).Get(), &spDispatcherQueueStatics));
			//IFC(spDispatcherQueueStatics->GetForCurrentThread(&m_spDispatcherQueue));

			m_spDispatcherQueue = Windows.System.DispatcherQueue.GetForCurrentThread();
			m_isInitialized = true;

			//Cleanup:
			//RRETURN(hr);
		}

		public IAsyncOperation<TResult> Start(FrameworkElement pTarget)
		{

			//HRESULT hr = S_OK;
			PickerFlyoutAsyncOperation<TResult> spAsyncOp;

			//// Validation
			//IFCEXPECT(m_isInitialized);
			//AssertInvariants();
			//if (m_spCurrentOperation)
			//{
			//	ROERROR_LOOKUP(E_ASYNC_OPERATION_NOT_STARTED, ERR_ASYNC_ALREADY_IN_PROGRESS);
			//}
			if (m_spCurrentOperation != null)
			{
				throw new InvalidOperationException("Async operation in progress.");
			}
			//if (m_flyoutState == FlyoutState::Open)
			//{
			//	ROERROR_LOOKUP(E_ASYNC_OPERATION_NOT_STARTED, ERR_FLYOUT_ALREADY_OPEN);
			//}

			//IFC((wrl::MakeAndInitialize<xaml_controls::PickerFlyoutAsyncOperation<TResult, OpName>>(&spAsyncOp)));
			spAsyncOp = new PickerFlyoutAsyncOperation<TResult>();
			//IFC(spAsyncOp->StartOperation(m_pAssociatedFlyoutNoRef));
			spAsyncOp.StartOperation(m_pAssociatedFlyoutNoRef);

			m_tpTargetForDeferredShowAt = pTarget;

			// UNO-TODO: TEMPORARY DISABLE THIS FEATURE
			// m_isShowAtForCurrentOperationDeferred = true; 

			//if (m_flyoutState == FlyoutState::Closed)
			//{
			//	IFC(BeginAttemptStartDeferredOperation());
			//}

			if (m_flyoutState == FlyoutState.Closed)
			{
				// The flyout is currently closed so we can try to show now, but we have to treat this
				// as a deferred op anyway and show asynchronously in case we're being called from the
				// closed event handler, as FlyoutBase may swallow a synchronous call to ShowAt in that case.

				BeginAttemptStartDeferredOperation();
			}

			//m_spCurrentOperation = static_cast<xaml_controls::IPickerFlyoutAsyncOperation<TResult>*>(spAsyncOp.Get());
			m_spCurrentOperation = spAsyncOp;
			//IFC(spAsyncOp.CopyTo(ppOperation));

			AssertInvariants();

			return spAsyncOp;
		}

		public void Complete(TResult result)
		{
			//HRESULT hr = S_OK;

			//IFCEXPECT(m_isInitialized);
			//AssertInvariants();

			//if (m_flyoutState == FlyoutState::Open)
			//{
			//	// If the flyout is not already closed or closing, then we're being called
			//	// because the user accepted a selection, and the flyout will be closed shortly
			//	// by PickerFlyoutBase::OnConfirmedImpl
			//	m_flyoutState = FlyoutState::Closing;
			//}

			if (m_flyoutState == FlyoutState.Open)
			{
				m_flyoutState = FlyoutState.Closing;
			}


			//if (m_spCurrentOperation)
			//{
			if (m_spCurrentOperation != null)
			{
				//	wrl::ComPtr<wf::IAsyncInfo> spAsyncInfo;
				//	wf::AsyncStatus asyncStatus = wf::AsyncStatus::Started;

				//	// If an operation is deferred, then it is not associated with the current
				//	// open/close cycle of this flyout, and we should not complete it unless it
				//	// has been cancelled.
				//	IFC(m_spCurrentOperation.As(&spAsyncInfo));
				//	IFC(spAsyncInfo->get_Status(&asyncStatus));
				var asyncStatus = m_spCurrentOperation.Status;
				if (!m_isShowAtForCurrentOperationDeferred || asyncStatus == AsyncStatus.Canceled)
				{
					AssertCompleteOperationPreconditions();

					//		// nulls out m_spCurrentOperation. This is important in the case of rentrancy;
					//		// the consumer's CompleteOperation handler may trigger another call to Start,
					//		// in which case we want Start to be able to return a deferred operation
					//		// rather than failing.
					//		wrl::ComPtr<xaml_controls::IPickerFlyoutAsyncOperation<TResult>> spCurrentOperation;
					//		spCurrentOperation.Swap(m_spCurrentOperation);
					m_isShowAtForCurrentOperationDeferred = false;
					m_tpTargetForDeferredShowAt = null;
					m_spCurrentOperation.CompleteOperation(result);
					m_spCurrentOperation = null;
					//		IFC(spCurrentOperation->CompleteOperation(result));
				}
				//}


				//Cleanup:
				//RRETURN(hr);
			}

			AssertInvariants();
		}

		private void OnOpening(object sender, object eventArgs)
		{
			m_flyoutState = FlyoutState.Open;
		}

		private void OnClosed(object sender, object eventArgs)
		{
			AssertInvariants();

			AsyncStatus asyncStatus = AsyncStatus.Canceled;
			m_flyoutState = FlyoutState.Closing;

			// We loop here because it is possible for the consumer's completion handler to cause a
			// new deferred operation to begin. If that operation is synchronously canceled for some
			// reason, we will need to complete the new operation immediately, since there will not
			// be another Closed event. And as are firing another completion handler, the situation
			// repeats itself.
			while (m_spCurrentOperation != null && asyncStatus == AsyncStatus.Canceled)
			{
				asyncStatus = m_spCurrentOperation.Status;
				// IFC(m_spCurrentOperation.As(&spAsyncInfo));
				//IFC(spAsyncInfo->get_Status(&asyncStatus));
				if (!m_isShowAtForCurrentOperationDeferred || asyncStatus == AsyncStatus.Canceled)
				{
					// Two cases:
					// 1. There is a non-deferred operation associated with the closing flyout. Since
					// the operation was not completed earlier, the closing of the flying triggers
					// completion of the operation.
					// 2. There is a deferred operation, but it has already been cancelled. We can
					// complete the canceled operation immediately rather than showing the flyout again.
					Complete(m_cancellationValueCallback());
				}

				if (m_isShowAtForCurrentOperationDeferred)
				{
					//m_spCurrentOperation.As(&spAsyncInfo);
					//IFC(spAsyncInfo->get_Status(&asyncStatus));
					asyncStatus = m_spCurrentOperation.Status;
					if (asyncStatus != AsyncStatus.Canceled)
					{
						// Consumer attempted to start an operation while the flyout was closing.
						// Show the flyout again, and clear the deferral info. We need to do this
						// asynchronously because FlyoutBase will not accept another ShowAt until
						// the flyout is fully closed, which happens after the Closed event.
						BeginAttemptStartDeferredOperation();
					}
				}
			}

			if (m_flyoutState == FlyoutState.Closing)
			{
				m_flyoutState = FlyoutState.Closed;
			}
		}

		private void BeginAttemptStartDeferredOperation()
		{
			m_spDispatcherQueue.TryEnqueue(AttemptStartDeferredOperation);
		}

		private void AttemptStartDeferredOperation()
		{
			AssertInvariants();

			if (m_flyoutState == FlyoutState.Closed)
			{
				//wrl::ComPtr<wf::IAsyncInfo> spAsyncInfo;
				//wf::AsyncStatus asyncStatus = wf::AsyncStatus::Started;

				//IFC(m_spCurrentOperation.As(&spAsyncInfo));
				//IFC(spAsyncInfo->get_Status(&asyncStatus));
				var asyncStatus = m_spCurrentOperation.Status;

				if (asyncStatus == AsyncStatus.Canceled)
				{
					Complete(m_cancellationValueCallback());
				}
				else
				{
					//NT_ASSERT(asyncStatus == wf::AsyncStatus::Started);
					Debug.Assert(asyncStatus == AsyncStatus.Started);
					m_pAssociatedFlyoutNoRef.ShowAt(m_tpTargetForDeferredShowAt);
					m_isShowAtForCurrentOperationDeferred = false;
					m_tpTargetForDeferredShowAt = null;
				}
			}
			// else something else reopened the flyout before this callback had a
			// chance to run, possibly with a different placement target. In this case
			// just keep the deferred operation active and wait again for the
			// current flyout to close.

			AssertInvariants();
		}

		private void AssertInvariants()
		{
			//NT_ASSERTMSG(
			//	"State saved for deferred operation, but operation is null.",
			//	m_spCurrentOperation || (!m_tpTargetForDeferredShowAt && !m_isShowAtForCurrentOperationDeferred));
			//NT_ASSERTMSG(
			//	"Operation must have an associated flyout",
			//	m_pAssociatedFlyoutNoRef || !m_isInitialized);
			Debug.Assert(m_spCurrentOperation != null || (m_tpTargetForDeferredShowAt == null && !m_isShowAtForCurrentOperationDeferred));

			Debug.Assert(m_pAssociatedFlyoutNoRef != null || !m_isInitialized);
		}

		private void AssertCompleteOperationPreconditions()
		{
			//NT_ASSERTMSG(
			//	"Attempting to complete a null operation",
			//	m_spCurrentOperation);

			//NT_ASSERT(SUCCEEDED(m_spCurrentOperation.As(&spAsyncInfo)));
			//NT_ASSERT(SUCCEEDED(spAsyncInfo->get_Status(&status)));
			var status = m_spCurrentOperation.Status;
			//NT_ASSERTMSG(
			//	"Attempting to complete an operation that's already been completed.",
			//	status != wf::AsyncStatus::Completed);
			//Debug.Assert(status == AsyncStatus.Completed);

			//NT_ASSERTMSG(
			//	"Attempting to complete an operation but still waiting to show flyout",
			//	!m_isShowAtForCurrentOperationDeferred || status == wf::AsyncStatus::Canceled);
			Debug.Assert(!m_isShowAtForCurrentOperationDeferred || status == AsyncStatus.Canceled);
		}
	}
}

