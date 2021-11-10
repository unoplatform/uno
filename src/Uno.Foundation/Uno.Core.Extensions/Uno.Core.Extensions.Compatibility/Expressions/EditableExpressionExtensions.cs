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
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Uno.Expressions;

namespace Uno.Extensions
{
    public static class EditableExpressionExtensions
    {
        public static EditableLambdaExpression<T> Edit<T>(this Expression<T> expression)
        {
            return new EditableLambdaExpression<T>(expression);
        }

        public static EditableConstantExpression Edit(this ConstantExpression expression)
        {
            return new EditableConstantExpression(expression);
        }

        public static EditableBinaryExpression Edit(this BinaryExpression expression)
        {
            return new EditableBinaryExpression(expression);
        }

        public static EditableConditionalExpression Edit(this ConditionalExpression expression)
        {
            return new EditableConditionalExpression(expression);
        }

        public static EditableInvocationExpression Edit(this InvocationExpression expression)
        {
            return new EditableInvocationExpression(expression);
        }

        public static EditableLambdaExpression Edit(this LambdaExpression expression)
        {
            return new EditableLambdaExpression(expression);
        }

        public static EditableParameterExpression Edit(this ParameterExpression expression)
        {
            return new EditableParameterExpression(expression);
        }

        public static EditableListInitExpression Edit(this ListInitExpression expression)
        {
            return new EditableListInitExpression(expression);
        }

        public static EditableMemberExpression Edit(this MemberExpression expression)
        {
            return new EditableMemberExpression(expression);
        }

        public static EditableMemberInitExpression Edit(this MemberInitExpression expression)
        {
            return new EditableMemberInitExpression(expression);
        }

        public static EditableMethodCallExpression Edit(this MethodCallExpression expression)
        {
            return new EditableMethodCallExpression(expression);
        }

        public static EditableNewArrayExpression Edit(this NewArrayExpression expression)
        {
            return new EditableNewArrayExpression(expression);
        }

        public static EditableNewExpression Edit(this NewExpression expression)
        {
            return new EditableNewExpression(expression);
        }

        public static EditableTypeBinaryExpression Edit(this TypeBinaryExpression expression)
        {
            return new EditableTypeBinaryExpression(expression);
        }

        public static EditableUnaryExpression Edit(this UnaryExpression expression)
        {
            return new EditableUnaryExpression(expression);
        }

        public static IEditableExpression Edit(this Expression expression)
        {
            if (expression is ConstantExpression) return Edit(expression as ConstantExpression);
            if (expression is BinaryExpression) return Edit(expression as BinaryExpression);
            if (expression is ConditionalExpression) return Edit(expression as ConditionalExpression);
            if (expression is InvocationExpression) return Edit(expression as InvocationExpression);
            if (expression is LambdaExpression) return Edit(expression as LambdaExpression);
            if (expression is ParameterExpression) return Edit(expression as ParameterExpression);
            if (expression is ListInitExpression) return Edit(expression as ListInitExpression);
            if (expression is MemberExpression) return Edit(expression as MemberExpression);
            if (expression is MemberInitExpression) return Edit(expression as MemberInitExpression);
            if (expression is MethodCallExpression) return Edit(expression as MethodCallExpression);
            if (expression is NewArrayExpression) return Edit(expression as NewArrayExpression);
            if (expression is NewExpression) return Edit(expression as NewExpression);
            if (expression is TypeBinaryExpression) return Edit(expression as TypeBinaryExpression);
            if (expression is UnaryExpression) return Edit(expression as UnaryExpression);
            return null;
        }

        public static IEnumerable<IEditableExpression> Edit(this IEnumerable<Expression> expressions)
        {
            return expressions.Select<Expression, IEditableExpression>(Edit);
        }

        public static IEnumerable<IEditableExpression> AllNodes(this IEditableExpression expression)
        {
            foreach (IEditableExpression node in expression.Nodes.SelectMany(item => item.AllNodes()))
            {
                yield return node;
            }

            yield return expression;
        }

        public static void Use(this IEditableExpression editableExpression, Expression expression)
        {
            editableExpression.AllNodes().ForEach(item => item.Use(expression));
        }

        public static void Use(this IEditableExpression editableExpression, IEnumerable<Expression> expressions)
        {
            expressions.ForEach(item => Use(editableExpression, item));
        }
    }
}