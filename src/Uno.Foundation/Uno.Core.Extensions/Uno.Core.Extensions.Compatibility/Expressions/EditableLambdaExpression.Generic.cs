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
using Uno.Extensions;

namespace Uno.Expressions
{
    internal class EditableLambdaExpression<T> : EditableExpression<Expression<T>>
    {
        private readonly EditableParameterExpressionCollection parameters;

        public EditableLambdaExpression(Expression<T> expression)
            : base(expression, false)
        {
            Body = expression.Body.Edit();
            parameters = new EditableParameterExpressionCollection(Body, expression.Parameters);
        }

        public IEditableExpression Body { get; set; }

        public EditableParameterExpressionCollection Parameters
        {
            get { return parameters; }
        }

        public override IEnumerable<IEditableExpression> Nodes
        {
            get
            {
                yield return Body;
                foreach (var item in Parameters.Items)
                {
                    yield return item;
                }
            }
        }

        public override Expression<T> DoToExpression()
        {
            return Expression.Lambda<T>(Body.ToExpression(), Parameters.ToExpression());
        }
    }
}