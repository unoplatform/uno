// Original source: https://github.com/xamarin/Xamarin.MacDev

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Xml;

namespace Uno.Sdk.MacDev;

public abstract class PropertyListFormat
{
	public static readonly PropertyListFormat Xml = new XmlFormat();
	public static readonly PropertyListFormat Binary = new BinaryFormat();
	public static readonly PropertyListFormat Json = new JsonFormat();

	// Stream must be seekable
	public static ReadWriteContext? CreateReadContext(Stream input) => Binary.StartReading(input) ?? Xml.StartReading(input);

	public static ReadWriteContext? CreateReadContext(byte[] array, int startIndex, int length) => CreateReadContext(new MemoryStream(array, startIndex, length));

	public static ReadWriteContext? CreateReadContext(byte[] array) => CreateReadContext(new MemoryStream(array, 0, array.Length));

	// returns null if the input is not of the correct format. Stream must be seekable
	public abstract ReadWriteContext? StartReading(Stream input);
	public abstract ReadWriteContext StartWriting(Stream output);

	public ReadWriteContext? StartReading(byte[] array, int startIndex, int length) => StartReading(new MemoryStream(array, startIndex, length));

	public ReadWriteContext? StartReading(byte[] array) => StartReading(new MemoryStream(array, 0, array.Length));

	internal class BinaryFormat : PropertyListFormat
	{
		// magic is bplist + 2 byte version id
		private static readonly byte[] BPLIST_MAGIC = [0x62, 0x70, 0x6C, 0x69, 0x73, 0x74];  // "bplist"
		private static readonly byte[] BPLIST_VERSION = [0x30, 0x30]; // "00"

		public override ReadWriteContext? StartReading(Stream input)
		{
			if (input.Length < BPLIST_MAGIC.Length + 2)
			{
				return null;
			}

			input.Seek(0, SeekOrigin.Begin);
			for (var i = 0; i < BPLIST_MAGIC.Length; i++)
			{
				if ((byte)input.ReadByte() != BPLIST_MAGIC[i])
				{
					return null;
				}
			}

			// skip past the 2 byte version id for now
			//  we currently don't bother checking it because it seems different versions of OSX might write different values here?
			input.Seek(2, SeekOrigin.Current);
			return new Context(input, true);
		}

		public override ReadWriteContext StartWriting(Stream output)
		{
			output.Write(BPLIST_MAGIC, 0, BPLIST_MAGIC.Length);
			output.Write(BPLIST_VERSION, 0, BPLIST_VERSION.Length);

			return new Context(output, false);
		}

		class Context : ReadWriteContext
		{
			private static readonly DateTime AppleEpoch = new(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc); //see CFDateGetAbsoluteTime

			//https://github.com/mono/referencesource/blob/mono/mscorlib/system/datetime.cs
			private const long TicksPerMillisecond = 10000;
			private const long TicksPerSecond = TicksPerMillisecond * 1000;
			private readonly Stream _stream;
			private int _currentLength;

			private CFBinaryPlistTrailer trailer;

			//for writing
			private readonly List<object> _objectRefs;
			private int _currentRef;
			private long[]? _offsets;

			public Context(Stream stream, bool reading)
			{
				_objectRefs = [];
				_stream = stream;
				if (reading)
				{
					trailer = CFBinaryPlistTrailer.Read(this);
					ReadObjectHead();
				}
			}

			#region Binary reading members
			protected override bool ReadBool() => CurrentType == PlistType.@true;

			protected override void ReadObjectHead()
			{
				var b = _stream.ReadByte();
				var len = 0L;
				var type = (PlistType)(b & 0xF0);
				if (type == PlistType.@null)
				{
					type = (PlistType)b;
				}
				else
				{
					len = b & 0x0F;
					if (len == 0xF)
					{
						ReadObjectHead();
						len = ReadInteger();
					}
				}
				CurrentType = type;
				_currentLength = (int)len;
			}

			protected override long ReadInteger() => CurrentType switch
			{
				PlistType.integer => ReadBigEndianInteger((int)Math.Pow(2, _currentLength)),
				_ => throw new NotSupportedException("Integer of type: " + CurrentType),
			};

			protected override double ReadReal()
			{
				var bytes = ReadBigEndianBytes((int)Math.Pow(2, _currentLength));
				return CurrentType switch
				{
					PlistType.real => bytes.Length switch
					{
						4 => (double)BitConverter.ToSingle(bytes, 0),
						8 => BitConverter.ToDouble(bytes, 0),
						_ => throw new NotSupportedException(bytes.Length + "-byte real"),
					},
					_ => throw new NotSupportedException("Real of type: " + CurrentType),
				};
			}

