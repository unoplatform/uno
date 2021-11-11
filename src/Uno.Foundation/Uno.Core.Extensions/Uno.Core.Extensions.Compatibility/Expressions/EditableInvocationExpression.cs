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
using Uno.Extensions;

namespace Uno.Expressions
{
    internal class EditableInvocationExpression : EditableExpression<InvocationExpression>
    {
        private readonly EditableExpressionCollection<Expression> arguments;

        public EditableInvocationExpression(InvocationExpression expression)
            : base(expression, false)
        {
            Expression = expression.Expression.Edit();
            arguments = new EditableExpressionCollection<Expression>(expression.Arguments);
        }

        public EditableExpressionCollection<Expression> Arguments
        {
            get { return arguments; }
        }

        public IEditableExpression Expression { get; set; }

        public override IEnumerable<IEditableExpression> Nodes
        {
            get { return Arguments.Items.Cast<IEditableExpression>(); }
        }

        public override InvocationExpression DoToExpression()
        {
            return System.Linq.Expressions.Expression.Invoke(Expression.ToExpression(), Arguments.ToExpression());
        }
    }
}