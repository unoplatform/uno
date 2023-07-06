namespace Windows.UI.Xaml.Data;

/// <summary>
/// Provides info about a range of items in the data source.
/// </summary>
public partial class ItemIndexRange
{
	/// <summary>
	/// Initializes an instance of the ItemIndexRange class.
	/// </summary>
	/// <param name="firstIndex">The index of the first item in the instance of the ItemIndexRange class.</param>
	/// <param name="length">The number of items in the instance of the ItemIndexRange class.</param>
	public ItemIndexRange(int firstIndex, uint length)
	{
		FirstIndex = firstIndex;
		Length = length;
	}

	/// <summary>
	/// Gets the index of the first item in the instance of the ItemIndexRange class.
	/// </summary>
	public int FirstIndex { get; }

	/// <summary>
	/// Gets the index of the last item in the instance of the ItemIndexRange class.
	/// </summary>
	public int LastIndex => GetLastIndexImpl();

	/// <summary>
	/// Gets the number of items in the instance of the ItemIndexRange class.
	/// </summary>
	public uint Length { get; }

#if HAS_UNO // Equality is only implemented in Uno Platform, not in WinUI and UWP.
	public override bool Equals(object obj)
	{
		return obj is ItemIndexRange other &&
			other.FirstIndex == FirstIndex &&
			other.Length == Length;
	}

	public override int GetHashCode() => (FirstIndex, Length).GetHashCode();
#endif
}
