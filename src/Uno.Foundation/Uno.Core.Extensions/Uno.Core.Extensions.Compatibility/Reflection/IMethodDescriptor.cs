#nullable disable

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
namespace Uno.Reflection
{
    internal interface IMethodDescriptor : IMemberDescriptor
    {
        object Invoke(object instance, params object[] args);

        /// <summary>
        /// Build a compiled method that will call the specified method.
        /// </summary>
        /// <param name="MethodInfo">The method to invoke</param>
        /// <returns>A delegate that will call the requested method</returns>
        Func<object, object[], object> ToCompiledMethodInvoke();

        /// <summary>
        /// Build a compiled method that will call the specified method.
        /// </summary>
        /// <param name="MethodInfo">The method to invoke</param>
        /// <param name="strict">Removes some type checking to enhance performance if set to false.</param>
        /// <returns>A delegate that will call the requested method</returns>
        /// <remarks>
        /// The use of the strict parameter is required if the caller of the generated method does not validate 
        /// parameter types before the call. An invalid parameter could result in unexpected behavior.
        /// </remarks>
        Func<object, object[], object> ToCompiledMethodInvoke(bool strict);
    }
}