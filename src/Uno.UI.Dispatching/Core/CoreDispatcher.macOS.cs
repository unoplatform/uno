#nullable disable

#if __MACOS__
using Foundation;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.Dispatching
{
    internal sealed partial class CoreDispatcher
    {
        private CoreFoundation.DispatchQueue _mainQueue = CoreFoundation.DispatchQueue.MainQueue;

        partial void EnqueueNative()
        {
            _mainQueue.DispatchAsync(DispatchItems);
		}

		private bool GetHasThreadAccess() => NSThread.IsMain;
    }
}
#endif
