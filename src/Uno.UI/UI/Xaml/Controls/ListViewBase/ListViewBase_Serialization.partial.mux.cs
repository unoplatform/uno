// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ListViewBase_Partial_Serialization.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d


#nullable enable

using System;
using System.Threading.Tasks;
using DirectUI;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Windows.Foundation;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;

namespace Microsoft.UI.Xaml.Controls;

public partial class ListViewBase
{
	internal string GetRelativeScrollPosition(
		ListViewItemToKeyHandler itemToKeyHandler)
	{
		Panel? spItemsPanel;
		string returnValue = string.Empty;

		spItemsPanel = ItemsPanelRoot;
		if (spItemsPanel is not null)
		{
			ModernCollectionBasePanel? spModernPanel = spItemsPanel as ModernCollectionBasePanel;

			if (spModernPanel is not null)
			{
				IBuffer spBuffer;
				ItemsPresenter spItemsPresenter;
				ItemsPresenter.ItemsPresenterParts firstVisiblePart = ItemsPresenter.ItemsPresenterParts.Header;
				ListSerializationData serializationData = default;
				serializationData.version = ListSerializationFormatVersions.ListSerializationFormatVersions_PhoneBlue;
				serializationData.elementType = ElementType.Other;     // ElementType::Other is a no-op during deserialization.

				spItemsPresenter = ItemsPresenter!;

				global::System.Diagnostics.Debug.Assert(spModernPanel is not null);

				spItemsPresenter.GetFirstVisiblePart(
					out firstVisiblePart,
					out serializationData.viewportOffset);

				if (firstVisiblePart == ItemsPresenter.ItemsPresenterParts.Header)
				{
					serializationData.elementType = ElementType.Header;
				}
				else if (firstVisiblePart == ItemsPresenter.ItemsPresenterParts.Footer)
				{
					serializationData.elementType = ElementType.Footer;
				}
				else
				{
					UIElement? spFirstVisibleElement;
					Controls.ElementType elementType = Controls.ElementType.ItemContainer;

					spModernPanel.GetFirstVisibleElementForSerialization(
						out elementType,
						out serializationData.viewportOffset,
						out spFirstVisibleElement);

					if (spFirstVisibleElement is not null)
					{
						object? spFirstVisibleObject;

						if (elementType == Controls.ElementType.ItemContainer)
						{
							IItemContainerMapping spModernPanelAsIICM;
							spModernPanelAsIICM = spModernPanel.GetItemContainerMapping();
							spFirstVisibleObject = spModernPanelAsIICM.ItemFromContainer(spFirstVisibleElement);

							serializationData.elementType = ElementType.Item;
						}
						else
						{
							object? spCollectionViewGroupAsII;
							ICollectionViewGroup spCollectionViewGroup;

							IGroupHeaderMapping spModernPanelAsIGHM;
							spModernPanelAsIGHM = spModernPanel.GetGroupHeaderMapping();
							spCollectionViewGroupAsII = spModernPanelAsIGHM.GroupFromHeader(spFirstVisibleElement);
							spCollectionViewGroup = (ICollectionViewGroup)spCollectionViewGroupAsII!;
							spFirstVisibleObject = spCollectionViewGroup.Group;

							serializationData.elementType = ElementType.GroupHeader;
						}

						if (spFirstVisibleObject is not null)
						{
							serializationData.serializationKey = itemToKeyHandler(spFirstVisibleObject);
						}

						// If, for some reason, we can't serialize (e.g. the serialization key is ...or empty, there is
						// no way to tell the difference in WinRT), the backup plan is to return a no-op buffer.
						if (serializationData.serializationKey is null)
						{
							serializationData.elementType = ElementType.Other;
							serializationData.viewportOffset = 0;
						}
					}
				}

				// Creates the serialization buffer, encode it and returns it as a string.
				spBuffer = CreateSerializationBuffer(serializationData);
				returnValue = EncodeBuffer(spBuffer);
			}
			else
			{
				// TODO Uno: ErrorHelper::OriginateErrorUsingResourceID(E_FAIL, ERROR_INCORRECT_PANEL_FOR_SERIALIZATION)
				throw new InvalidOperationException("The ItemsPanel of this ListViewBase does not support serialization.");
			}
		}
		else
		{
			// TODO Uno: ErrorHelper::OriginateErrorUsingResourceID(E_INVALIDARG, ERROR_ITEMSPANELROOT_NOT_READY)
			throw new ArgumentException("The ItemsPanelRoot is not ready.");
		}

		return returnValue;
	}

