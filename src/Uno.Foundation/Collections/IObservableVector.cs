namespace Windows.Foundation.Collections
{
	internal interface IObservableVector
    {
		/// <summary>
        /// Occurs when the vector changes.
        /// </summary>
        event VectorChangedEventHandler UntypedVectorChanged;
    }
}
