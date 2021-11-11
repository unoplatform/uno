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
    internal interface IValueMemberDescriptor : IMemberDescriptor
    {
        object GetValue(object instance);
        void SetValue(object instance, object value);

        /// <summary>
        /// Creates a compiled method that will allow a the assignation of the current member.
        /// </summary>
        /// <returns>A delegate taking an instance as the first parameter, and the value as the second parameter.</returns>
        Action<object, object> ToCompiledSetValue();

        /// <summary>
        /// Creates a compiled method that will allow a the assignation of the current member.
        /// </summary>
        /// <param name="strict">Removes some type checking to enhance performance if set to false.</param>
        /// <returns>A delegate taking an instance as the first parameter, and the value as the second parameter.</returns>
        /// <remarks>
        /// The use of the strict parameter is required if the caller of the generated method does not validate 
        /// parameter types before the call. Invalid parameters could result in unexpected behavior.
        /// </remarks>
        Action<object, object> ToCompiledSetValue(bool strict);

        /// <summary>
        /// Creates a compiled method that will get the value of  of the current member. 
        /// </summary>
        /// <param name="fieldInfo">The field to get the value from.</param>
        /// <returns></returns>
        Func<object, object> ToCompiledGetValue();

        /// <summary>
        /// Creates a compiled method that will get the value of  of the current member.
        /// </summary>
        /// <param name="fieldInfo">The field to get the value from.</param>
        /// <param name="strict">Removes some type checking to enhance performance if set to false.</param>
        /// <returns>A delegate taking an instance as the first parameter, and returns the value of the field.</returns>
        /// <remarks>
        /// The use of the strict parameter is required if the caller of the generated method does not validate 
        /// parameter types before the call. An invalid parameter could result in unexpected behavior.
        /// </remarks>
        Func<object, object> ToCompiledGetValue(bool strict);
    }
}