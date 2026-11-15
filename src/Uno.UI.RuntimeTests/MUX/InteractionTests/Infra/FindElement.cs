// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference controls/test/testinfra/MUXTestInfra/Common/FindElement.cs, commit fee1e8acc

using Microsoft.UI.Xaml.Tests.MUXControls.InteractionTests.Infra;
using System;
using System.Collections.Generic;
using System.Text;
using Common;

using Microsoft.Windows.Apps.Test.Automation;
using Microsoft.Windows.Apps.Test.Foundation;
using Microsoft.Windows.Apps.Test.Foundation.Controls;
using Microsoft.Windows.Apps.Test.Foundation.Patterns;
using Microsoft.Windows.Apps.Test.Foundation.Waiters;

namespace Microsoft.UI.Xaml.Tests.MUXControls.InteractionTests.Common
{
	public class FindElement
	{
		public static UIObject ById(string id)
		{
			return FindCore.ById(id, shouldWait: true);
		}

		public static UIObject ByName(string name)
		{
			return FindCore.ByName(name, shouldWait: true);
		}

		public static UIObject ByNameOrId(string nameOrId)
		{
			return FindCore.ByNameOrId(nameOrId, shouldWait: true);
		}

		public static UIObject ByClassName(string className)
		{
			return FindCore.ByClassName(className, shouldWait: true);
		}

		public static UIObject ByNameAndClassName(string name, string className)
		{
			return FindCore.ByNameAndClassName(name, className, shouldWait: true);
		}

		public static T ById<T>(string id)
		{
			UIObject obj = ById(id);

			if (obj != null)
			{
				return (T)Activator.CreateInstance(typeof(T), obj);
			}
			else
			{
				return default(T);
			}
		}

		public static T ByName<T>(string name)
		{
			UIObject obj = ByName(name);

			if (obj != null)
			{
				return (T)Activator.CreateInstance(typeof(T), obj);
			}
			else
			{
				return default(T);
			}
		}

		public static T ByNameOrId<T>(string name)
		{
			UIObject obj = ByNameOrId(name);

			if (obj != null)
			{
				return (T)Activator.CreateInstance(typeof(T), obj);
			}
			else
			{
				return default(T);
			}
		}

		public static UIObject GetDescendantByName(UIObject parent, string name)
		{
			if (parent == null)
			{
				throw new ArgumentNullException("parent");
			}

			if (string.IsNullOrWhiteSpace(name))
			{
				throw name == null ? new ArgumentNullException("name") : new ArgumentException("name");
			}

			foreach (UIObject obj in parent.Descendants)
			{
				if (obj.Name == name)
				{
					return obj;
				}
			}

			Log.Comment("GetDescendantByName - No matching descendant found for '" + name + "'");
			return null;
		}
	}

	public enum FindBy { Id, Name }

	public class VerifyElement
	{
		public static void Found(string idOrName, FindBy findBy)
		{
			UIObject ui = findBy == FindBy.Name ? TryFindElement.ByName(idOrName) : TryFindElement.ById(idOrName);
			Verify.IsNotNull(ui, "Expected to find element with " + findBy.ToString() + " '" + idOrName + "'");
		}
		public static void NotFound(string idOrName, FindBy findBy)
		{
			ElementCache.Clear();
			UIObject ui = findBy == FindBy.Name ? TryFindElement.ByName(idOrName) : TryFindElement.ById(idOrName);
			Verify.IsNull(ui, "Expected not to be able to find element with " + findBy.ToString() + " '" + idOrName + "'");
		}
	}

	public class TryFindElement
	{
		public static UIObject ById(string id)
		{
			return FindCore.ById(id, shouldWait: false);
		}

		public static UIObject ByName(string name)
		{
			return FindCore.ByName(name, shouldWait: false);
		}

		public static UIObject ByNameOrId(string nameOrId)
		{
			return FindCore.ByNameOrId(nameOrId, shouldWait: false);
		}

		public static UIObject ByClassName(string className)
		{
			return FindCore.ByClassName(className, shouldWait: false);
		}

		public static UIObject ByNameAndClassName(string name, string className)
		{
			return FindCore.ByNameAndClassName(name, className, shouldWait: false);
		}

