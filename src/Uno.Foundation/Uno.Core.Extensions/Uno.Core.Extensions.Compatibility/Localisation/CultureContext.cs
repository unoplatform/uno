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
#if !WINDOWS_UWP
using System;
using System.Globalization;
using System.Threading;

namespace Uno.Localisation
{
	internal class CultureContext : IDisposable
	{
		private readonly CultureInfo _previousUICulture;
		private readonly CultureInfo _previousCulture;

		public CultureContext(CultureInfo uiCulture, CultureInfo culture = null)
		{
			_previousUICulture = CultureInfo.CurrentUICulture;
			_previousCulture = CultureInfo.CurrentCulture;

			SetCurrentCulture(uiCulture, culture);
		}

		public void Dispose()
		{
			SetCurrentCulture(_previousUICulture, _previousCulture);
		}

		public static void SetCurrentCulture(CultureInfo uiCulture, CultureInfo culture = null)
		{
			if(uiCulture == null)
			{
				throw new ArgumentNullException("uiCulture");
			}

			Thread.CurrentThread.CurrentUICulture = uiCulture;
			Thread.CurrentThread.CurrentCulture = culture ?? uiCulture;
		}

		public static CultureContext Create(string cultureIdentifier)
		{
			return new CultureContext(new CultureInfo(cultureIdentifier));
		}
	}
}
#endif