// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference SplitButtonTestHelper.cpp, tag winui3/release/1.4.2

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Documents;

namespace Microsoft.UI.Private.Controls
{
	internal class SplitButtonTestHelper
	{
		private bool m_simulateTouch = false;

		[ThreadStatic] private static SplitButtonTestHelper s_instance;

		private static SplitButtonTestHelper EnsureInstance()
		{
			if (s_instance is not { })
			{
				s_instance = new SplitButtonTestHelper();
			}

			return s_instance;
		}

		public static bool SimulateTouch
		{
			get
			{
				var instance = EnsureInstance();
				return instance.m_simulateTouch;
			}
			set
			{
				var instance = EnsureInstance();
				instance.m_simulateTouch = value;
			}
		}
	}
}