	internal IAsyncAction SetRelativeScrollPositionAsync(
		string relativeScrollPosition,
		ListViewKeyToItemHandler keyToItemHandler)
	{
		IBuffer spBuffer;
		WeakReference<ListViewBase> wrThis;

		ListSerializationData serializationData = default;
		string? serializationKey = null;
		bool parsingSucceeded = false;

		// Decodes and parse the buffer.
		spBuffer = DecodeBuffer(relativeScrollPosition);
		parsingSucceeded = ParseSerializationBuffer(spBuffer, out serializationData);

		bool isHeader = (serializationData.elementType == ElementType.Header);
		bool isFooter = (serializationData.elementType == ElementType.Footer);
		double offset = serializationData.viewportOffset;

		bool runChildAsyncOperation = parsingSucceeded && !isHeader && !isFooter;

		serializationKey = serializationData.serializationKey;
		serializationData.serializationKey = null;

		wrThis = new WeakReference<ListViewBase>(this);

		// TODO Uno: DependentAsyncWorker is replaced by an AsyncAction running the child operation, then the continuation.
		return AsyncAction.FromTask(async ct =>
		{
			// Returns the child async operation.
			object? item = null;
			AsyncStatus status = AsyncStatus.Completed;
			Exception? childError = null;

			if (runChildAsyncOperation)
			{
				try
				{
					item = await keyToItemHandler(serializationKey!).AsTask(ct);
				}
				catch (OperationCanceledException)
				{
					status = AsyncStatus.Canceled;
				}
				catch (Exception e)
				{
					status = AsyncStatus.Error;
					childError = e;
				}
			}

			// Runs after the child operation has completed.
			if (wrThis.TryGetTarget(out var spThis) && status == AsyncStatus.Completed && parsingSucceeded)
			{
				Action<object?> scrollCommand =
					spThis.GetDeserializationScrollCommand(isHeader, isFooter, offset);

				// Schedule the scroll operation right away if the panel is ready.
				// Otherwise, defer it.
				{
					Panel? spItemsPanel;

					spItemsPanel = spThis.ItemsPanelRoot;
					if (spItemsPanel is not null)
					{
						scrollCommand(item);
					}
					else
					{
						DeferPoint deferUntil = (isHeader || isFooter) ? DeferPoint.ListViewBaseLoaded : DeferPoint.ItemsHostAvailable;

						if (isFooter)
						{
							spThis.m_isDeferredElementFooter = true;
						}

						spThis.ScheduleDeferredScrollCommand(scrollCommand, item, deferUntil);
					}
				}
			}

			if (childError is not null)
			{
				global::System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(childError).Throw();
			}
		});
	}

	private IBuffer CreateSerializationBuffer(
		ListSerializationData serializationData)
	{
		IBuffer returnValue;

		DataWriter spDataWriter;
		uint serializationKeyCodeUnitCount = 0;
		uint serializationKeyCodeUnitCountWritten = 0;

		spDataWriter = new DataWriter();

		spDataWriter.WriteInt32((int)serializationData.version);
		spDataWriter.WriteInt32((int)serializationData.elementType);
		spDataWriter.WriteDouble(serializationData.viewportOffset);

		serializationKeyCodeUnitCount = spDataWriter.MeasureString(serializationData.serializationKey!);
		spDataWriter.WriteUInt32(serializationKeyCodeUnitCount);
		serializationKeyCodeUnitCountWritten = spDataWriter.WriteString(serializationData.serializationKey!);

		global::System.Diagnostics.Debug.Assert(serializationKeyCodeUnitCount == serializationKeyCodeUnitCountWritten);

#if !HAS_UNO // TODO Uno: telemetry.
		if (serializationKeyCodeUnitCount != serializationKeyCodeUnitCountWritten)
		{
			TraceLoggingWrite(
				g_hTraceProvider,
				"ListViewBaseSerializationInvalidWrite",
				TraceLoggingUInt32(serializationKeyCodeUnitCount, "serializationKeyCodeUnitCount"),
				TraceLoggingWideString(serializationData.serializationKey.GetRawBuffer(&serializationKeyCodeUnitCount), "serializationKey"),
				TraceLoggingUInt32(serializationKeyCodeUnitCountWritten, "serializationKeyCodeUnitCountWritten"),
				TelemetryPrivacyDataTag(PDT_ProductAndServicePerformance),
				TraceLoggingKeyword(MICROSOFT_KEYWORD_TELEMETRY));
		}
#endif

		returnValue = spDataWriter.DetachBuffer();

		return returnValue;
	}

	private bool ParseSerializationBuffer(
		IBuffer pBuffer,
		out ListSerializationData pSerializationData)
	{
		DataReader spDataReader;
		bool pSucceeded = false;

		pSerializationData = default;

		spDataReader = DataReader.FromBuffer(pBuffer);

		pSerializationData.version = (ListSerializationFormatVersions)spDataReader.ReadInt32();

		if (pSerializationData.version == ListSerializationFormatVersions.ListSerializationFormatVersions_PhoneBlue)
		{
			uint serializationKeyCodeUnitCount = 0;

			pSerializationData.elementType = (ElementType)spDataReader.ReadInt32();
			pSerializationData.viewportOffset = spDataReader.ReadDouble();

			serializationKeyCodeUnitCount = spDataReader.ReadUInt32();
			pSerializationData.serializationKey = spDataReader.ReadString(serializationKeyCodeUnitCount);

			pSucceeded = true;
		}

		return pSucceeded;
	}

	private Action<object?> GetDeserializationScrollCommand(
		bool isHeader,
		bool isFooter,
		double offset)
	{
		return (object? scrollToItem) =>
		{
			Panel? itemsPanel;
			itemsPanel = ItemsPanelRoot;
			if (itemsPanel is not null)
			{
				ModernCollectionBasePanel? modernPanel = itemsPanel as ModernCollectionBasePanel;

				if (modernPanel is not null)
				{
					double effectiveOffset =
						isHeader || isFooter ?
						offset :
						modernPanel.AdjustViewportOffsetForDeserialization(offset);

					ScrollIntoViewInternal(
						scrollToItem,
						isHeader,
						isFooter,
						false /* isFromPublicAPI */,
						ScrollIntoViewAlignment.Leading,
						effectiveOffset);
				}
			}
		};
	}

	private string EncodeBuffer(
		IBuffer pBuffer)
	{
		string pBase64String;

		pBase64String = CryptographicBuffer.EncodeToBase64String(pBuffer);

		return pBase64String;
	}

	private IBuffer DecodeBuffer(
		string base64String)
	{
		IBuffer ppBuffer;

		ppBuffer = CryptographicBuffer.DecodeFromBase64String(base64String);

		return ppBuffer;
	}
}

