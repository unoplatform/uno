declare enum ContactProperty {
    Address = "address",
    Email = "email",
    Icon = "icon",
    Name = "name",
    Tel = "tel"
}
declare class ContactInfo {
    address: PaymentAddress[];
    email: string[];
    name: string;
    tel: string;
}
declare class ContactsManager {
    select(props: ContactProperty[], options: any): Promise<ContactInfo[]>;
}
interface Navigator {
    contacts: ContactsManager;
}
declare namespace Windows.ApplicationModel.Contacts {
    class ContactPicker {
        static isSupported(): boolean;
        static pickContacts(pickMultiple: boolean): Promise<string>;
    }
}
declare namespace Windows.ApplicationModel.Core {
    /**
     * Support file for the Windows.ApplicationModel.Core
     * */
    class CoreApplication {
        private static _initializedExportsResolve;
        private static _initializedExports;
        static initialize(): void;
        /**
         * Provides a promised that resolves when CoreApplication is initialized
         */
        static waitForInitialized(): Promise<void>;
        static initializeExports(): Promise<void>;
    }
}
interface Clipboard {
    writeText(newClipText: string): Promise<void>;
    readText(): Promise<string>;
}
interface NavigatorClipboard {
    readonly clipboard?: Clipboard;
}
interface Navigator extends NavigatorClipboard {
}
declare namespace Uno.Utils {
    class Clipboard {
        private static dispatchContentChanged;
        private static dispatchGetContent;
        static startContentChanged(): void;
        static stopContentChanged(): void;
        static setText(text: string): string;
        static getText(): Promise<string>;
        private static onClipboardChanged;
    }
}
interface NavigatorDataTransferManager {
    share(data: any): Promise<void>;
}
interface Navigator extends NavigatorDataTransferManager {
}
declare namespace Windows.ApplicationModel.DataTransfer {
    class DataTransferManager {
        static isSupported(): boolean;
        static showShareUI(title: string, text: string, url: string): Promise<string>;
    }
}
declare namespace Uno.Devices.Enumeration.Internal.Providers.Midi {
    class MidiDeviceClassProvider {
        static findDevices(findInputDevices: boolean): string;
    }
}
declare namespace Uno.Devices.Enumeration.Internal.Providers.Midi {
    class MidiDeviceConnectionWatcher {
        private static dispatchStateChanged;
        static startStateChanged(): void;
        static stopStateChanged(): void;
        static onStateChanged(event: WebMidi.MIDIConnectionEvent): void;
    }
}
declare namespace Windows.Devices.Geolocation {
    class Geolocator {
        private static dispatchAccessRequest;
        private static dispatchGeoposition;
        private static dispatchError;
        private static interopInitialized;
        private static positionWatches;
        static initialize(): void;
        static requestAccess(): void;
        static getGeoposition(desiredAccuracyInMeters: number, maximumAge: number, timeout: number, requestId: string): void;
        static startPositionWatch(desiredAccuracyInMeters: number, requestId: string): boolean;
        static stopPositionWatch(desiredAccuracyInMeters: number, requestId: string): void;
        private static handleGeoposition;
        private static handleError;
        private static getAccurateCurrentPosition;
    }
}
declare module Windows.Devices.Input {
    enum PointerDeviceType {
        Touch = 0,
        Pen = 1,
        Mouse = 2
    }
}
declare namespace Windows.Devices.Midi {
    class MidiInPort {
        private static dispatchMessage;
        private static instanceMap;
        private managedId;
        private inputPort;
        private constructor();
        static createPort(managedId: string, encodedDeviceId: string): void;
        static removePort(managedId: string): void;
        static startMessageListener(managedId: string): void;
        static stopMessageListener(managedId: string): void;
        private messageReceived;
    }
}
declare namespace Windows.Devices.Midi {
    class MidiOutPort {
        static sendBuffer(encodedDeviceId: string, timestamp: number, data: number[]): void;
    }
}
declare namespace Uno.Devices.Midi.Internal {
    class WasmMidiAccess {
        private static midiAccess;
        static request(systemExclusive: boolean): Promise<string>;
        static getMidi(): WebMidi.MIDIAccess;
    }
}
declare class Accelerometer {
    constructor(config: any);
    addEventListener(type: "reading" | "activate", listener: (this: this, ev: Event) => any, useCapture?: boolean): void;
    removeEventListener(type: "reading", listener: (this: this, ev: Event) => any, useCapture?: boolean): void;
    start(): void;
    stop(): void;
    x: number;
    y: number;
    z: number;
}
interface Window {
    Accelerometer: typeof Accelerometer;
}
declare namespace Windows.Devices.Sensors {
    class Accelerometer {
        private static dispatchReading;
        private static accelerometer;
        static initialize(): boolean;
        static startReading(): void;
        static stopReading(): void;
        private static readingChangedHandler;
    }
}
declare class Gyroscope {
    constructor(config: any);
    addEventListener(type: "reading" | "activate", listener: (this: this, ev: Event) => any, useCapture?: boolean): void;
}
interface Window {
    Gyroscope: Gyroscope;
}
declare namespace Windows.Devices.Sensors {
    class Gyrometer {
        private static dispatchReading;
        private static gyroscope;
        static initialize(): boolean;
        static startReading(): void;
        static stopReading(): void;
        private static readingChangedHandler;
    }
}
declare class AmbientLightSensor {
    constructor(config: any);
    addEventListener(type: "reading", listener: (this: this, ev: Event) => any): void;
    removeEventListener(type: "reading", listener: (this: this, ev: Event) => any): void;
    start(): void;
    stop(): void;
    illuminance: number;
}
interface Window {
    AmbientLightSensor: AmbientLightSensor;
}
declare namespace Windows.Devices.Sensors {
    class LightSensor {
        private static dispatchReading;
        private static ambientLightSensor;
        static initialize(): boolean;
        static startReading(): void;
        static stopReading(): void;
        private static readingChangedHandler;
    }
}
declare class Magnetometer {
    constructor(config: any);
    addEventListener(type: "reading" | "activate", listener: (this: this, ev: Event) => any, useCapture?: boolean): void;
}
interface Window {
    Magnetometer: Magnetometer;
}
declare namespace Windows.Devices.Sensors {
    class Magnetometer {
        private static dispatchReading;
        private static magnetometer;
        static initialize(): boolean;
        static startReading(): void;
        static stopReading(): void;
        private static readingChangedHandler;
    }
}
declare namespace Uno.UI.Dispatching {
    class NativeDispatcher {
        static _dispatcherCallback: any;
        static _isReady: boolean;
        static init(isReady: Promise<boolean>): void;
        static WakeUp(force: boolean): void;
    }
}
declare namespace Windows.Gaming.Input {
    class Gamepad {
        private static dispatchGamepadAdded;
        private static dispatchGamepadRemoved;
        static getConnectedGamepadIds(): string;
        static getReading(id: number): string;
        static startGamepadAdded(): void;
        static endGamepadAdded(): void;
        static startGamepadRemoved(): void;
        static endGamepadRemoved(): void;
        private static onGamepadConnected;
        private static onGamepadDisconnected;
    }
}
declare namespace Windows.Graphics.Display {
    enum DisplayOrientations {
        None = 0,
        Landscape = 1,
        Portrait = 2,
        LandscapeFlipped = 4,
        PortraitFlipped = 8
    }
    export class DisplayInformation {
        private static readonly DpiCheckInterval;
        private static lastDpi;
        private static dpiWatcher;
        private static dispatchOrientationChanged;
        private static dispatchDpiChanged;
        private static lockingSupported;
        static getDevicePixelRatio(): number;
        static getScreenWidth(): number;
        static getScreenHeight(): number;
        static getScreenOrientationAngle(): number | null;
        static getScreenOrientationType(): string | null;
        static startOrientationChanged(): void;
        static stopOrientationChanged(): void;
        static startDpiChanged(): void;
        static stopDpiChanged(): void;
        static setOrientationAsync(uwpOrientations: DisplayOrientations): Promise<void>;
        private static parseUwpOrientation;
        private static updateDpi;
        private static onOrientationChange;
    }
    export {};
}
declare namespace Uno.Helpers.Theming {
    enum SystemTheme {
        Light = "Light",
        Dark = "Dark"
    }
}
declare namespace Uno.Helpers.Theming {
    class SystemThemeHelper {
        private static dispatchThemeChange;
        static getSystemTheme(): string;
        static observeSystemTheme(): void;
    }
}
interface Window {
    SpeechRecognition: any;
    webkitSpeechRecognition: any;
}
declare namespace Windows.Media {
    class SpeechRecognizer {
        private static dispatchResult;
        private static dispatchHypothesis;
        private static dispatchStatus;
        private static dispatchError;
        private static instanceMap;
        private managedId;
        private recognition;
        private constructor();
        static initialize(managedId: string, culture: string): void;
        static recognize(managedId: string): boolean;
        static removeInstance(managedId: string): void;
        private onResult;
        private onSpeechStart;
        private onError;
    }
}
declare namespace Windows.Networking.Connectivity {
    class ConnectionProfile {
        static hasInternetAccess(): boolean;
    }
}
declare namespace Windows.Networking.Connectivity {
    class NetworkInformation {
        private static dispatchStatusChanged;
        static startStatusChanged(): void;
        static stopStatusChanged(): void;
        static networkStatusChanged(): void;
    }
}
interface Navigator {
    webkitVibrate(pattern: number | number[]): boolean;
    mozVibrate(pattern: number | number[]): boolean;
    msVibrate(pattern: number | number[]): boolean;
}
declare namespace Windows.Phone.Devices.Notification {
    class VibrationDevice {
        static initialize(): boolean;
        static vibrate(duration: number): boolean;
    }
}
declare namespace Windows.Security.Authentication.Web {
    class WebAuthenticationBroker {
        static getReturnUrl(): string;
        static authenticateUsingIframe(iframeId: string, urlNavigate: string, urlRedirect: string, timeout: number): Promise<string>;
        static authenticateUsingWindow(urlNavigate: string, urlRedirect: string, title: string, popUpWidth: number, popUpHeight: number, timeout: number): Promise<string>;
        private static startMonitoringRedirect;
    }
}
declare namespace Windows.Storage {
    class ApplicationDataContainer {
        private static buildStorageKey;
        private static buildStoragePrefix;
        /**
         * Try to get a value from localStorage
         * */
        private static getValue;
        /**
         * Set a value to localStorage
         * */
        private static setValue;
        /**
         * Determines if a key is contained in localStorage
         * */
        private static containsKey;
        /**
         * Gets a key by index in localStorage
         * */
        private static getKeyByIndex;
        /**
         * Determines the number of items contained in localStorage
         * */
        private static getCount;
        /**
         * Clears items contained in localStorage
         * */
        private static clear;
        /**
         * Removes an item contained in localStorage
         * */
        private static remove;
        /**
         * Gets a key by index in localStorage
         * */
        private static getValueByIndex;
    }
}
declare namespace Windows.Storage {
    class AssetManager {
        static DownloadAssetsManifest(path: string): Promise<string>;
        static DownloadAsset(path: string): Promise<string>;
    }
}
declare namespace Uno.Storage {
    class NativeStorageFile {
        static getBasicPropertiesAsync(guid: string): Promise<string>;
    }
}
declare namespace Uno.Storage {
    class NativeStorageFolder {
        /**
         * Creates a new folder inside another folder.
         * @param parentGuid The GUID of the folder to create in.
         * @param folderName The name of the new folder.
         */
        static createFolderAsync(parentGuid: string, folderName: string): Promise<string>;
        /**
         * Creates a new file inside another folder.
         * @param parentGuid The GUID of the folder to create in.
         * @param folderName The name of the new file.
         */
        static createFileAsync(parentGuid: string, fileName: string): Promise<string>;
        /**
         * Tries to get a folder in the given parent folder by name.
         * @param parentGuid The GUID of the parent folder to get.
         * @param folderName The name of the folder to look for.
         * @returns A GUID of the folder if found, otherwise null.
         */
        static tryGetFolderAsync(parentGuid: string, folderName: string): Promise<string>;
        /**
        * Tries to get a file in the given parent folder by name.
        * @param parentGuid The GUID of the parent folder to get.
        * @param folderName The name of the folder to look for.
        * @returns A GUID of the folder if found, otherwise null.
        */
        static tryGetFileAsync(parentGuid: string, fileName: string): Promise<string>;
        static deleteItemAsync(parentGuid: string, itemName: string): Promise<string>;
        static getItemsAsync(folderGuid: string): Promise<string>;
        static getFoldersAsync(folderGuid: string): Promise<string>;
        static getFilesAsync(folderGuid: string): Promise<string>;
        static getPrivateRootAsync(): Promise<string>;
        private static getEntriesAsync;
    }
}
declare namespace Uno.Storage {
    class NativeStorageItem {
        private static generateGuidBinding;
        private static _guidToItemMap;
        private static _itemToGuidMap;
        static addItem(guid: string, item: FileSystemHandle | File): void;
        static removeItem(guid: string): void;
        static getItem(guid: string): FileSystemHandle | File;
        static getFile(guid: string): Promise<File>;
        static getGuid(item: FileSystemHandle | File): string;
        static getInfos(...items: Array<FileSystemHandle | File>): NativeStorageItemInfo[];
        private static storeItems;
        private static generateGuids;
    }
}
declare namespace Uno.Storage {
    class NativeStorageItemInfo {
        id: string;
        name: string;
        path: string;
        isFile: boolean;
    }
}
declare namespace Windows.Storage {
    class StorageFolder {
        private static _isInitialized;
        private static _isSynchronizing;
        private static dispatchStorageInitialized;
        /**
         * Determine if IndexDB is available, some browsers and modes disable it.
         * */
        static isIndexDBAvailable(): boolean;
        /**
         * Setup the storage persistence of a given set of paths.
         * */
        private static makePersistent;
        /**
         * Setup the storage persistence of a given path.
         * */
        static setupStorage(path: string): void;
        private static onStorageInitialized;
        /**
         * Synchronize the IDBFS memory cache back to IndexedDB
         * populate: requests the filesystem to be popuplated from the IndexedDB
         * onSynchronized: function invoked when the synchronization finished
         * */
        private static synchronizeFileSystem;
    }
}
declare namespace Windows.Storage.Pickers {
    class FileOpenPicker {
        static isNativeSupported(): boolean;
        static nativePickFilesAsync(multiple: boolean, showAllEntry: boolean, fileTypesJson: string, id: string, startIn: StartInDirectory): Promise<string>;
        static uploadPickFilesAsync(multiple: boolean, targetPath: string, accept: string): Promise<string>;
    }
}
declare namespace Windows.Storage.Pickers {
    class FileSavePicker {
        static isNativeSupported(): boolean;
        static nativePickSaveFileAsync(showAllEntry: boolean, fileTypesJson: string, suggestedFileName: string, id: string, startIn: StartInDirectory): Promise<string>;
        static SaveAs(fileName: string, dataPtr: any, size: number): void;
    }
}
declare namespace Windows.Storage.Pickers {
    class FolderPicker {
        static isNativeSupported(): boolean;
        static pickSingleFolderAsync(id: string, startIn: StartInDirectory): Promise<string>;
    }
}
declare namespace Uno.Storage.Pickers {
    class NativeFilePickerAcceptType {
        description: string;
        accept: NativeFilePickerAcceptTypeItem[];
    }
}
declare namespace Uno.Storage.Pickers {
    class NativeFilePickerAcceptTypeItem {
        mimeType: string;
        extensions: string[];
    }
}
declare namespace Uno.Storage.Streams {
    class NativeFileReadStream {
        private static _streamMap;
        private _file;
        private constructor();
        static openAsync(streamId: string, fileId: string): Promise<string>;
        static readAsync(streamId: string, targetArrayPointer: number, offset: number, count: number, position: number): Promise<string>;
        static close(streamId: string): void;
    }
}
declare namespace Uno.Storage.Streams {
    class NativeFileWriteStream {
        private static _streamMap;
        private _stream;
        private _buffer;
        private constructor();
        static openAsync(streamId: string, fileId: string): Promise<string>;
        private static verifyPermissionAsync;
        static writeAsync(streamId: string, dataArrayPointer: number, offset: number, count: number, position: number): Promise<string>;
        static closeAsync(streamId: string): Promise<string>;
        static truncateAsync(streamId: string, length: number): Promise<string>;
    }
}
declare namespace Windows.System {
    class MemoryManager {
        static getAppMemoryUsage(): any;
    }
}
interface Navigator {
    wakeLock: WakeLock;
}
declare enum WakeLockType {
    screen = "screen"
}
interface WakeLock {
    request(type: WakeLockType): Promise<WakeLockSentinel>;
}
interface WakeLockSentinel {
    release(): Promise<void>;
}
declare namespace Windows.System.Display {
    class DisplayRequest {
        private static activeScreenLockPromise;
        static activateScreenLock(): void;
        static deactivateScreenLock(): void;
    }
}
declare namespace Windows.System {
    class Launcher {
        /**
        * Load the specified URL into a new tab or window
        * @param url URL to load
        * @returns "True" or "False", depending on whether a new window could be opened or not
        */
        static open(url: string): string;
    }
}
declare class BatteryManager {
    charging: boolean;
    level: number;
    dischargingTime: number;
    addEventListener(type: "chargingchange" | "dischargingtimechange" | "levelchange", listener: (this: this, ev: Event) => any, useCapture?: boolean): void;
    removeEventListener(type: "chargingchange" | "dischargingtimechange" | "levelchange", listener: (this: this, ev: Event) => any, useCapture?: boolean): void;
}
interface Navigator {
    getBattery(): Promise<BatteryManager>;
}
declare namespace Windows.System.Power {
    class PowerManager {
        private static battery;
        private static dispatchChargingChanged;
        private static dispatchRemainingChargePercentChanged;
        private static dispatchRemainingDischargeTimeChanged;
        static initializeAsync(): Promise<string>;
        static startChargingChange(): void;
        static endChargingChange(): void;
        static startRemainingChargePercentChange(): void;
        static endRemainingChargePercentChange(): void;
        static startRemainingDischargeTimeChange(): void;
        static endRemainingDischargeTimeChange(): void;
        static getBatteryStatus(): string;
        static getPowerSupplyStatus(): string;
        static getRemainingChargePercent(): number;
        static getRemainingDischargeTime(): number;
        private static onChargingChange;
        private static onDischargingTimeChange;
        private static onLevelChange;
    }
}
declare namespace Windows.System.Profile {
    class AnalyticsInfo {
        static getDeviceType(): string;
    }
}
interface Window {
    opr: any;
    opera: any;
    mozVibrate(pattern: number | number[]): boolean;
    msVibrate(pattern: number | number[]): boolean;
    InstallTrigger: any;
    HTMLElement: any;
    StyleMedia: any;
    chrome: any;
    CSS: any;
    safari: any;
}
interface Document {
    documentMode: any;
}
declare namespace Windows.System.Profile {
    class AnalyticsVersionInfo {
        static getUserAgent(): string;
        static getBrowserName(): string;
    }
}
declare namespace Windows.UI.Core {
    class SystemNavigationManager {
        private static _current;
        static get current(): SystemNavigationManager;
        private _isEnabled;
        constructor();
        enable(): void;
        disable(): void;
        private clearStack;
    }
}
interface Navigator {
    setAppBadge(value: number): void;
    clearAppBadge(): void;
}
declare namespace Windows.UI.Notifications {
    class BadgeUpdater {
        static setNumber(value: number): void;
        static clear(): void;
    }
}
declare namespace Windows.UI.ViewManagement {
    class ApplicationView {
        static setFullScreenMode(turnOn: boolean): boolean;
        /**
            * Sets the browser window title
            * @param message the new title
            */
        static setWindowTitle(title: string): string;
        /**
            * Gets the currently set browser window title
            */
        static getWindowTitle(): string;
    }
}
declare namespace Windows.UI.ViewManagement {
    class ApplicationViewTitleBar {
        static setBackgroundColor(colorString: string): void;
    }
}
