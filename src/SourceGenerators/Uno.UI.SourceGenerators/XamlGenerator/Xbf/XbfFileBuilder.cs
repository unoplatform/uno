#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Uno.Extensions;
using Uno.UI.Xaml.Xbf;

namespace Uno.UI.SourceGenerators.XamlGenerator.Xbf;

/// <summary>
/// Raised when a XAML construct cannot be expressed in the XBF node stream yet, so that
/// the generator falls back to C# for the object being built.
/// </summary>
internal sealed class XbfNotSupportedException : Exception
{
	public XbfNotSupportedException(string reason) : base(reason)
	{
		Reason = reason;
	}

	public string Reason { get; }
}

/// <summary>
/// Writes the nodes of an XBF node stream.
/// </summary>
/// <remarks>
/// Port of the encoding of XamlBinaryFormatSubWriter2 (PersistNode / PersistConstant /
/// Persist7BitEncodedInt), with the Uno deviations: types, properties and strings are all
/// referenced through 7-bit encoded indexes rather than PersistedXamlNode2.
/// </remarks>
internal sealed class XbfNodeWriter
{
	private readonly MemoryStream _stream = new();

	public int Length => (int)_stream.Length;

	public void WriteNodeType(ObjectWriterNodeType nodeType) => _stream.WriteByte((byte)nodeType);

	public void WriteByte(byte value) => _stream.WriteByte(value);

	public void WriteBool(bool value) => _stream.WriteByte(value ? (byte)1 : (byte)0);

	// Persist7BitEncodedInt
	public void WriteUInt(int value)
	{
		var remaining = (uint)value;
		while (remaining >= 0x80)
		{
			_stream.WriteByte((byte)(remaining | 0x80));
			remaining >>= 7;
		}

		_stream.WriteByte((byte)remaining);
	}

	public void WriteInt32(int value)
	{
		_stream.WriteByte((byte)value);
		_stream.WriteByte((byte)(value >> 8));
		_stream.WriteByte((byte)(value >> 16));
		_stream.WriteByte((byte)(value >> 24));
	}

	public void WriteInt64(long value)
	{
		WriteInt32((int)value);
		WriteInt32((int)(value >> 32));
	}

	public void WriteSingle(float value) => WriteBytes(BitConverter.GetBytes(value));

	public void WriteDouble(double value) => WriteInt64(BitConverter.DoubleToInt64Bits(value));

	public void WriteBytes(byte[] bytes) => _stream.Write(bytes, 0, bytes.Length);

	/// <summary>
	/// Writes nodes the reader skips over: their byte length, then the nodes.
	/// </summary>
	public void WriteBlock(XbfNodeWriter content)
	{
		WriteUInt(content.Length);
		content._stream.WriteTo(_stream);
	}

	public void WriteTo(Stream target) => _stream.WriteTo(target);
}

/// <summary>
/// Collects the XBF of a XAML file: the node streams of its objects, its string table and the
/// types, properties and constants the generated metadata provider resolves.
/// </summary>
internal sealed class XbfFileBuilder
{
	private sealed class PropertyEntry
	{
		public string? DependencyProperty;
		public string? Setter;
		public string? Getter;
	}

	private sealed class TypeEntry
	{
		public string Name = "";
		public bool HasActivator;
	}

	private readonly List<string> _strings = new();
	private readonly Dictionary<string, int> _stringIds = new(StringComparer.Ordinal);
	private readonly List<TypeEntry> _types = new();
	private readonly Dictionary<string, int> _typeIds = new(StringComparer.Ordinal);
	private readonly List<PropertyEntry> _properties = new();
	private readonly Dictionary<string, int> _propertyIds = new(StringComparer.Ordinal);
	private readonly List<string> _adders = new();
	private readonly Dictionary<string, int> _adderIds = new(StringComparer.Ordinal);
	private readonly List<string> _constants = new();
	private readonly Dictionary<string, int> _constantIds = new(StringComparer.Ordinal);
	private readonly MemoryStream _nodeStream = new();

	public XbfFileBuilder(string className)
	{
		ClassName = className;
	}

	public string ClassName { get; }

	public int RootCount { get; private set; }

	public bool IsEmpty => RootCount == 0;

	public readonly record struct Checkpoint(int Strings, int Types, int Properties, int Adders, int Constants);

	public Checkpoint CreateCheckpoint() => new(_strings.Count, _types.Count, _properties.Count, _adders.Count, _constants.Count);

