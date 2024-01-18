---
uid: Uno.Features.BluetoothDevice
---

# Support for Windows.Devices.Bluetooth

The support for `Windows.Devices.Bluetooth` is not available at this point, but some of the `BluetoothDevice` methods (getting device selectors) are implemented.

## Note for contributors of this Windows.Devices.Bluetooth

To implement the `BluetoothDevice.GetDeviceSelectorFromClassOfDevice` method, you can use some of the information below.

This method should iterate all `ServiceCapabilities`, and use bits from it to construct part of query, and the method may look like:

  ```csharp
return _deviceSelectorPrefix +
    "((System.Devices.Aep.Bluetooth.Cod.Major:=" + classOfDevice.MajorClass +
    "AND System.Devices.Aep.Bluetooth.Cod.Minor:=" + classOfDevice.MinorClass +
    "AND  " e.g. "System.Devices.Aep.Bluetooth.Cod.Services.Information:=System.StructuredQueryType.Boolean#True"
    ") OR " + _deviceSelectorIssueInquiry + "#True)";
  ```
