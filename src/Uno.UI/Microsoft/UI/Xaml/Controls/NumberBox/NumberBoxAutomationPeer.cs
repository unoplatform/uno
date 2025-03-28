// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference NumberBox.cpp, commit 8d856a3c9393d13d9d49a20d5cde984d1f5b397a

using Uno.UI.Helpers.WinUI;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Automation.Provider;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
	public class NumberBoxAutomationPeer : AutomationPeer, IRangeValueProvider
	{
		private NumberBox _owner;

		internal NumberBoxAutomationPeer(NumberBox owner)
		{
			_owner = owner;
		}

		// IAutomationPeerOverrides
		protected override object GetPatternCore(PatternInterface patternInterface)
		{
			if (patternInterface == PatternInterface.RangeValue)
			{
				return this;
			}

			return base.GetPatternCore(patternInterface);
		}

		protected override string GetClassNameCore()
		{
			return nameof(NumberBox);
		}

		protected override string GetNameCore()
		{
			var name = base.GetNameCore();

			if (string.IsNullOrEmpty(name))
			{
				name = SharedHelpers.TryGetStringRepresentationFromObject(_owner.Header);
			}

			return name;
		}

		protected override AutomationControlType GetAutomationControlTypeCore()
		{
			return AutomationControlType.Spinner;
		}

		private NumberBox GetImpl()
		{
			return _owner;
		}

		// IRangeValueProvider
		public double Minimum => GetImpl().Minimum;

		public double Maximum => GetImpl().Maximum;

		public double Value => GetImpl().Value;

		public double SmallChange => GetImpl().SmallChange;

		public double LargeChange => GetImpl().LargeChange;

		public bool IsReadOnly => false;

		public void SetValue(double value) => GetImpl().Value = value;

		internal void RaiseValueChangedEvent(double oldValue, double newValue)
		{
			if (ApiInformation.IsPropertyPresent("Windows.UI.Xaml.Automation.RangeValuePatternIdentifiers", nameof(RangeValuePatternIdentifiers.ValueProperty)))
			{
				RaisePropertyChangedEvent(RangeValuePatternIdentifiers.ValueProperty,
							   oldValue,
							   newValue);
			}
		}
	}
}