	/// <summary>
	/// Forgets what was registered since <paramref name="checkpoint"/>, for an object that ends up built in C#.
	/// </summary>
	public void Rollback(Checkpoint checkpoint)
	{
		Truncate(_strings, _stringIds, checkpoint.Strings);
		Truncate(_adders, _adderIds, checkpoint.Adders);
		Truncate(_constants, _constantIds, checkpoint.Constants);

		if (_types.Count > checkpoint.Types)
		{
			foreach (var key in _typeIds.Where(p => p.Value >= checkpoint.Types).Select(p => p.Key).ToArray())
			{
				_typeIds.Remove(key);
			}
			_types.RemoveRange(checkpoint.Types, _types.Count - checkpoint.Types);
		}

		if (_properties.Count > checkpoint.Properties)
		{
			foreach (var key in _propertyIds.Where(p => p.Value >= checkpoint.Properties).Select(p => p.Key).ToArray())
			{
				_propertyIds.Remove(key);
			}
			_properties.RemoveRange(checkpoint.Properties, _properties.Count - checkpoint.Properties);
		}

		static void Truncate(List<string> list, Dictionary<string, int> ids, int count)
		{
			for (var i = list.Count - 1; i >= count; i--)
			{
				ids.Remove(list[i]);
				list.RemoveAt(i);
			}
		}
	}

	/// <summary>
	/// Adds the nodes of an object to the node stream.
	/// </summary>
	/// <returns>The offset of the object in the node stream.</returns>
	public int AppendRoot(XbfNodeWriter nodes)
	{
		var offset = (int)_nodeStream.Length;
		nodes.WriteTo(_nodeStream);
		RootCount++;
		return offset;
	}

	public int GetStringId(string value)
	{
		if (!_stringIds.TryGetValue(value, out var id))
		{
			_stringIds[value] = id = _strings.Count;
			_strings.Add(value);
		}

		return id;
	}

	/// <param name="globalizedTypeName">The type, as C# can name it from anywhere.</param>
	/// <param name="needsActivator">Whether the node stream creates instances of the type.</param>
	public int GetTypeId(string globalizedTypeName, bool needsActivator)
	{
		if (!_typeIds.TryGetValue(globalizedTypeName, out var id))
		{
			_typeIds[globalizedTypeName] = id = _types.Count;
			_types.Add(new TypeEntry { Name = globalizedTypeName });
		}

		_types[id].HasActivator |= needsActivator;

		return id;
	}

	private PropertyEntry GetProperty(string key, out int index)
	{
		if (!_propertyIds.TryGetValue(key, out index))
		{
			_propertyIds[key] = index = _properties.Count;
			_properties.Add(new PropertyEntry());
		}

		return _properties[index];
	}

	/// <param name="dependencyProperty">The C# expression of the DependencyProperty.</param>
	public int GetDependencyPropertyId(string dependencyProperty)
	{
		GetProperty(dependencyProperty, out var index).DependencyProperty = dependencyProperty;

		// The low bit tells the reader that the property is set through SetValue.
		return (index << 1) | 1;
	}

	/// <param name="key">What identifies the property: its owner and name.</param>
	/// <param name="setter">The C# statement assigning <c>v</c> to the property of <c>t</c>.</param>
	public int GetSetterId(string key, string setter)
	{
		GetProperty(key, out var index).Setter = setter;

		return index << 1;
	}

	/// <param name="key">What identifies the property: its owner and name.</param>
	/// <param name="getter">The C# expression reading the property of <c>t</c>.</param>
	public int GetGetterId(string key, string getter)
	{
		GetProperty(key, out var index).Getter = getter;

		return index;
	}

	/// <param name="adder">The C# statement adding <c>v</c> to the collection <c>c</c>.</param>
	public int GetAdderId(string adder)
	{
		if (!_adderIds.TryGetValue(adder, out var id))
		{
			_adderIds[adder] = id = _adders.Count;
			_adders.Add(adder);
		}

		return id;
	}

	/// <param name="expression">The C# expression of the value.</param>
	public int GetConstantId(string expression)
	{
		if (!_constantIds.TryGetValue(expression, out var id))
		{
			_constantIds[expression] = id = _constants.Count;
			_constants.Add(expression);
		}

		return id;
	}

	private byte[] BuildDocument()
	{
		using var metadata = new MemoryStream();
		var stringData = new MemoryStream();
		var offsets = new List<int>(_strings.Count + 1);

		foreach (var value in _strings)
		{
			offsets.Add((int)stringData.Length);
			var bytes = Encoding.UTF8.GetBytes(value);
			stringData.Write(bytes, 0, bytes.Length);
		}
		offsets.Add((int)stringData.Length);

		foreach (var offset in offsets)
		{
			WriteInt32(metadata, offset);
		}
		stringData.WriteTo(metadata);

		using var document = new MemoryStream();
		WriteInt32(document, unchecked((int)XbfConstants.Magic));
		WriteInt32(document, (int)metadata.Length);
		WriteInt32(document, (int)_nodeStream.Length);
		WriteInt32(document, XbfConstants.MajorFileVersion);
		WriteInt32(document, XbfConstants.MinorFileVersion);
		WriteInt32(document, _strings.Count);
		metadata.WriteTo(document);
		_nodeStream.WriteTo(document);

		return document.ToArray();

		static void WriteInt32(Stream stream, int value)
		{
			stream.WriteByte((byte)value);
			stream.WriteByte((byte)(value >> 8));
			stream.WriteByte((byte)(value >> 16));
			stream.WriteByte((byte)(value >> 24));
		}
	}

