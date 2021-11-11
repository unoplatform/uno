// ******************************************************************
// Copyright � 2015-2018 nventive inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// ******************************************************************
using System;

namespace Uno.Threading
{
    internal class Synchronizable<T> : ISynchronizable<T>
    {
#if WINDOWS_PHONE
		
		// Prior to WP 8.0.10322 (GDR2), ReaderWriterLockSlim had a severe bug. Falling back to a standard lock
		// as a safety precaution.
		private readonly static bool UseExclusiveLock = Environment.OSVersion.Version < new Version(8, 0, 10322);
#endif

        private readonly ISynchronizableLock<T> syncLock;

        public Synchronizable(T instance)
        {
#if XAMARIN
			// RWLockSlim on mono (3.2 as of writing) seems to be time consuming, particularly when performing unusually long SpinLocks.
			// Until this is resolved, revert to standard locking.
			syncLock = new SynchronizableExclusiveLock<T>(instance);
#else
	#if WINDOWS_PHONE
			if (UseExclusiveLock)
			{
				syncLock = new SynchronizableExclusiveLock<T>(instance);
			}
			else
	#endif
			{
				syncLock = new SynchronizableLock<T>(instance);
			}
#endif // XAMARIN
        }

        #region ISynchronizable<T> Members

        public ISynchronizableLock<T> Lock
        {
            get { return syncLock; }
        }

        #endregion
    }
}
