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
    internal class EditableListInitExpression : EditableExpression<ListInitExpression>
    {
        private readonly List<EditableElementInit> initializers;

        public EditableListInitExpression(ListInitExpression expression)
            : base(expression, false)
        {
            initializers = new List<EditableElementInit>(expression.Initializers.Select(item => new EditableElementInit(item)));
            NewExpression = expression.NewExpression.Edit();
        }

        public EditableNewExpression NewExpression { get; set; }

        public IList<EditableElementInit> Initializers
        {
            get { return initializers; }
        }

        public override IEnumerable<IEditableExpression> Nodes
        {
            get
            {
                yield return NewExpression;
                foreach (var init in Initializers.SelectMany(item => item.Arguments.Items).Cast<IEditableExpression>())
                {
                    yield return init;
                }
            }
        }

        public override ListInitExpression DoToExpression()
        {
            return Expression.ListInit(NewExpression.DoToExpression(),
                                       initializers.Select(item => item.ToElementInit()).ToArray());
        }
    }
}