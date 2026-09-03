using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Windows.Services.Store.Internal;

public interface IStoreContextExtension
{
	Task<StoreRateAndReviewResult> RequestRateAndReviewAsync(CancellationToken token);
}