		public static T ById<T>(string id)
		{
			UIObject obj = ById(id);

			if (obj != null)
			{
				return (T)Activator.CreateInstance(typeof(T), obj);
			}
			else
			{
				return default(T);
			}
		}

		public static T ByName<T>(string name)
		{
			UIObject obj = ByName(name);

			if (obj != null)
			{
				return (T)Activator.CreateInstance(typeof(T), obj);
			}
			else
			{
				return default(T);
			}
		}

		public static T ByNameOrId<T>(string name)
		{
			UIObject obj = ByNameOrId(name);

			if (obj != null)
			{
				return (T)Activator.CreateInstance(typeof(T), obj);
			}
			else
			{
				return default(T);
			}
		}
	}

	public class FindCore
	{
		public static UIObject ById(string id, bool shouldWait)
		{
			return ElementCache.TryGetObjectById(id, shouldWait);
		}

		public static UIObject ByName(string name, bool shouldWait)
		{
			return ElementCache.TryGetObjectByName(name, shouldWait);
		}

		public static UIObject ByNameOrId(string nameOrId, bool shouldWait)
		{
			return ElementCache.TryGetObjectByNameOrId(nameOrId, shouldWait);
		}

		public static UIObject ByClassName(string className, bool shouldWait)
		{
			return ElementCache.TryGetObjectByClassName(className, shouldWait);
		}

		public static UIObject ByNameAndClassName(string name, string className, bool shouldWait)
		{
			UIObject root = TestEnvironment.Application.ApplicationFrameWindow ?? TestEnvironment.Application.CoreWindow;
			return ByNameAndClassName(root, name, className, shouldWait);
		}

		public static UIObject ByNameAndClassName(UIObject root, string name, string className, bool shouldWait)
		{
			UICondition condition = UICondition.CreateFromName(name).AndWith(UICondition.CreateFromClassName(className));
			UIObject uiObject = null;

			root.Descendants.TryFind(condition, out uiObject);

			if (shouldWait && !root.Descendants.TryFind(condition, out uiObject))
			{
				Log.Comment("Object name = '{0}' className = '{1}' didn't exist, waiting...", name, className);
				try
				{
					TestEnvironment.WaitUntilElementLoadedByName(name);
					TestEnvironment.WaitUntilElementLoadedByClassName(className);
				}
				catch (WaiterTimedOutException)
				{
					Log.Error("Could not find object with condition '{0}'!", condition.ToString());
					DumpHelper.DumpFullContext();

					throw;
				}

				root.Descendants.TryFind(condition, out uiObject);
				if (uiObject != null)
				{
					Log.Comment("...Found");
				}
				else
				{
					Log.Comment("...Not Found");
				}

			}

			return uiObject;
		}
	}

	//
	// This class stores in dictionaries a cached version of the interesting elements in the UIA tree,
	// those being elements with automation IDs, automation names, or automation class names.
	// We want to store these in a cache because the UIA tree isn't going to be changing very much
	// during a single test, so instead of traversing the UIA tree every single time we need to find
	// a UI object, we instead traverse the UIA tree once, store everything we find, and then retrieve
	// it from a dictionary for very rapid access.
	//
	// That does, however, mean that this can be storing a stale view of the UIA tree.
	// If a test has modified the UIA tree in a way that would cause a false positive - e.g. replacing
	// one UI object with another with the same ID or name - you should call ElementCache.Clear()
	// in order to let us know to flush the cache and recreate it.
	//
	// You should not need to call ElementCache.Clear() for false negatives, though - if we search for
	// an element that isn't found, we automatically will assume that our view is stale and will
	// refresh the cache in that circumstance.
	//
	public class ElementCache
	{
		private static Dictionary<string, UIObject> objectFromIdCache = new Dictionary<string, UIObject>();
		private static Dictionary<string, UIObject> objectFromNameCache = new Dictionary<string, UIObject>();
		private static Dictionary<string, UIObject> objectFromClassNameCache = new Dictionary<string, UIObject>();

		public static UIObject TryGetObjectById(string id, bool shouldWait)
		{
			return TryGetObject(id, objectFromIdCache, shouldWait ? new Action<string>(TestEnvironment.WaitUntilElementLoadedById) : null);
		}

		public static UIObject TryGetObjectByName(string name, bool shouldWait)
		{
			return TryGetObject(name, objectFromNameCache, shouldWait ? new Action<string>(TestEnvironment.WaitUntilElementLoadedByName) : null);
		}

