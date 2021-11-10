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
using System.Runtime.Serialization;
#if !SILVERLIGHT && !WINDOWS_UWP && !XAMARIN
namespace Uno
{
    /// <summary>
    /// A ReadOnlyException is thrown when accessing a logically readonly
    /// accessor when it is presents a readwrite interface.
    /// </summary>
    public class ReadOnlyException : ApplicationException
    {
        /// <summary>
        /// See Exception Pattern.
        /// </summary>
        public ReadOnlyException()
        {
        }

        /// <summary>
        /// See Exception Pattern.
        /// </summary>
        /// <param name="message"></param>
        public ReadOnlyException(string message)
            : base(message)
        {
        }


        /// <summary>
        /// See Exception Pattern.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected ReadOnlyException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// See Exception Pattern.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public ReadOnlyException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
#endif