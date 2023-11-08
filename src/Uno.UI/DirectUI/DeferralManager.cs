// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Runtime.InteropServices.ComTypes;

namespace DirectUI;

//-------------------------------------------------------------------------------------------------
//  Class DeferralManager
// 
//  Abstract:
//      Helper for managing the creation and completion of deferrals. Keeps a count of the 
//      number of active deferrals and completes the async action when the count reaches zero.
//      Assumes that calls into public methods are made on the UI thread. 
//
//  Template Paramters:
//      TDeferral - A WinRT deferral object class, expected have an Initialize method with
//                  input parameters DeferralManager<TDeferral>* and ULONG. See 
//                  ContentDialogClosingDeferral_Partial.h for an example.
//
//  Notes:
//      The deferral pattern allows an application-provided callback handler to get a "deferral
//      object". Rather than continuing synchronously after the callback handler returns control,
//      the platform will wait for the application to call Complete() on the deferral object. 
//      The same result could be achieved by requiring that the application callback handler
//      return an IAsyncAction to the platform, which the platform would await. Rather than calling
//      Complete() on a deferral object, in this case the application would simply complete the 
//      IAsyncAction. 
//      The purpose of the deferral pattern is primarirly to avoid forcing the application to
//      provide the async task completion logic contained in this class.
//
//      See ContentDialog_Partial.*, ContentDialogButtonClickDeferral_Partial.h, and
//      ContentDialogButtonClickEventArgs_Partial.h for a usage example.
//
//  Lifetime management:
//      Note that the manager instance must stay alive at least until the deferral provider and
//      any generated deferral objects are finished using it, hence deferral provider and deferral 
//      object implementations should hold a ref to the manager they are using. In most cases,
//      it will also be desirable for the deferral executor hold a single manager for each deferral 
//      type, so that multiple deferral generations of the same deferral type cannot be in 
//      progress at once. The deferral executor should also check the pIsGenerationAlreadyInProgress
//      output paramter of the Prepare() method in this case and take appropriate action.
//      
//  Terminology:
//
//      Deferral Object
//      A "deferral object" is an object that implements a Complete() method, and is used to 
//      delay the execution of some operation until Complete() is called.
//
//      Deferral Provider
//      A "deferral provider" is an object that implements a GetDeferral method, which returns
//      creates and returns a new deferral object.
//
//      Deferral Generations
//      This class is intended to be reusable for multiple iterations of the pattern. We refer here
//      a single iteration of the pattern as a "deferral generation." A generation encompases:
//      1. A call to Prepare(), readying an instance of the manager for calls to GetDeferral() and
//         CompleteDeferral()
//      2. Zero or more pairs of calls to GetDeferral() and CompleteDeferral() triggered by the 
//         consumer of the deferral pattern, generally through the WinRT deferal provider and 
//         deferal types, respectively.
//         A call to ContinueWith(), which provides a callback to be executed as soon as all
//         of the deferral objects obtained through GetDeferral() have been completed.
//         ContinueWith() may be called at any point after Prepare() is called.
//      3. Execution of the provided callback.
//      To avoid situations where deferal objects or deferal providers associated with a 
//      generation are used after the generation has been completed, Prepare() returns the id of 
//      the current generation, and the deferal provider and deferral objects must pass in the
//      id of the generation they are associated with when calling GetDeferral() or 
//      CompleteDeferral()
//  
//      Deferral Executor
//      A "deferral executor" is an object responsible for preparing a deferral generation and
//      providing the logic to be executed when the deferral generation is completed.
//-------------------------------------------------------------------------------------------------

internal class DeferralManager<TDeferral>
{
	typedef std::function<HRESULT()> DeferralCompletionHandler;

        DeferralManager() :

			m_deferralCount(0)
	// Call to begin a deferral generation
	private void Prepare(
		_Out_ ULONG* pDeferralGeneration,
		_Out_ boolean* pIsGenerationAlreadyInProgress)
	{
		*pDeferralGeneration = DeferralManager < TDeferral >::s_deferralGeneration;
		*pIsGenerationAlreadyInProgress = false;

		if (m_deferralCount != 0)
		{
			*pIsGenerationAlreadyInProgress = true;
			goto Cleanup;
		}

		MUX_ASSERT(!m_callback);

		// Increment the deferral count to prevent completion of the generation before ContinueWith() 
		// is called. This allows the deferral manager to safely provide and complete deferrals as
		// soon as this method returns without fear of entering a bad state.
		IncrementDeferralCount();
	}

	//  Registers the specified function to be called back when the deferral generation is 
	//  completed.
	private void ContinueWith(DeferralCompletionHandler completionCallback)
	{
		m_callback = completionCallback;
		RRETURN(DecrementDeferralCount());
	}

	// Deferral executors should use this to disconnect the provided callback in cases when
	// their state has changed and it is no longer appropriate to execute their defferal 
	// completion logic.
	void Disconnect()
	{
		m_callback = nullptr;
	}

	// The WinRT deferral provider should delegate to this method to implement GetDeferral()
	_Check_return_ HRESULT GetDeferral(_In_ ULONG generation, _Outptr_ TDeferral** ppDeferral)
	{
		HRESULT hr = S_OK;
		ctl::ComPtr<TDeferral> spDeferral;

		if (m_deferralCount == 0 || generation != DeferralManager < TDeferral >::s_deferralGeneration)
		{
			IFC(ErrorHelper::OriginateErrorUsingResourceID(E_FAIL, ERROR_DEFERRAL_COMPLETED));
		}

		IFC(ctl::make(this, DeferralManager < TDeferral >::s_deferralGeneration, &spDeferral));
		IncrementDeferralCount();
		IFC(spDeferral.CopyTo(ppDeferral));

	Cleanup:
		RRETURN(hr);
	}

	// The WinRT deferral class should delegate to this method to implement Complete()
	_Check_return_ HRESULT CompleteDeferral(_In_ ULONG generation)
	{
		HRESULT hr = S_OK;

		if (m_deferralCount == 0 || generation != DeferralManager < TDeferral >::s_deferralGeneration)
		{
			IFC(ErrorHelper::OriginateErrorUsingResourceID(E_FAIL, ERROR_DEFERRAL_COMPLETED));
		}

		IFC(DecrementDeferralCount());

	Cleanup:
		RRETURN(hr);
	}

	private:

        inline void IncrementDeferralCount()
	{
		m_deferralCount++;
	}

	_Check_return_ HRESULT DecrementDeferralCount()
	{
		HRESULT hr = S_OK;
		if (0 == --m_deferralCount)
		{
			DeferralCompletionHandler callback = m_callback;
			m_callback = nullptr;

				::InterlockedIncrement(&DeferralManager < TDeferral >::s_deferralGeneration);

			if (callback)
			{
				IFC(callback());
			}
		}

	Cleanup:
		RRETURN(hr);
	}

	static ULONG s_deferralGeneration;

	DeferralCompletionHandler m_callback;
	UINT32 m_deferralCount;
};

template<typename TDeferral>
	ULONG DeferralManager<TDeferral>::s_deferralGeneration = 1;
}
