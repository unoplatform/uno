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

namespace Uno.Decorator
{
    /// <summary>
    /// A implementation of IDecorator.
    /// </summary>
    /// <typeparam name="T">The type to decorate.</typeparam>
    internal class Decorator<T> : IDecorator<T>
    {
        /// <summary>
        /// Constructs a new Decorator for a default(T) target.
        /// </summary>
        public Decorator()
            : this(default(T))
        {
        }

        /// <summary>
        /// Constructs a new Decorator for a specified target.
        /// </summary>
        /// <param name="target">The target.</param>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Justification="Legacy code")]
		public Decorator(T target)
        {
            // TODO: Accessing virtual methods within constructor
            Target = target;
        }

        #region IDecorator<T> Members

        /// <summary>
        /// See base.
        /// </summary>
        public virtual T Target { get; set; }

        #endregion
    }
}