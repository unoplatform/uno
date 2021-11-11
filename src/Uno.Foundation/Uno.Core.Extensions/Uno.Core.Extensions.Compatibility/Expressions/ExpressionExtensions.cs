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
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Uno.Extensions
{
    internal static class ExpressionExtensions
    {
        public static string GetMemberName<T>(this Expression<Func<T>> selector)
        {
            var member = selector.FindMember();

            return member == null ? null : member.Name;
        }

        public static MemberInfo FindMember<T>(this Expression<Func<T>> selector)
        {
            var memberExpression = selector.Body as MemberExpression;

            if (memberExpression == null)
            {
                selector.Body.Maybe<UnaryExpression>(u => memberExpression = u.Operand as MemberExpression);
            }

            if (memberExpression == null)
            {
                return null;
            }
            else
            {
                return memberExpression.Member;
            }
        }

        public static string GetSelectedMemberName<T, TValue>(this Expression<Func<T, TValue>> selector)
        {
            if (selector == null)
            {
                return null;
            }
            else
            {
                if (selector.Body is MemberExpression)
                {
                    var memberExpression = selector.Body as MemberExpression;
                    return memberExpression.Member.Name;
                }
                else if (selector.Body is UnaryExpression)
                {
                    var unaryExpression = selector.Body as UnaryExpression;
                    return (unaryExpression.Operand as MemberExpression).Member.Name;
                }
                return null;

            }
        }

        public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> left,
                                                       params Expression<Func<T, bool>>[] items)
        {
            var result = left;

            items.ForEach(item => result = Binary(CompositionType.And, result, item));

            return result;
        }

        public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> left,
                                                      params Expression<Func<T, bool>>[] items)
        {
            var result = left;

            items.ForEach(item => result = Binary(CompositionType.Or, result, item));

            return result;
        }

        public static Expression<Func<T, bool>> Not<T>(this Expression<Func<T, bool>> expression)
        {
            return Expression.Lambda<Func<T, bool>>(Expression.Not(expression.Body), expression.Parameters);
        }

        public static Expression<Func<bool>> And(this Expression<Func<bool>> left, params Expression<Func<bool>>[] items)
        {
            var result = left;

            items.ForEach(item => result = Binary(CompositionType.And, result, item));

            return result;
        }

        public static Expression<Func<bool>> Or(this Expression<Func<bool>> left, params Expression<Func<bool>>[] items)
        {
            var result = left;

            items.ForEach(item => result = Binary(CompositionType.Or, result, item));

            return result;
        }

        public static Expression<Func<bool>> Not(this Expression<Func<bool>> expression)
        {
            return Expression.Lambda<Func<bool>>(Expression.Not(expression.Body), expression.Parameters);
        }

        private static Expression<T> Binary<T>(CompositionType type, Expression<T> left, Expression<T> right)
        {
            if (left == null)
            {
                return right;
            }

            if (right == null)
            {
                return left;
            }

            right = ReplaceParameters(left, right);

            var newBody = Binary(type, left.Body, right.Body);

            return Expression.Lambda<T>(newBody, left.Parameters);
        }

        private static Expression<T> ReplaceParameters<T>(Expression<T> source, Expression<T> target)
        {
            var editableTarget = target.Edit();

            editableTarget.Use(source.Parameters.Cast<Expression>());

            target = editableTarget.ToExpression();

            return target;
        }

        private static Expression Binary(CompositionType type, Expression left, Expression right)
        {
            return type == CompositionType.And ? Expression.And(left, right) : Expression.Or(left, right);
        }
    }
}