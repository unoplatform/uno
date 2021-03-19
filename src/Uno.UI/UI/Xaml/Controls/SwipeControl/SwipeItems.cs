// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.


namespace Windows.UI.Xaml.Controls
{
	public partial class SwipeItems
	{
		public void SwipeItems()
		{
			// create the Collection
			var collection = new Vector<SwipeItem, MakeVectorParam<VectorFlag.DependencyObjectBase>() > ();

			put_Items(collection);
		}

		void OnPropertyChanged(DependencyPropertyChangedEventArgs& args)
		{
			if (args.Property() == s_ModeProperty)
			{
				if (unbox_value<SwipeMode>(args.NewValue()) == SwipeMode.Execute && m_items.get().Size() > 1)
				{
					throw hresult_invalid_argument("Execute items should only have one item.");
				}
			}
		}

		void SwipeItems.put_Items(
			Collections.IVector<SwipeItem>& value)
		{
			if (Mode() == SwipeMode.Execute && value.Size() > 1)
			{
				throw hresult_invalid_argument("Execute items should only have one item.");
			}

			m_items.set(value);
			m_vectorChangedEventSource(this, null);
		}

		SwipeItem GetAt(uint index)
		{
			if (index >= m_items.get().Size())
			{
				throw hresult_out_of_bounds();
			}

			return m_items.get().GetAt(index);
		}

		uint Size()
		{
			return m_items.get().Size();
		}

		bool IndexOf(SwipeItem & value, uint32_t& index)
		{
			if (index >= m_items.get().Size())
			{
				throw hresult_out_of_bounds();
			}

			return m_items.get().IndexOf(value, index);
		}

		void SetAt(uint index, SwipeItem & value)
		{
			if (index >= m_items.get().Size())
			{
				throw hresult_out_of_bounds();
			}

			m_items.get().SetAt(index, value);
			m_vectorChangedEventSource(this, null);
		}

		void InsertAt(uint index, SwipeItem & value)
		{
			if (Mode() == SwipeMode.Execute && m_items.get().Size() > 0)
			{
				throw hresult_invalid_argument("Execute items should only have one item.");
			}

			if (index > m_items.get().Size())
			{
				throw hresult_out_of_bounds();
			}

			m_items.get().InsertAt(index, value);
			m_vectorChangedEventSource(this, null);
		}

		void RemoveAt(uint index)
		{
			if (index >= m_items.get().Size())
			{
				throw hresult_out_of_bounds();
			}

			m_items.get().RemoveAt(index);
			m_vectorChangedEventSource(this, null);
		}

		void Append(SwipeItem & value)
		{
			if (Mode() == SwipeMode.Execute && m_items.get().Size() > 0)
			{
				throw hresult_invalid_argument("Execute items should only have one item.");
			}

			m_items.get().Append(value);
			m_vectorChangedEventSource(this, null);
		}

		void RemoveAtEnd()
		{
			m_items.get().RemoveAtEnd();
			m_vectorChangedEventSource(this, null);
		}

		void Clear()
		{
			m_items.get().Clear();
			m_vectorChangedEventSource(this, null);
		}

		IVectorView<SwipeItem> GetView()
		{
			return m_items.get().GetView();
		}

		event_token VectorChanged(VectorChangedEventHandler<SwipeItem> & handler)
		{
			return m_vectorChangedEventSource.add(handler);
		}

		void VectorChanged(event_token token)
		{
			m_vectorChangedEventSource.remove(token);
		}
	}
}
