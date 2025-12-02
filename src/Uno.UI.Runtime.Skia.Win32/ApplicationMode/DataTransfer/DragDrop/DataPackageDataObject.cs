using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Windows.ApplicationModel.DataTransfer;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.System.Ole;
using Uno.Foundation.Logging;
using IDataObject = Windows.Win32.System.Com.IDataObject;

namespace Uno.UI.Runtime.Skia.Win32;

/// <summary>
/// Wraps an Uno <see cref="DataPackageView"/> into a Win32 <see cref="IDataObject"/>.
/// Used for outgoing drag operations from Uno to native Win32.
/// </summary>
internal unsafe class DataPackageDataObject : IDataObject.Interface
{
	private readonly DataPackageView _data;
	private readonly List<FORMATETC> _formats;

	public DataPackageDataObject(DataPackageView data)
	{
		_data = data;
		_formats = new List<FORMATETC>();

		// Build list of available formats
		if (_data.Contains(StandardDataFormats.Text))
		{
			_formats.Add(CreateFormatEtc(CLIPBOARD_FORMAT.CF_UNICODETEXT));
		}

		if (_data.Contains(StandardDataFormats.Rtf))
		{
			_formats.Add(CreateFormatEtc((CLIPBOARD_FORMAT)PInvoke.RegisterClipboardFormat("Rich Text Format")));
		}

		if (_data.Contains(StandardDataFormats.Html))
		{
			_formats.Add(CreateFormatEtc((CLIPBOARD_FORMAT)PInvoke.RegisterClipboardFormat("HTML Format")));
		}

		if (_data.Contains(StandardDataFormats.Bitmap))
		{
			_formats.Add(CreateFormatEtc(CLIPBOARD_FORMAT.CF_DIB));
		}

		if (_data.Contains(StandardDataFormats.StorageItems))
		{
			_formats.Add(CreateFormatEtc(CLIPBOARD_FORMAT.CF_HDROP));
		}
	}

	private static FORMATETC CreateFormatEtc(CLIPBOARD_FORMAT format)
	{
		return new FORMATETC
		{
			cfFormat = (ushort)format,
			ptd = null,
			dwAspect = 1, // DVASPECT_CONTENT
			lindex = -1,
			tymed = (uint)TYMED.TYMED_HGLOBAL
		};
	}

	HRESULT IDataObject.Interface.GetData(FORMATETC* pformatetcIn, STGMEDIUM* pmedium)
	{
		if (pformatetcIn == null || pmedium == null)
		{
			return new HRESULT(unchecked((int)0x80070057)); // E_INVALIDARG
		}

		*pmedium = default;

		var format = (CLIPBOARD_FORMAT)pformatetcIn->cfFormat;

		try
		{
			if (format == CLIPBOARD_FORMAT.CF_UNICODETEXT && _data.Contains(StandardDataFormats.Text))
			{
				var text = _data.GetTextAsync().GetAwaiter().GetResult();
				if (text != null)
				{
					pmedium->tymed = TYMED.TYMED_HGLOBAL;
					pmedium->u.hGlobal = Win32ClipboardExtension.WriteStringToHGlobal(text);
					return HRESULT.S_OK;
				}
			}
			else if (_data.Contains(StandardDataFormats.StorageItems) && format == CLIPBOARD_FORMAT.CF_HDROP)
			{
				var items = _data.GetStorageItemsAsync().GetAwaiter().GetResult();
				if (items != null && items.Count > 0)
				{
					var paths = items.Select(item => item.Path).Where(p => !string.IsNullOrEmpty(p)).ToList();
					if (paths.Count > 0)
					{
						pmedium->tymed = TYMED.TYMED_HGLOBAL;
						pmedium->u.hGlobal = Win32ClipboardExtension.WriteFileListToHGlobal(paths);
						return HRESULT.S_OK;
					}
				}
			}
		}
		catch (Exception ex)
		{
			this.Log().LogError($"Error getting data for format {format}: {ex.Message}");
		}

		return new HRESULT(unchecked((int)0x80040064)); // DV_E_FORMATETC
	}

	HRESULT IDataObject.Interface.GetDataHere(FORMATETC* pformatetc, STGMEDIUM* pmedium)
	{
		return new HRESULT(unchecked((int)0x80004001)); // E_NOTIMPL
	}

	HRESULT IDataObject.Interface.QueryGetData(FORMATETC* pformatetc)
	{
		if (pformatetc == null)
		{
			return new HRESULT(unchecked((int)0x80070057)); // E_INVALIDARG
		}

		// Check if the requested format is in our list
		var requestedFormat = *pformatetc;
		foreach (var format in _formats)
		{
			if (format.cfFormat == requestedFormat.cfFormat &&
				(format.tymed & requestedFormat.tymed) != 0 &&
				format.dwAspect == requestedFormat.dwAspect)
			{
				return HRESULT.S_OK;
			}
		}

		return new HRESULT(unchecked((int)0x80040064)); // DV_E_FORMATETC
	}

