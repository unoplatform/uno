#nullable enable

using System.Threading.Tasks;

namespace Windows.Foundation;

internal interface IAsyncActionWithProgressInternal<TProgress> : IAsyncActionWithProgress<TProgress>
{
	Task Task { get; }
}