		public static UIObject TryGetObjectByNameOrId(string nameOrId, bool shouldWait)
		{
			return TryGetObject(nameOrId, new List<Dictionary<string, UIObject>> { objectFromNameCache, objectFromIdCache }, shouldWait ? new Action<string>(TestEnvironment.WaitUntilElementLoadedByNameOrId) : null);
		}

		public static UIObject TryGetObjectByClassName(string className, bool shouldWait)
		{
			return TryGetObject(className, objectFromClassNameCache, shouldWait ? new Action<string>(TestEnvironment.WaitUntilElementLoadedByClassName) : null);
		}

		private static UIObject TryGetObject(string key, Dictionary<string, UIObject> dictionary, Action<string> waitAction = null)
		{
			return TryGetObject(key, new List<Dictionary<string, UIObject>> { dictionary }, waitAction);
		}

		private static UIObject TryGetObject(string key, List<Dictionary<string, UIObject>> dictionaries, Action<string> waitAction = null)
		{
			UIObject obj = null;

			Func<bool> tryGetObjectFromDictionaries = () =>
			{
				foreach (Dictionary<string, UIObject> dictionary in dictionaries)
				{
					if (dictionary.TryGetValue(key, out obj))
					{
						return true;
					}
				}

				return false;
			};

			if (!tryGetObjectFromDictionaries())
			{
				Refresh();

				if (!tryGetObjectFromDictionaries() && waitAction != null)
				{
					try
					{
						waitAction(key);
					}
					catch (WaiterTimedOutException)
					{
						Log.Error("Could not find '{0}'!", key);
						DumpHelper.DumpFullContext();

						throw;
					}

					tryGetObjectFromDictionaries();
				}
			}

			return obj;
		}

		public static void Clear()
		{
			objectFromIdCache.Clear();
			objectFromNameCache.Clear();
			objectFromClassNameCache.Clear();
		}

		public static void Refresh()
		{
			Clear();
			// Uno: lookups are synchronous on the UI thread, so flush layout instead of waiting for idle.
			Wait.ForLayout();

			UIObject window = TestEnvironment.Application.ApplicationFrameWindow ?? TestEnvironment.Application.CoreWindow;
			Dictionary<UIObject, int> objectToTimesSeenDictionary = new Dictionary<UIObject, int>();
			foreach (UIObject obj in window.Descendants)
			{
				// Check if we encounter the same element twice in a row indicating a loop in the UIObjects.
				if (!objectToTimesSeenDictionary.TryGetValue(obj, out int dupeCount))
				{
					dupeCount = 0;
					objectToTimesSeenDictionary[obj] = 0;
				}

				dupeCount++;
				objectToTimesSeenDictionary[obj] = dupeCount;

				// Only break if an item was encountered more then 10 times.
				if (dupeCount >= 10)
				{
					break;
				}

				if (!string.IsNullOrWhiteSpace(obj.AutomationId) && !objectFromIdCache.ContainsKey(obj.AutomationId))
				{
					objectFromIdCache.Add(obj.AutomationId, obj);
				}

				if (!string.IsNullOrWhiteSpace(obj.Name) && !objectFromNameCache.ContainsKey(obj.Name))
				{
					objectFromNameCache.Add(obj.Name, obj);
				}

				if (!string.IsNullOrWhiteSpace(obj.ClassName) && !objectFromClassNameCache.ContainsKey(obj.ClassName))
				{
					objectFromClassNameCache.Add(obj.ClassName, obj);
				}
			}
		}

		public static void Dump()
		{
			Log.Comment("========== Element Cache ==========");
			Log.Comment("IDs:");
			DumpDictionary(objectFromIdCache);
			Log.Comment("Names:");
			DumpDictionary(objectFromNameCache);
			Log.Comment("Class names:");
			DumpDictionary(objectFromClassNameCache);
			Log.Comment("===================================");
		}

		private static void DumpDictionary(Dictionary<string, UIObject> dictionary)
		{
			if (dictionary.Keys.Count > 0)
			{
				foreach (string key in dictionary.Keys)
				{
					Log.Comment("    '{0}' => '{1}'", key, dictionary[key].ToString());
				}
			}
			else
			{
				Log.Comment("    (none)");
			}
		}
	}
}