			protected override DateTime ReadDate()
			{
				var bytes = ReadBigEndianBytes(8);
				var seconds = BitConverter.ToDouble(bytes, 0);
				// We need to manually convert the seconds to ticks because
				//  .NET DateTime/TimeSpan methods dealing with (milli)seconds
				//  round to the nearest millisecond (bxc #29079)
				return AppleEpoch.AddTicks((long)(seconds * TicksPerSecond));
			}

			protected override byte[] ReadData()
			{
				var bytes = new byte[_currentLength];

				_stream.Read(bytes, 0, _currentLength);

				return bytes;
			}

			protected override string ReadString()
			{
				byte[] bytes;
				switch (CurrentType)
				{
					case PlistType.@string: // ASCII
						bytes = new byte[_currentLength];

						_stream.Read(bytes, 0, bytes.Length);

						return Encoding.ASCII.GetString(bytes);
					case PlistType.wideString: //CFBinaryPList.c: Unicode string...big-endian 2-byte uint16_t
						bytes = new byte[_currentLength * 2];

						_stream.Read(bytes, 0, bytes.Length);

						return Encoding.BigEndianUnicode.GetString(bytes);
				}

				throw new NotSupportedException("String of type: " + CurrentType);
			}

			public override bool ReadArray(PArray array)
			{
				if (CurrentType != PlistType.array)
				{
					return false;
				}

				array.Clear();

				// save currentLength as it will be overwritten by next ReadObjectHead call
				var len = _currentLength;
				for (var i = 0; i < len; i++)
				{
					var obj = ReadObjectByRef();
					if (obj is not null)
					{
						array.Add(obj);
					}
				}

				return true;
			}

			public override bool ReadDict(PDictionary dict)
			{
				if (CurrentType != PlistType.dict)
				{
					return false;
				}

				dict.Clear();

				// save currentLength as it will be overwritten by next ReadObjectHead call
				var len = _currentLength;
				var keys = new string[len];
				for (var i = 0; i < len; i++)
				{
					keys[i] = ((PString)ReadObjectByRef()!).Value;
				}

				for (var i = 0; i < len; i++)
				{
					dict.Add(keys[i], ReadObjectByRef()!);
				}

				return true;
			}

			PObject? ReadObjectByRef()
			{
				// read index into offset table
				var objRef = (long)ReadBigEndianUInteger(trailer.ObjectRefSize);

				// read offset in file from table
				var lastPos = _stream.Position;
				_stream.Seek(trailer.OffsetTableOffset + objRef * trailer.OffsetEntrySize, SeekOrigin.Begin);
				_stream.Seek((long)ReadBigEndianUInteger(trailer.OffsetEntrySize), SeekOrigin.Begin);

				ReadObjectHead();
				var obj = ReadObject();

				// restore original position
				_stream.Seek(lastPos, SeekOrigin.Begin);
				return obj;
			}

			byte[] ReadBigEndianBytes(int count)
			{
				var bytes = new byte[count];

				_stream.Read(bytes, 0, count);

				if (BitConverter.IsLittleEndian)
				{
					Array.Reverse(bytes);
				}

				return bytes;
			}

			long ReadBigEndianInteger(int numBytes)
			{
				var bytes = ReadBigEndianBytes(numBytes);
				return numBytes switch
				{
					1 => bytes[0],
					2 => BitConverter.ToUInt16(bytes, 0),
					4 => BitConverter.ToUInt32(bytes, 0),
					8 => BitConverter.ToInt64(bytes, 0),
					_ => throw new NotSupportedException(bytes.Length + "-byte integer"),
				};
			}

			ulong ReadBigEndianUInteger(int numBytes)
			{
				var bytes = ReadBigEndianBytes(numBytes);
				return numBytes switch
				{
					1 => bytes[0],
					2 => BitConverter.ToUInt16(bytes, 0),
					4 => BitConverter.ToUInt32(bytes, 0),
					8 => BitConverter.ToUInt64(bytes, 0),
					_ => throw new NotSupportedException(bytes.Length + "-byte integer"),
				};
			}

			ulong ReadBigEndianUInt64()
			{
				var bytes = ReadBigEndianBytes(8);
				return BitConverter.ToUInt64(bytes, 0);
			}
			#endregion

