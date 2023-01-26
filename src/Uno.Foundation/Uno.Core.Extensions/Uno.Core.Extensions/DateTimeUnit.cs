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

namespace Uno
{
	[Flags]
	internal enum DateTimeUnit
	{
		Year = 1,
		Month = 2,
		Day = 4,
		Hour = 8,
		Minute = 16,
		Second = 32,
		Millisecond = 64,
		ToMonth = Year | Month,
		ToDay = ToMonth | Day,
		ToHour = ToDay | Hour,
		ToMinute = ToHour | Minute,
		ToSecond = ToMinute | Second,
		ToMillisecond = ToSecond | Millisecond
	}
}
