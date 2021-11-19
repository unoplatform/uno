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
    internal class Int32Support : ValueSupport<int>
    {
        protected override int CoreAnd(int lhs, int rhs)
        {
            return lhs & rhs;
        }

        protected override int CoreOr(int lhs, int rhs)
        {
            return lhs | rhs;
        }

        protected override int CoreXor(int lhs, int rhs)
        {
            return lhs ^ rhs;
        }

        protected override int CoreAdd(int lhs, int rhs)
        {
            return lhs + rhs;
        }

        protected override int CoreSubstract(int lhs, int rhs)
        {
            return lhs - rhs;
        }

        protected override int CoreMultiply(int lhs, int rhs)
        {
            return lhs*rhs;
        }

        protected override int CoreDivide(int lhs, int rhs)
        {
            return lhs/rhs;
        }

        protected override int CoreNegate(int instance)
        {
            return -instance;
        }

        protected override int CoreNot(int instance)
        {
            return ~instance;
        }
    }
}