			#region Binary writing members
			public override void WriteObject(PObject value)
			{
				if (_offsets is null)
				{
					InitOffsetTable(value);
				}

				base.WriteObject(value);
			}

			protected override void Write(PBoolean boolean) =>
				WriteObjectHead(boolean, boolean ? PlistType.@true : PlistType.@false);

			protected override void Write(PNumber number)
			{
				if (WriteObjectHead(number, PlistType.integer))
				{
					Write(number.Value);
				}
			}

			protected override void Write(PReal real)
			{
				if (WriteObjectHead(real, PlistType.real))
				{
					Write(real.Value);
				}
			}

			protected override void Write(PDate date)
			{
				if (WriteObjectHead(date, PlistType.date))
				{
					var bytes = MakeBigEndian(BitConverter.GetBytes(date.Value.Subtract(AppleEpoch).TotalSeconds));
					_stream.Write(bytes, 0, bytes.Length);
				}
			}

			protected override void Write(PData data)
			{
				var bytes = data.Value;
				if (WriteObjectHead(data, PlistType.data, bytes.Length))
				{
					_stream.Write(bytes, 0, bytes.Length);
				}
			}

			protected override void Write(PString str)
			{
				var type = PlistType.@string;
				byte[] bytes;

				if (str.Value.Any(c => c > 127))
				{
					type = PlistType.wideString;
					bytes = Encoding.BigEndianUnicode.GetBytes(str.Value);
				}
				else
				{
					bytes = Encoding.ASCII.GetBytes(str.Value);
				}

				if (WriteObjectHead(str, type, str.Value.Length))
				{
					_stream.Write(bytes, 0, bytes.Length);
				}
			}

			protected override void Write(PArray array)
			{
				if (!WriteObjectHead(array, PlistType.array, array.Count))
				{
					return;
				}

				var curRef = _currentRef;

				foreach (var item in array)
				{
					Write(GetObjRef(item), trailer.ObjectRefSize);
				}

				_currentRef = curRef;

				foreach (var item in array)
				{
					WriteObject(item);
				}
			}

			protected override void Write(PDictionary dict)
			{
				if (!WriteObjectHead(dict, PlistType.dict, dict.Count))
				{
					return;
				}

				// it would be better not to loop so many times, but we have to do it
				//  if we want to lay things out the same way apple does

				var curRef = _currentRef;

				//write key refs
				foreach (var item in dict)
				{
					Write(GetObjRef(item.Key!), trailer.ObjectRefSize);
				}

				//write value refs
				foreach (var item in dict)
				{
					Write(GetObjRef(item.Value), trailer.ObjectRefSize);
				}

				_currentRef = curRef;

				//write keys and values
				foreach (var item in dict)
				{
					WriteObject(item.Key!);
				}

				foreach (var item in dict)
				{
					WriteObject(item.Value);
				}
			}

			bool WriteObjectHead(PObject obj, PlistType type, int size = 0)
			{
				var id = GetObjRef(obj);
				if (_offsets![id] != 0) // if we've already been written, don't write us again
				{
					return false;
				}

				_offsets[id] = _stream.Position;
				switch (type)
				{
					case PlistType.@null:
					case PlistType.@false:
					case PlistType.@true:
					case PlistType.fill:
						_stream.WriteByte((byte)type);
						break;
					case PlistType.date:
						_stream.WriteByte(0x33);
						break;
					case PlistType.integer:
					case PlistType.real:
						break;
					default:
						if (size < 15)
						{
							_stream.WriteByte((byte)((byte)type | size));
						}
						else
						{
							_stream.WriteByte((byte)((byte)type | 0xF));
							Write(size);
						}
						break;
				}
				return true;
			}

			void Write(double value)
			{
				if (value >= float.MinValue && value <= float.MaxValue)
				{
					_stream.WriteByte((byte)PlistType.real | 0x2);
					var bytes = MakeBigEndian(BitConverter.GetBytes((float)value));
					_stream.Write(bytes, 0, bytes.Length);
				}
				else
				{
					_stream.WriteByte((byte)PlistType.real | 0x3);
					var bytes = MakeBigEndian(BitConverter.GetBytes(value));
					_stream.Write(bytes, 0, bytes.Length);
				}
			}

