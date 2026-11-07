// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference: dxaml\test\managed\enterprise\Moco\ListViewBaseTests.cs, commit bac7a9c33ff

#pragma warning disable CS0168 // Disable TestCleanupWrapper warnings
#pragma warning disable 168 // for cleanup imported member

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Common;
using MUXControlsTestApp.Utilities;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Tests.Common;
using Private.Infrastructure;
using Uno.UI.RuntimeTests;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using ListView = Microsoft.UI.Xaml.Controls.ListView;
using UIExecutor = MUXControlsTestApp.Utilities.RunOnUIThread;

namespace Microsoft.UI.Xaml.Tests.Enterprise;

public partial class ListViewBaseTests
{
	public partial class MyTemplateSelector : DataTemplateSelector
	{
		private List<DataTemplate> _templates= new List<DataTemplate>();
		public List<DataTemplate> Templates
		{
			get
			{
				return _templates;
			}
		}

		public Func<object, int> SelectTemplateFunc;

		protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
		{
			return Templates[SelectTemplateFunc(item)];
		}

		protected override DataTemplate SelectTemplateCore(object item)
		{
			return Templates[SelectTemplateFunc(item)];
		}
	}

	public class Group : ObservableCollection<string>
	{
		public string Name { get; }
		public Group(string name)
		{
			Name = name;
		}
		public Group(string name, IEnumerable<string> items)
			: base(items)
		{
			Name = name;
		}
	}

	public partial class MyListViewItem : ListViewItem
	{
		public double ExpectedWidth { get; set; }

		public bool IsParentItemsWrapGrid { get; set; }

		protected override Size MeasureOverride(Size availableSize)
		{
			Log.Comment("Item Available Size:" + availableSize);
			if (!IsParentItemsWrapGrid || (IsParentItemsWrapGrid && firstMeasure))
			{
				Verify.AreEqual(ExpectedWidth, availableSize.Width);
				firstMeasure = false;
			}

			return base.MeasureOverride(availableSize);
		}

		private static bool firstMeasure = true;
	}

	public partial class MyListViewHeaderItem : ListViewHeaderItem
	{
		public double ExpectedWidth { get; set; }

		protected override Size MeasureOverride(Size availableSize)
		{
			Log.Comment("Header Available Size:" + availableSize);
			Verify.AreEqual(ExpectedWidth, availableSize.Width);
			return base.MeasureOverride(availableSize);
		}
	}

	private async Task ValidateFocusedGridViewItemContent(string content)
	{
		await TestServices.WindowHelper.WaitForIdle();
		UIExecutor.Execute(() =>
		{
			var focused = FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot) as GridViewItem;
			Verify.IsNotNull(focused);
			Verify.AreEqual(content, focused.Content.ToString());
		});
	}

	private VisualStateGroup GetCommonStates(ListViewItem item)
	{
		FrameworkElement itemTemplate = (FrameworkElement)VisualTreeHelper.GetChild(item, 0);
		Log.Comment("Item template root: {0}", itemTemplate.Name);

		var groups = VisualStateManager.GetVisualStateGroups(itemTemplate);
		Verify.IsNotNull(groups, "Cannot find visual state groups!!!");
		Verify.IsGreaterThan(groups.Count, 0, "Groups count was not greater than 0!!!");

		VisualStateGroup commonStates = null;
		foreach (VisualStateGroup g in groups)
		{
			Log.Comment("Group Name: {0}", g.Name);
			if (g.Name.Equals("CommonStates", StringComparison.OrdinalIgnoreCase))
			{
				Log.Comment("Found CommonStates.");
				commonStates = g;
				break;
			}
		}

		Verify.IsNotNull(commonStates, "Cannot find CommonStates group!!!");

		return commonStates;
	}

	private int GetFocusedItemIndex(Microsoft.UI.Xaml.Controls.ListViewBase listViewBase)
	{
		int focusedIndex = -1;
		UIExecutor.Execute(() =>
		{
			var focused = (DependencyObject)FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot);
			Verify.IsNotNull(focused);
			focusedIndex = listViewBase.IndexFromContainer(focused);
		});

		return focusedIndex;
	}

	private static ObservableCollection<ObservableCollection<int>> GetGroupedData(int numGroups, int itemsPerGroup)
	{
		ObservableCollection<ObservableCollection<int>> groups = new();
		for (int i = 0; i < numGroups; i++)
		{
			ObservableCollection<int> group = new();
			for (int j = 0; j < itemsPerGroup; j++)
			{
				group.Add(i * 1000 + j);
			}

			groups.Add(group);
		}

		return groups;
	}

	class TemplateComponentLogger
	{
		public List<Tuple<int, int>> ProcessBindingCalls { get; set; } = new List<Tuple<int, int>>();
		public List<int> RecycleCalls { get; set; } = new List<int>();
	}

	partial class ElementTemplateComponent : IDataTemplateComponent
	{
		private int _numPhases;
		private int _data;
		private TemplateComponentLogger _logger;

		public ElementTemplateComponent(int numPhases, TemplateComponentLogger logger)
		{
			_numPhases = numPhases;
			_logger = logger;
		}

		public void ProcessBindings(object item, int itemIndex, int phase, out int nextPhase)
		{
			_data = (int)item;
			_logger.ProcessBindingCalls.Add(new Tuple<int, int>(_data, phase));
			nextPhase = phase >= _numPhases - 1 ? -1 : phase + 1;
			Log.Comment(string.Format("Index:{0}  Phase:{1}", item.ToString(), phase));
		}

		public void Recycle()
		{
			_logger.RecycleCalls.Add(_data);
			Log.Comment(string.Format("Recycle Index:{0}", _data));
		}

	}
}
