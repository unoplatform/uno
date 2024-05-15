using System.Threading.Tasks;

namespace UITests.Shared.Helpers;

internal interface IWaitableSample
{
	Task SamplePreparedTask { get; }
}
