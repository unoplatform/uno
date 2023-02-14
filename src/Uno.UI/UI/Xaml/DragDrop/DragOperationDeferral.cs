#nullable enable

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.UI.Xaml
{
	public partial class DragOperationDeferral
	{
		private readonly TaskCompletionSource<object> _completion = new TaskCompletionSource<object>();

		internal DragOperationDeferral()
		{
		}

		public void Complete()
			=> _completion.TrySetResult(new object());

		internal async Task Completed(CancellationToken ct)
		{
			using var _ = ct.Register(() => _completion.TrySetCanceled(ct));
			await _completion.Task;
		}
	}
}
