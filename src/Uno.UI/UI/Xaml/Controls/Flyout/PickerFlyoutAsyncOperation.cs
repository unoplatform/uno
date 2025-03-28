using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Controls
{
	internal class PickerFlyoutAsyncOperation<TResult> : IAsyncOperationInternal<TResult>
	{
		private readonly TaskCompletionSource<TResult> _tcs;
		private readonly IAsyncOperation<TResult> _asyncOperation;

		//private readonly FlyoutBase _pAssociatedFlyout;
		private FlyoutBase m_spPendingFlyout;

		public PickerFlyoutAsyncOperation()
		{
			_tcs = new TaskCompletionSource<TResult>();
			_asyncOperation = _tcs.Task.AsAsyncOperation();
		}

		public Exception ErrorCode => _asyncOperation.ErrorCode;
		public uint Id => _asyncOperation.Id;
		public AsyncStatus Status => _asyncOperation.Status;

		public void Cancel()
		{
			//HRESULT hr = S_OK;

			//IFC(__super::Cancel());
			_tcs.TrySetCanceled();


			//if (m_spPendingFlyout)
			//{
			//	IFC(m_spPendingFlyout->Hide());
			//}

			if (m_spPendingFlyout != null)
			{
				m_spPendingFlyout.Hide();
			}

			//Cleanup:
			//return hr;
		}

		public void Close() => throw new NotImplementedException();

		public AsyncOperationCompletedHandler<TResult> Completed { get; set; }

		public TResult GetResults()
		{
			//HRESULT hr = S_OK;
			//TResult tmp;

			//IFC(this->GetResult(&tmp));
			//*result = static_cast<T_abi>(tmp);

			//Cleanup:
			//return S_OK;
			return _tcs.Task.Result;
		}

		public void StartOperation(FlyoutBase pAssociatedFlyout)
		{
			//HRESULT hr = S_OK;
			//NT_ASSERTMSG("StartOperation should never be called on an operation already associated with a flyout.", !m_spPendingFlyout);
			Debug.Assert(m_spPendingFlyout == null);

			m_spPendingFlyout = pAssociatedFlyout;
			//IFC(base_type::StartOperation());

			//Cleanup:
			//return hr;
		}

		public void CompleteOperation(TResult result)
		{
			//HRESULT hr = S_OK;
			//NT_ASSERTMSG("Expected operation to have a ref to the associated open flyout.", m_spPendingFlyout);

			//m_spPendingFlyout.Reset();
			m_spPendingFlyout = null;
			//this->SetResult(result);
			//IFC(this->FireCompletionImpl());

			//Cleanup:
			//return hr;
			_tcs.TrySetResult(result);
		}

		public Task<TResult> Task => _tcs.Task;
	}
}
