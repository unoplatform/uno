// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Windows.UI.Xaml.Controls;
using Common;

#if USING_TAEF
using WEX.TestExecution;
using WEX.TestExecution.Markup;
using WEX.Logging.Interop;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
#endif

namespace Windows.UI.Xaml.Tests.MUXControls.ApiTests.RepeaterTests.Common.Mocks
{
	using ItemsSourceView = Microsoft/* UWP don't rename */.UI.Xaml.Controls.ItemsSourceView;
	using IKeyIndexMapping = Microsoft/* UWP don't rename */.UI.Xaml.Controls.IKeyIndexMapping;

	public class MockItemsSource : CustomItemsSourceView
	{
		private List<GetAtCallInfo> _recordedGetAtCalls = new List<GetAtCallInfo>();
		protected List<KeyFromIndexCallInfo> _recordedKeyFromIndexCalls = new List<KeyFromIndexCallInfo>();
		private int _getSizeCallsCount;

		public Func<int> GetSizeFunc { get; set; }
		public Func<int, object> GetAtFunc { get; set; }
		public Func<bool> HasKeyIndexMappingFunc { get; set; }
		public Func<int, string> GetItemIdFunc { get; set; }

		public static MockItemsSource CreateDataSource<T>(ObservableCollection<T> data, bool supportsUniqueIds)
		{
			var mock = new MockItemsSource
			{
				GetSizeFunc = () => data.Count,
				GetAtFunc = (index) => data[index],
				HasKeyIndexMappingFunc = () => supportsUniqueIds,
				GetItemIdFunc = (index) =>
				{
					if (supportsUniqueIds)
					{
						return data[index].ToString();
					}
					else
					{
						throw new InvalidOperationException();
					}
				}
			};

			data.CollectionChanged += (s, e) => mock.OnItemsSourceChanged(e);

			return mock;
		}

		public static MockItemsSource CreateDataSource(WinRTCollection data, bool supportsUniqueIds)
		{
			MockItemsSource mock = null;
			if (supportsUniqueIds)
			{
				mock = new MockItemsSourceWithUniqueIdMapping
				{
					GetSizeFunc = () => data.Count,
					GetAtFunc = (index) => data[index],
					GetItemIdFunc = (index) =>
					{
						if (supportsUniqueIds)
						{
							return data[index].ToString();
						}
						else
						{
							throw new InvalidOperationException();
						}
					}
				};
			}
			else
			{
				mock = new MockItemsSource
				{
					GetSizeFunc = () => data.Count,
					GetAtFunc = (index) => data[index],
					GetItemIdFunc = (index) =>
					{
						if (supportsUniqueIds)
						{
							return data[index].ToString();
						}
						else
						{
							throw new InvalidOperationException();
						}
					}
				};

			}

			data.VectorChanged += (s, e) => mock.OnItemsSourceChanged(e.ConvertToDataSourceChangedEventArgs());

			return mock;
		}

		public void ValidateGetSizeCalls(int expectedCallCount)
		{
			Log.Comment("Validating GetSize calls");
			Verify.AreEqual(expectedCallCount, _getSizeCallsCount);
			_getSizeCallsCount = 0;
		}

		public void ValidateGetAtCalls(params GetAtCallInfo[] expected)
		{
			Log.Comment("Validating GetAt calls");
			Verify.AreEqual(expected.Length, _recordedGetAtCalls.Count);
			for (int i = 0; i < expected.Length; ++i)
			{
				Verify.AreEqual(expected[i].Index, _recordedGetAtCalls[i].Index);
			}

			_recordedGetAtCalls.Clear();
		}

		public void ValidateGetItemIdCalls(params KeyFromIndexCallInfo[] expected)
		{
			Log.Comment("Validating GetItemId calls");
			Verify.AreEqual(expected.Length, _recordedKeyFromIndexCalls.Count);
			for (int i = 0; i < expected.Length; ++i)
			{
				Verify.AreEqual(expected[i].Index, _recordedKeyFromIndexCalls[i].Index);
			}

			_recordedKeyFromIndexCalls.Clear();
		}

		public new void OnItemsSourceChanged(NotifyCollectionChangedEventArgs args)
		{
			base.OnItemsSourceChanged(args);
		}

		protected override int GetSizeCore()
		{
			++_getSizeCallsCount;
			return GetSizeFunc != null ? GetSizeFunc() : default(int);
		}

		protected override object GetAtCore(int index)
		{
			_recordedGetAtCalls.Add(new GetAtCallInfo(index));
			return GetAtFunc != null ? GetAtFunc(index) : null;
		}

		public class GetAtCallInfo
		{
			public int Index { get; private set; }

			public GetAtCallInfo(int index)
			{
				Index = index;
			}
		}

		public class KeyFromIndexCallInfo
		{
			public int Index { get; private set; }

			public KeyFromIndexCallInfo(int index)
			{
				Index = index;
			}
		}
	}

	public class MockItemsSourceWithUniqueIdMapping : MockItemsSource, IKeyIndexMapping
	{
		public string KeyFromIndex(int index)
		{
			_recordedKeyFromIndexCalls.Add(new KeyFromIndexCallInfo(index));
			return GetItemIdFunc != null ? GetItemIdFunc(index) : null;
		}

		public int IndexFromKey(string id)
		{
			throw new NotImplementedException();
		}
	}
}
