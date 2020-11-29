#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;

namespace Windows.Foundation
{
	internal interface IAsyncActionWithProgressInternal<TProgress> : IAsyncActionWithProgress<TProgress>
	{
		Task Task { get; }
	}
}
