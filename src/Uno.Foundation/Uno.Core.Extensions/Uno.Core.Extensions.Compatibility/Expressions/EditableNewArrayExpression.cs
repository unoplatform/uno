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
using System.Linq.Expressions;

namespace Uno.Expressions
{
    public class EditableNewArrayExpression : EditableExpression<NewArrayExpression>
    {
        private readonly EditableExpressionCollection<Expression> expressions;

        public EditableNewArrayExpression(NewArrayExpression expression)
            : base(expression)
        {
            Type = expression.Type;
            expressions = new EditableExpressionCollection<Expression>(expression.Expressions);
        }

        public override ExpressionType NodeType
        {
            get { return base.NodeType; }
            set
            {
                switch (value)
                {
                    case System.Linq.Expressions.ExpressionType.NewArrayInit:
                    case System.Linq.Expressions.ExpressionType.NewArrayBounds:
                        base.NodeType = value;
                        break;

                    default:
                        throw new InvalidOperationException(
                            "NodeType for NewArrayExpression must be ExpressionType.NewArrayInit or ExpressionType.NewArrayBounds");
                }
            }
        }

        public Type Type { get; set; }

        public EditableExpressionCollection<Expression> Expressions
        {
            get { return expressions; }
        }

        public override IEnumerable<IEditableExpression> Nodes
        {
            get { return Expressions.Items.Cast<IEditableExpression>(); }
        }

        public override NewArrayExpression DoToExpression()
        {
            switch (NodeType)
            {
                case System.Linq.Expressions.ExpressionType.NewArrayInit:
                    return Expression.NewArrayInit(Type, expressions.ToExpression());

                case System.Linq.Expressions.ExpressionType.NewArrayBounds:
                    return Expression.NewArrayBounds(Type, expressions.ToExpression());

                default:
                    throw new InvalidOperationException(
                        "NodeType for NewArrayExpression must be ExpressionType.NewArrayInit or ExpressionType.NewArrayBounds");
            }
        }
    }
}