	/// <summary>
	/// Writes the metadata provider of the file: its XBF and what resolves the indexes of its node stream.
	/// </summary>
	public void WriteProviderClass(IIndentedStringBuilder writer)
	{
		const string NoInlining = "[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]";
		const string DependencyProperty = "global::Microsoft.UI.Xaml.DependencyProperty";

		var document = BuildDocument();

		writer.AppendLineIndented("[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");
		using (writer.BlockInvariant($"internal sealed class {ClassName} : global::Uno.UI.Xaml.Xbf.XbfMetadataProvider"))
		{
			writer.AppendLineIndented($"internal static readonly {ClassName} Instance = new();");
			writer.AppendLineIndented($"// XBF: {document.Length} bytes, {RootCount} objects, {_strings.Count} strings, {_types.Count} types, {_properties.Count} properties, {_constants.Count} constants");

			var data = new StringBuilder(document.Length * 4 + 128);
			data.Append("protected override global::System.ReadOnlySpan<byte> Data => new byte[] {");
			for (var i = 0; i < document.Length; i++)
			{
				if ((i & 0x3F) == 0)
				{
					data.Append('\n');
				}
				data.Append(document[i].ToString(CultureInfo.InvariantCulture));
				data.Append(',');
			}
			data.Append("\n};");
			writer.AppendLineIndented(data.ToString());

			writer.AppendLineIndented($"protected override int TypeCount => {_types.Count};");
			writer.AppendLineIndented($"protected override int PropertyCount => {_properties.Count};");

			// Each entry is its own method so that a type is only loaded, and its members only
			// resolved, once the node stream actually needs it.
			WriteDispatcher("protected override object CreateInstance(int i)", "C", _types.Select(t => t.HasActivator).ToArray(), isVoid: false, arguments: "");
			for (var i = 0; i < _types.Count; i++)
			{
				if (_types[i].HasActivator)
				{
					writer.AppendLineIndented($"{NoInlining} private static object C{i}() => new {_types[i].Name}();");
				}
			}

			WriteDispatcher("protected override global::System.Type GetTypeCore(int i)", "T", _types.Select(_ => true).ToArray(), isVoid: false, arguments: "");
			for (var i = 0; i < _types.Count; i++)
			{
				writer.AppendLineIndented($"{NoInlining} private static global::System.Type T{i}() => typeof({_types[i].Name});");
			}

			WriteDispatcher($"protected override {DependencyProperty} GetDependencyPropertyCore(int i)", "P", _properties.Select(p => p.DependencyProperty is not null).ToArray(), isVoid: false, arguments: "");
			for (var i = 0; i < _properties.Count; i++)
			{
				if (_properties[i].DependencyProperty is { } dependencyProperty)
				{
					writer.AppendLineIndented($"{NoInlining} private static {DependencyProperty} P{i}() => {dependencyProperty};");
				}
			}

			WriteDispatcher("protected override void SetPropertyValue(int i, object t, object v)", "S", _properties.Select(p => p.Setter is not null).ToArray(), isVoid: true, arguments: "t, v");
			for (var i = 0; i < _properties.Count; i++)
			{
				if (_properties[i].Setter is { } setter)
				{
					writer.AppendLineIndented($"{NoInlining} private static void S{i}(object t, object v) => {setter};");
				}
			}

			WriteDispatcher("protected override object GetPropertyValue(int i, object t)", "G", _properties.Select(p => p.Getter is not null).ToArray(), isVoid: false, arguments: "t");
			for (var i = 0; i < _properties.Count; i++)
			{
				if (_properties[i].Getter is { } getter)
				{
					writer.AppendLineIndented($"{NoInlining} private static object G{i}(object t) => {getter};");
				}
			}

			WriteDispatcher("protected override void AddToCollection(int i, object c, object v)", "A", _adders.Select(_ => true).ToArray(), isVoid: true, arguments: "c, v");
			for (var i = 0; i < _adders.Count; i++)
			{
				writer.AppendLineIndented($"{NoInlining} private static void A{i}(object c, object v) => {_adders[i]};");
			}

			WriteDispatcher("protected override object GetConstant(int i)", "K", _constants.Select(_ => true).ToArray(), isVoid: false, arguments: "");
			for (var i = 0; i < _constants.Count; i++)
			{
				writer.AppendLineIndented($"{NoInlining} private static object K{i}() => {_constants[i]};");
			}
		}

		void WriteDispatcher(string signature, string prefix, bool[] entries, bool isVoid, string arguments)
		{
			using (writer.BlockInvariant(signature))
			{
				if (entries.Any(e => e))
				{
					using (writer.BlockInvariant("switch (i)"))
					{
						for (var i = 0; i < entries.Length; i++)
						{
							if (entries[i])
							{
								writer.AppendLineIndented(isVoid
									? $"case {i}: {prefix}{i}({arguments}); return;"
									: $"case {i}: return {prefix}{i}({arguments});");
							}
						}
					}
				}

				writer.AppendLineIndented("throw new global::System.InvalidOperationException();");
			}
		}
	}
}
