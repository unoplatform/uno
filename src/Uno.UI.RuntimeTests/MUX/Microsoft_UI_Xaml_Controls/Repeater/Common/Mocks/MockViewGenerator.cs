// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
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
	using ElementFactory = Microsoft/* UWP don't rename */.UI.Xaml.Controls.ElementFactory;
	using ElementFactoryGetArgs = Microsoft/* UWP don't rename */.UI.Xaml.Controls.ElementFactoryGetArgs;
	using ElementFactoryRecycleArgs = Microsoft/* UWP don't rename */.UI.Xaml.Controls.ElementFactoryRecycleArgs;
	using RepeaterTestHooks = Microsoft.UI.Private.Controls.RepeaterTestHooks;

	public class MockElementFactory : ElementFactory
	{
		private List<GetElementCallInfo> _recordedGetElementCalls = new List<GetElementCallInfo>();
		private List<RecycleElementCallInfo> _recordedRecycleElementCalls = new List<RecycleElementCallInfo>();

		public Func<int, UIElement, UIElement> GetElementFunc { get; set; }
		public Action<UIElement, UIElement> ClearElementFunc { get; set; }

		public static MockElementFactory CreateElementFactory<T>(List<T> mapping) where T : UIElement
		{
			return new MockElementFactory
			{
				GetElementFunc = (index, owner) => mapping[index]
			};
		}

		public static MockElementFactory CreateElementFactory<T>(
			Dictionary<int, T> mapping) where T : UIElement
		{
			return new MockElementFactory
			{
				GetElementFunc = (index, owner) => mapping[index]
			};
		}

		public void ValidateGetElementCalls(params GetElementCallInfo[] expected)
		{
			Log.Comment("Validating GetElement calls");
			Verify.AreEqual(expected.Length, _recordedGetElementCalls.Count);
			for (int i = 0; i < expected.Length; ++i)
			{
				Verify.AreEqual(expected[i].Index, _recordedGetElementCalls[i].Index);
				Verify.AreEqual(expected[i].Owner, _recordedGetElementCalls[i].Owner);
			}

			_recordedGetElementCalls.Clear();
		}

		public void ValidateRecycleElementCalls(params RecycleElementCallInfo[] expected)
		{
			Log.Comment("Validating RecycleElement calls");
			Verify.AreEqual(expected.Length, _recordedRecycleElementCalls.Count);
			for (int i = 0; i < expected.Length; ++i)
			{
				Verify.AreEqual(expected[i].Element, _recordedRecycleElementCalls[i].Element);
				Verify.AreEqual(expected[i].Owner, _recordedRecycleElementCalls[i].Owner);
			}

			_recordedRecycleElementCalls.Clear();
		}

		protected override UIElement GetElementCore(ElementFactoryGetArgs args)
		{
			int index = RepeaterTestHooks.GetElementFactoryElementIndex(args);
			_recordedGetElementCalls.Add(new GetElementCallInfo(index, args.Parent));
			return GetElementFunc != null ? GetElementFunc(index, args.Parent) : null;
		}

		protected override void RecycleElementCore(ElementFactoryRecycleArgs args)
		{
			_recordedRecycleElementCalls.Add(new RecycleElementCallInfo(args.Element, args.Parent));
			if (ClearElementFunc != null) { ClearElementFunc(args.Element, args.Parent); }
		}

		public class GetElementCallInfo
		{
			public int Index { get; private set; }
			public UIElement Owner { get; private set; }

			public GetElementCallInfo(int index, UIElement owner)
			{
				Index = index;
				Owner = owner;
			}
		}

		public class RecycleElementCallInfo
		{
			public UIElement Element { get; private set; }
			public UIElement Owner { get; private set; }

			public RecycleElementCallInfo(UIElement element, UIElement owner)
			{
				Element = element;
				Owner = owner;
			}
		}
	}
}
