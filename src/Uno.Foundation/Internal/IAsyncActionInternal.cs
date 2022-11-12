#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;

namespace Windows.Foundation;

internal interface IAsyncActionInternal : IAsyncAction
{
	Task Task { get; }
}
