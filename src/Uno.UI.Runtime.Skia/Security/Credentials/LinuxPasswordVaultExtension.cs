#nullable enable

using System;
using System.Runtime.InteropServices;
using Uno.Foundation.Extensibility;
using Windows.ApplicationModel;
using Windows.Security.Credentials;

namespace Uno.UI.Runtime.Skia;

internal sealed unsafe class LinuxPasswordVaultExtension : IPasswordVaultExtension
{
	private const string ApplicationAttribute = "uno-application";
	private const string ContentType = "application/octet-stream";
	private const string DefaultCollection = "default";
	private const string SchemaName = "org.unoplatform.PasswordVault";

	private static readonly Native.GHashFunc StringHash = Native.g_str_hash;
	private static readonly Native.GEqualFunc StringEqual = Native.g_str_equal;
	private static readonly Native.GDestroyNotify FreeHGlobal = Marshal.FreeHGlobal;
	private static readonly Native.GDestroyNotify ObjectUnref = Native.g_object_unref;

	private readonly string _scope = Package.Current.Id.Name;

	public static void Register()
		=> ApiExtensibility.Register(
			typeof(IPasswordVaultExtension),
			_ => new LinuxPasswordVaultExtension());

	public byte[]? Read()
	{
		Native.SecretService* service = null;
		Native.GHashTable* attributes = null;
		Native.GList* results = null;
		Native.GError* error = null;
		Native.SecretValue* value = null;

		try
		{
			service = GetService();
			attributes = CreateAttributes();
			var schema = CreateSchema();
			results = Native.secret_service_search_sync(
				service,
				ref schema,
				attributes,
				Native.SecretSearchFlags.Unlock |
					Native.SecretSearchFlags.All |
					Native.SecretSearchFlags.LoadSecrets,
				0,
				out error);
			ThrowIfError(error, "search the Secret Service");
			error = null;

			if (results is null || results->Data == 0)
			{
				return null;
			}

			var item = (Native.SecretItem*)results->Data;
			Native.secret_item_load_secret_sync(item, 0, out error);
			ThrowIfError(error, "load the Secret Service item");
			error = null;

			value = Native.secret_item_get_secret(item);
			if (value is null)
			{
				throw new InvalidOperationException("The Secret Service returned an empty credential item.");
			}

			var bytes = Native.secret_value_get(value, out var length);
			if (length > int.MaxValue || (length > 0 && bytes == 0))
			{
				throw new InvalidOperationException("The Secret Service returned invalid credential data.");
			}

			var managedLength = checked((int)length);
			var result = new byte[managedLength];
			if (managedLength > 0)
			{
				Marshal.Copy(bytes, result, 0, managedLength);
			}
			return result;
		}
		catch (DllNotFoundException exception)
		{
			throw new PlatformNotSupportedException(
				"PasswordVault requires libsecret and a Secret Service provider on Linux.",
				exception);
		}
		finally
		{
			if (error is not null)
			{
				Native.g_error_free(error);
			}
			if (value is not null)
			{
				Native.secret_value_unref(value);
			}
			if (results is not null)
			{
				Native.g_list_free_full(results, ObjectUnref);
			}
			if (attributes is not null)
			{
				Native.g_hash_table_destroy(attributes);
			}
			if (service is not null)
			{
				Native.g_object_unref((nint)service);
			}
		}
	}

	public void Write(byte[] data)
	{
		Native.SecretService* service = null;
		Native.GHashTable* attributes = null;
		Native.SecretValue* value = null;
		Native.GError* error = null;

		try
		{
			service = GetService();
			attributes = CreateAttributes();
			value = Native.secret_value_new(
				data,
				checked((nint)data.Length),
				ContentType);
			if (value is null)
			{
				throw new InvalidOperationException("PasswordVault could not allocate a Secret Service value.");
			}

			var schema = CreateSchema();
			var stored = Native.secret_service_store_sync(
				service,
				ref schema,
				attributes,
				DefaultCollection,
				$"Uno PasswordVault ({_scope})",
				value,
				0,
				out error);
			ThrowIfError(error, "write to the Secret Service");
			error = null;
			if (!stored)
			{
				throw new InvalidOperationException("PasswordVault could not write to the Secret Service.");
			}
		}
		catch (DllNotFoundException exception)
		{
			throw new PlatformNotSupportedException(
				"PasswordVault requires libsecret and a Secret Service provider on Linux.",
				exception);
		}
		finally
		{
			if (error is not null)
			{
				Native.g_error_free(error);
			}
			if (value is not null)
			{
				Native.secret_value_unref(value);
			}
			if (attributes is not null)
			{
				Native.g_hash_table_destroy(attributes);
			}
			if (service is not null)
			{
				Native.g_object_unref((nint)service);
			}
		}
	}

	private Native.GHashTable* CreateAttributes()
	{
		var attributes = Native.g_hash_table_new_full(
			StringHash,
			StringEqual,
			FreeHGlobal,
			FreeHGlobal);
		Native.g_hash_table_insert(
			attributes,
			Marshal.StringToHGlobalAnsi(ApplicationAttribute),
			Marshal.StringToHGlobalAnsi(_scope));
		return attributes;
	}