	HRESULT IDataObject.Interface.GetCanonicalFormatEtc(FORMATETC* pformatectIn, FORMATETC* pformatetcOut)
	{
		if (pformatetcOut == null)
		{
			return new HRESULT(unchecked((int)0x80070057)); // E_INVALIDARG
		}

		*pformatetcOut = default;
		return new HRESULT(0x00040130); // DATA_S_SAMEFORMATETC
	}

	HRESULT IDataObject.Interface.SetData(FORMATETC* pformatetc, STGMEDIUM* pmedium, BOOL fRelease)
	{
		return new HRESULT(unchecked((int)0x80004001)); // E_NOTIMPL
	}

	HRESULT IDataObject.Interface.EnumFormatEtc(uint dwDirection, IEnumFORMATETC** ppenumFormatEtc)
	{
		if (ppenumFormatEtc == null)
		{
			return new HRESULT(unchecked((int)0x80070057)); // E_INVALIDARG
		}

		if (dwDirection != (uint)DATADIR.DATADIR_GET)
		{
			*ppenumFormatEtc = null;
			return new HRESULT(unchecked((int)0x80004001)); // E_NOTIMPL
		}

		try
		{
			var enumerator = new FormatEnumerator(_formats.ToArray());
			var comScope = ComHelpers.TryGetComScope<IEnumFORMATETC>(enumerator, out HRESULT hr);
			if (hr.Failed)
			{
				*ppenumFormatEtc = null;
				return hr;
			}

			*ppenumFormatEtc = (IEnumFORMATETC*)comScope.Value;
			return HRESULT.S_OK;
		}
		catch (Exception ex)
		{
			this.Log().LogError($"Error creating format enumerator: {ex.Message}");
			*ppenumFormatEtc = null;
			return new HRESULT(unchecked((int)0x8000FFFF)); // E_UNEXPECTED
		}
	}

	HRESULT IDataObject.Interface.DAdvise(FORMATETC* pformatetc, uint advf, IAdviseSink* pAdvSink, uint* pdwConnection)
	{
		return new HRESULT(unchecked((int)0x80040003)); // OLE_E_ADVISENOTSUPPORTED
	}

	HRESULT IDataObject.Interface.DUnadvise(uint dwConnection)
	{
		return new HRESULT(unchecked((int)0x80040003)); // OLE_E_ADVISENOTSUPPORTED
	}

	HRESULT IDataObject.Interface.EnumDAdvise(IEnumSTATDATA** ppenumAdvise)
	{
		return new HRESULT(unchecked((int)0x80040003)); // OLE_E_ADVISENOTSUPPORTED
	}

	// Format enumerator implementation
	private class FormatEnumerator : IEnumFORMATETC.Interface
	{
		private readonly FORMATETC[] _formats;
		private int _current;

		public FormatEnumerator(FORMATETC[] formats)
		{
			_formats = formats;
			_current = 0;
		}

		private FormatEnumerator(FORMATETC[] formats, int current)
		{
			_formats = formats;
			_current = current;
		}

		HRESULT IEnumFORMATETC.Interface.Next(uint celt, FORMATETC* rgelt, uint* pceltFetched)
		{
			if (rgelt == null)
			{
				return new HRESULT(unchecked((int)0x80070057)); // E_INVALIDARG
			}

			uint fetched = 0;
			while (fetched < celt && _current < _formats.Length)
			{
				rgelt[fetched] = _formats[_current];
				_current++;
				fetched++;
			}

			if (pceltFetched != null)
			{
				*pceltFetched = fetched;
			}

			return fetched == celt ? HRESULT.S_OK : new HRESULT(1); // S_FALSE
		}

		HRESULT IEnumFORMATETC.Interface.Skip(uint celt)
		{
			_current += (int)celt;
			return _current < _formats.Length ? HRESULT.S_OK : new HRESULT(1); // S_FALSE
		}

		HRESULT IEnumFORMATETC.Interface.Reset()
		{
			_current = 0;
			return HRESULT.S_OK;
		}

		HRESULT IEnumFORMATETC.Interface.Clone(IEnumFORMATETC** ppenum)
		{
			if (ppenum == null)
			{
				return new HRESULT(unchecked((int)0x80070057)); // E_INVALIDARG
			}

			try
			{
				var clone = new FormatEnumerator(_formats, _current);
				var comScope = ComHelpers.TryGetComScope<IEnumFORMATETC>(clone, out HRESULT hr);
				if (hr.Failed)
				{
					*ppenum = null;
					return hr;
				}

				*ppenum = (IEnumFORMATETC*)comScope.Value;
				return HRESULT.S_OK;
			}
			catch (Exception ex)
			{
				typeof(FormatEnumerator).Log().LogError($"Error cloning enumerator: {ex.Message}");
				*ppenum = null;
				return new HRESULT(unchecked((int)0x8000FFFF)); // E_UNEXPECTED
			}
		}
	}
}
