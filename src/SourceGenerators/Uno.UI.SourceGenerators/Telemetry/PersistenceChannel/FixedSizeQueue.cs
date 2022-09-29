#nullable disable

// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// 2019/04/12 (Jerome Laban <jerome.laban@nventive.com>):
//	- Extracted from dotnet.exe
//

using System.Collections.Generic;

namespace Uno.UI.SourceGenerators.Telemetry.PersistenceChannel
{
    /// <summary>
    ///     A light fixed size queue. If Enqueue is called and queue's limit has reached the last item will be removed.
    ///     This data structure is thread safe.
    /// </summary>
    internal class FixedSizeQueue<T>
    {
        private readonly int _maxSize;
        private readonly Queue<T> _queue = new Queue<T>();
        private readonly object _queueLockObj = new object();

        internal FixedSizeQueue(int maxSize)
        {
            _maxSize = maxSize;
        }

        internal void Enqueue(T item)
        {
            lock (_queueLockObj)
            {
                if (_queue.Count == _maxSize)
                {
                    _queue.Dequeue();
                }

                _queue.Enqueue(item);
            }
        }

        internal bool Contains(T item)
        {
            lock (_queueLockObj)
            {
                return _queue.Contains(item);
            }
        }
    }
}
