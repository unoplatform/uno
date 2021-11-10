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
#if !XAMARIN && !WINDOWS_UWP
using System;

namespace Uno
{
    public class WeakReference<T>
    {
        private readonly WeakReference _reference;
        public WeakReference(T target)
        {
            _reference = new WeakReference(target);
        }

        public T Target
        {
            get { return (T)_reference.Target; }
            set { _reference.Target = value; }
        }

        public bool IsAlive
        {
            get { return _reference.IsAlive; }
        }

        public bool TrackResurrection
        {
            get { return _reference.TrackResurrection; }
        }

        public bool TryGetTarget(out T target)
        {
            target = Target;
            return _reference.IsAlive;
        }

        public T GetTarget()
        {
            return Target;
        }
    }
}
#endif