			void Write(long value)
			{
				if (value < 0 || value > uint.MaxValue)
				{ //they always write negative numbers with 8 bytes
					_stream.WriteByte((byte)PlistType.integer | 0x3);
					var bytes = MakeBigEndian(BitConverter.GetBytes(value));
					_stream.Write(bytes, 0, bytes.Length);
				}
				else if (value <= byte.MaxValue)
				{
					_stream.WriteByte((byte)PlistType.integer);
					_stream.WriteByte((byte)value);
				}
				else if (value <= ushort.MaxValue)
				{
					_stream.WriteByte((byte)PlistType.integer | 0x1);
					var bytes = MakeBigEndian(BitConverter.GetBytes((short)value));
					_stream.Write(bytes, 0, bytes.Length);
				}
				else if (value <= uint.MaxValue)
				{
					_stream.WriteByte((byte)PlistType.integer | 0x2);
					var bytes = MakeBigEndian(BitConverter.GetBytes((int)value));
					_stream.Write(bytes, 0, bytes.Length);
				}
			}

			void Write(long value, int byteCount)
			{
				byte[] bytes;
				switch (byteCount)
				{
					case 1:
						_stream.WriteByte((byte)value);
						break;
					case 2:
						bytes = MakeBigEndian(BitConverter.GetBytes((short)value));
						_stream.Write(bytes, 0, bytes.Length);
						break;
					case 4:
						bytes = MakeBigEndian(BitConverter.GetBytes((int)value));
						_stream.Write(bytes, 0, bytes.Length);
						break;
					case 8:
						bytes = MakeBigEndian(BitConverter.GetBytes(value));
						_stream.Write(bytes, 0, bytes.Length);
						break;
					default:
						throw new NotSupportedException(byteCount + "-byte integer");
				}
			}

			void InitOffsetTable(PObject topLevel)
			{
				var count = 0;
				MakeObjectRefs(topLevel, ref count);
				trailer.ObjectRefSize = GetMinByteLength(count);
				_offsets = new long[count];
			}

			void MakeObjectRefs(object obj, ref int count)
			{
				if (obj is null)
				{
					return;
				}

				if (ShouldDuplicate(obj) || !_objectRefs.Any(val => PObjectEqualityComparer.Instance.Equals(val, obj)))
				{
					_objectRefs.Add(obj);
					count++;
				}

				// for containers, also count their contents
				var pobj = obj as PObject;
				if (pobj is not null)
				{
					switch (pobj.Type)
					{

						case PObjectType.Array:
							foreach (var child in (PArray)obj)
							{
								MakeObjectRefs(child, ref count);
							}

							break;
						case PObjectType.Dictionary:
							foreach (var child in (PDictionary)obj)
							{
								MakeObjectRefs(child.Key!, ref count);
							}

							foreach (var child in (PDictionary)obj)
							{
								MakeObjectRefs(child.Value, ref count);
							}

							break;
					}
				}
			}

			static bool ShouldDuplicate(object obj)
			{
				if (obj is not PObject pobj)
				{
					return false;
				}

				return pobj.Type == PObjectType.Boolean || pobj.Type == PObjectType.Array || pobj.Type == PObjectType.Dictionary ||
					(pobj.Type == PObjectType.String && ((PString)pobj).Value.Any(c => c > 255)); //LAMESPEC: this is weird. Some things are duplicated
			}

			int GetObjRef(object obj)
			{
				if (_currentRef < _objectRefs.Count && PObjectEqualityComparer.Instance.Equals(_objectRefs[_currentRef], obj))
				{
					return _currentRef++;
				}

				return _objectRefs.FindIndex(val => PObjectEqualityComparer.Instance.Equals(val, obj));
			}

			static int GetMinByteLength(long value)
			{
				if (value >= 0 && value < byte.MaxValue)
				{
					return 1;
				}

				if (value >= short.MinValue && value < short.MaxValue)
				{
					return 2;
				}

				if (value >= int.MinValue && value < int.MaxValue)
				{
					return 4;
				}

				return 8;
			}

			static byte[] MakeBigEndian(byte[] bytes)
			{
				if (BitConverter.IsLittleEndian)
				{
					Array.Reverse(bytes);
				}

				return bytes;
			}
			#endregion

			public override void Dispose()
			{
				if (_offsets is not null)
				{
					trailer.OffsetTableOffset = _stream.Position;
					trailer.OffsetEntrySize = GetMinByteLength(trailer.OffsetTableOffset);
					foreach (var offset in _offsets)
					{
						Write(offset, trailer.OffsetEntrySize);
					}

					//LAMESPEC: seems like they always add 6 extra bytes here. not sure why
					for (var i = 0; i < 6; i++)
					{
						_stream.WriteByte(0);
					}

					trailer.Write(this);
				}
			}

