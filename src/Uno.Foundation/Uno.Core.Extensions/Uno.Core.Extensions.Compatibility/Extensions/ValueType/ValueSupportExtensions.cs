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
namespace Uno.Extensions.ValueType
{
    internal static class ValueSupportExtensions
    {
        public static T And<T>(this T lhs, T rhs)
            where T : struct
        {
            return ValueSupport.Get<T>().And(lhs, rhs);
        }

        public static T Or<T>(this T lhs, T rhs)
            where T : struct
        {
            return ValueSupport.Get<T>().Or(lhs, rhs);
        }

        public static T Xor<T>(this T lhs, T rhs)
            where T : struct
        {
            return ValueSupport.Get<T>().Xor(lhs, rhs);
        }

        public static T Add<T>(this T lhs, T rhs)
            where T : struct
        {
            return ValueSupport.Get<T>().Add(lhs, rhs);
        }

        public static T Substract<T>(this T lhs, T rhs)
            where T : struct
        {
            return ValueSupport.Get<T>().Substract(lhs, rhs);
        }

        public static T Multiply<T>(this T lhs, T rhs)
            where T : struct
        {
            return ValueSupport.Get<T>().Multiply(lhs, rhs);
        }

        public static T Divide<T>(this T lhs, T rhs)
            where T : struct
        {
            return ValueSupport.Get<T>().Divide(lhs, rhs);
        }

        public static T Negate<T>(this T instance)
            where T : struct
        {
            return ValueSupport.Get<T>().Negate(instance);
        }

        public static T Not<T>(this T instance)
            where T : struct
        {
            return ValueSupport.Get<T>().Not(instance);
        }

        public static bool ContainsAny<T>(this T lhs, T rhs)
            where T : struct
        {
            return !Equals(lhs.And(rhs), (T) (object) 0);
        }

        public static bool ContainsAll<T>(this T lhs, T rhs)
            where T : struct
        {
            return Equals(lhs.And(rhs), rhs);
        }
    }
}