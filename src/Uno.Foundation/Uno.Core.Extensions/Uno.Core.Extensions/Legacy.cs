#nullable disable

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
using System.Text;

namespace Uno
{
	/// <summary>
	/// Marks a member or a class as legacy. To be used in conjuction with the Obsolete attribute
	/// to mark elements as obsolete, but fail compilation based on static analysis rules.
	/// </summary>
	[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Event | AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
	internal sealed class LegacyAttribute : Attribute
	{
		readonly string _ruleId;

		// This is a positional argument
		public LegacyAttribute(string ruleId)
		{
			_ruleId = ruleId;
		}

		public string RuleId
		{
			get { return _ruleId; }
		}
	}
}