			class PObjectEqualityComparer : IEqualityComparer<object>
			{
				public static readonly PObjectEqualityComparer Instance = new();

				PObjectEqualityComparer()
				{
				}

				public new bool Equals(object? x, object? y)
				{
					var vx = x as IPValueObject;
					var vy = y as IPValueObject;

					if (vx is null && vy is null)
					{
						return EqualityComparer<object?>.Default.Equals(x, y);
					}

					if (vx is null && x is not null && vy?.Value is not null)
					{
						return vy.Value.Equals(x);
					}

					if (vy is null && y is not null && vx?.Value is not null)
					{
						return vx.Value.Equals(y);
					}

					if (vx is null || vy is null)
					{
						return false;
					}

					return vx.Value?.Equals(vy.Value) == true;
				}

				public int GetHashCode(object obj)
				{
					var valueObj = obj as IPValueObject;
					if (valueObj is not null)
					{
						return valueObj.Value.GetHashCode();
					}

					return obj.GetHashCode();
				}
			}

			struct CFBinaryPlistTrailer
			{
				const int TRAILER_SIZE = 26;

				public int OffsetEntrySize;
				public int ObjectRefSize;
				public long ObjectCount;
				public long TopLevelRef;
				public long OffsetTableOffset;

				public static CFBinaryPlistTrailer Read(Context ctx)
				{
					var pos = ctx._stream.Position;
					ctx._stream.Seek(-TRAILER_SIZE, SeekOrigin.End);
					var result = new CFBinaryPlistTrailer
					{
						OffsetEntrySize = ctx._stream.ReadByte(),
						ObjectRefSize = ctx._stream.ReadByte(),
						ObjectCount = (long)ctx.ReadBigEndianUInt64(),
						TopLevelRef = (long)ctx.ReadBigEndianUInt64(),
						OffsetTableOffset = (long)ctx.ReadBigEndianUInt64()
					};
					ctx._stream.Seek(pos, SeekOrigin.Begin);
					return result;
				}

				public readonly void Write(Context ctx)
				{
					byte[] bytes;
					ctx._stream.WriteByte((byte)OffsetEntrySize);
					ctx._stream.WriteByte((byte)ObjectRefSize);
					//LAMESPEC: apple's comments say this is the number of entries in the offset table, but this really *is* number of objects??!?!
					bytes = MakeBigEndian(BitConverter.GetBytes((long)ctx._objectRefs!.Count));
					ctx._stream.Write(bytes, 0, bytes.Length);
					bytes = new byte[8]; //top level always at offset 0
					ctx._stream.Write(bytes, 0, bytes.Length);
					bytes = MakeBigEndian(BitConverter.GetBytes(OffsetTableOffset));
					ctx._stream.Write(bytes, 0, bytes.Length);
				}
			}
		}
	}

	// Adapted from:
	//https://github.com/mono/monodevelop/blob/07d9e6c07e5be8fe1d8d6f4272d3969bb087a287/main/src/addins/MonoDevelop.MacDev/MonoDevelop.MacDev.Plist/PlistDocument.cs
	internal class XmlFormat : PropertyListFormat
	{
		private const string PLIST_HEADER = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
";
		private static readonly Encoding _outputEncoding = new UTF8Encoding(false, false);

