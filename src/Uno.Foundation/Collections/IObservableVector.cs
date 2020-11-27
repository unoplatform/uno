namespace Windows.Foundation.Collections
{
	internal interface IObservableVector
	{
		/// <summary>
		/// Occurs when the vector changes.
		/// </summary>
		event VectorChangedEventHandler UntypedVectorChanged;

		public object this[int index] { get; }

		public int Count { get; }

		public int IndexOf(object item);

		public void Add(object item);

		public void Insert(int index, object item);

		public void RemoveAt(int index);
	}
}
