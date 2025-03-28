#nullable enable

using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Input;

namespace Uno.UI.Xaml.Input
{
	internal class FocusAsyncOperation
	{
		private FocusMovementResult? _focusMovementResult;
		private TaskCompletionSource<FocusMovementResult> _completionSource = new TaskCompletionSource<FocusMovementResult>();

		public FocusAsyncOperation(Guid correlationId)
		{
			CorrelationId = correlationId;
		}

		public IAsyncOperation<FocusMovementResult> CreateAsyncOperation() =>
			AsyncOperation.FromTask(ct =>
			{
				ct.Register(() =>
				{
					_completionSource.TrySetCanceled(ct);
				});
				return _completionSource.Task;
			});

		public Guid CorrelationId { get; set; }

		internal void CoreSetResults(FocusMovementResult coreFocusMovementResult)
		{
			if (_focusMovementResult == null)
			{
				_focusMovementResult = new FocusMovementResult();
			}

			bool wasMoved = coreFocusMovementResult.WasMoved && !coreFocusMovementResult.WasCanceled;

			_focusMovementResult.Succeeded = wasMoved;
		}

		internal void CoreFireCompletion()
		{
			_completionSource.SetResult(_focusMovementResult ?? new FocusMovementResult());
		}
	}
}