		public override ReadWriteContext? StartReading(Stream input)
		{
			//allow DTD but not try to resolve it from web
			var settings = new XmlReaderSettings()
			{
				CloseInput = true,
				DtdProcessing = DtdProcessing.Ignore,
				XmlResolver = null,
			};

			XmlReader? reader = null;
			input.Seek(0, SeekOrigin.Begin);
			try
			{
				reader = XmlReader.Create(input, settings);
				reader.ReadToDescendant("plist");
				while (reader.Read() && reader.NodeType != XmlNodeType.Element)
				{
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Exception: {0}", ex);
			}

			if (reader is null || reader.EOF)
			{
				return null;
			}

			return new Context(reader);
		}

		public override ReadWriteContext StartWriting(Stream output)
		{
			var writer = new StreamWriter(output, _outputEncoding);
			writer.Write(PLIST_HEADER);

			return new Context(writer);
		}

		class Context : ReadWriteContext
		{
			private const string DATETIME_FORMAT = "yyyy-MM-dd'T'HH:mm:ssK";
			private readonly XmlReader? _reader;
			private readonly TextWriter? _writer;

			private int _indentLevel;
			private string _indentString = string.Empty;

			public Context(XmlReader reader)
			{
				_reader = reader;
				ReadObjectHead();
			}

			public Context(TextWriter writer)
			{
				_writer = writer;
			}

			#region XML reading members
			protected override void ReadObjectHead()
			{
				try
				{
					CurrentType = (PlistType)Enum.Parse(typeof(PlistType), _reader!.LocalName);
				}
				catch (Exception ex)
				{
					throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Failed to parse PList data type: {0}", _reader?.LocalName), ex);
				}
			}

			protected override bool ReadBool()
			{
				// Create the PBoolean object, then move to the xml reader to next node
				// so we are ready to parse the next object. 'bool' types don't have
				// content so we have to move the reader manually, unlike integers which
				// implicitly move to the next node because we parse the content.
				var result = CurrentType == PlistType.@true;
				_reader!.Read();
				return result;
			}

			protected override long ReadInteger() => _reader!.ReadElementContentAsLong();

			protected override double ReadReal() => _reader!.ReadElementContentAsDouble();

			protected override DateTime ReadDate() => DateTime.ParseExact(_reader!.ReadElementContentAsString(), DATETIME_FORMAT, CultureInfo.InvariantCulture).ToUniversalTime();

			protected override byte[] ReadData() => Convert.FromBase64String(_reader!.ReadElementContentAsString());

			protected override string ReadString() => _reader!.ReadElementContentAsString();

			public override bool ReadArray(PArray array)
			{
				if (CurrentType != PlistType.array)
				{
					return false;
				}

				array.Clear();

				if (_reader!.IsEmptyElement)
				{
					_reader.Read();
					return true;
				}

				// advance to first node
				_reader.ReadStartElement();
				while (!_reader.EOF && _reader.NodeType != XmlNodeType.Element && _reader.NodeType != XmlNodeType.EndElement)
				{
					if (!_reader.Read())
					{
						break;
					}
				}

				while (!_reader.EOF && _reader.NodeType != XmlNodeType.EndElement)
				{
					if (_reader.NodeType == XmlNodeType.Element)
					{
						ReadObjectHead();

						var val = ReadObject();
						if (val is not null)
						{
							array.Add(val);
						}
					}
					else if (!_reader.Read())
					{
						break;
					}
				}

				if (!_reader.EOF && _reader.NodeType == XmlNodeType.EndElement && _reader.Name == "array")
				{
					_reader.ReadEndElement();
					return true;
				}

				return false;
			}

			public override bool ReadDict(PDictionary dict)
			{
				if (CurrentType != PlistType.dict)
				{
					return false;
				}

				dict.Clear();

				if (_reader!.IsEmptyElement)
				{
					_reader.Read();
					return true;
				}

				_reader.ReadToDescendant("key");

				while (!_reader.EOF && _reader.NodeType == XmlNodeType.Element)
				{
					var key = _reader.ReadElementString();

					while (!_reader.EOF && _reader.NodeType != XmlNodeType.Element && _reader.Read())
					{
						if (_reader.NodeType == XmlNodeType.EndElement)
						{
							throw new FormatException(string.Format(CultureInfo.InvariantCulture, "No value found for key {0}", key));
						}
					}

					ReadObjectHead();
					var result = ReadObject();
					if (result is not null)
					{
						// Keys are not required to be unique. The last entry wins.
						dict[key] = result;
					}

					do
					{
						if (_reader.NodeType == XmlNodeType.Element && _reader.Name == "key")
						{
							break;
						}

						if (_reader.NodeType == XmlNodeType.EndElement)
						{
							break;
						}
					} while (_reader.Read());
				}

				if (!_reader.EOF && _reader.NodeType == XmlNodeType.EndElement && _reader.Name == "dict")
				{
					_reader.ReadEndElement();
					return true;
				}

				return false;
			}
			#endregion

			#region XML writing members
			protected override void Write(PBoolean boolean) => WriteLine(boolean.Value ? "<true/>" : "<false/>");

			protected override void Write(PNumber number) => WriteLine("<integer>" + SecurityElement.Escape(number.Value.ToString(CultureInfo.InvariantCulture)) + "</integer>");

			protected override void Write(PReal real) => WriteLine("<real>" + SecurityElement.Escape(real.Value.ToString(CultureInfo.InvariantCulture)) + "</real>");

			protected override void Write(PDate date) => WriteLine("<date>" + SecurityElement.Escape(date.Value.ToString(DATETIME_FORMAT, CultureInfo.InvariantCulture)) + "</date>");

			protected override void Write(PData data) => WriteLine("<data>" + SecurityElement.Escape(Convert.ToBase64String(data.Value)) + "</data>");

			protected override void Write(PString str) => WriteLine("<string>" + SecurityElement.Escape(str.Value) + "</string>");

			protected override void Write(PArray array)
			{
				if (array.Count == 0)
				{
					WriteLine("<array/>");
					return;
				}

				WriteLine("<array>");
				IncreaseIndent();

				foreach (var item in array)
				{
					WriteObject(item);
				}

				DecreaseIndent();
				WriteLine("</array>");
			}

			protected override void Write(PDictionary dict)
			{
				if (dict.Count == 0)
				{
					WriteLine("<dict/>");
					return;
				}

				WriteLine("<dict>");
				IncreaseIndent();

				foreach (var kv in dict)
				{
					WriteLine("<key>" + SecurityElement.Escape(kv.Key) + "</key>");
					WriteObject(kv.Value);
				}

				DecreaseIndent();
				WriteLine("</dict>");
			}

			void WriteLine(string value)
			{
				_writer!.Write(_indentString);
				_writer.Write(value);
				_writer.Write('\n');
			}

			void IncreaseIndent()
			{
				_indentString = new string('\t', ++_indentLevel);
			}

			void DecreaseIndent()
			{
				_indentString = new string('\t', --_indentLevel);
			}
			#endregion

			public override void Dispose()
			{
				if (_writer is not null)
				{
					_writer.Write("</plist>\n");
					_writer.Flush();
					_writer.Dispose();
				}
			}
		}
	}

