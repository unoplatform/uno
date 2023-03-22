#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Windows.Foundation.Collections;
using System.Runtime.InteropServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_Foundation.Collections
{
	[TestClass]
	internal class Given_PropertySet
	{

		PropertySet _testSet = new();
		static bool _wasEventFired;

		[TestInitialize]
		public void Init()
		{
			_testSet.MapChanged += _testSet_MapChanged;
		}

		private void _testSet_MapChanged([In] IObservableMap<string, object?> sender, [In] IMapChangedEventArgs<string> @event)
		{
			_wasEventFired = true;
		}

		[TestCleanup]
		public void Cleanup()
		{
			_testSet.MapChanged -= _testSet_MapChanged;
			_testSet.Clear();
		}

		[TestMethod]
		public void When_ModificationEvent()
		{

			// .Add
			_wasEventFired = false;
			_testSet.Add("thisisKey", "andValue");

			if (!_wasEventFired)
			{
				Assert.Fail("PropertySet.Add didn't fire event");
			}

			// .Contains
			if (!_testSet.ContainsKey("thisisKey"))
			{
				Assert.Fail("PropertySet doesn't contain .Added key");
			}

			// .TryGet
			object? retObject;
			if (!_testSet.TryGetValue("thisisKey", out retObject))
			{
				Assert.Fail("PropertySet.TryGetValue reports non existent key (but PropertySet.Contains reported that key exists)");
			}
			if (retObject is null)
			{
				Assert.Fail("PropertySet.TryGetValue returns null");
			}

			if ((string)retObject != "andValue")
			{
				Assert.Fail("PropertySet.TryGetValue returns something, but not value we placed into it");
			}

			// .Remove
			_wasEventFired = false;
			_testSet.Remove("thisisKey");

			if (!_wasEventFired)
			{
				Assert.Fail("PropertySet.Remove didn't fire event");
			}
			if (_testSet.ContainsKey("thisisKey"))
			{
				Assert.Fail("PropertySet.Remove didn't remove item");
			}

			// .Count
			_testSet.Add("thisisKey", "andValue");
			_testSet.Add("thisisKey1", "andValue2");
			if (_testSet.Count != 2)
			{
				Assert.Fail("PropertySet.Count seems not correct");
			}

			// .Clear
			_wasEventFired = false;
			_testSet.Clear();

			if (!_wasEventFired)
			{
				Assert.Fail("PropertySet.Clear didn't fire event");
			}

			// .Count, again
			if (_testSet.Count > 0)
			{
				Assert.Fail("PropertySet.Clear didn't remove all items");
			}
		}
	}
}
