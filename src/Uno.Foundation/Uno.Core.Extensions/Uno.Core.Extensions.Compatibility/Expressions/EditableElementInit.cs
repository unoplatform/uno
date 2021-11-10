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
using System.Linq.Expressions;
using System.Reflection;

namespace Uno.Expressions
{
    public class EditableElementInit
    {
        private readonly EditableExpressionCollection<Expression> arguments;

        public EditableElementInit(ElementInit elementInit)
        {
            AddMethod = elementInit.AddMethod;
            arguments = new EditableExpressionCollection<Expression>(elementInit.Arguments);
        }

        public MethodInfo AddMethod { get; set; }

        public EditableExpressionCollection<Expression> Arguments
        {
            get { return arguments; }
        }

        public ElementInit ToElementInit()
        {
            return Expression.ElementInit(AddMethod, Arguments.ToExpression());
        }
    }
}