	internal class JsonFormat : PropertyListFormat
	{
		static readonly Encoding outputEncoding = new UTF8Encoding(false, false);

		public override ReadWriteContext StartReading(Stream input)
		{
			throw new NotImplementedException();
		}

		public override ReadWriteContext StartWriting(Stream output)
		{
			var writer = new StreamWriter(output, outputEncoding);

			return new Context(writer);
		}

		class Context(TextWriter writer) : ReadWriteContext
		{
			const string DATETIME_FORMAT = "yyyy-MM-dd'T'HH:mm:ssK";

			readonly TextWriter writer = writer;

			string indentString = "";
			int indentLevel;

			#region XML reading members
			protected override void ReadObjectHead()
			{
				throw new NotImplementedException();
			}

			protected override bool ReadBool()
			{
				throw new NotImplementedException();
			}

			protected override long ReadInteger()
			{
				throw new NotImplementedException();
			}

			protected override double ReadReal()
			{
				throw new NotImplementedException();
			}

			protected override DateTime ReadDate()
			{
				throw new NotImplementedException();
			}

			protected override byte[] ReadData()
			{
				throw new NotImplementedException();
			}

			protected override string ReadString()
			{
				throw new NotImplementedException();
			}

			public override bool ReadArray(PArray array)
			{
				throw new NotImplementedException();
			}

			public override bool ReadDict(PDictionary dict)
			{
				throw new NotImplementedException();
			}
			#endregion

			#region XML writing members
			void Quote(string text)
			{
				var quoted = new StringBuilder(text.Length + 2, (text.Length * 2) + 2);

				quoted.Append('"');
				for (int i = 0; i < text.Length; i++)
				{
					if (text[i] == '\\' || text[i] == '"')
					{
						quoted.Append('\\');
					}

					quoted.Append(text[i]);
				}
				quoted.Append('"');

				writer.Write(quoted);
			}

			protected override void Write(PBoolean boolean)
			{
				writer.Write(boolean.Value ? "true" : "false");
			}

			protected override void Write(PNumber number)
			{
				writer.Write(number.Value.ToString(CultureInfo.InvariantCulture));
			}

			protected override void Write(PReal real)
			{
				writer.Write(real.Value.ToString(CultureInfo.InvariantCulture));
			}

			protected override void Write(PDate date)
			{
				writer.Write("\"" + date.Value.ToString(DATETIME_FORMAT, CultureInfo.InvariantCulture) + "\"");
			}

			protected override void Write(PData data)
			{
				Quote(Convert.ToBase64String(data.Value));
			}

			protected override void Write(PString str)
			{
				Quote(str.Value);
			}

