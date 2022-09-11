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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Uno.Extensions
{
    internal static class ComparableExtensions
    {
        public static T Min<T>(this T left, T right)
            where T : IComparable
        {
            if (left == null)
            {
                return right;
            }
            else if (right == null)
            {
                return left;
            }
            else
            {
                return left.CompareTo(right) < 0 ? left : right;
            }
        }

        public static T Max<T>(this T left, T right)
            where T : IComparable
        {
            if (left == null)
            {
                return right;
            }
            else if (right == null)
            {
                return left;
            }
            else
            {
                return left.CompareTo(right) > 0 ? left : right;
            }
        }

        public static T? Min<T>(this T? left, T? right)
            where T : struct, IComparable
        {
            if (left == null)
            {
                return right;
            }
            else if (right == null)
            {
                return left;
            }
            else
            {
                return left.Value.CompareTo(right.Value) < 0 ? left.Value : right.Value;
            }
        }

        public static T? Max<T>(this T? left, T? right)
            where T : struct, IComparable
        {
            if (left == null)
            {
                return right;
            }
            else if (right == null)
            {
                return left;
            }
            else
            {
                return left.Value.CompareTo(right.Value) > 0 ? left.Value : right.Value;
            }
        }

        public static int SafeCompareTo<T>(this T left, T right)
            where T : IComparable
        {
            if(left != null)
            {
                return left.CompareTo(right);
            }
            else if(right == null)
            {
                return 0;
            }
            else
            {
                return -1;
            }
        }
    }
}
