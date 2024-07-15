// Original source: https://github.com/xamarin/Xamarin.MacDev

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.Sdk.MacDev;

public abstract class PObject
{
	public static PObject Create(PObjectType type) => type switch
	{
		PObjectType.Dictionary => new PDictionary(),
		PObjectType.Array => new PArray(),
		PObjectType.Number => new PNumber(0),
		PObjectType.Real => new PReal(0),
		PObjectType.Boolean => new PBoolean(true),
		PObjectType.Data => new PData([]),
		PObjectType.String => new PString(""),
		PObjectType.Date => new PDate(DateTime.Now),
		_ => throw new ArgumentOutOfRangeException(nameof(type)),
	};

	public static IEnumerable<KeyValuePair<string?, PObject>> ToEnumerable(PObject obj)
	{
		if (obj is PDictionary dictionary)
		{
			return dictionary;
		}

		if (obj is PArray array)
		{
			return array.Select(k => new KeyValuePair<string?, PObject>(k is IPValueObject valueObject ? valueObject.Value.ToString() : null, k));
		}

		return [];
	}

	private PObjectContainer? _parent;
	public PObjectContainer? Parent
	{
		get => _parent;
		set
		{
			if (_parent is not null && value is not null)
			{
				throw new NotSupportedException("Already parented.");
			}

			_parent = value;
		}
	}

	public abstract PObject Clone();

	public void Replace(PObject newObject)
	{
		var p = Parent;
		if (p is PDictionary dict)
		{
			var key = dict.GetKey(this);
			if (key is null)
			{
				return;
			}

			Remove();
			dict[key] = newObject;
		}
		else if (p is PArray arr)
		{
			arr.Replace(this, newObject);
		}
	}

	public string? Key
	{
		get
		{
			if (Parent is PDictionary dict)
			{
				return dict.GetKey(this);
			}
			return null;
		}
	}

	public void Remove()
	{
		if (Parent is PDictionary dict)
		{
			dict.Remove(Key!);
		}
		else if (Parent is PArray arr)
		{
			arr.Remove(this);
		}
		else
		{
			if (Parent is null)
			{
				throw new InvalidOperationException("Can't remove from null parent");
			}

			throw new InvalidOperationException("Can't remove from parent " + Parent);
		}
	}

	public abstract PObjectType Type { get; }

	public static implicit operator PObject(string value) => new PString(value);

	public static implicit operator PObject(long value) => new PNumber(value);

	public static implicit operator PObject(double value) => new PReal(value);

	public static implicit operator PObject(bool value) => new PBoolean(value);

	public static implicit operator PObject(DateTime value) => new PDate(value);

	public static implicit operator PObject(byte[] value) => new PData(value);

	protected virtual void OnChanged(EventArgs e)
	{
		if (SuppressChangeEvents)
		{
			return;
		}

		var handler = Changed;
		if (handler is not null)
		{
			handler(this, e);
		}

		Parent?.OnCollectionChanged(Key!, this);
	}

	protected bool SuppressChangeEvents { get; set; }

	public event EventHandler? Changed;

	public byte[] ToByteArray(PropertyListFormat format)
	{
		using var stream = new MemoryStream();
		using (var context = format.StartWriting(stream))
		{
			context.WriteObject(this);
		}

		return stream.ToArray();
	}

	public byte[] ToByteArray(bool binary)
	{
		var format = binary ? PropertyListFormat.Binary : PropertyListFormat.Xml;

		return ToByteArray(format);
	}

	public string ToJson() => Encoding.UTF8.GetString(ToByteArray(PropertyListFormat.Json));

	public string ToXml() => Encoding.UTF8.GetString(ToByteArray(PropertyListFormat.Xml));

	public static PObject? FromByteArray(byte[] array, int startIndex, int length, out bool isBinary)
	{
		var ctx = PropertyListFormat.Binary.StartReading(array, startIndex, length);

		isBinary = true;

		try
		{
			if (ctx is null)
			{
				isBinary = false;
				ctx = PropertyListFormat.CreateReadContext(array, startIndex, length);
				if (ctx is null)
				{
					return null;
				}
			}

			return ctx.ReadObject();
		}
		finally
		{
			ctx?.Dispose();
		}
	}

	public static PObject? FromByteArray(byte[] array, out bool isBinary) => FromByteArray(array, 0, array.Length, out isBinary);

	public static PObject? FromString(string str)
	{
		var ctx = PropertyListFormat.CreateReadContext(Encoding.UTF8.GetBytes(str));
		return ctx?.ReadObject();
	}

	public static PObject? FromStream(Stream stream)
	{
		var ctx = PropertyListFormat.CreateReadContext(stream);
		return ctx?.ReadObject();
	}

	public static PObject? FromFile(string fileName) => FromFile(fileName, out var _);

	public static Task<PObject?> FromFileAsync(string fileName) => Task.Run(() =>
	{
		return FromFile(fileName, out var _);
	});

	public static PObject? FromFile(string fileName, out bool isBinary)
	{
		using var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
		var ctx = PropertyListFormat.Binary.StartReading(stream);

		isBinary = true;

		try
		{
			if (ctx is null)
			{
				ctx = PropertyListFormat.CreateReadContext(stream);
				isBinary = false;

				if (ctx is null)
				{
					throw new FormatException("Unrecognized property list format.");
				}
			}

			return ctx.ReadObject();
		}
		finally
		{
			ctx?.Dispose();
		}
	}

	public Task SaveAsync(string filename, bool atomic = false, bool binary = false) => Task.Run(() => Save(filename, atomic, binary));

	public void Save(string filename, bool atomic = false, bool binary = false)
	{
		var tempFile = atomic ? GetTempFileName(filename) : filename;

		try
		{
			var dir = Path.GetDirectoryName(tempFile);

			if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);
			}

			using (var stream = new FileStream(tempFile, FileMode.Create, FileAccess.Write))
			{
				using var ctx = binary ? PropertyListFormat.Binary.StartWriting(stream) : PropertyListFormat.Xml.StartWriting(stream);
				ctx.WriteObject(this);
			}

			if (atomic)
			{
				if (File.Exists(filename))
				{
					File.Replace(tempFile, filename, null, true);
				}
				else
				{
					File.Move(tempFile, filename);
				}
			}
		}
		finally
		{
			if (atomic)
			{
				File.Delete(tempFile); // just in case- no exception is raised if file is not found
			}
		}
	}

	static string GetTempFileName(string filename)
	{
		var tempfile = filename + ".tmp";
		var i = 1;

		while (File.Exists(tempfile))
		{
			tempfile = filename + ".tmp." + (i++);
		}

		return tempfile;
	}
}
