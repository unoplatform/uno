// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;


namespace Windows.UI.Xaml.Tests.MUXControls.ApiTests.RepeaterTests.Common
{
	using RecyclingElementFactory = Microsoft/* UWP don't rename */.UI.Xaml.Controls.RecyclingElementFactory;
	using RepeaterTestHooks = Microsoft.UI.Private.Controls.RepeaterTestHooks;
	using ElementFactoryGetArgs = Microsoft/* UWP don't rename */.UI.Xaml.Controls.ElementFactoryGetArgs;
	using ElementFactoryRecycleArgs = Microsoft/* UWP don't rename */.UI.Xaml.Controls.ElementFactoryRecycleArgs;

	public class RecyclingElementFactoryDerived : RecyclingElementFactory
	{
		private List<UIElement> _realizedElements = new List<UIElement>();
		private List<int> _realizedElementIndices = new List<int>();

		public Func<int, UIElement, UIElement, UIElement> GetElementFunc { get; set; }

		public Action<UIElement, UIElement> ClearElementFunc { get; set; }

		public Func<object, UIElement, string> SelectTemplateIdFunc { get; set; }

		public List<int> RealizedElementIndices { get { return _realizedElementIndices; } }

		public bool ValidateElementIndices { get; set; }

		public RecyclingElementFactoryDerived()
		{
			ValidateElementIndices = true;
		}

		protected override UIElement GetElementCore(ElementFactoryGetArgs context)
		{
			UIElement element = base.GetElementCore(context);
			int index = RepeaterTestHooks.GetElementFactoryElementIndex(context);
			if (GetElementFunc != null)
			{
				element = GetElementFunc(index, context.Parent, element);
			}

			if (!RealizedElementIndices.Contains(index))
			{
				_realizedElements.Add(element);
				_realizedElementIndices.Add(index);
			}
			else if (ValidateElementIndices)
			{
				throw new InvalidOperationException("Cannot request an element that has already been realized.");
			}

			return element;
		}

		protected override void RecycleElementCore(ElementFactoryRecycleArgs context)
		{
			base.RecycleElementCore(context);
			if (ClearElementFunc != null) { ClearElementFunc(context.Element, context.Parent); }

			int elementIndex = _realizedElements.IndexOf(context.Element);
			if (elementIndex != -1)
			{
				_realizedElements.RemoveAt(elementIndex);
				_realizedElementIndices.RemoveAt(elementIndex);
			}
			else if (ValidateElementIndices)
			{
				throw new InvalidOperationException("Cannot clear an element that has not been created or already been cleared.");
			}
		}

		protected override string OnSelectTemplateKeyCore(object data, UIElement owner)
		{
			if (SelectTemplateIdFunc != null)
			{
				return SelectTemplateIdFunc(data, owner);
			}
			else
			{
				return base.OnSelectTemplateKeyCore(data, owner);
			}
		}
	}
}
