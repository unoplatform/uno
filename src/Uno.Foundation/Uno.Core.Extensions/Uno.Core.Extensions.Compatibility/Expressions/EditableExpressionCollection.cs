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
    internal class EditableExpressionCollection<T>
        where T : Expression
    {
        private readonly IList<EditableExpression<T>> items;

        public EditableExpressionCollection(IEnumerable<Expression> items)
        {
            this.items = new List<EditableExpression<T>>(items.Edit().Cast<EditableExpression<T>>());
        }

        // TODO parameter can be type IEnumerable
        public EditableExpressionCollection(IEnumerable<T> items)
            : this(items.Cast<Expression>())
        {
        }

        public IList<EditableExpression<T>> Items
        {
            get { return items; }
        }

        public virtual T[] ToExpression()
        {
            return items.Select(item => item.DoToExpression()).ToArray();
        }
    }
}