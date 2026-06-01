using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml;
using Windows.Foundation.Collections;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Data;

[TestClass]
[RunsOnUIThread]
public class Given_CollectionView
{
	[TestMethod]
	public void When_Grouped_With_ItemsPath()
	{
		var data = new List<GroupItemDto>
		{
			new("1", "Test 1|1", "Group 1"),
			new("2", "Test 2|1", "Group 1"),
			new("3", "Test 3|1", "Group 1"),
			new("4", "Test 1|2", "Group 2"),
			new("5", "Test 2|2", "Group 2"),
			new("6", "Test 3|2", "Group 2"),
			new("7", "Test 1|3", "Group 3"),
			new("8", "Test 2|3", "Group 3"),
			new("9", "Test 3|3", "Group 3"),
		};

		var groupedItems = from item in data
						   group item by item.GroupName into g
						   select new { GroupName = g.Key, Items = g.ToList() };

		var cvs = new CollectionViewSource();
		cvs.IsSourceGrouped = true;
		cvs.ItemsPath = new PropertyPath("Items");
		cvs.Source = groupedItems.ToList();

		var type = GetItemType(cvs.View);
		Assert.AreEqual(typeof(GroupItemDto), type);
	}

	[TestMethod]
	public void When_Grouped_Get_Count()
	{
		var data = new List<GroupItemDto>
		{
			new("1", "Test 1|1", "Group 1"),
			new("2", "Test 2|1", "Group 1"),
			new("3", "Test 3|1", "Group 1"),
			new("4", "Test 1|2", "Group 2"),
			new("5", "Test 2|2", "Group 2"),
			new("6", "Test 3|2", "Group 2"),
			new("7", "Test 1|3", "Group 3"),
			new("8", "Test 2|3", "Group 3"),
			new("9", "Test 3|3", "Group 3"),
		};

		var groupedItems = from item in data
						   group item by item.GroupName into g
						   select new { GroupName = g.Key, Items = g.ToList() };

		var cvs = new CollectionViewSource();
		cvs.IsSourceGrouped = true;
		cvs.ItemsPath = new PropertyPath("Items");
		cvs.Source = groupedItems.ToList();

		var count = GetCount(cvs.View);
		Assert.AreEqual(9, count);
	}

	private int GetCount(ICollectionView view)
	{
		int num = 0;
		IEnumerable dataSource = view;
		if (dataSource != null)
		{
			IEnumerator enumerator = dataSource.GetEnumerator();
			if (enumerator != null)
			{
				while (enumerator.MoveNext())
				{
					num++;
				}
			}
		}
		return num;
	}

	private Type GetItemType(IEnumerable list)
	{
		Type type = list.GetType();
		Type type2 = null;
		bool flag = false;
		if (type2 == null || type2 == typeof(object) || flag)
		{
			Type type3 = null;
			IEnumerator enumerator = list.GetEnumerator();
			type3 = ((!enumerator.MoveNext() || enumerator.Current == null) ?
				(from object x in list select x.GetType()).FirstOrDefault() : enumerator.Current.GetType());
			if (type3 != typeof(object))
			{
				return type3;
			}
		}
		if (flag)
		{
			return null;
		}
		return type2;
	}

	private record GroupItemDto(string Number, string Name, string GroupName);
}