			protected override void Write(PArray array)
			{
				if (array.Count == 0)
				{
					writer.Write("[]");
					return;
				}

				writer.WriteLine("[");
				IncreaseIndent();

				int i = 0;
				foreach (var item in array)
				{
					writer.Write(indentString);
					WriteObject(item);
					if (++i < array.Count)
					{
						writer.Write(',');
					}

					writer.WriteLine();
				}

				DecreaseIndent();
				writer.Write(indentString);
				writer.Write("]");
			}

			protected override void Write(PDictionary dict)
			{
				if (dict.Count == 0)
				{
					writer.Write("{}");
					return;
				}

				writer.WriteLine("{");
				IncreaseIndent();

				int i = 0;
				foreach (var kv in dict)
				{
					writer.Write(indentString);
					Quote(kv.Key!);
					writer.Write(": ");
					WriteObject(kv.Value);
					if (++i < dict.Count)
					{
						writer.Write(',');
					}

					writer.WriteLine();
				}

				DecreaseIndent();
				writer.Write(indentString);
				writer.Write("}");
			}

			void IncreaseIndent()
			{
				indentString = new string(' ', (++indentLevel) * 2);
			}

			void DecreaseIndent()
			{
				indentString = new string(' ', (--indentLevel) * 2);
			}
			#endregion

			public override void Dispose()
			{
				if (writer is not null)
				{
					writer.WriteLine();
					writer.Flush();
					writer.Dispose();
				}
			}
		}
	}

	public abstract class ReadWriteContext : IDisposable
	{
		// Binary: The type is encoded in the 4 high bits; the low bits are data (except: null, true, false)
		// Xml: The enum value name == element tag name (this actually reads a superset of the format, since null, fill and wideString are not plist xml elements afaik)
		protected enum PlistType : byte
		{
			@null = 0x00,
			@false = 0x08,
			@true = 0x09,
			fill = 0x0F,
			integer = 0x10,
			real = 0x20,
			date = 0x30,
			data = 0x40,
			@string = 0x50,
			wideString = 0x60,
			array = 0xA0,
			dict = 0xD0,
		}

		#region Reading members
		public PObject? ReadObject()
		{
			switch (CurrentType)
			{
				case PlistType.@true:
				case PlistType.@false:
					return new PBoolean(ReadBool());
				case PlistType.fill:
					ReadObjectHead();
					return ReadObject();

				case PlistType.integer:
					return new PNumber(ReadInteger());
				case PlistType.real:
					return new PReal(ReadReal());    //FIXME: we should probably make PNumber take floating point as well as ints

				case PlistType.date:
					return new PDate(ReadDate());
				case PlistType.data:
					return new PData(ReadData());

				case PlistType.@string:
				case PlistType.wideString:
					return new PString(ReadString());

				case PlistType.array:
					var array = new PArray();
					ReadArray(array);
					return array;

				case PlistType.dict:
					var dict = new PDictionary();
					ReadDict(dict);
					return dict;
			}
			return null;
		}

		protected abstract void ReadObjectHead();
		protected PlistType CurrentType { get; set; }

		protected abstract bool ReadBool();
		protected abstract long ReadInteger();
		protected abstract double ReadReal();
		protected abstract DateTime ReadDate();
		protected abstract byte[] ReadData();
		protected abstract string ReadString();

		public abstract bool ReadArray(PArray array);
		public abstract bool ReadDict(PDictionary dict);
		#endregion

		#region Writing members
		public virtual void WriteObject(PObject value)
		{
			switch (value.Type)
			{
				case PObjectType.Boolean:
					Write((PBoolean)value);
					return;
				case PObjectType.Number:
					Write((PNumber)value);
					return;
				case PObjectType.Real:
					Write((PReal)value);
					return;
				case PObjectType.Date:
					Write((PDate)value);
					return;
				case PObjectType.Data:
					Write((PData)value);
					return;
				case PObjectType.String:
					Write((PString)value);
					return;
				case PObjectType.Array:
					Write((PArray)value);
					return;
				case PObjectType.Dictionary:
					Write((PDictionary)value);
					return;
			}
			throw new NotSupportedException(value.Type.ToString());
		}

		protected abstract void Write(PBoolean boolean);
		protected abstract void Write(PNumber number);
		protected abstract void Write(PReal real);
		protected abstract void Write(PDate date);
		protected abstract void Write(PData data);
		protected abstract void Write(PString str);
		protected abstract void Write(PArray array);
		protected abstract void Write(PDictionary dict);
		#endregion

		public abstract void Dispose();
	}
}
