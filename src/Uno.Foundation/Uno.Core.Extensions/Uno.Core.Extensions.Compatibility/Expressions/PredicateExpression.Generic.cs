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
using System.Linq.Expressions;
using Uno.Extensions;

namespace Uno.Expressions
{
    public class PredicateExpression<T>
    {
        private readonly Expression<Func<T, bool>> expression;

        public PredicateExpression(Expression<Func<T, bool>> expression)
        {
            //TODO Could make sure the parameter is "item"
            this.expression = expression;
        }

        public Expression<Func<T, bool>> Expression
        {
            get { return expression; }
        }

        public static implicit operator Expression<Func<T, bool>>(PredicateExpression<T> proxy)
        {
            return proxy.Expression;
        }

        public static implicit operator PredicateExpression<T>(Expression<Func<T, bool>> expression)
        {
            return new PredicateExpression<T>(expression);
        }

#if !WINDOWS_PHONE
        public static implicit operator Func<T, bool>(PredicateExpression<T> proxy)
        {
            return proxy.Expression.Compile();
        }
#endif

        public static bool operator true(PredicateExpression<T> expression)
        {
            return false;
        }

        public static bool operator false(PredicateExpression<T> expression)
        {
            return false;
        }

        public static PredicateExpression<T> operator &(PredicateExpression<T> lhs, PredicateExpression<T> rhs)
        {
            if (lhs == null)
            {
                return rhs;
            }

            if (rhs == null)
            {
                return lhs;
            }

            return lhs.Expression.And(rhs.Expression);
        }

        public static PredicateExpression<T> operator |(PredicateExpression<T> lhs, PredicateExpression<T> rhs)
        {
            if (lhs == null)
            {
                return rhs;
            }

            if (rhs == null)
            {
                return lhs;
            }

            return lhs.Expression.Or(rhs.Expression);
        }
    }
}