	private static Native.SecretService* GetService()
	{
		var service = Native.secret_service_get_sync(
			Native.SecretServiceFlags.OpenSession |
				Native.SecretServiceFlags.LoadCollections,
			0,
			out var error);
		try
		{
			ThrowIfError(error, "open the Secret Service");
			if (service is null)
			{
				throw new InvalidOperationException("PasswordVault could not connect to the Secret Service.");
			}
			return service;
		}
		finally
		{
			if (error is not null)
			{
				Native.g_error_free(error);
			}
		}
	}

	private static Native.SecretSchema CreateSchema()
	{
		var schema = new Native.SecretSchema
		{
			Name = SchemaName,
			Flags = Native.SecretSchemaFlags.None,
			Attributes = new Native.SecretSchemaAttribute[32]
		};
		schema.Attributes[0] = new Native.SecretSchemaAttribute
		{
			Name = ApplicationAttribute,
			Type = Native.SecretSchemaAttributeType.String
		};
		return schema;
	}

	private static void ThrowIfError(Native.GError* error, string operation)
	{
		if (error is null)
		{
			return;
		}

		var message = Marshal.PtrToStringUTF8(error->Message) ?? "Unknown Secret Service error.";
		throw new InvalidOperationException(
			$"PasswordVault could not {operation} (error {error->Code}): {message}");
	}

	private static class Native
	{
		private const string GLib = "libglib-2.0.so.0";
		private const string GObject = "libgobject-2.0.so.0";
		private const string LibSecret = "libsecret-1.so.0";

		internal enum SecretSchemaAttributeType
		{
			String
		}

		[Flags]
		internal enum SecretSchemaFlags
		{
			None = 0,
			DontMatchName = 1 << 1
		}

		[Flags]
		internal enum SecretServiceFlags
		{
			None = 0,
			OpenSession = 1 << 1,
			LoadCollections = 1 << 2
		}

		[Flags]
		internal enum SecretSearchFlags
		{
			None = 0,
			All = 1 << 1,
			Unlock = 1 << 2,
			LoadSecrets = 1 << 3
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
		internal struct SecretSchemaAttribute
		{
			[MarshalAs(UnmanagedType.LPStr)]
			public string? Name;
			public SecretSchemaAttributeType Type;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
		internal struct SecretSchema
		{
			[MarshalAs(UnmanagedType.LPStr)]
			public string Name;
			public SecretSchemaFlags Flags;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
			public SecretSchemaAttribute[] Attributes;
			private int _reserved;
			private nint _reserved1;
			private nint _reserved2;
			private nint _reserved3;
			private nint _reserved4;
			private nint _reserved5;
			private nint _reserved6;
			private nint _reserved7;
		}

		internal struct SecretService
		{
		}

		internal struct SecretItem
		{
		}

		internal struct SecretValue
		{
		}

		internal struct GHashTable
		{
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct GList
		{
			public nint Data;
			public GList* Next;
			public GList* Previous;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct GError
		{
			public uint Domain;
			public int Code;
			public nint Message;
		}

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate uint GHashFunc(nint key);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate bool GEqualFunc(nint left, nint right);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate void GDestroyNotify(nint data);

		[DllImport(GLib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern uint g_str_hash(nint key);

		[DllImport(GLib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern bool g_str_equal(nint left, nint right);

		[DllImport(GLib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern GHashTable* g_hash_table_new_full(
			GHashFunc hash,
			GEqualFunc equal,
			GDestroyNotify keyDestroy,
			GDestroyNotify valueDestroy);

		[DllImport(GLib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern bool g_hash_table_insert(GHashTable* table, nint key, nint value);

		[DllImport(GLib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void g_hash_table_destroy(GHashTable* table);

		[DllImport(GLib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void g_list_free_full(GList* list, GDestroyNotify free);

		[DllImport(GLib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void g_error_free(GError* error);

		[DllImport(GObject, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void g_object_unref(nint value);

		[DllImport(LibSecret, CallingConvention = CallingConvention.Cdecl)]
		internal static extern SecretService* secret_service_get_sync(
			SecretServiceFlags flags,
			nint cancellable,
			out GError* error);

		[DllImport(LibSecret, CallingConvention = CallingConvention.Cdecl)]
		internal static extern GList* secret_service_search_sync(
			SecretService* service,
			ref SecretSchema schema,
			GHashTable* attributes,
			SecretSearchFlags flags,
			nint cancellable,
			out GError* error);

		[DllImport(LibSecret, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		internal static extern bool secret_service_store_sync(
			SecretService* service,
			ref SecretSchema schema,
			GHashTable* attributes,
			string collection,
			string label,
			SecretValue* value,
			nint cancellable,
			out GError* error);

		[DllImport(LibSecret, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void secret_item_load_secret_sync(
			SecretItem* item,
			nint cancellable,
			out GError* error);

		[DllImport(LibSecret, CallingConvention = CallingConvention.Cdecl)]
		internal static extern SecretValue* secret_item_get_secret(SecretItem* item);

		[DllImport(LibSecret, CallingConvention = CallingConvention.Cdecl)]
		internal static extern SecretValue* secret_value_new(
			byte[] secret,
			nint length,
			[MarshalAs(UnmanagedType.LPStr)] string contentType);

		[DllImport(LibSecret, CallingConvention = CallingConvention.Cdecl)]
		internal static extern nint secret_value_get(
			SecretValue* value,
			out nuint length);

		[DllImport(LibSecret, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void secret_value_unref(SecretValue* value);
	}
}
