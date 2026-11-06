// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference incrementalloading.h, incrementalloading.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d

//  Abstract:
//      Provides the async operation implementation required for ListViewBase's
//      incremental loading functionality.


#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Data;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

internal class IncrementalLoadAsyncCompleteHandler
{
	private LoadMoreItemsOperation? m_spChainedAsyncOperation;
#pragma warning disable CS0414 // Only used in #if !HAS_UNO code.
	private bool m_chainedInvokeCalled;
#pragma warning restore CS0414

	internal IncrementalLoadAsyncCompleteHandler()
	{
		m_chainedInvokeCalled = false;
	}

#if !HAS_UNO // TODO Uno: C# has no deterministic destructor, and a finalizer would close the operation off the UI thread.
	~IncrementalLoadAsyncCompleteHandler()
	{
		if (m_spChainedAsyncOperation is not null && !m_chainedInvokeCalled)
		{
			m_spChainedAsyncOperation.Close();
		}

		m_spChainedAsyncOperation = null;
	}
#endif

	// Called by data source when the incremental data load is completed.
	internal void Invoke(IAsyncOperation<LoadMoreItemsResult> asyncInfo, AsyncStatus status)
	{
		// Notify the chained async operation of data load completion
		if (m_spChainedAsyncOperation is not null)
		{
			// TODO Uno: GetResults throws on a canceled or failed operation, which would escape the Completed handler.
			LoadMoreItemsResult result = default;
			if (status == AsyncStatus.Completed)
			{
				result = asyncInfo.GetResults();
			}

			m_chainedInvokeCalled = true;
			m_spChainedAsyncOperation.RaisedCompleteWithResult(result.Count, status);
			m_spChainedAsyncOperation = null;
		}
		else
		{
			IAsyncInfo spAsyncInfo = asyncInfo;
			spAsyncInfo.Close();
		}
	}

	internal void SetChainedAsyncOperation(LoadMoreItemsOperation? pChainedAsyncOperation)
	{
		global::System.Diagnostics.Debug.Assert(m_spChainedAsyncOperation is null);
		m_spChainedAsyncOperation = pChainedAsyncOperation;
	}
}

internal class LoadMoreItemsOperation
{
	private uint m_count;
	private WeakReference<ListViewBase>? m_wrListViewBase;
	private IAsyncInfo? m_spDataSourceAsyncInfo;
	// Skip raising event if data is yet processing
	private bool m_isDataProcessing;

	// TODO Uno: DXamlAsyncBase/AsyncBase have no C# form. The IAsyncOperation handed to callers is Uno's AsyncOperation,
	// completed through this source. The unique id comes from AsyncOperation, and m_spThis (a self reference) is not needed with a GC.
	private readonly TaskCompletionSource<LoadMoreItemsResult> m_completion = new();
	private readonly AsyncOperation<LoadMoreItemsResult> m_operation;

	internal LoadMoreItemsOperation()
	{
		m_count = 0;
		m_isDataProcessing = false;

		m_operation = AsyncOperation.FromTask(ct =>
		{
			ct.Register(() =>
			{
				OnCancel();
				m_completion.TrySetCanceled();
			});
			return m_completion.Task;
		});
	}

	// wf::IAsyncAction<LoadMoreItemsResults> forwarders
	internal IAsyncOperation<LoadMoreItemsResult> Operation => m_operation;

	internal AsyncOperationCompletedHandler<LoadMoreItemsResult>? Completed
	{
		get => m_operation.Completed;
		set => m_operation.Completed = value;
	}

	internal LoadMoreItemsResult GetResults() => new LoadMoreItemsResult { Count = m_count };

	internal bool HasStatus() => m_spDataSourceAsyncInfo is not null;

	internal AsyncStatus GetStatus() => m_spDataSourceAsyncInfo!.Status;

	internal void Init(ListViewBase pOwner, bool isDataProcessing)
	{
		m_isDataProcessing = isDataProcessing;
		m_wrListViewBase = new WeakReference<ListViewBase>(pOwner);
	}

	internal void Start() => OnStart();

	internal void Cancel() => m_operation.Cancel();

	internal void Close() => OnClose();

	private void OnStart()
	{
		ListViewBase? spListViewBase = null;

		// Application has called Start on the AsyncOperation we returned via ListViewBase.LoadMoreItemsAsync API.
		// Initiate incremental load on the data source.
		m_spDataSourceAsyncInfo = null;
		m_wrListViewBase?.TryGetTarget(out spListViewBase);
		if (spListViewBase is not null)
		{
			m_spDataSourceAsyncInfo = spListViewBase.LoadMoreItemsAsyncInternal(this);
		}

		// if we have an associated ListView and this listview does not generate an async info that is
		// because there's nothing else to load, we complete this operation synchronously
		if (!m_isDataProcessing && spListViewBase is not null)
		{
			if (m_spDataSourceAsyncInfo is not null)
			{
				AsyncStatus status = m_spDataSourceAsyncInfo.Status;

				if (status == AsyncStatus.Completed)
				{
					// Since the task is completed then we're done, just raise our completed handler
					// and be done
					var spAsyncOperation = (IAsyncOperation<LoadMoreItemsResult>)m_spDataSourceAsyncInfo;
					LoadMoreItemsResult result = spAsyncOperation.GetResults();

					RaisedCompleteWithResult(result.Count, AsyncStatus.Completed);
				}
			}
			else
			{
				RaisedCompleteWithResult(0, AsyncStatus.Completed);
			}
		}
	}

	private void OnCancel()
	{
		try
		{
			// Application has called Cancel on the AsyncOperation we returned via ListViewBase.LoadMoreItemsAsync API.
			// Call Cancel on the AsyncOperation of the DataSource.
			m_spDataSourceAsyncInfo?.Cancel();
		}
		finally
		{
			m_isDataProcessing = false;
		}
	}

	private void OnClose()
	{
		try
		{
			// Application has called Close on the AsyncOperation we returned via ListViewBase.LoadMoreItemsAsync API.
			// Call Close on the AsyncOperation of the DataSource.
			m_spDataSourceAsyncInfo?.Close();
		}
		finally
		{
			m_isDataProcessing = false;
			m_spDataSourceAsyncInfo = null;
		}
	}

	// Raises completed event to the caller of ListViewBase.LoadMoreItemsAsync
	// REVIEW: should this have status arg?
	internal void RaisedCompleteWithResult(uint result, AsyncStatus status)
	{
		ListViewBase? spListViewBase = null;

		if (m_isDataProcessing)
		{
			m_count = result;
			switch (status)
			{
				case AsyncStatus.Canceled:
					Cancel();
					break;
				case AsyncStatus.Error:
					m_completion.TrySetException(new InvalidOperationException("LoadMoreItemsAsync failed."));
					break;
			}

			// do not raise completion event if ListViewBase destroyed or marker as ready for GC.
			m_wrListViewBase?.TryGetTarget(out spListViewBase);
			if (spListViewBase is not null)
			{
				m_completion.TrySetResult(GetResults());
			}
		}
		else
		{
			// TODO Uno: WinUI leaves the public operation's completion to AsyncBase; here nothing else would complete it.
			m_count = result;
			m_completion.TrySetResult(GetResults());
		}
	}
}

