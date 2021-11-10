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
using System.Linq.Expressions;
using System.Reflection;
using Uno.Extensions;

namespace Uno.Expressions
{
    public class EditableMethodCallExpression : EditableExpression<MethodCallExpression>
    {
        private readonly EditableExpressionCollection<Expression> arguments;

        public EditableMethodCallExpression(MethodCallExpression expression)
            : base(expression, false)
        {
            Method = expression.Method;
            Object = expression.Object.Edit();
            arguments = new EditableExpressionCollection<Expression>(expression.Arguments);
        }

        public MethodInfo Method { get; set; }

        public IEditableExpression Object { get; set; }

        public EditableExpressionCollection<Expression> Arguments
        {
            get { return arguments; }
        }

        public override IEnumerable<IEditableExpression> Nodes
        {
            get
            {
                yield return Object;
                foreach (var item in Arguments.Items)
                {
                    yield return item;
                }
            }
        }

        public override MethodCallExpression DoToExpression()
        {
            return Expression.Call(Object.ToExpression(), Method, arguments.ToExpression());
        }
    }
}