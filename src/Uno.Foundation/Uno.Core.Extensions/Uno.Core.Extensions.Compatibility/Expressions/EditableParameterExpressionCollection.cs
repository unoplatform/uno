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
    internal class EditableParameterExpressionCollection : EditableExpressionCollection<ParameterExpression>
    {
        private readonly IEditableExpression body;
        private IEnumerable<ParameterExpression> parametersOverride;

        public EditableParameterExpressionCollection(IEditableExpression body, IEnumerable<ParameterExpression> items)
            : base(items)
        {
            this.body = body;
        }

        public IEnumerable<EditableParameterExpression> Parameters
        {
            get { return Items.Cast<EditableParameterExpression>(); }
        }

        public void Use(IEnumerable<ParameterExpression> parameters)
        {
            parametersOverride = parameters;
        }

        public override ParameterExpression[] ToExpression()
        {
            var expressions = new List<ParameterExpression>();

            if (parametersOverride == null)
            {
                var bodyParams = body.AllNodes().OfType<EditableParameterExpression>();

                foreach (var param in Parameters)
                {
                    // TODO: Closure problem - Do we want closure?
                    var param1 = param;
                    //TODO Check if more than one param matches?
                    var bodyParam = bodyParams.FirstOrDefault(item => item.Type == param1.Type && item.Name == param1.Name);

                    expressions.Add((bodyParam ?? param).DoToExpression());
                }
            }
            else
            {
                expressions.AddRange(parametersOverride);
            }

            return expressions.ToArray();
        }
    }
}