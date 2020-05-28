using Windows.UI.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.DataBinding
{
    /// <summary>
    /// Provides a centralized garbage collection controlled for IBinder implementation used through mixins.
    /// </summary>
    internal static class BinderCollector
    {
        private static bool _collectRequested;

        /// <summary>
        /// Requests a garbage collection to ensure that subviews for the currently collected view will also be reclaimed.
        /// </summary>
        internal static void RequestCollect()
        {
            if (!_collectRequested)
            {
                // If there are subviews, trigger a collection to force the newly freed
                // level to be collected. Otherwise, this would rely on normal collection
                // which could take a long time to reclaim the memory of a very deep UI tree.
                _collectRequested = true;

                // Dispatch first on the idle dispatcher to improve the 
                // odds of the user not using the UI
                CoreDispatcher.Main.RunIdleAsync(
                    async _ => {

                        try
                        {
                            // Then dispatch the collect to the task pool, so it does not block the UI
                            // while collecting, particularly if the collection is concurrent, using these parameters :
                            // --setenv=MONO_GC_PARAMS=soft-heap-limit=512m,nursery-size=64m,evacuation-threshold=66,major=marksweep,concurrent-sweep
                            await Task.Run(() => GC.Collect());
                        }
                        finally
                        {
                            _collectRequested = false;
                        }
                    }
                );
            }
        }
    }
}