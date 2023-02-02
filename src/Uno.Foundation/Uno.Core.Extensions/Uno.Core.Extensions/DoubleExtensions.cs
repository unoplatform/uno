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
using System.Text;

namespace Uno.Extensions
{
	internal static class DoubleExtensions
	{
		/// <summary>
		/// Clamps the value between a minimum and maximum (clamping means limiting to a certain range)
		/// </summary>
		/// <param name="valueToClamp">value to clamp</param>
		/// <param name="minimum">minimal value possible</param>
		/// <param name="maximum">maximum value possible</param>
		/// <returns></returns>
		public static double Clamp(this double valueToClamp, double minimum, double maximum)
		{
			return Math.Min(Math.Max(valueToClamp, minimum), maximum);
		}
	}
}
