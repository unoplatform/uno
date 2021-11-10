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
using System.Linq.Expressions;

namespace Uno.Expressions
{
    public abstract class EditableExpression<T> : IEditableExpression
        where T : Expression
    {
        private readonly bool nodeTypeEditable;
        private readonly T originalExpression;
        private Expression expressionOverride;
        private ExpressionType nodeType;

        // TODO: convert to protected
        public EditableExpression(T expression)
            : this(expression, true)
        {
        }

        // TODO: convert to protected
        public EditableExpression(T expression, bool nodeTypeEditable)
        {
            //TODO Validate expression != null
            originalExpression = expression;
            nodeType = expression.NodeType;
            this.nodeTypeEditable = nodeTypeEditable;
        }

        #region IEditableExpression Members

        public Expression OriginalExpression
        {
            get { return originalExpression; }
        }

        public virtual ExpressionType NodeType
        {
            get { return nodeType; }
            set
            {
                if (!nodeTypeEditable)
                {
                    throw new InvalidOperationException("Cannot change NodeType!");
                }
                nodeType = value;
            }
        }

        public Type ExpressionType
        {
            get { return typeof (T); }
        }

        Expression IEditableExpression.ToExpression()
        {
            return ToExpression();
        }

        public virtual IEnumerable<IEditableExpression> Nodes
        {
            get { return new IEditableExpression[0]; }
        }

        public virtual void Use(Expression expression)
        {
            if (ShouldUse(expression))
            {
                expressionOverride = expression;
            }
        }

        #endregion

        public virtual T ToExpression()
        {
            if (expressionOverride == null)
            {
                return DoToExpression();
            }
            return expressionOverride as T;
        }

        public abstract T DoToExpression();

        protected virtual bool ShouldUse(Expression expression)
        {
            if (expression.NodeType == NodeType &&
                expression is T)
            {
                var castedExpression = expression as T;

                return ShouldUse(castedExpression);
            }
            
            return false;
        }

        protected virtual bool ShouldUse(T expression)
        {
            return true;
        }
    }
}