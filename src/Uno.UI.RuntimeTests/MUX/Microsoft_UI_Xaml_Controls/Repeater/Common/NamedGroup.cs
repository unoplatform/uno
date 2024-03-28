// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Windows.UI.Xaml.Tests.MUXControls.ApiTests.RepeaterTests.Common
{
	public class NamedGroup<T> : ObservableCollection<T>
	{
		public string Name { get; private set; }

		public NamedGroup(string name, IEnumerable<T> collection)
			: base(collection)
		{
			Name = name;
		}
	}
}
