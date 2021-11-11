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
using System.Linq.Expressions;

namespace Uno.Expressions
{
    internal class EditableParameterExpression : EditableExpression<ParameterExpression>
    {
        public EditableParameterExpression(ParameterExpression expression)
            : base(expression, false)
        {
            Type = expression.Type;
            Name = expression.Name;
        }

        public Type Type { get; set; }

        public string Name { get; set; }

        public override ParameterExpression DoToExpression()
        {
            return Expression.Parameter(Type, Name);
        }

        protected override bool ShouldUse(ParameterExpression expression)
        {
            return expression.Type == Type /* &&
                    expression.Name == Name*/;
                //TODO Support Name, Index strategies to compare parameters (Require Parent Collection for index?)
        }
    }
}