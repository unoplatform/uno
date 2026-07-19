namespace Windows.Foundation;

public enum PropertyType
{
	Empty,
	/// <summary>A byte.</summary>
	UInt8,
	/// <summary>A signed 16-bit (2-byte) integer.</summary>
	Int16,
	/// <summary>An unsigned 16-bit (2-byte) integer.</summary>
	UInt16,
	/// <summary>A signed 32-bit (4-byte) integer.</summary>
	Int32,
	/// <summary>An unsigned 32-bit (4-byte) integer.</summary>
	UInt32,
	/// <summary>A signed 64-bit (8-byte) integer.</summary>
	Int64,
	/// <summary>An unsigned 64-bit (8-byte) integer.</summary>
	UInt64,
	/// <summary>A signed 32-bit (4-byte) floating-point number.</summary>
	Single,
	Double,
	Char16,
	Boolean,
	/// <summary>A Windows Runtime HSTRING.</summary>
	String,
	/// <summary>An object implementing the IInspectable interface.</summary>
	Inspectable,
	DateTime,
	/// <summary>A time interval.</summary>
	TimeSpan,
	/// <summary>A globally unique identifier.</summary>
	Guid,
	/// <summary>An ordered pair of floating-point x- and y-coordinates that defines a point in a two-dimensional plane.</summary>
	Point,
	/// <summary>An ordered pair of float-point numbers that specify a height and width.</summary>
	Size,
	/// <summary>A set of four floating-point numbers that represent the location and size of a rectangle.</summary>
	Rect,
	/// <summary>A type not specified in this enumeration.</summary>
	OtherType,
	UInt8Array = 1025,
	Int16Array,
	UInt16Array,
	Int32Array,
	UInt32Array,
	Int64Array,
	UInt64Array,
	SingleArray,
	/// <summary>An array of Double values.</summary>
	DoubleArray,
	/// <summary>An array of Char values.</summary>
	Char16Array,
	/// <summary>An array of Boolean values.</summary>
	BooleanArray,
	StringArray,
	InspectableArray,
	/// <summary>An array of DateTime values.</summary>
	DateTimeArray,
	TimeSpanArray,
	GuidArray,
	PointArray,
	SizeArray,
	RectArray,
	OtherTypeArray
}
