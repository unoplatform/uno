// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference controls/test/testinfra/MUXTestInfra/Common/ValueChangedEventWaiter.cs, commit fee1e8acc

using System;

using Microsoft.Windows.Apps.Test.Automation;
using Microsoft.Windows.Apps.Test.Foundation;
using Microsoft.Windows.Apps.Test.Foundation.Controls;
using Microsoft.Windows.Apps.Test.Foundation.Patterns;
using Microsoft.Windows.Apps.Test.Foundation.Waiters;

namespace Microsoft.UI.Xaml.Tests.MUXControls.InteractionTests.Common
{
	public class ValueChangedEventWaiter : UIEventWaiter
	{
		private UIObject root;
		private string expectedValue;
		private bool expectMatch;

		public ValueChangedEventWaiter(IValue root, string expectedValue = "", bool expectMatch = true)
			: base(new PropertyChangedEventSource(root as UIObject, Scope.Element, UIProperty.Get("Value.Value")))
		{
			this.root = root as UIObject;
			this.expectedValue = expectedValue;
			this.expectMatch = expectMatch;

			if (expectedValue.Length > 0)
			{
				this.AddFilter(new Predicate<WaiterEventArgs>(args => expectMatch ? root.Value == expectedValue : root.Value != expectedValue));
			}

			this.Start();
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
		}

		public override string ToString()
		{
			if (expectedValue.Length > 0)
			{
				return string.Format("ValueChangedEventWaiter with Condition:  Value of {0} changed to {2}'{1}'.", root.AutomationId, expectedValue, expectMatch ? string.Empty : "not be ");
			}
			else
			{
				return string.Format("ValueChangedEventWaiter with Condition:  Value of {0} changed.", root.AutomationId);
			}
		}
	}
}
