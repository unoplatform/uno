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
    public class EditableConditionalExpression : EditableExpression<ConditionalExpression>
    {
        public EditableConditionalExpression(ConditionalExpression expression)
            : base(expression)
        {
            Test = expression.Test.Edit();
            IfTrue = expression.IfTrue.Edit();
            IfFalse = expression.IfFalse.Edit();
        }

        public IEditableExpression Test { get; set; }

        public IEditableExpression IfTrue { get; set; }

        public IEditableExpression IfFalse { get; set; }

        public override IEnumerable<IEditableExpression> Nodes
        {
            get
            {
                yield return Test;
                yield return IfTrue;
                yield return IfFalse;
            }
        }

        public override ConditionalExpression DoToExpression()
        {
            return Expression.Condition(Test.ToExpression(), IfTrue.ToExpression(), IfFalse.ToExpression());
        }
    }
}