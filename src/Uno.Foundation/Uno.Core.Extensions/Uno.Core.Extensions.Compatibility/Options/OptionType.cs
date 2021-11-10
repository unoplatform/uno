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
namespace Uno
{
	/// <summary>
	/// Represents the different possible types of an <see cref="Option{T}"/>
	/// </summary>
	public enum OptionType : byte
	{
		/// <summary>
		/// The option does not have value
		/// </summary>
		None = 0,

		/// <summary>
		/// The option have a value
		/// </summary>
		Some = 1
	}
}