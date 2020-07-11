var Uno;
(function (Uno) {
    var Utils;
    (function (Utils) {
        class Clipboard {
            static startContentChanged() {
                ['cut', 'copy', 'paste'].forEach(function (event) {
                    document.addEventListener(event, Clipboard.onClipboardChanged);
                });
            }
            static stopContentChanged() {
                ['cut', 'copy', 'paste'].forEach(function (event) {
                    document.removeEventListener(event, Clipboard.onClipboardChanged);
                });
            }
            static setText(text) {
                const nav = navigator;
                if (nav.clipboard) {
                    // Use clipboard object when available
                    nav.clipboard.writeText(text);
                    // Trigger change notification, as clipboard API does
                    // not execute "copy".
                    Clipboard.onClipboardChanged();
                }
                else {
                    // Hack when the clipboard is not available
                    const textarea = document.createElement("textarea");
                    textarea.value = text;
                    document.body.appendChild(textarea);
                    textarea.select();
                    document.execCommand("copy");
                    document.body.removeChild(textarea);
                }
                return "ok";
            }
            static getText() {
                const nav = navigator;
                if (nav.clipboard) {
                    return nav.clipboard.readText();
                }
                return Promise.resolve(null);
            }
            static onClipboardChanged() {
                if (!Clipboard.dispatchContentChanged) {
                    Clipboard.dispatchContentChanged =
                        Module.mono_bind_static_method("[Uno] Windows.ApplicationModel.DataTransfer.Clipboard:DispatchContentChanged");
                }
                Clipboard.dispatchContentChanged();
            }
        }
        Utils.Clipboard = Clipboard;
    })(Utils = Uno.Utils || (Uno.Utils = {}));
})(Uno || (Uno = {}));
var Windows;
(function (Windows) {
    var UI;
    (function (UI) {
        var Core;
        (function (Core) {
            /**
             * Support file for the Windows.UI.Core
             * */
            class CoreDispatcher {
                static init(isReady) {
                    MonoSupport.jsCallDispatcher.registerScope("CoreDispatcher", Windows.UI.Core.CoreDispatcher);
                    CoreDispatcher.initMethods();
                    CoreDispatcher._isReady = isReady;
                }
                /**
                 * Enqueues a core dispatcher callback on the javascript's event loop
                 *
                 * */
                static WakeUp() {
                    // Is there a Ready promise ?
                    if (CoreDispatcher._isReady) {
                        // Are we already waiting for a Ready promise ?
                        if (!CoreDispatcher._isWaitingReady) {
                            CoreDispatcher._isReady
                                .then(() => {
                                CoreDispatcher.InnerWakeUp();
                                CoreDispatcher._isReady = null;
                            });
                            CoreDispatcher._isWaitingReady = true;
                        }
                    }
                    else {
                        CoreDispatcher.InnerWakeUp();
                    }
                    return true;
                }
                static InnerWakeUp() {
                    window.setImmediate(() => {
                        try {
                            CoreDispatcher._coreDispatcherCallback();
                        }
                        catch (e) {
                            console.error(`Unhandled dispatcher exception: ${e} (${e.stack})`);
                            throw e;
                        }
                    });
                }
                static initMethods() {
                    if (Uno.UI.WindowManager.isHosted) {
                        console.debug("Hosted Mode: Skipping CoreDispatcher initialization ");
                    }
                    else {
                        if (!CoreDispatcher._coreDispatcherCallback) {
                            CoreDispatcher._coreDispatcherCallback = Module.mono_bind_static_method("[Uno] Windows.UI.Core.CoreDispatcher:DispatcherCallback");
                        }
                    }
                }
            }
            CoreDispatcher._isFirstCall = true;
            Core.CoreDispatcher = CoreDispatcher;
        })(Core = UI.Core || (UI.Core = {}));
    })(UI = Windows.UI || (Windows.UI = {}));
})(Windows || (Windows = {}));
var Uno;
(function (Uno) {
    var UI;
    (function (UI) {
        class HtmlDom {
            /**
             * Initialize various polyfills used by Uno
             */
            static initPolyfills() {
                this.isConnectedPolyfill();
            }
            static isConnectedPolyfill() {
                function get() {
                    // polyfill implementation
                    return document.contains(this);
                }
                (supported => {
                    if (!supported) {
                        Object.defineProperty(Node.prototype, "isConnected", { get });
                    }
                })("isConnected" in Node.prototype);
            }
        }
        UI.HtmlDom = HtmlDom;
    })(UI = Uno.UI || (Uno.UI = {}));
})(Uno || (Uno = {}));
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : new P(function (resolve) { resolve(result.value); }).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
var Uno;
(function (Uno) {
    var Http;
    (function (Http) {
        class HttpClient {
            static send(config) {
                return __awaiter(this, void 0, void 0, function* () {
                    const params = {
                        method: config.method,
                        cache: config.cacheMode || "default",
                        headers: new Headers(config.headers)
                    };
                    if (config.payload) {
                        params.body = yield this.blobFromBase64(config.payload, config.payloadType);
                    }
                    try {
                        const response = yield fetch(config.url, params);
                        let responseHeaders = "";
                        response.headers.forEach((v, k) => responseHeaders += `${k}:${v}\n`);
                        const responseBlob = yield response.blob();
                        const responsePayload = responseBlob ? yield this.base64FromBlob(responseBlob) : "";
                        this.dispatchResponse(config.id, response.status, responseHeaders, responsePayload);
                    }
                    catch (error) {
                        this.dispatchError(config.id, `${error.message || error}`);
                        console.error(error);
                    }
                });
            }
            static blobFromBase64(base64, contentType) {
                return __awaiter(this, void 0, void 0, function* () {
                    contentType = contentType || "application/octet-stream";
                    const url = `data:${contentType};base64,${base64}`;
                    return yield (yield fetch(url)).blob();
                });
            }
            static base64FromBlob(blob) {
                return new Promise(resolve => {
                    const reader = new FileReader();
                    reader.onloadend = () => {
                        const dataUrl = reader.result;
                        const base64 = dataUrl.split(",", 2)[1];
                        resolve(base64);
                    };
                    reader.readAsDataURL(blob);
                });
            }
            static dispatchResponse(requestId, status, headers, payload) {
                this.initMethods();
                const requestIdStr = MonoRuntime.mono_string(requestId);
                const statusStr = MonoRuntime.mono_string("" + status);
                const headersStr = MonoRuntime.mono_string(headers);
                const payloadStr = MonoRuntime.mono_string(payload);
                MonoRuntime.call_method(this.dispatchResponseMethod, null, [requestIdStr, statusStr, headersStr, payloadStr]);
            }
            static dispatchError(requestId, error) {
                this.initMethods();
                const requestIdStr = MonoRuntime.mono_string(requestId);
                const errorStr = MonoRuntime.mono_string(error);
                MonoRuntime.call_method(this.dispatchErrorMethod, null, [requestIdStr, errorStr]);
            }
            static initMethods() {
                if (this.dispatchResponseMethod) {
                    return; // already initialized.
                }
                const asm = MonoRuntime.assembly_load("Uno.UI.Wasm");
                const httpClass = MonoRuntime.find_class(asm, "Uno.UI.Wasm", "WasmHttpHandler");
                this.dispatchResponseMethod = MonoRuntime.find_method(httpClass, "DispatchResponse", -1);
                this.dispatchErrorMethod = MonoRuntime.find_method(httpClass, "DispatchError", -1);
            }
        }
        Http.HttpClient = HttpClient;
    })(Http = Uno.Http || (Uno.Http = {}));
})(Uno || (Uno = {}));
// eslint-disable-next-line @typescript-eslint/no-namespace
var MonoSupport;
(function (MonoSupport) {
    /**
     * This class is used by https://github.com/mono/mono/blob/fa726d3ac7153d87ed187abd422faa4877f85bb5/sdks/wasm/dotnet_support.js#L88 to perform
     * unmarshaled invocation of javascript from .NET code.
     * */
    class jsCallDispatcher {
        /**
         * Registers a instance for a specified identier
         * @param identifier the scope name
         * @param instance the instance to use for the scope
         */
        static registerScope(identifier, instance) {
            jsCallDispatcher.registrations.set(identifier, instance);
        }
        static findJSFunction(identifier) {
            if (!identifier) {
                return jsCallDispatcher.dispatch;
            }
            else {
                if (!jsCallDispatcher._isUnoRegistered) {
                    jsCallDispatcher.registerScope("UnoStatic", Uno.UI.WindowManager);
                    jsCallDispatcher.registerScope("UnoStatic_Windows_Storage_StorageFolder", Windows.Storage.StorageFolder);
                    jsCallDispatcher.registerScope("UnoStatic_Windows_Storage_ApplicationDataContainer", Windows.Storage.ApplicationDataContainer);
                    jsCallDispatcher._isUnoRegistered = true;
                }
                const { ns, methodName } = jsCallDispatcher.parseIdentifier(identifier);
                var instance = jsCallDispatcher.registrations.get(ns);
                if (instance) {
                    var boundMethod = instance[methodName].bind(instance);
                    var methodId = jsCallDispatcher.cacheMethod(boundMethod);
                    return () => methodId;
                }
                else {
                    throw `Unknown scope ${ns}`;
                }
            }
        }
        /**
         * Internal dispatcher for methods invoked through TSInteropMarshaller
         * @param id The method ID obtained when invoking WebAssemblyRuntime.InvokeJSUnmarshalled with a method name
         * @param pParams The parameters structure ID
         * @param pRet The pointer to the return value structure
         */
        static dispatch(id, pParams, pRet) {
            return jsCallDispatcher.methodMap[id + ""](pParams, pRet);
        }
        /**
         * Parses the method identifier
         * @param identifier
         */
        static parseIdentifier(identifier) {
            var parts = identifier.split(':');
            const ns = parts[0];
            const methodName = parts[1];
            return { ns, methodName };
        }
        /**
         * Adds the a resolved method for a given identifier
         * @param identifier the findJSFunction identifier
         * @param boundMethod the method to call
         */
        static cacheMethod(boundMethod) {
            var methodId = Object.keys(jsCallDispatcher.methodMap).length;
            jsCallDispatcher.methodMap[methodId + ""] = boundMethod;
            return methodId;
        }
        static getMethodMapId(methodHandle) {
            return methodHandle + "";
        }
    }
    jsCallDispatcher.registrations = new Map();
    jsCallDispatcher.methodMap = {};
    MonoSupport.jsCallDispatcher = jsCallDispatcher;
})(MonoSupport || (MonoSupport = {}));
// Export the DotNet helper for WebAssembly.JSInterop.InvokeJSUnmarshalled
window.DotNet = MonoSupport;
// eslint-disable-next-line @typescript-eslint/no-namespace
var Uno;
(function (Uno) {
    var UI;
    (function (UI) {
        class WindowManager {
            constructor(containerElementId, loadingElementId) {
                this.containerElementId = containerElementId;
                this.loadingElementId = loadingElementId;
                this.allActiveElementsById = {};
                this.uiElementRegistrations = {};
                this.initDom();
            }
            /**
             * Defines if the WindowManager is running in hosted mode, and should skip the
             * initialization of WebAssembly, use this mode in conjunction with the Uno.UI.WpfHost
             * to improve debuggability.
             */
            static get isHosted() {
                return WindowManager._isHosted;
            }
            /**
             * Defines if the WindowManager is responsible to raise the loading, loaded and unloaded events,
             * or if they are raised directly by the managed code to reduce interop.
             */
            static get isLoadEventsEnabled() {
                return WindowManager._isLoadEventsEnabled;
            }
            /**
                * Initialize the WindowManager
                * @param containerElementId The ID of the container element for the Xaml UI
                * @param loadingElementId The ID of the loading element to remove once ready
                */
            static init(isHosted, isLoadEventsEnabled, containerElementId = "uno-body", loadingElementId = "uno-loading") {
                WindowManager._isHosted = isHosted;
                WindowManager._isLoadEventsEnabled = isLoadEventsEnabled;
                Windows.UI.Core.CoreDispatcher.init(WindowManager.buildReadyPromise());
                this.current = new WindowManager(containerElementId, loadingElementId);
                MonoSupport.jsCallDispatcher.registerScope("Uno", this.current);
                this.current.init();
                return "ok";
            }
            /**
             * Builds a promise that will signal the ability for the dispatcher
             * to initiate work.
             * */
            static buildReadyPromise() {
                return new Promise(resolve => {
                    Promise.all([WindowManager.buildSplashScreen()]).then(() => resolve(true));
                });
            }
            /**
             * Build the splashscreen image eagerly
             * */
            static buildSplashScreen() {
                return new Promise(resolve => {
                    const img = new Image();
                    let loaded = false;
                    let loadingDone = () => {
                        if (!loaded) {
                            loaded = true;
                            if (img.width !== 0 && img.height !== 0) {
                                // Materialize the image content so it shows immediately
                                // even if the dispatcher is blocked thereafter by all
                                // the Uno initialization work. The resulting canvas is not used.
                                //
                                // If the image fails to load, setup the splashScreen anyways with the
                                // proper sample.
                                let canvas = document.createElement('canvas');
                                canvas.width = img.width;
                                canvas.height = img.height;
                                let ctx = canvas.getContext("2d");
                                ctx.drawImage(img, 0, 0);
                            }
                            if (document.readyState === "loading") {
                                document.addEventListener("DOMContentLoaded", () => {
                                    WindowManager.setupSplashScreen(img);
                                    resolve(true);
                                });
                            }
                            else {
                                WindowManager.setupSplashScreen(img);
                                resolve(true);
                            }
                        }
                    };
                    // Preload the splash screen so the image element
                    // created later on 
                    img.onload = loadingDone;
                    img.onerror = loadingDone;
                    img.src = String(UnoAppManifest.splashScreenImage);
                    // If there's no response, skip the loading
                    setTimeout(loadingDone, 2000);
                });
            }
            /**
                * Initialize the WindowManager
                * @param containerElementId The ID of the container element for the Xaml UI
                * @param loadingElementId The ID of the loading element to remove once ready
                */
            static initNative(pParams) {
                const params = WindowManagerInitParams.unmarshal(pParams);
                WindowManager.init(params.IsHostedMode, params.IsLoadEventsEnabled);
                return true;
            }
            /**
                * Creates the UWP-compatible splash screen
                *
                */
            static setupSplashScreen(splashImage) {
                if (UnoAppManifest && UnoAppManifest.splashScreenImage) {
                    const loading = document.getElementById("loading");
                    if (loading) {
                        loading.remove();
                    }
                    const unoBody = document.getElementById("uno-body");
                    if (unoBody) {
                        const unoLoading = document.createElement("div");
                        unoLoading.id = "uno-loading";
                        if (UnoAppManifest.splashScreenColor) {
                            const body = document.getElementsByTagName("body")[0];
                            body.style.backgroundColor = UnoAppManifest.splashScreenColor;
                        }
                        splashImage.id = "uno-loading-splash";
                        splashImage.classList.add("uno-splash");
                        unoLoading.appendChild(splashImage);
                        unoBody.appendChild(unoLoading);
                    }
                }
            }
            /**
                * Reads the window's search parameters
                *
                */
            static findLaunchArguments() {
                if (typeof URLSearchParams === "function") {
                    return new URLSearchParams(window.location.search).toString();
                }
                else {
                    const queryIndex = document.location.search.indexOf('?');
                    if (queryIndex !== -1) {
                        return document.location.search.substring(queryIndex + 1);
                    }
                    return "";
                }
            }
            /**
                * Create a html DOM element representing a Xaml element.
                *
                * You need to call addView to connect it to the DOM.
                */
            createContent(contentDefinition) {
                this.createContentInternal(contentDefinition);
                return "ok";
            }
            /**
                * Create a html DOM element representing a Xaml element.
                *
                * You need to call addView to connect it to the DOM.
                */
            createContentNative(pParams) {
                const params = WindowManagerCreateContentParams.unmarshal(pParams);
                const def = {
                    id: this.handleToString(params.HtmlId),
                    handle: params.Handle,
                    isFocusable: params.IsFocusable,
                    isSvg: params.IsSvg,
                    tagName: params.TagName,
                    uiElementRegistrationId: params.UIElementRegistrationId,
                };
                this.createContentInternal(def);
                return true;
            }
            createContentInternal(contentDefinition) {
                // Create the HTML element
                const element = contentDefinition.isSvg
                    ? document.createElementNS("http://www.w3.org/2000/svg", contentDefinition.tagName)
                    : document.createElement(contentDefinition.tagName);
                element.id = contentDefinition.id;
                const uiElementRegistration = this.uiElementRegistrations[this.handleToString(contentDefinition.uiElementRegistrationId)];
                if (!uiElementRegistration) {
                    throw `UIElement registration id ${contentDefinition.uiElementRegistrationId} is unknown.`;
                }
                element.setAttribute("XamlType", uiElementRegistration.typeName);
                element.setAttribute("XamlHandle", this.handleToString(contentDefinition.handle));
                if (uiElementRegistration.isFrameworkElement) {
                    this.setAsUnarranged(element);
                }
                if (element.hasOwnProperty("tabindex")) {
                    element["tabindex"] = contentDefinition.isFocusable ? 0 : -1;
                }
                else {
                    element.setAttribute("tabindex", contentDefinition.isFocusable ? "0" : "-1");
                }
                if (contentDefinition) {
                    let classes = element.classList.value;
                    for (const className of uiElementRegistration.classNames) {
                        classes += " uno-" + className;
                    }
                    element.classList.value = classes;
                }
                // Add the html element to list of elements
                this.allActiveElementsById[contentDefinition.id] = element;
            }
            registerUIElement(typeName, isFrameworkElement, classNames) {
                const registrationId = Object.keys(this.uiElementRegistrations).length;
                this.uiElementRegistrations[this.handleToString(registrationId)] = {
                    classNames: classNames,
                    isFrameworkElement: isFrameworkElement,
                    typeName: typeName,
                };
                return registrationId;
            }
            registerUIElementNative(pParams, pReturn) {
                const params = WindowManagerRegisterUIElementParams.unmarshal(pParams);
                const registrationId = this.registerUIElement(params.TypeName, params.IsFrameworkElement, params.Classes);
                const ret = new WindowManagerRegisterUIElementReturn();
                ret.RegistrationId = registrationId;
                ret.marshal(pReturn);
                return true;
            }
            getView(elementHandle) {
                const element = this.allActiveElementsById[this.handleToString(elementHandle)];
                if (!element) {
                    throw `Element id ${elementHandle} not found.`;
                }
                return element;
            }
            /**
                * Set a name for an element.
                *
                * This is mostly for diagnostic purposes.
                */
            setName(elementId, name) {
                this.setNameInternal(elementId, name);
                return "ok";
            }
            /**
                * Set a name for an element.
                *
                * This is mostly for diagnostic purposes.
                */
            setNameNative(pParam) {
                const params = WindowManagerSetNameParams.unmarshal(pParam);
                this.setNameInternal(params.HtmlId, params.Name);
                return true;
            }
            setNameInternal(elementId, name) {
                this.getView(elementId).setAttribute("xamlname", name);
            }
            /**
                * Set a name for an element.
                *
                * This is mostly for diagnostic purposes.
                */
            setXUid(elementId, name) {
                this.setXUidInternal(elementId, name);
                return "ok";
            }
            /**
                * Set a name for an element.
                *
                * This is mostly for diagnostic purposes.
                */
            setXUidNative(pParam) {
                const params = WindowManagerSetXUidParams.unmarshal(pParam);
                this.setXUidInternal(params.HtmlId, params.Uid);
                return true;
            }
            setXUidInternal(elementId, name) {
                this.getView(elementId).setAttribute("xuid", name);
            }
            /**
                * Set an attribute for an element.
                */
            setAttributes(elementId, attributes) {
                const element = this.getView(elementId);
                for (const name in attributes) {
                    if (attributes.hasOwnProperty(name)) {
                        element.setAttribute(name, attributes[name]);
                    }
                }
                return "ok";
            }
            /**
                * Set an attribute for an element.
                */
            setAttributesNative(pParams) {
                const params = WindowManagerSetAttributesParams.unmarshal(pParams);
                const element = this.getView(params.HtmlId);
                for (let i = 0; i < params.Pairs_Length; i += 2) {
                    element.setAttribute(params.Pairs[i], params.Pairs[i + 1]);
                }
                return true;
            }
            /**
                * Set an attribute for an element.
                */
            setAttributeNative(pParams) {
                const params = WindowManagerSetAttributeParams.unmarshal(pParams);
                const element = this.getView(params.HtmlId);
                element.setAttribute(params.Name, params.Value);
                return true;
            }
            /**
                * Removes an attribute for an element.
                */
            removeAttribute(elementId, name) {
                const element = this.getView(elementId);
                element.removeAttribute(name);
                return "ok";
            }
            /**
                * Removes an attribute for an element.
                */
            removeAttributeNative(pParams) {
                const params = WindowManagerRemoveAttributeParams.unmarshal(pParams);
                const element = this.getView(params.HtmlId);
                element.removeAttribute(params.Name);
                return true;
            }
            /**
                * Get an attribute for an element.
                */
            getAttribute(elementId, name) {
                return this.getView(elementId).getAttribute(name);
            }
            /**
                * Set a property for an element.
                */
            setProperty(elementId, properties) {
                const element = this.getView(elementId);
                for (const name in properties) {
                    if (properties.hasOwnProperty(name)) {
                        var setVal = properties[name];
                        if (setVal === "true") {
                            element[name] = true;
                        }
                        else if (setVal === "false") {
                            element[name] = false;
                        }
                        else {
                            element[name] = setVal;
                        }
                    }
                }
                return "ok";
            }
            /**
                * Set a property for an element.
                */
            setPropertyNative(pParams) {
                const params = WindowManagerSetPropertyParams.unmarshal(pParams);
                const element = this.getView(params.HtmlId);
                for (let i = 0; i < params.Pairs_Length; i += 2) {
                    var setVal = params.Pairs[i + 1];
                    if (setVal === "true") {
                        element[params.Pairs[i]] = true;
                    }
                    else if (setVal === "false") {
                        element[params.Pairs[i]] = false;
                    }
                    else {
                        element[params.Pairs[i]] = setVal;
                    }
                }
                return true;
            }
            /**
                * Get a property for an element.
                */
            getProperty(elementId, name) {
                const element = this.getView(elementId);
                return element[name] || "";
            }
            /**
                * Set the CSS style of a html element.
                *
                * To remove a value, set it to empty string.
                * @param styles A dictionary of styles to apply on html element.
                */
            setStyle(elementId, styles) {
                const element = this.getView(elementId);
                for (const style in styles) {
                    if (styles.hasOwnProperty(style)) {
                        element.style.setProperty(style, styles[style]);
                    }
                }
                return "ok";
            }
            /**
            * Set the CSS style of a html element.
            *
            * To remove a value, set it to empty string.
            * @param styles A dictionary of styles to apply on html element.
            */
            setStyleNative(pParams) {
                const params = WindowManagerSetStylesParams.unmarshal(pParams);
                const element = this.getView(params.HtmlId);
                const elementStyle = element.style;
                const pairs = params.Pairs;
                for (let i = 0; i < params.Pairs_Length; i += 2) {
                    const key = pairs[i];
                    const value = pairs[i + 1];
                    elementStyle.setProperty(key, value);
                }
                return true;
            }
            /**
            * Set a single CSS style of a html element
            *
            */
            setStyleDoubleNative(pParams) {
                const params = WindowManagerSetStyleDoubleParams.unmarshal(pParams);
                const element = this.getView(params.HtmlId);
                element.style.setProperty(params.Name, this.handleToString(params.Value));
                return true;
            }
            setArrangeProperties(elementId, clipToBounds) {
                const element = this.getView(elementId);
                this.setAsArranged(element);
                this.setClipToBounds(element, clipToBounds);
                return "ok";
            }
            /**
                * Remove the CSS style of a html element.
                */
            resetStyle(elementId, names) {
                this.resetStyleInternal(elementId, names);
                return "ok";
            }
            /**
                * Remove the CSS style of a html element.
                */
            resetStyleNative(pParams) {
                const params = WindowManagerResetStyleParams.unmarshal(pParams);
                this.resetStyleInternal(params.HtmlId, params.Styles);
                return true;
            }
            resetStyleInternal(elementId, names) {
                const element = this.getView(elementId);
                for (const name of names) {
                    element.style.setProperty(name, "");
                }
            }
            /**
             * Set + Unset CSS classes on an element
             */
            setUnsetClasses(elementId, cssClassesToSet, cssClassesToUnset) {
                const element = this.getView(elementId);
                if (cssClassesToSet) {
                    cssClassesToSet.forEach(c => {
                        element.classList.add(c);
                    });
                }
                if (cssClassesToUnset) {
                    cssClassesToUnset.forEach(c => {
                        element.classList.remove(c);
                    });
                }
            }
            setUnsetClassesNative(pParams) {
                const params = WindowManagerSetUnsetClassesParams.unmarshal(pParams);
                this.setUnsetClasses(params.HtmlId, params.CssClassesToSet, params.CssClassesToUnset);
                return true;
            }
            /**
             * Set CSS classes on an element from a specified list
             */
            setClasses(elementId, cssClassesList, classIndex) {
                const element = this.getView(elementId);
                for (let i = 0; i < cssClassesList.length; i++) {
                    if (i === classIndex) {
                        element.classList.add(cssClassesList[i]);
                    }
                    else {
                        element.classList.remove(cssClassesList[i]);
                    }
                }
                return "ok";
            }
            setClassesNative(pParams) {
                const params = WindowManagerSetClassesParams.unmarshal(pParams);
                this.setClasses(params.HtmlId, params.CssClasses, params.Index);
                return true;
            }
            /**
            * Arrange and clips a native elements
            *
            */
            arrangeElementNative(pParams) {
                const params = WindowManagerArrangeElementParams.unmarshal(pParams);
                const element = this.getView(params.HtmlId);
                const style = element.style;
                style.position = "absolute";
                style.top = params.Top + "px";
                style.left = params.Left + "px";
                style.width = params.Width === NaN ? "auto" : params.Width + "px";
                style.height = params.Height === NaN ? "auto" : params.Height + "px";
                if (params.Clip) {
                    style.clip = `rect(${params.ClipTop}px, ${params.ClipRight}px, ${params.ClipBottom}px, ${params.ClipLeft}px)`;
                }
                else {
                    style.clip = "";
                }
                this.setAsArranged(element);
                this.setClipToBounds(element, params.ClipToBounds);
                return true;
            }
            setAsArranged(element) {
                element.classList.remove(WindowManager.unoUnarrangedClassName);
            }
            setAsUnarranged(element) {
                element.classList.add(WindowManager.unoUnarrangedClassName);
            }
            setClipToBounds(element, clipToBounds) {
                if (clipToBounds) {
                    element.classList.add(WindowManager.unoClippedToBoundsClassName);
                }
                else {
                    element.classList.remove(WindowManager.unoClippedToBoundsClassName);
                }
            }
            /**
            * Sets the transform matrix of an element
            *
            */
            setElementTransformNative(pParams) {
                const params = WindowManagerSetElementTransformParams.unmarshal(pParams);
                const element = this.getView(params.HtmlId);
                var style = element.style;
                const matrix = `matrix(${params.M11},${params.M12},${params.M21},${params.M22},${params.M31},${params.M32})`;
                style.transform = matrix;
                this.setAsArranged(element);
                this.setClipToBounds(element, params.ClipToBounds);
                return true;
            }
            setPointerEvents(htmlId, enabled) {
                const element = this.getView(htmlId);
                element.style.pointerEvents = enabled ? "auto" : "none";
            }
            setPointerEventsNative(pParams) {
                const params = WindowManagerSetPointerEventsParams.unmarshal(pParams);
                this.setPointerEvents(params.HtmlId, params.Enabled);
                return true;
            }
            /**
                * Load the specified URL into a new tab or window
                * @param url URL to load
                * @returns "True" or "False", depending on whether a new window could be opened or not
                */
            open(url) {
                const newWindow = window.open(url, "_blank");
                return newWindow != null
                    ? "True"
                    : "False";
            }
            /**
                * Issue a browser alert to user
                * @param message message to display
                */
            alert(message) {
                window.alert(message);
                return "ok";
            }
            /**
                * Sets the browser window title
                * @param message the new title
                */
            setWindowTitle(title) {
                document.title = title || UnoAppManifest.displayName;
                return "ok";
            }
            /**
                * Gets the currently set browser window title
                */
            getWindowTitle() {
                return document.title || UnoAppManifest.displayName;
            }
            /**
                * Add an event handler to a html element.
                *
                * @param eventName The name of the event
                * @param onCapturePhase true means "on trickle down" (going down to target), false means "on bubble up" (bubbling back to ancestors). Default is false.
                */
            registerEventOnView(elementId, eventName, onCapturePhase = false, eventExtractorId) {
                this.registerEventOnViewInternal(elementId, eventName, onCapturePhase, eventExtractorId);
                return "ok";
            }
            /**
                * Add an event handler to a html element.
                *
                * @param eventName The name of the event
                * @param onCapturePhase true means "on trickle down", false means "on bubble up". Default is false.
                */
            registerEventOnViewNative(pParams) {
                const params = WindowManagerRegisterEventOnViewParams.unmarshal(pParams);
                this.registerEventOnViewInternal(params.HtmlId, params.EventName, params.OnCapturePhase, params.EventExtractorId);
                return true;
            }
            registerPointerEventsOnView(pParams) {
                const params = WindowManagerRegisterEventOnViewParams.unmarshal(pParams);
                const element = this.getView(params.HtmlId);
                element.addEventListener("pointerenter", WindowManager.onPointerEnterReceived);
                element.addEventListener("pointerleave", WindowManager.onPointerLeaveReceived);
                element.addEventListener("pointerdown", WindowManager.onPointerEventReceived);
                element.addEventListener("pointerup", WindowManager.onPointerEventReceived);
                element.addEventListener("pointercancel", WindowManager.onPointerEventReceived);
            }
            static onPointerEventReceived(evt) {
                const element = evt.currentTarget;
                const payload = WindowManager.pointerEventExtractor(evt);
                const handled = WindowManager.current.dispatchEvent(element, evt.type, payload);
                if (handled) {
                    evt.stopPropagation();
                }
            }
            static onPointerEnterReceived(evt) {
                const element = evt.target;
                const e = evt;
                if (e.explicitOriginalTarget) { // FF only
                    // It happens on FF that when another control which is over the 'element' has been updated, like text or visibility changed,
                    // we receive a pointer enter/leave of an element which is under an element that is capable to handle pointers,
                    // which is unexpected as the "pointerenter" should not bubble.
                    // So we have to validate that this event is effectively due to the pointer entering the control.
                    // We achieve this by browsing up the elements under the pointer (** not the visual tree**) 
                    for (let elt of document.elementsFromPoint(evt.pageX, evt.pageY)) {
                        if (elt == element) {
                            // We found our target element, we can raise the event and stop the loop
                            WindowManager.onPointerEventReceived(evt);
                            return;
                        }
                        let htmlElt = elt;
                        if (htmlElt.style.pointerEvents != "none") {
                            // This 'htmlElt' is handling the pointers events, this mean that we can stop the loop.
                            // However, if this 'htmlElt' is one of our child it means that the event was legitimate
                            // and we have to raise it for the 'element'.
                            while (htmlElt.parentElement) {
                                htmlElt = htmlElt.parentElement;
                                if (htmlElt == element) {
                                    WindowManager.onPointerEventReceived(evt);
                                    return;
                                }
                            }
                            // We found an element this is capable to handle the pointers but which is not one of our child
                            // (probably a sibling which is covering the element). It means that the pointerEnter/Leave should
                            // not have bubble to the element, and we can mute it.
                            return;
                        }
                    }
                }
                else {
                    WindowManager.onPointerEventReceived(evt);
                }
            }
            static onPointerLeaveReceived(evt) {
                const element = evt.target;
                const e = evt;
                if (e.explicitOriginalTarget // FF only
                    && e.explicitOriginalTarget !== event.currentTarget
                    && event.isOver(element)) {
                    // If the event was re-targeted, it's suspicious as the leave event should not bubble
                    // This happens on FF when another control which is over the 'element' has been updated, like text or visibility changed.
                    // So we have to validate that this event is effectively due to the pointer leaving the element.
                    // We achieve that by buffering it until the next few 'pointermove' on document for which we validate the new pointer location.
                    // It's common to get a move right after the leave with the same pointer's location,
                    // so we wait up to 3 pointer move before dropping the leave event.
                    var attempt = 3;
                    WindowManager.current.ensurePendingLeaveEventProcessing();
                    WindowManager.current.processPendingLeaveEvent = (move) => {
                        if (!move.isOverDeep(element)) {
                            // Raising deferred pointerleave on element " + element.id);
                            WindowManager.onPointerEventReceived(evt);
                            WindowManager.current.processPendingLeaveEvent = null;
                        }
                        else if (--attempt <= 0) {
                            // Drop deferred pointerleave on element " + element.id);
                            WindowManager.current.processPendingLeaveEvent = null;
                        }
                        else {
                            // Requeue deferred pointerleave on element " + element.id);
                        }
                    };
                }
                else {
                    WindowManager.onPointerEventReceived(evt);
                }
            }
            /**
             * Ensure that any pending leave event are going to be processed (cf @see processPendingLeaveEvent )
             */
            ensurePendingLeaveEventProcessing() {
                if (this._isPendingLeaveProcessingEnabled) {
                    return;
                }
                // Register an event listener on move in order to process any pending event (leave).
                document.addEventListener("pointermove", evt => {
                    if (this.processPendingLeaveEvent) {
                        this.processPendingLeaveEvent(evt);
                    }
                }, true); // in the capture phase to get it as soon as possible, and to make sure to respect the events ordering
                this._isPendingLeaveProcessingEnabled = true;
            }
            /**
                * Add an event handler to a html element.
                *
                * @param eventName The name of the event
                * @param onCapturePhase true means "on trickle down", false means "on bubble up". Default is false.
                */
            registerEventOnViewInternal(elementId, eventName, onCapturePhase = false, eventExtractorId) {
                const element = this.getView(elementId);
                const eventExtractor = this.getEventExtractor(eventExtractorId);
                const eventHandler = (event) => {
                    const eventPayload = eventExtractor
                        ? `${eventExtractor(event)}`
                        : "";
                    var handled = this.dispatchEvent(element, eventName, eventPayload);
                    if (handled) {
                        event.stopPropagation();
                    }
                };
                element.addEventListener(eventName, eventHandler, onCapturePhase);
            }
            /**
             * pointer event extractor to be used with registerEventOnView
             * @param evt
             */
            static pointerEventExtractor(evt) {
                if (!evt) {
                    return "";
                }
                let src = evt.target;
                if (src) {
                    // The XAML SvgElement are UIElement in Uno (so they have a XamlHandle),
                    // but as on WinUI they are not part of the visual tree, they should not be used as OriginalElement.
                    // Instead we should use the actual parent <svg /> which is the XAML Shape.
                    const shape = src.ownerSVGElement;
                    if (shape) {
                        src = shape;
                    }
                }
                let srcHandle = "0";
                while (src) {
                    let handle = src.getAttribute("XamlHandle");
                    if (handle) {
                        srcHandle = handle;
                        break;
                    }
                    src = src.parentElement;
                }
                let pointerId, pointerType, pressure;
                let wheelDeltaX, wheelDeltaY;
                if (evt instanceof WheelEvent) {
                    pointerId = evt.mozInputSource ? 0 : 1; // Try to match the mouse pointer ID 0 for FF, 1 for others
                    pointerType = "mouse";
                    pressure = 0.5; // like WinUI
                    wheelDeltaX = evt.deltaX;
                    wheelDeltaY = evt.deltaY;
                    switch (evt.deltaMode) {
                        case WheelEvent.DOM_DELTA_LINE: // Actually this is supported only by FF
                            const lineSize = WindowManager.wheelLineSize;
                            wheelDeltaX *= lineSize;
                            wheelDeltaY *= lineSize;
                            break;
                        case WheelEvent.DOM_DELTA_PAGE:
                            wheelDeltaX *= document.documentElement.clientWidth;
                            wheelDeltaY *= document.documentElement.clientHeight;
                            break;
                    }
                }
                else {
                    pointerId = evt.pointerId;
                    pointerType = evt.pointerType;
                    pressure = evt.pressure;
                    wheelDeltaX = 0;
                    wheelDeltaY = 0;
                }
                return `${pointerId};${evt.clientX};${evt.clientY};${(evt.ctrlKey ? "1" : "0")};${(evt.shiftKey ? "1" : "0")};${evt.buttons};${evt.button};${pointerType};${srcHandle};${evt.timeStamp};${pressure};${wheelDeltaX};${wheelDeltaY}`;
            }
            static get wheelLineSize() {
                // In web browsers, scroll might happen by pixels, line or page.
                // But WinUI works only with pixels, so we have to convert it before send the value to the managed code.
                // The issue is that there is no easy way get the "size of a line", instead we have to determine the CSS "line-height"
                // defined in the browser settings. 
                // https://stackoverflow.com/questions/20110224/what-is-the-height-of-a-line-in-a-wheel-event-deltamode-dom-delta-line
                if (this._wheelLineSize == undefined) {
                    const el = document.createElement('div');
                    el.style.fontSize = 'initial';
                    el.style.display = 'none';
                    document.body.appendChild(el);
                    const fontSize = window.getComputedStyle(el).fontSize;
                    document.body.removeChild(el);
                    this._wheelLineSize = fontSize ? parseInt(fontSize) : 16; /* 16 = The current common default font size */
                    // Based on observations, even if the event reports 3 lines (the settings of windows),
                    // the browser will actually scroll of about 6 lines of text.
                    this._wheelLineSize *= 2.0;
                }
                return this._wheelLineSize;
            }
            /**
             * keyboard event extractor to be used with registerEventOnView
             * @param evt
             */
            keyboardEventExtractor(evt) {
                return (evt instanceof KeyboardEvent) ? evt.key : "0";
            }
            /**
             * tapped (mouse clicked / double clicked) event extractor to be used with registerEventOnView
             * @param evt
             */
            tappedEventExtractor(evt) {
                return evt
                    ? `0;${evt.clientX};${evt.clientY};${(evt.ctrlKey ? "1" : "0")};${(evt.shiftKey ? "1" : "0")};${evt.button};mouse`
                    : "";
            }
            /**
             * focus event extractor to be used with registerEventOnView
             * @param evt
             */
            focusEventExtractor(evt) {
                if (evt) {
                    const targetElement = evt.target;
                    if (targetElement) {
                        const targetXamlHandle = targetElement.getAttribute("XamlHandle");
                        if (targetXamlHandle) {
                            return `${targetXamlHandle}`;
                        }
                    }
                }
                return "";
            }
            customEventDetailExtractor(evt) {
                if (evt) {
                    const detail = evt.detail;
                    if (detail) {
                        return JSON.stringify(detail);
                    }
                }
                return "";
            }
            customEventDetailStringExtractor(evt) {
                return evt ? `${evt.detail}` : "";
            }
            /**
             * Gets the event extractor function. See UIElement.HtmlEventExtractor
             * @param eventExtractorName an event extractor name.
             */
            getEventExtractor(eventExtractorId) {
                if (eventExtractorId) {
                    //
                    // NOTE TO MAINTAINERS: Keep in sync with Windows.UI.Xaml.UIElement.HtmlEventExtractor
                    //
                    switch (eventExtractorId) {
                        case 1:
                            return WindowManager.pointerEventExtractor;
                        case 3:
                            return this.keyboardEventExtractor;
                        case 2:
                            return this.tappedEventExtractor;
                        case 4:
                            return this.focusEventExtractor;
                        case 6:
                            return this.customEventDetailExtractor;
                        case 5:
                            return this.customEventDetailStringExtractor;
                    }
                    throw `Event extractor ${eventExtractorId} is not supported`;
                }
                return null;
            }
            /**
                * Set or replace the root content element.
                */
            setRootContent(elementId) {
                if (this.rootContent && Number(this.rootContent.id) === elementId) {
                    return null; // nothing to do
                }
                if (this.rootContent) {
                    // Remove existing
                    this.containerElement.removeChild(this.rootContent);
                    if (WindowManager.isLoadEventsEnabled) {
                        this.dispatchEvent(this.rootContent, "unloaded");
                    }
                    this.rootContent.classList.remove(WindowManager.unoRootClassName);
                }
                if (!elementId) {
                    return null;
                }
                // set new root
                const newRootElement = this.getView(elementId);
                newRootElement.classList.add(WindowManager.unoRootClassName);
                this.rootContent = newRootElement;
                if (WindowManager.isLoadEventsEnabled) {
                    this.dispatchEvent(this.rootContent, "loading");
                }
                this.containerElement.appendChild(this.rootContent);
                if (WindowManager.isLoadEventsEnabled) {
                    this.dispatchEvent(this.rootContent, "loaded");
                }
                this.setAsArranged(newRootElement); // patch because root is not measured/arranged
                this.resize();
                return "ok";
            }
            /**
                * Set a view as a child of another one.
                *
                * "Loading" & "Loaded" events will be raised if necessary.
                *
                * @param index Position in children list. Appended at end if not specified.
                */
            addView(parentId, childId, index) {
                this.addViewInternal(parentId, childId, index);
                return "ok";
            }
            /**
                * Set a view as a child of another one.
                *
                * "Loading" & "Loaded" events will be raised if necessary.
                *
                * @param pParams Pointer to a WindowManagerAddViewParams native structure.
                */
            addViewNative(pParams) {
                const params = WindowManagerAddViewParams.unmarshal(pParams);
                this.addViewInternal(params.HtmlId, params.ChildView, params.Index != -1 ? params.Index : null);
                return true;
            }
            addViewInternal(parentId, childId, index) {
                const parentElement = this.getView(parentId);
                const childElement = this.getView(childId);
                let shouldRaiseLoadEvents = false;
                if (WindowManager.isLoadEventsEnabled) {
                    const alreadyLoaded = this.getIsConnectedToRootElement(childElement);
                    shouldRaiseLoadEvents = !alreadyLoaded && this.getIsConnectedToRootElement(parentElement);
                    if (shouldRaiseLoadEvents) {
                        this.dispatchEvent(childElement, "loading");
                    }
                }
                if (index != null && index < parentElement.childElementCount) {
                    const insertBeforeElement = parentElement.children[index];
                    parentElement.insertBefore(childElement, insertBeforeElement);
                }
                else {
                    parentElement.appendChild(childElement);
                }
                if (shouldRaiseLoadEvents) {
                    this.dispatchEvent(childElement, "loaded");
                }
            }
            /**
                * Remove a child from a parent element.
                *
                * "Unloading" & "Unloaded" events will be raised if necessary.
                */
            removeView(parentId, childId) {
                this.removeViewInternal(parentId, childId);
                return "ok";
            }
            /**
                * Remove a child from a parent element.
                *
                * "Unloading" & "Unloaded" events will be raised if necessary.
                */
            removeViewNative(pParams) {
                const params = WindowManagerRemoveViewParams.unmarshal(pParams);
                this.removeViewInternal(params.HtmlId, params.ChildView);
                return true;
            }
            removeViewInternal(parentId, childId) {
                const parentElement = this.getView(parentId);
                const childElement = this.getView(childId);
                const shouldRaiseLoadEvents = WindowManager.isLoadEventsEnabled
                    && this.getIsConnectedToRootElement(childElement);
                parentElement.removeChild(childElement);
                // Mark the element as unarranged, so if it gets measured while being
                // disconnected from the root element, it won't be visible.
                this.setAsUnarranged(childElement);
                if (shouldRaiseLoadEvents) {
                    this.dispatchEvent(childElement, "unloaded");
                }
            }
            /**
                * Destroy a html element.
                *
                * The element won't be available anymore. Usually indicate the managed
                * version has been scavenged by the GC.
                */
            destroyView(elementId) {
                this.destroyViewInternal(elementId);
                return "ok";
            }
            /**
                * Destroy a html element.
                *
                * The element won't be available anymore. Usually indicate the managed
                * version has been scavenged by the GC.
                */
            destroyViewNative(pParams) {
                const params = WindowManagerDestroyViewParams.unmarshal(pParams);
                this.destroyViewInternal(params.HtmlId);
                return true;
            }
            destroyViewInternal(elementId) {
                const element = this.getView(elementId);
                if (element.parentElement) {
                    element.parentElement.removeChild(element);
                }
                delete this.allActiveElementsById[elementId];
            }
            getBoundingClientRect(elementId) {
                const bounds = this.getView(elementId).getBoundingClientRect();
                return `${bounds.left};${bounds.top};${bounds.right - bounds.left};${bounds.bottom - bounds.top}`;
            }
            getBBox(elementId) {
                const bbox = this.getBBoxInternal(elementId);
                return `${bbox.x};${bbox.y};${bbox.width};${bbox.height}`;
            }
            getBBoxNative(pParams, pReturn) {
                const params = WindowManagerGetBBoxParams.unmarshal(pParams);
                const bbox = this.getBBoxInternal(params.HtmlId);
                const ret = new WindowManagerGetBBoxReturn();
                ret.X = bbox.x;
                ret.Y = bbox.y;
                ret.Width = bbox.width;
                ret.Height = bbox.height;
                ret.marshal(pReturn);
                return true;
            }
            getBBoxInternal(elementId) {
                return this.getView(elementId).getBBox();
            }
            setSvgElementRect(pParams) {
                const params = WindowManagerSetSvgElementRectParams.unmarshal(pParams);
                const element = this.getView(params.HtmlId);
                element.x.baseVal.value = params.X;
                element.y.baseVal.value = params.Y;
                element.width.baseVal.value = params.Width;
                element.height.baseVal.value = params.Height;
                return true;
            }
            /**
                * Use the Html engine to measure the element using specified constraints.
                *
                * @param maxWidth string containing width in pixels. Empty string means infinite.
                * @param maxHeight string containing height in pixels. Empty string means infinite.
                */
            measureView(viewId, maxWidth, maxHeight) {
                const ret = this.measureViewInternal(Number(viewId), maxWidth ? Number(maxWidth) : NaN, maxHeight ? Number(maxHeight) : NaN);
                return `${ret[0]};${ret[1]}`;
            }
            /**
                * Use the Html engine to measure the element using specified constraints.
                *
                * @param maxWidth string containing width in pixels. Empty string means infinite.
                * @param maxHeight string containing height in pixels. Empty string means infinite.
                */
            measureViewNative(pParams, pReturn) {
                const params = WindowManagerMeasureViewParams.unmarshal(pParams);
                const ret = this.measureViewInternal(params.HtmlId, params.AvailableWidth, params.AvailableHeight);
                const ret2 = new WindowManagerMeasureViewReturn();
                ret2.DesiredWidth = ret[0];
                ret2.DesiredHeight = ret[1];
                ret2.marshal(pReturn);
                return true;
            }
            measureElement(element) {
                const offsetWidth = element.offsetWidth;
                const offsetHeight = element.offsetHeight;
                const resultWidth = offsetWidth ? offsetWidth : element.clientWidth;
                const resultHeight = offsetHeight ? offsetHeight : element.clientHeight;
                // +1 is added to take rounding/flooring into account
                return [resultWidth + 1, resultHeight];
            }
            measureViewInternal(viewId, maxWidth, maxHeight) {
                const element = this.getView(viewId);
                const elementStyle = element.style;
                const originalStyleCssText = elementStyle.cssText;
                let parentElement = null;
                let parentElementWidthHeight = null;
                let unconnectedRoot = null;
                let cleanupUnconnectedRoot = function (owner) {
                    if (unconnectedRoot !== null) {
                        owner.removeChild(unconnectedRoot);
                    }
                };
                try {
                    if (!element.isConnected) {
                        // If the element is not connected to the DOM, we need it
                        // to be connected for the measure to provide a meaningful value.
                        unconnectedRoot = element;
                        while (unconnectedRoot.parentElement) {
                            // Need to find the top most "unconnected" parent
                            // of this element
                            unconnectedRoot = unconnectedRoot.parentElement;
                        }
                        this.containerElement.appendChild(unconnectedRoot);
                    }
                    // As per W3C css-transform spec:
                    // https://www.w3.org/TR/css-transforms-1/#propdef-transform
                    //
                    // > For elements whose layout is governed by the CSS box model, any value other than none
                    // > for the transform property also causes the element to establish a containing block for
                    // > all descendants.Its padding box will be used to layout for all of its
                    // > absolute - position descendants, fixed - position descendants, and descendant fixed
                    // > background attachments.
                    //
                    // We use this feature to allow an measure of text without being influenced by the bounds
                    // of the viewport. We just need to temporary set both the parent width & height to a very big value.
                    parentElement = element.parentElement;
                    parentElementWidthHeight = { width: parentElement.style.width, height: parentElement.style.height };
                    parentElement.style.width = WindowManager.MAX_WIDTH;
                    parentElement.style.height = WindowManager.MAX_HEIGHT;
                    const updatedStyles = {};
                    for (let i = 0; i < elementStyle.length; i++) {
                        const key = elementStyle[i];
                        updatedStyles[key] = elementStyle.getPropertyValue(key);
                    }
                    if (updatedStyles.hasOwnProperty("width")) {
                        delete updatedStyles.width;
                    }
                    if (updatedStyles.hasOwnProperty("height")) {
                        delete updatedStyles.height;
                    }
                    // This is required for an unconstrained measure (otherwise the parents size is taken into account)
                    updatedStyles.position = "fixed";
                    updatedStyles["max-width"] = Number.isFinite(maxWidth) ? maxWidth + "px" : "none";
                    updatedStyles["max-height"] = Number.isFinite(maxHeight) ? maxHeight + "px" : "none";
                    let updatedStyleString = "";
                    for (let key in updatedStyles) {
                        if (updatedStyles.hasOwnProperty(key)) {
                            updatedStyleString += key + ": " + updatedStyles[key] + "; ";
                        }
                    }
                    // We use a string to prevent the browser to update the element between
                    // each style assignation. This way, the browser will update the element only once.
                    elementStyle.cssText = updatedStyleString;
                    if (element instanceof HTMLImageElement) {
                        const imgElement = element;
                        return [imgElement.naturalWidth, imgElement.naturalHeight];
                    }
                    else if (element instanceof HTMLInputElement) {
                        const inputElement = element;
                        cleanupUnconnectedRoot(this.containerElement);
                        // Create a temporary element that will contain the input's content
                        var textOnlyElement = document.createElement("p");
                        textOnlyElement.style.cssText = updatedStyleString;
                        textOnlyElement.innerText = inputElement.value;
                        unconnectedRoot = textOnlyElement;
                        this.containerElement.appendChild(unconnectedRoot);
                        var textSize = this.measureElement(textOnlyElement);
                        var inputSize = this.measureElement(element);
                        // Take the width of the inner text, but keep the height of the input element.
                        return [textSize[0], inputSize[1]];
                    }
                    else {
                        return this.measureElement(element);
                    }
                }
                finally {
                    elementStyle.cssText = originalStyleCssText;
                    if (parentElement && parentElementWidthHeight) {
                        parentElement.style.width = parentElementWidthHeight.width;
                        parentElement.style.height = parentElementWidthHeight.height;
                    }
                    cleanupUnconnectedRoot(this.containerElement);
                }
            }
            scrollTo(pParams) {
                const params = WindowManagerScrollToOptionsParams.unmarshal(pParams);
                const elt = this.getView(params.HtmlId);
                const opts = ({
                    left: params.HasLeft ? params.Left : undefined,
                    top: params.HasTop ? params.Top : undefined,
                    behavior: (params.DisableAnimation ? "auto" : "smooth")
                });
                elt.scrollTo(opts);
                return true;
            }
            rawPixelsToBase64EncodeImage(dataPtr, width, height) {
                const rawCanvas = document.createElement("canvas");
                rawCanvas.width = width;
                rawCanvas.height = height;
                const ctx = rawCanvas.getContext("2d");
                const imgData = ctx.createImageData(width, height);
                const bufferSize = width * height * 4;
                for (let i = 0; i < bufferSize; i += 4) {
                    imgData.data[i + 0] = Module.HEAPU8[dataPtr + i + 2];
                    imgData.data[i + 1] = Module.HEAPU8[dataPtr + i + 1];
                    imgData.data[i + 2] = Module.HEAPU8[dataPtr + i + 0];
                    imgData.data[i + 3] = Module.HEAPU8[dataPtr + i + 3];
                }
                ctx.putImageData(imgData, 0, 0);
                return rawCanvas.toDataURL();
            }
            /**
             * Sets the provided image with a mono-chrome version of the provided url.
             * @param viewId the image to manipulate
             * @param url the source image
             * @param color the color to apply to the monochrome pixels
             */
            setImageAsMonochrome(viewId, url, color) {
                const element = this.getView(viewId);
                if (element.tagName.toUpperCase() === "IMG") {
                    const imgElement = element;
                    var img = new Image();
                    img.onload = buildMonochromeImage;
                    img.src = url;
                    function buildMonochromeImage() {
                        // create a colored version of img
                        const c = document.createElement('canvas');
                        const ctx = c.getContext('2d');
                        c.width = img.width;
                        c.height = img.height;
                        ctx.drawImage(img, 0, 0);
                        ctx.globalCompositeOperation = 'source-atop';
                        ctx.fillStyle = color;
                        ctx.fillRect(0, 0, img.width, img.height);
                        ctx.globalCompositeOperation = 'source-over';
                        imgElement.src = c.toDataURL();
                    }
                    return "ok";
                }
                else {
                    throw `setImageAsMonochrome: Element id ${viewId} is not an Img.`;
                }
            }
            setPointerCapture(viewId, pointerId) {
                this.getView(viewId).setPointerCapture(pointerId);
                return "ok";
            }
            releasePointerCapture(viewId, pointerId) {
                this.getView(viewId).releasePointerCapture(pointerId);
                return "ok";
            }
            focusView(elementId) {
                const element = this.getView(elementId);
                if (!(element instanceof HTMLElement)) {
                    throw `Element id ${elementId} is not focusable.`;
                }
                element.focus();
                return "ok";
            }
            /**
                * Set the Html content for an element.
                *
                * Those html elements won't be available as XamlElement in managed code.
                * WARNING: you should avoid mixing this and `addView` for the same element.
                */
            setHtmlContent(viewId, html) {
                this.setHtmlContentInternal(viewId, html);
                return "ok";
            }
            /**
                * Set the Html content for an element.
                *
                * Those html elements won't be available as XamlElement in managed code.
                * WARNING: you should avoid mixing this and `addView` for the same element.
                */
            setHtmlContentNative(pParams) {
                const params = WindowManagerSetContentHtmlParams.unmarshal(pParams);
                this.setHtmlContentInternal(params.HtmlId, params.Html);
                return true;
            }
            setHtmlContentInternal(viewId, html) {
                this.getView(viewId).innerHTML = html;
            }
            /**
             * Gets the Client and Offset size of the specified element
             *
             * This method is used to determine the size of the scroll bars, to
             * mask the events coming from that zone.
             */
            getClientViewSize(elementId) {
                const element = this.getView(elementId);
                return `${element.clientWidth};${element.clientHeight};${element.offsetWidth};${element.offsetHeight}`;
            }
            /**
             * Gets the Client and Offset size of the specified element
             *
             * This method is used to determine the size of the scroll bars, to
             * mask the events coming from that zone.
             */
            getClientViewSizeNative(pParams, pReturn) {
                const params = WindowManagerGetClientViewSizeParams.unmarshal(pParams);
                const element = this.getView(params.HtmlId);
                const ret2 = new WindowManagerGetClientViewSizeReturn();
                ret2.ClientWidth = element.clientWidth;
                ret2.ClientHeight = element.clientHeight;
                ret2.OffsetWidth = element.offsetWidth;
                ret2.OffsetHeight = element.offsetHeight;
                ret2.marshal(pReturn);
                return true;
            }
            /**
             * Gets a dependency property value.
             *
             * Note that the casing of this method is intentionally Pascal for platform alignment.
             */
            GetDependencyPropertyValue(elementId, propertyName) {
                if (!WindowManager.getDependencyPropertyValueMethod) {
                    WindowManager.getDependencyPropertyValueMethod = Module.mono_bind_static_method("[Uno.UI] Uno.UI.Helpers.Automation:GetDependencyPropertyValue");
                }
                const element = this.getView(elementId);
                const htmlId = Number(element.getAttribute("XamlHandle"));
                return WindowManager.getDependencyPropertyValueMethod(htmlId, propertyName);
            }
            /**
             * Sets a dependency property value.
             *
             * Note that the casing of this method is intentionally Pascal for platform alignment.
             */
            SetDependencyPropertyValue(elementId, propertyNameAndValue) {
                if (!WindowManager.setDependencyPropertyValueMethod) {
                    WindowManager.setDependencyPropertyValueMethod = Module.mono_bind_static_method("[Uno.UI] Uno.UI.Helpers.Automation:SetDependencyPropertyValue");
                }
                const element = this.getView(elementId);
                const htmlId = Number(element.getAttribute("XamlHandle"));
                return WindowManager.setDependencyPropertyValueMethod(htmlId, propertyNameAndValue);
            }
            /**
                * Remove the loading indicator.
                *
                * In a future version it will also handle the splashscreen.
                */
            activate() {
                this.removeLoading();
                return "ok";
            }
            init() {
                if (UnoAppManifest.displayName) {
                    document.title = UnoAppManifest.displayName;
                }
                window.addEventListener("beforeunload", () => WindowManager.dispatchSuspendingMethod());
            }
            static initMethods() {
                if (WindowManager.isHosted) {
                    console.debug("Hosted Mode: Skipping MonoRuntime initialization ");
                }
                else {
                    if (!WindowManager.resizeMethod) {
                        WindowManager.resizeMethod = Module.mono_bind_static_method("[Uno.UI] Windows.UI.Xaml.Window:Resize");
                    }
                    if (!WindowManager.dispatchEventMethod) {
                        WindowManager.dispatchEventMethod = Module.mono_bind_static_method("[Uno.UI] Windows.UI.Xaml.UIElement:DispatchEvent");
                    }
                    if (!WindowManager.focusInMethod) {
                        WindowManager.focusInMethod = Module.mono_bind_static_method("[Uno.UI] Windows.UI.Xaml.Input.FocusManager:ReceiveFocusNative");
                    }
                    if (!WindowManager.dispatchSuspendingMethod) {
                        WindowManager.dispatchSuspendingMethod = Module.mono_bind_static_method("[Uno.UI] Windows.UI.Xaml.Application:DispatchSuspending");
                    }
                }
            }
            initDom() {
                this.containerElement = document.getElementById(this.containerElementId);
                if (!this.containerElement) {
                    // If not found, we simply create a new one.
                    this.containerElement = document.createElement("div");
                }
                document.body.addEventListener("focusin", this.onfocusin);
                document.body.appendChild(this.containerElement);
                window.addEventListener("resize", x => this.resize());
                window.addEventListener("contextmenu", x => {
                    if (!(x.target instanceof HTMLInputElement)) {
                        x.preventDefault();
                    }
                });
                window.addEventListener("blur", this.onWindowBlur);
            }
            removeLoading() {
                if (!this.loadingElementId) {
                    return;
                }
                const element = document.getElementById(this.loadingElementId);
                if (element) {
                    element.parentElement.removeChild(element);
                }
                // UWP Window's default background is white.
                const body = document.getElementsByTagName("body")[0];
                body.style.backgroundColor = "#fff";
            }
            resize() {
                if (WindowManager.isHosted) {
                    UnoDispatch.resize(`${document.documentElement.clientWidth};${document.documentElement.clientHeight}`);
                }
                else {
                    WindowManager.resizeMethod(document.documentElement.clientWidth, document.documentElement.clientHeight);
                }
            }
            onfocusin(event) {
                if (WindowManager.isHosted) {
                    console.warn("Focus not supported in hosted mode");
                }
                else {
                    const newFocus = event.target;
                    const handle = newFocus.getAttribute("XamlHandle");
                    const htmlId = handle ? Number(handle) : -1; // newFocus may not be an Uno element
                    WindowManager.focusInMethod(htmlId);
                }
            }
            onWindowBlur() {
                if (WindowManager.isHosted) {
                    console.warn("Focus not supported in hosted mode");
                }
                else {
                    // Unset managed focus when Window loses focus
                    WindowManager.focusInMethod(-1);
                }
            }
            dispatchEvent(element, eventName, eventPayload = null) {
                const htmlId = Number(element.getAttribute("XamlHandle"));
                // console.debug(`${element.getAttribute("id")}: Raising event ${eventName}.`);
                if (!htmlId) {
                    throw `No attribute XamlHandle on element ${element}. Can't raise event.`;
                }
                if (WindowManager.isHosted) {
                    // Dispatch to the C# backed UnoDispatch class. Events propagated
                    // this way always succeed because synchronous calls are not possible
                    // between the host and the browser, unlike wasm.
                    UnoDispatch.dispatch(this.handleToString(htmlId), eventName, eventPayload);
                    return true;
                }
                else {
                    return WindowManager.dispatchEventMethod(htmlId, eventName, eventPayload || "");
                }
            }
            getIsConnectedToRootElement(element) {
                const rootElement = this.rootContent;
                if (!rootElement) {
                    return false;
                }
                return rootElement === element || rootElement.contains(element);
            }
            handleToString(handle) {
                // Fastest conversion as of 2020-03-25 (when compared to String(handle) or handle.toString())
                return handle + "";
            }
            setCursor(cssCursor) {
                const unoBody = document.getElementById(this.containerElementId);
                if (unoBody) {
                    //always cleanup
                    if (this.cursorStyleElement != undefined) {
                        this.cursorStyleElement.remove();
                        this.cursorStyleElement = undefined;
                    }
                    //only add custom overriding style if not auto 
                    if (cssCursor != "auto") {
                        // this part is only to override default css:  .uno-buttonbase {cursor: pointer;}
                        this.cursorStyleElement = document.createElement("style");
                        this.cursorStyleElement.innerHTML = ".uno-buttonbase { cursor: " + cssCursor + "; }";
                        document.body.appendChild(this.cursorStyleElement);
                    }
                    unoBody.style.cursor = cssCursor;
                }
                return "ok";
            }
        }
        WindowManager._isHosted = false;
        WindowManager._isLoadEventsEnabled = false;
        WindowManager.unoRootClassName = "uno-root-element";
        WindowManager.unoUnarrangedClassName = "uno-unarranged";
        WindowManager.unoClippedToBoundsClassName = "uno-clippedToBounds";
        WindowManager._cctor = (() => {
            WindowManager.initMethods();
            UI.HtmlDom.initPolyfills();
        })();
        WindowManager._wheelLineSize = undefined;
        WindowManager.MAX_WIDTH = `${Number.MAX_SAFE_INTEGER}vw`;
        WindowManager.MAX_HEIGHT = `${Number.MAX_SAFE_INTEGER}vh`;
        UI.WindowManager = WindowManager;
        if (typeof define === "function") {
            define([`${config.uno_app_base}/AppManifest`], () => {
            });
        }
        else {
            throw `The Uno.Wasm.Boostrap is not up to date, please upgrade to a later version`;
        }
    })(UI = Uno.UI || (Uno.UI = {}));
})(Uno || (Uno = {}));
// Ensure the "Uno" namespace is available globally
window.Uno = Uno;
window.Windows = Windows;
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class ApplicationDataContainer_ClearParams {
    static unmarshal(pData) {
        const ret = new ApplicationDataContainer_ClearParams();
        {
            const ptr = Module.getValue(pData + 0, "*");
            if (ptr !== 0) {
                ret.Locality = String(Module.UTF8ToString(ptr));
            }
            else {
                ret.Locality = null;
            }
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class ApplicationDataContainer_ContainsKeyParams {
    static unmarshal(pData) {
        const ret = new ApplicationDataContainer_ContainsKeyParams();
        {
            const ptr = Module.getValue(pData + 0, "*");
            if (ptr !== 0) {
                ret.Key = String(Module.UTF8ToString(ptr));
            }
            else {
                ret.Key = null;
            }
        }
        {
            const ptr = Module.getValue(pData + 4, "*");
            if (ptr !== 0) {
                ret.Value = String(Module.UTF8ToString(ptr));
            }
            else {
                ret.Value = null;
            }
        }
        {
            const ptr = Module.getValue(pData + 8, "*");
            if (ptr !== 0) {
                ret.Locality = String(Module.UTF8ToString(ptr));
            }
            else {
                ret.Locality = null;
            }
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class ApplicationDataContainer_ContainsKeyReturn {
    marshal(pData) {
        Module.setValue(pData + 0, this.ContainsKey, "i32");
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class ApplicationDataContainer_GetCountParams {
    static unmarshal(pData) {
        const ret = new ApplicationDataContainer_GetCountParams();
        {
            const ptr = Module.getValue(pData + 0, "*");
            if (ptr !== 0) {
                ret.Locality = String(Module.UTF8ToString(ptr));
            }
            else {
                ret.Locality = null;
            }
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class ApplicationDataContainer_GetCountReturn {
    marshal(pData) {
        Module.setValue(pData + 0, this.Count, "i32");
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class ApplicationDataContainer_GetKeyByIndexParams {
    static unmarshal(pData) {
        const ret = new ApplicationDataContainer_GetKeyByIndexParams();
        {
            const ptr = Module.getValue(pData + 0, "*");
            if (ptr !== 0) {
                ret.Locality = String(Module.UTF8ToString(ptr));
            }
            else {
                ret.Locality = null;
            }
        }
        {
            ret.Index = Number(Module.getValue(pData + 4, "i32"));
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class ApplicationDataContainer_GetKeyByIndexReturn {
    marshal(pData) {
        {
            const stringLength = lengthBytesUTF8(this.Value);
            const pString = Module._malloc(stringLength + 1);
            stringToUTF8(this.Value, pString, stringLength + 1);
            Module.setValue(pData + 0, pString, "*");
        }
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class ApplicationDataContainer_GetValueByIndexParams {
    static unmarshal(pData) {
        const ret = new ApplicationDataContainer_GetValueByIndexParams();
        {
            const ptr = Module.getValue(pData + 0, "*");
            if (ptr !== 0) {
                ret.Locality = String(Module.UTF8ToString(ptr));
            }
            else {
                ret.Locality = null;
            }
        }
        {
            ret.Index = Number(Module.getValue(pData + 4, "i32"));
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class ApplicationDataContainer_GetValueByIndexReturn {
    marshal(pData) {
        {
            const stringLength = lengthBytesUTF8(this.Value);
            const pString = Module._malloc(stringLength + 1);
            stringToUTF8(this.Value, pString, stringLength + 1);
            Module.setValue(pData + 0, pString, "*");
        }
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class ApplicationDataContainer_RemoveParams {
    static unmarshal(pData) {
        const ret = new ApplicationDataContainer_RemoveParams();
        {
            const ptr = Module.getValue(pData + 0, "*");
            if (ptr !== 0) {
                ret.Locality = String(Module.UTF8ToString(ptr));
            }
            else {
                ret.Locality = null;
            }
        }
        {
            const ptr = Module.getValue(pData + 4, "*");
            if (ptr !== 0) {
                ret.Key = String(Module.UTF8ToString(ptr));
            }
            else {
                ret.Key = null;
            }
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class ApplicationDataContainer_RemoveReturn {
    marshal(pData) {
        Module.setValue(pData + 0, this.Removed, "i32");
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class ApplicationDataContainer_SetValueParams {
    static unmarshal(pData) {
        const ret = new ApplicationDataContainer_SetValueParams();
        {
            const ptr = Module.getValue(pData + 0, "*");
            if (ptr !== 0) {
                ret.Key = String(Module.UTF8ToString(ptr));
            }
            else {
                ret.Key = null;
            }
        }
        {
            const ptr = Module.getValue(pData + 4, "*");
            if (ptr !== 0) {
                ret.Value = String(Module.UTF8ToString(ptr));
            }
            else {
                ret.Value = null;
            }
        }
        {
            const ptr = Module.getValue(pData + 8, "*");
            if (ptr !== 0) {
                ret.Locality = String(Module.UTF8ToString(ptr));
            }
            else {
                ret.Locality = null;
            }
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class ApplicationDataContainer_TryGetValueParams {
    static unmarshal(pData) {
        const ret = new ApplicationDataContainer_TryGetValueParams();
        {
            const ptr = Module.getValue(pData + 0, "*");
            if (ptr !== 0) {
                ret.Key = String(Module.UTF8ToString(ptr));
            }
            else {
                ret.Key = null;
            }
        }
        {
            const ptr = Module.getValue(pData + 4, "*");
            if (ptr !== 0) {
                ret.Locality = String(Module.UTF8ToString(ptr));
            }
            else {
                ret.Locality = null;
            }
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class ApplicationDataContainer_TryGetValueReturn {
    marshal(pData) {
        {
            const stringLength = lengthBytesUTF8(this.Value);
            const pString = Module._malloc(stringLength + 1);
            stringToUTF8(this.Value, pString, stringLength + 1);
            Module.setValue(pData + 0, pString, "*");
        }
        Module.setValue(pData + 4, this.HasValue, "i32");
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class StorageFolderMakePersistentParams {
    static unmarshal(pData) {
        const ret = new StorageFolderMakePersistentParams();
        {
            ret.Paths_Length = Number(Module.getValue(pData + 0, "i32"));
        }
        {
            const pArray = Module.getValue(pData + 4, "*");
            if (pArray !== 0) {
                ret.Paths = new Array();
                for (var i = 0; i < ret.Paths_Length; i++) {
                    const value = Module.getValue(pArray + i * 4, "*");
                    if (value !== 0) {
                        ret.Paths.push(String(MonoRuntime.conv_string(value)));
                    }
                    else {
                        ret.Paths.push(null);
                    }
                }
            }
            else {
                ret.Paths = null;
            }
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerAddViewParams {
    static unmarshal(pData) {
        const ret = new WindowManagerAddViewParams();
        {
            ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
        }
        {
            ret.ChildView = Number(Module.getValue(pData + 4, "*"));
        }
        {
            ret.Index = Number(Module.getValue(pData + 8, "i32"));
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerArrangeElementParams {
    static unmarshal(pData) {
        const ret = new WindowManagerArrangeElementParams();
        {
            ret.Top = Number(Module.getValue(pData + 0, "double"));
        }
        {
            ret.Left = Number(Module.getValue(pData + 8, "double"));
        }
        {
            ret.Width = Number(Module.getValue(pData + 16, "double"));
        }
        {
            ret.Height = Number(Module.getValue(pData + 24, "double"));
        }
        {
            ret.ClipTop = Number(Module.getValue(pData + 32, "double"));
        }
        {
            ret.ClipLeft = Number(Module.getValue(pData + 40, "double"));
        }
        {
            ret.ClipBottom = Number(Module.getValue(pData + 48, "double"));
        }
        {
            ret.ClipRight = Number(Module.getValue(pData + 56, "double"));
        }
        {
            ret.HtmlId = Number(Module.getValue(pData + 64, "*"));
        }
        {
            ret.Clip = Boolean(Module.getValue(pData + 68, "i32"));
        }
        {
            ret.ClipToBounds = Boolean(Module.getValue(pData + 72, "i32"));
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerCreateContentParams {
    static unmarshal(pData) {
        const ret = new WindowManagerCreateContentParams();
        {
            ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
        }
        {
            const ptr = Module.getValue(pData + 4, "*");
            if (ptr !== 0) {
                ret.TagName = String(Module.UTF8ToString(ptr));
            }
            else {
                ret.TagName = null;
            }
        }
        {
            ret.Handle = Number(Module.getValue(pData + 8, "*"));
        }
        {
            ret.UIElementRegistrationId = Number(Module.getValue(pData + 12, "i32"));
        }
        {
            ret.IsSvg = Boolean(Module.getValue(pData + 16, "i32"));
        }
        {
            ret.IsFocusable = Boolean(Module.getValue(pData + 20, "i32"));
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerDestroyViewParams {
    static unmarshal(pData) {
        const ret = new WindowManagerDestroyViewParams();
        {
            ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerGetBBoxParams {
    static unmarshal(pData) {
        const ret = new WindowManagerGetBBoxParams();
        {
            ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerGetBBoxReturn {
    marshal(pData) {
        Module.setValue(pData + 0, this.X, "double");
        Module.setValue(pData + 8, this.Y, "double");
        Module.setValue(pData + 16, this.Width, "double");
        Module.setValue(pData + 24, this.Height, "double");
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerGetClientViewSizeParams {
    static unmarshal(pData) {
        const ret = new WindowManagerGetClientViewSizeParams();
        {
            ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerGetClientViewSizeReturn {
    marshal(pData) {
        Module.setValue(pData + 0, this.OffsetWidth, "double");
        Module.setValue(pData + 8, this.OffsetHeight, "double");
        Module.setValue(pData + 16, this.ClientWidth, "double");
        Module.setValue(pData + 24, this.ClientHeight, "double");
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerInitParams {
    static unmarshal(pData) {
        const ret = new WindowManagerInitParams();
        {
            ret.IsHostedMode = Boolean(Module.getValue(pData + 0, "i32"));
        }
        {
            ret.IsLoadEventsEnabled = Boolean(Module.getValue(pData + 4, "i32"));
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerMeasureViewParams {
    static unmarshal(pData) {
        const ret = new WindowManagerMeasureViewParams();
        {
            ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
        }
        {
            ret.AvailableWidth = Number(Module.getValue(pData + 8, "double"));
        }
        {
            ret.AvailableHeight = Number(Module.getValue(pData + 16, "double"));
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerMeasureViewReturn {
    marshal(pData) {
        Module.setValue(pData + 0, this.DesiredWidth, "double");
        Module.setValue(pData + 8, this.DesiredHeight, "double");
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerRegisterEventOnViewParams {
    static unmarshal(pData) {
        const ret = new WindowManagerRegisterEventOnViewParams();
        {
            ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
        }
        {
            const ptr = Module.getValue(pData + 4, "*");
            if (ptr !== 0) {
                ret.EventName = String(Module.UTF8ToString(ptr));
            }
            else {
                ret.EventName = null;
            }
        }
        {
            ret.OnCapturePhase = Boolean(Module.getValue(pData + 8, "i32"));
        }
        {
            ret.EventExtractorId = Number(Module.getValue(pData + 12, "i32"));
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerRegisterPointerEventsOnViewParams {
    static unmarshal(pData) {
        const ret = new WindowManagerRegisterPointerEventsOnViewParams();
        {
            ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerRegisterUIElementParams {
    static unmarshal(pData) {
        const ret = new WindowManagerRegisterUIElementParams();
        {
            const ptr = Module.getValue(pData + 0, "*");
            if (ptr !== 0) {
                ret.TypeName = String(Module.UTF8ToString(ptr));
            }
            else {
                ret.TypeName = null;
            }
        }
        {
            ret.IsFrameworkElement = Boolean(Module.getValue(pData + 4, "i32"));
        }
        {
            ret.Classes_Length = Number(Module.getValue(pData + 8, "i32"));
        }
        {
            const pArray = Module.getValue(pData + 12, "*");
            if (pArray !== 0) {
                ret.Classes = new Array();
                for (var i = 0; i < ret.Classes_Length; i++) {
                    const value = Module.getValue(pArray + i * 4, "*");
                    if (value !== 0) {
                        ret.Classes.push(String(MonoRuntime.conv_string(value)));
                    }
                    else {
                        ret.Classes.push(null);
                    }
                }
            }
            else {
                ret.Classes = null;
            }
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerRegisterUIElementReturn {
    marshal(pData) {
        Module.setValue(pData + 0, this.RegistrationId, "i32");
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerRemoveAttributeParams {
    static unmarshal(pData) {
        const ret = new WindowManagerRemoveAttributeParams();
        {
            ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
        }
        {
            const ptr = Module.getValue(pData + 4, "*");
            if (ptr !== 0) {
                ret.Name = String(Module.UTF8ToString(ptr));
            }
            else {
                ret.Name = null;
            }
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerRemoveViewParams {
    static unmarshal(pData) {
        const ret = new WindowManagerRemoveViewParams();
        {
            ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
        }
        {
            ret.ChildView = Number(Module.getValue(pData + 4, "*"));
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerResetStyleParams {
    static unmarshal(pData) {
        const ret = new WindowManagerResetStyleParams();
        {
            ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
        }
        {
            ret.Styles_Length = Number(Module.getValue(pData + 4, "i32"));
        }
        {
            const pArray = Module.getValue(pData + 8, "*");
            if (pArray !== 0) {
                ret.Styles = new Array();
                for (var i = 0; i < ret.Styles_Length; i++) {
                    const value = Module.getValue(pArray + i * 4, "*");
                    if (value !== 0) {
                        ret.Styles.push(String(MonoRuntime.conv_string(value)));
                    }
                    else {
                        ret.Styles.push(null);
                    }
                }
            }
            else {
                ret.Styles = null;
            }
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerScrollToOptionsParams {
    static unmarshal(pData) {
        const ret = new WindowManagerScrollToOptionsParams();
        {
            ret.Left = Number(Module.getValue(pData + 0, "double"));
        }
        {
            ret.Top = Number(Module.getValue(pData + 8, "double"));
        }
        {
            ret.HasLeft = Boolean(Module.getValue(pData + 16, "i32"));
        }
        {
            ret.HasTop = Boolean(Module.getValue(pData + 20, "i32"));
        }
        {
            ret.DisableAnimation = Boolean(Module.getValue(pData + 24, "i32"));
        }
        {
            ret.HtmlId = Number(Module.getValue(pData + 28, "*"));
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetAttributeParams {
    static unmarshal(pData) {
        const ret = new WindowManagerSetAttributeParams();
        {
            ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
        }
        {
            const ptr = Module.getValue(pData + 4, "*");
            if (ptr !== 0) {
                ret.Name = String(Module.UTF8ToString(ptr));
            }
            else {
                ret.Name = null;
            }
        }
        {
            const ptr = Module.getValue(pData + 8, "*");
            if (ptr !== 0) {
                ret.Value = String(Module.UTF8ToString(ptr));
            }
            else {
                ret.Value = null;
            }
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetAttributesParams {
    static unmarshal(pData) {
        const ret = new WindowManagerSetAttributesParams();
        {
            ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
        }
        {
            ret.Pairs_Length = Number(Module.getValue(pData + 4, "i32"));
        }
        {
            const pArray = Module.getValue(pData + 8, "*");
            if (pArray !== 0) {
                ret.Pairs = new Array();
                for (var i = 0; i < ret.Pairs_Length; i++) {
                    const value = Module.getValue(pArray + i * 4, "*");
                    if (value !== 0) {
                        ret.Pairs.push(String(MonoRuntime.conv_string(value)));
                    }
                    else {
                        ret.Pairs.push(null);
                    }
                }
            }
            else {
                ret.Pairs = null;
            }
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetClassesParams {
    static unmarshal(pData) {
        const ret = new WindowManagerSetClassesParams();
        {
            ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
        }
        {
            ret.CssClasses_Length = Number(Module.getValue(pData + 4, "i32"));
        }
        {
            const pArray = Module.getValue(pData + 8, "*");
            if (pArray !== 0) {
                ret.CssClasses = new Array();
                for (var i = 0; i < ret.CssClasses_Length; i++) {
                    const value = Module.getValue(pArray + i * 4, "*");
                    if (value !== 0) {
                        ret.CssClasses.push(String(MonoRuntime.conv_string(value)));
                    }
                    else {
                        ret.CssClasses.push(null);
                    }
                }
            }
            else {
                ret.CssClasses = null;
            }
        }
        {
            ret.Index = Number(Module.getValue(pData + 12, "i32"));
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetContentHtmlParams {
    static unmarshal(pData) {
        const ret = new WindowManagerSetContentHtmlParams();
        {
            ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
        }
        {
            const ptr = Module.getValue(pData + 4, "*");
            if (ptr !== 0) {
                ret.Html = String(Module.UTF8ToString(ptr));
            }
            else {
                ret.Html = null;
            }
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetElementTransformParams {
    static unmarshal(pData) {
        const ret = new WindowManagerSetElementTransformParams();
        {
            ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
        }
        {
            ret.M11 = Number(Module.getValue(pData + 8, "double"));
        }
        {
            ret.M12 = Number(Module.getValue(pData + 16, "double"));
        }
        {
            ret.M21 = Number(Module.getValue(pData + 24, "double"));
        }
        {
            ret.M22 = Number(Module.getValue(pData + 32, "double"));
        }
        {
            ret.M31 = Number(Module.getValue(pData + 40, "double"));
        }
        {
            ret.M32 = Number(Module.getValue(pData + 48, "double"));
        }
        {
            ret.ClipToBounds = Boolean(Module.getValue(pData + 56, "i32"));
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetNameParams {
    static unmarshal(pData) {
        const ret = new WindowManagerSetNameParams();
        {
            ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
        }
        {
            const ptr = Module.getValue(pData + 4, "*");
            if (ptr !== 0) {
                ret.Name = String(Module.UTF8ToString(ptr));
            }
            else {
                ret.Name = null;
            }
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetPointerEventsParams {
    static unmarshal(pData) {
        const ret = new WindowManagerSetPointerEventsParams();
        {
            ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
        }
        {
            ret.Enabled = Boolean(Module.getValue(pData + 4, "i32"));
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetPropertyParams {
    static unmarshal(pData) {
        const ret = new WindowManagerSetPropertyParams();
        {
            ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
        }
        {
            ret.Pairs_Length = Number(Module.getValue(pData + 4, "i32"));
        }
        {
            const pArray = Module.getValue(pData + 8, "*");
            if (pArray !== 0) {
                ret.Pairs = new Array();
                for (var i = 0; i < ret.Pairs_Length; i++) {
                    const value = Module.getValue(pArray + i * 4, "*");
                    if (value !== 0) {
                        ret.Pairs.push(String(MonoRuntime.conv_string(value)));
                    }
                    else {
                        ret.Pairs.push(null);
                    }
                }
            }
            else {
                ret.Pairs = null;
            }
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetStyleDoubleParams {
    static unmarshal(pData) {
        const ret = new WindowManagerSetStyleDoubleParams();
        {
            ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
        }
        {
            const ptr = Module.getValue(pData + 4, "*");
            if (ptr !== 0) {
                ret.Name = String(Module.UTF8ToString(ptr));
            }
            else {
                ret.Name = null;
            }
        }
        {
            ret.Value = Number(Module.getValue(pData + 8, "double"));
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetStylesParams {
    static unmarshal(pData) {
        const ret = new WindowManagerSetStylesParams();
        {
            ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
        }
        {
            ret.Pairs_Length = Number(Module.getValue(pData + 4, "i32"));
        }
        {
            const pArray = Module.getValue(pData + 8, "*");
            if (pArray !== 0) {
                ret.Pairs = new Array();
                for (var i = 0; i < ret.Pairs_Length; i++) {
                    const value = Module.getValue(pArray + i * 4, "*");
                    if (value !== 0) {
                        ret.Pairs.push(String(MonoRuntime.conv_string(value)));
                    }
                    else {
                        ret.Pairs.push(null);
                    }
                }
            }
            else {
                ret.Pairs = null;
            }
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetSvgElementRectParams {
    static unmarshal(pData) {
        const ret = new WindowManagerSetSvgElementRectParams();
        {
            ret.X = Number(Module.getValue(pData + 0, "double"));
        }
        {
            ret.Y = Number(Module.getValue(pData + 8, "double"));
        }
        {
            ret.Width = Number(Module.getValue(pData + 16, "double"));
        }
        {
            ret.Height = Number(Module.getValue(pData + 24, "double"));
        }
        {
            ret.HtmlId = Number(Module.getValue(pData + 32, "*"));
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetUnsetClassesParams {
    static unmarshal(pData) {
        const ret = new WindowManagerSetUnsetClassesParams();
        {
            ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
        }
        {
            ret.CssClassesToSet_Length = Number(Module.getValue(pData + 4, "i32"));
        }
        {
            const pArray = Module.getValue(pData + 8, "*");
            if (pArray !== 0) {
                ret.CssClassesToSet = new Array();
                for (var i = 0; i < ret.CssClassesToSet_Length; i++) {
                    const value = Module.getValue(pArray + i * 4, "*");
                    if (value !== 0) {
                        ret.CssClassesToSet.push(String(MonoRuntime.conv_string(value)));
                    }
                    else {
                        ret.CssClassesToSet.push(null);
                    }
                }
            }
            else {
                ret.CssClassesToSet = null;
            }
        }
        {
            ret.CssClassesToUnset_Length = Number(Module.getValue(pData + 12, "i32"));
        }
        {
            const pArray = Module.getValue(pData + 16, "*");
            if (pArray !== 0) {
                ret.CssClassesToUnset = new Array();
                for (var i = 0; i < ret.CssClassesToUnset_Length; i++) {
                    const value = Module.getValue(pArray + i * 4, "*");
                    if (value !== 0) {
                        ret.CssClassesToUnset.push(String(MonoRuntime.conv_string(value)));
                    }
                    else {
                        ret.CssClassesToUnset.push(null);
                    }
                }
            }
            else {
                ret.CssClassesToUnset = null;
            }
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetXUidParams {
    static unmarshal(pData) {
        const ret = new WindowManagerSetXUidParams();
        {
            ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
        }
        {
            const ptr = Module.getValue(pData + 4, "*");
            if (ptr !== 0) {
                ret.Uid = String(Module.UTF8ToString(ptr));
            }
            else {
                ret.Uid = null;
            }
        }
        return ret;
    }
}
PointerEvent.prototype.isOver = function (element) {
    const bounds = element.getBoundingClientRect();
    return this.pageX >= bounds.left
        && this.pageX < bounds.right
        && this.pageY >= bounds.top
        && this.pageY < bounds.bottom;
};
PointerEvent.prototype.isOverDeep = function (element) {
    if (!element) {
        return false;
    }
    else if (element.style.pointerEvents != "none") {
        return this.isOver(element);
    }
    else {
        for (let elt of element.children) {
            if (this.isOverDeep(elt)) {
                return true;
            }
        }
    }
};
var Uno;
(function (Uno) {
    var UI;
    (function (UI) {
        var Interop;
        (function (Interop) {
            class AsyncInteropHelper {
                static init() {
                    if (AsyncInteropHelper.dispatchErrorMethod) {
                        return; // already initialized
                    }
                    const w = window;
                    AsyncInteropHelper.dispatchResultMethod =
                        w.Module.mono_bind_static_method("[Uno.Foundation] Uno.Foundation.WebAssemblyRuntime:DispatchAsyncResult");
                    AsyncInteropHelper.dispatchErrorMethod =
                        w.Module.mono_bind_static_method("[Uno.Foundation] Uno.Foundation.WebAssemblyRuntime:DispatchAsyncError");
                }
                static Invoke(handle, promiseFunction) {
                    AsyncInteropHelper.init();
                    try {
                        promiseFunction()
                            .then(str => {
                            if (typeof str == "string") {
                                AsyncInteropHelper.dispatchResultMethod(handle, str);
                            }
                            else {
                                AsyncInteropHelper.dispatchResultMethod(handle, null);
                            }
                        })
                            .catch(err => {
                            AsyncInteropHelper.dispatchErrorMethod(handle, err);
                        });
                    }
                    catch (err) {
                        AsyncInteropHelper.dispatchErrorMethod(handle, err);
                    }
                }
            }
            Interop.AsyncInteropHelper = AsyncInteropHelper;
        })(Interop = UI.Interop || (UI.Interop = {}));
    })(UI = Uno.UI || (Uno.UI = {}));
})(Uno || (Uno = {}));
var Uno;
(function (Uno) {
    var Foundation;
    (function (Foundation) {
        var Interop;
        (function (Interop) {
            class ManagedObject {
                static init() {
                    ManagedObject.dispatchMethod = Module.mono_bind_static_method("[Uno.Foundation] Uno.Foundation.Interop.JSObject:Dispatch");
                }
                static dispatch(handle, method, parameters) {
                    if (!ManagedObject.dispatchMethod) {
                        ManagedObject.init();
                    }
                    ManagedObject.dispatchMethod(handle, method, parameters || "");
                }
            }
            Interop.ManagedObject = ManagedObject;
        })(Interop = Foundation.Interop || (Foundation.Interop = {}));
    })(Foundation = Uno.Foundation || (Uno.Foundation = {}));
})(Uno || (Uno = {}));
var Uno;
(function (Uno) {
    var UI;
    (function (UI) {
        var Interop;
        (function (Interop) {
            class Runtime {
                static init() {
                    return "";
                }
            }
            Runtime.engine = Runtime.init();
            Interop.Runtime = Runtime;
        })(Interop = UI.Interop || (UI.Interop = {}));
    })(UI = Uno.UI || (Uno.UI = {}));
})(Uno || (Uno = {}));
var Uno;
(function (Uno) {
    var UI;
    (function (UI) {
        var Interop;
        (function (Interop) {
            class Xaml {
            }
            Interop.Xaml = Xaml;
        })(Interop = UI.Interop || (UI.Interop = {}));
    })(UI = Uno.UI || (Uno.UI = {}));
})(Uno || (Uno = {}));
// ReSharper disable InconsistentNaming
// eslint-disable-next-line @typescript-eslint/no-namespace
var Windows;
(function (Windows) {
    var Storage;
    (function (Storage) {
        class ApplicationDataContainer {
            static buildStorageKey(locality, key) {
                return `UnoApplicationDataContainer_${locality}_${key}`;
            }
            static buildStoragePrefix(locality) {
                return `UnoApplicationDataContainer_${locality}_`;
            }
            /**
             * Try to get a value from localStorage
             * */
            static tryGetValue(pParams, pReturn) {
                const params = ApplicationDataContainer_TryGetValueParams.unmarshal(pParams);
                const ret = new ApplicationDataContainer_TryGetValueReturn();
                const storageKey = ApplicationDataContainer.buildStorageKey(params.Locality, params.Key);
                if (localStorage.hasOwnProperty(storageKey)) {
                    ret.HasValue = true;
                    ret.Value = localStorage.getItem(storageKey);
                }
                else {
                    ret.Value = "";
                    ret.HasValue = false;
                }
                ret.marshal(pReturn);
                return true;
            }
            /**
             * Set a value to localStorage
             * */
            static setValue(pParams) {
                const params = ApplicationDataContainer_SetValueParams.unmarshal(pParams);
                const storageKey = ApplicationDataContainer.buildStorageKey(params.Locality, params.Key);
                localStorage.setItem(storageKey, params.Value);
                return true;
            }
            /**
             * Determines if a key is contained in localStorage
             * */
            static containsKey(pParams, pReturn) {
                const params = ApplicationDataContainer_ContainsKeyParams.unmarshal(pParams);
                const ret = new ApplicationDataContainer_ContainsKeyReturn();
                const storageKey = ApplicationDataContainer.buildStorageKey(params.Locality, params.Key);
                ret.ContainsKey = localStorage.hasOwnProperty(storageKey);
                ret.marshal(pReturn);
                return true;
            }
            /**
             * Gets a key by index in localStorage
             * */
            static getKeyByIndex(pParams, pReturn) {
                const params = ApplicationDataContainer_GetKeyByIndexParams.unmarshal(pParams);
                const ret = new ApplicationDataContainer_GetKeyByIndexReturn();
                let localityIndex = 0;
                let returnKey = "";
                const prefix = ApplicationDataContainer.buildStoragePrefix(params.Locality);
                for (let i = 0; i < localStorage.length; i++) {
                    const storageKey = localStorage.key(i);
                    if (storageKey.startsWith(prefix)) {
                        if (localityIndex === params.Index) {
                            returnKey = storageKey.substr(prefix.length);
                        }
                        localityIndex++;
                    }
                }
                ret.Value = returnKey;
                ret.marshal(pReturn);
                return true;
            }
            /**
             * Determines the number of items contained in localStorage
             * */
            static getCount(pParams, pReturn) {
                const params = ApplicationDataContainer_GetCountParams.unmarshal(pParams);
                const ret = new ApplicationDataContainer_GetCountReturn();
                ret.Count = 0;
                const prefix = ApplicationDataContainer.buildStoragePrefix(params.Locality);
                for (let i = 0; i < localStorage.length; i++) {
                    const storageKey = localStorage.key(i);
                    if (storageKey.startsWith(prefix)) {
                        ret.Count++;
                    }
                }
                ret.marshal(pReturn);
                return true;
            }
            /**
             * Clears items contained in localStorage
             * */
            static clear(pParams) {
                const params = ApplicationDataContainer_ClearParams.unmarshal(pParams);
                const prefix = ApplicationDataContainer.buildStoragePrefix(params.Locality);
                const itemsToRemove = [];
                for (let i = 0; i < localStorage.length; i++) {
                    const storageKey = localStorage.key(i);
                    if (storageKey.startsWith(prefix)) {
                        itemsToRemove.push(storageKey);
                    }
                }
                for (const item in itemsToRemove) {
                    localStorage.removeItem(itemsToRemove[item]);
                }
                return true;
            }
            /**
             * Removes an item contained in localStorage
             * */
            static remove(pParams, pReturn) {
                const params = ApplicationDataContainer_RemoveParams.unmarshal(pParams);
                const ret = new ApplicationDataContainer_RemoveReturn();
                const storageKey = ApplicationDataContainer.buildStorageKey(params.Locality, params.Key);
                ret.Removed = localStorage.hasOwnProperty(storageKey);
                if (ret.Removed) {
                    localStorage.removeItem(storageKey);
                }
                ret.marshal(pReturn);
                return true;
            }
            /**
             * Gets a key by index in localStorage
             * */
            static getValueByIndex(pParams, pReturn) {
                const params = ApplicationDataContainer_GetValueByIndexParams.unmarshal(pParams);
                const ret = new ApplicationDataContainer_GetKeyByIndexReturn();
                let localityIndex = 0;
                let returnKey = "";
                const prefix = ApplicationDataContainer.buildStoragePrefix(params.Locality);
                for (let i = 0; i < localStorage.length; i++) {
                    const storageKey = localStorage.key(i);
                    if (storageKey.startsWith(prefix)) {
                        if (localityIndex === params.Index) {
                            returnKey = localStorage.getItem(storageKey);
                        }
                        localityIndex++;
                    }
                }
                ret.Value = returnKey;
                ret.marshal(pReturn);
                return true;
            }
        }
        Storage.ApplicationDataContainer = ApplicationDataContainer;
    })(Storage = Windows.Storage || (Windows.Storage = {}));
})(Windows || (Windows = {}));
// eslint-disable-next-line @typescript-eslint/no-namespace
var Windows;
(function (Windows) {
    var Storage;
    (function (Storage) {
        class StorageFolder {
            /**
             * Determine if IndexDB is available, some browsers and modes disable it.
             * */
            static isIndexDBAvailable() {
                try {
                    // IndexedDB may not be available in private mode
                    window.indexedDB;
                    return true;
                }
                catch (err) {
                    return false;
                }
            }
            /**
             * Setup the storage persistence of a given set of paths.
             * */
            static makePersistent(pParams) {
                const params = StorageFolderMakePersistentParams.unmarshal(pParams);
                for (var i = 0; i < params.Paths.length; i++) {
                    this.setupStorage(params.Paths[i]);
                }
            }
            /**
             * Setup the storage persistence of a given path.
             * */
            static setupStorage(path) {
                if (Uno.UI.WindowManager.isHosted) {
                    console.debug("Hosted Mode: skipping IndexDB initialization");
                    StorageFolder.onStorageInitialized();
                    return;
                }
                if (!this.isIndexDBAvailable()) {
                    console.warn("IndexedDB is not available (private mode or uri starts with file:// ?), changes will not be persisted.");
                    StorageFolder.onStorageInitialized();
                    return;
                }
                if (typeof IDBFS === 'undefined') {
                    console.warn(`IDBFS is not enabled in mono's configuration, persistence is disabled`);
                    StorageFolder.onStorageInitialized();
                    return;
                }
                console.debug("Making persistent: " + path);
                FS.mkdir(path);
                FS.mount(IDBFS, {}, path);
                // Ensure to sync pseudo file system on unload (and periodically for safety)
                if (!this._isInit) {
                    // Request an initial sync to populate the file system
                    FS.syncfs(true, err => {
                        if (err) {
                            console.error(`Error synchronizing filesystem from IndexDB: ${err} (errno: ${err.errno})`);
                        }
                        StorageFolder.onStorageInitialized();
                    });
                    window.addEventListener("beforeunload", this.synchronizeFileSystem);
                    setInterval(this.synchronizeFileSystem, 10000);
                    this._isInit = true;
                }
            }
            static onStorageInitialized() {
                if (!StorageFolder.dispatchStorageInitialized) {
                    StorageFolder.dispatchStorageInitialized =
                        Module.mono_bind_static_method("[Uno] Windows.Storage.StorageFolder:DispatchStorageInitialized");
                }
                StorageFolder.dispatchStorageInitialized();
            }
            /**
             * Synchronize the IDBFS memory cache back to IndexDB
             * */
            static synchronizeFileSystem() {
                FS.syncfs(err => {
                    if (err) {
                        console.error(`Error synchronizing filesystem from IndexDB: ${err} (errno: ${err.errno})`);
                    }
                });
            }
        }
        StorageFolder._isInit = false;
        Storage.StorageFolder = StorageFolder;
    })(Storage = Windows.Storage || (Windows.Storage = {}));
})(Windows || (Windows = {}));
var Windows;
(function (Windows) {
    var Devices;
    (function (Devices) {
        var Geolocation;
        (function (Geolocation) {
            let GeolocationAccessStatus;
            (function (GeolocationAccessStatus) {
                GeolocationAccessStatus["Allowed"] = "Allowed";
                GeolocationAccessStatus["Denied"] = "Denied";
                GeolocationAccessStatus["Unspecified"] = "Unspecified";
            })(GeolocationAccessStatus || (GeolocationAccessStatus = {}));
            let PositionStatus;
            (function (PositionStatus) {
                PositionStatus["Ready"] = "Ready";
                PositionStatus["Initializing"] = "Initializing";
                PositionStatus["NoData"] = "NoData";
                PositionStatus["Disabled"] = "Disabled";
                PositionStatus["NotInitialized"] = "NotInitialized";
                PositionStatus["NotAvailable"] = "NotAvailable";
            })(PositionStatus || (PositionStatus = {}));
            class Geolocator {
                static initialize() {
                    this.positionWatches = {};
                    if (!this.dispatchAccessRequest) {
                        this.dispatchAccessRequest = Module.mono_bind_static_method("[Uno] Windows.Devices.Geolocation.Geolocator:DispatchAccessRequest");
                    }
                    if (!this.dispatchError) {
                        this.dispatchError = Module.mono_bind_static_method("[Uno] Windows.Devices.Geolocation.Geolocator:DispatchError");
                    }
                    if (!this.dispatchGeoposition) {
                        this.dispatchGeoposition = Module.mono_bind_static_method("[Uno] Windows.Devices.Geolocation.Geolocator:DispatchGeoposition");
                    }
                }
                //checks for permission to the geolocation services
                static requestAccess() {
                    Geolocator.initialize();
                    if (navigator.geolocation) {
                        navigator.geolocation.getCurrentPosition((_) => {
                            Geolocator.dispatchAccessRequest(GeolocationAccessStatus.Allowed);
                        }, (error) => {
                            if (error.code == error.PERMISSION_DENIED) {
                                Geolocator.dispatchAccessRequest(GeolocationAccessStatus.Denied);
                            }
                            else if (error.code == error.POSITION_UNAVAILABLE ||
                                error.code == error.TIMEOUT) {
                                //position unavailable but we still have permission
                                Geolocator.dispatchAccessRequest(GeolocationAccessStatus.Allowed);
                            }
                            else {
                                Geolocator.dispatchAccessRequest(GeolocationAccessStatus.Unspecified);
                            }
                        }, { enableHighAccuracy: false, maximumAge: 86400000, timeout: 100 });
                    }
                    else {
                        Geolocator.dispatchAccessRequest(GeolocationAccessStatus.Denied);
                    }
                }
                //retrieves a single geoposition
                static getGeoposition(desiredAccuracyInMeters, maximumAge, timeout, requestId) {
                    Geolocator.initialize();
                    if (navigator.geolocation) {
                        this.getAccurateCurrentPosition((position) => Geolocator.handleGeoposition(position, requestId), (error) => Geolocator.handleError(error, requestId), desiredAccuracyInMeters, {
                            enableHighAccuracy: desiredAccuracyInMeters < 50,
                            maximumAge: maximumAge,
                            timeout: timeout
                        });
                    }
                    else {
                        Geolocator.dispatchError(PositionStatus.NotAvailable, requestId);
                    }
                }
                static startPositionWatch(desiredAccuracyInMeters, requestId) {
                    Geolocator.initialize();
                    if (navigator.geolocation) {
                        Geolocator.positionWatches[requestId] = navigator.geolocation.watchPosition((position) => Geolocator.handleGeoposition(position, requestId), (error) => Geolocator.handleError(error, requestId));
                        return true;
                    }
                    else {
                        return false;
                    }
                }
                static stopPositionWatch(desiredAccuracyInMeters, requestId) {
                    navigator.geolocation.clearWatch(Geolocator.positionWatches[requestId]);
                    delete Geolocator.positionWatches[requestId];
                }
                static handleGeoposition(position, requestId) {
                    var serializedGeoposition = position.coords.latitude + ":" +
                        position.coords.longitude + ":" +
                        position.coords.altitude + ":" +
                        position.coords.altitudeAccuracy + ":" +
                        position.coords.accuracy + ":" +
                        position.coords.heading + ":" +
                        position.coords.speed + ":" +
                        position.timestamp;
                    Geolocator.dispatchGeoposition(serializedGeoposition, requestId);
                }
                static handleError(error, requestId) {
                    if (error.code == error.TIMEOUT) {
                        Geolocator.dispatchError(PositionStatus.NoData, requestId);
                    }
                    else if (error.code == error.PERMISSION_DENIED) {
                        Geolocator.dispatchError(PositionStatus.Disabled, requestId);
                    }
                    else if (error.code == error.POSITION_UNAVAILABLE) {
                        Geolocator.dispatchError(PositionStatus.NotAvailable, requestId);
                    }
                }
                //this attempts to squeeze out the requested accuracy from the GPS by utilizing the set timeout
                //adapted from https://github.com/gregsramblings/getAccurateCurrentPosition/blob/master/geo.js		
                static getAccurateCurrentPosition(geolocationSuccess, geolocationError, desiredAccuracy, options) {
                    var lastCheckedPosition;
                    var locationEventCount = 0;
                    var watchId;
                    var timerId;
                    var checkLocation = function (position) {
                        lastCheckedPosition = position;
                        locationEventCount = locationEventCount + 1;
                        //is the accuracy enough?
                        if (position.coords.accuracy <= desiredAccuracy) {
                            clearTimeout(timerId);
                            navigator.geolocation.clearWatch(watchId);
                            foundPosition(position);
                        }
                    };
                    var stopTrying = function () {
                        navigator.geolocation.clearWatch(watchId);
                        foundPosition(lastCheckedPosition);
                    };
                    var onError = function (error) {
                        clearTimeout(timerId);
                        navigator.geolocation.clearWatch(watchId);
                        geolocationError(error);
                    };
                    var foundPosition = function (position) {
                        geolocationSuccess(position);
                    };
                    watchId = navigator.geolocation.watchPosition(checkLocation, onError, options);
                    timerId = setTimeout(stopTrying, options.timeout);
                }
                ;
            }
            Geolocation.Geolocator = Geolocator;
        })(Geolocation = Devices.Geolocation || (Devices.Geolocation = {}));
    })(Devices = Windows.Devices || (Windows.Devices = {}));
})(Windows || (Windows = {}));
var Windows;
(function (Windows) {
    var Devices;
    (function (Devices) {
        var Sensors;
        (function (Sensors) {
            class Accelerometer {
                static initialize() {
                    if (window.DeviceMotionEvent) {
                        this.dispatchReading = Module.mono_bind_static_method("[Uno] Windows.Devices.Sensors.Accelerometer:DispatchReading");
                        return true;
                    }
                    return false;
                }
                static startReading() {
                    window.addEventListener("devicemotion", Accelerometer.readingChangedHandler);
                }
                static stopReading() {
                    window.removeEventListener("devicemotion", Accelerometer.readingChangedHandler);
                }
                static readingChangedHandler(event) {
                    Accelerometer.dispatchReading(event.accelerationIncludingGravity.x, event.accelerationIncludingGravity.y, event.accelerationIncludingGravity.z);
                }
            }
            Sensors.Accelerometer = Accelerometer;
        })(Sensors = Devices.Sensors || (Devices.Sensors = {}));
    })(Devices = Windows.Devices || (Windows.Devices = {}));
})(Windows || (Windows = {}));
var Windows;
(function (Windows) {
    var Devices;
    (function (Devices) {
        var Sensors;
        (function (Sensors) {
            class Gyrometer {
                static initialize() {
                    try {
                        if (typeof window.Gyroscope === "function") {
                            this.dispatchReading = Module.mono_bind_static_method("[Uno] Windows.Devices.Sensors.Gyrometer:DispatchReading");
                            let GyroscopeClass = window.Gyroscope;
                            this.gyroscope = new GyroscopeClass({ referenceFrame: "device" });
                            return true;
                        }
                    }
                    catch (error) {
                        //sensor not available
                        console.log("Gyroscope could not be initialized.");
                    }
                    return false;
                }
                static startReading() {
                    this.gyroscope.addEventListener("reading", Gyrometer.readingChangedHandler);
                    this.gyroscope.start();
                }
                static stopReading() {
                    this.gyroscope.removeEventListener("reading", Gyrometer.readingChangedHandler);
                    this.gyroscope.stop();
                }
                static readingChangedHandler(event) {
                    Gyrometer.dispatchReading(Gyrometer.gyroscope.x, Gyrometer.gyroscope.y, Gyrometer.gyroscope.z);
                }
            }
            Sensors.Gyrometer = Gyrometer;
        })(Sensors = Devices.Sensors || (Devices.Sensors = {}));
    })(Devices = Windows.Devices || (Windows.Devices = {}));
})(Windows || (Windows = {}));
var Windows;
(function (Windows) {
    var Devices;
    (function (Devices) {
        var Sensors;
        (function (Sensors) {
            class Magnetometer {
                static initialize() {
                    try {
                        if (typeof window.Magnetometer === "function") {
                            this.dispatchReading = Module.mono_bind_static_method("[Uno] Windows.Devices.Sensors.Magnetometer:DispatchReading");
                            let MagnetometerClass = window.Magnetometer;
                            this.magnetometer = new MagnetometerClass({ referenceFrame: 'device' });
                            return true;
                        }
                    }
                    catch (error) {
                        //sensor not available
                        console.log("Magnetometer could not be initialized.");
                    }
                    return false;
                }
                static startReading() {
                    this.magnetometer.addEventListener("reading", Magnetometer.readingChangedHandler);
                    this.magnetometer.start();
                }
                static stopReading() {
                    this.magnetometer.removeEventListener("reading", Magnetometer.readingChangedHandler);
                    this.magnetometer.stop();
                }
                static readingChangedHandler(event) {
                    Magnetometer.dispatchReading(Magnetometer.magnetometer.x, Magnetometer.magnetometer.y, Magnetometer.magnetometer.z);
                }
            }
            Sensors.Magnetometer = Magnetometer;
        })(Sensors = Devices.Sensors || (Devices.Sensors = {}));
    })(Devices = Windows.Devices || (Windows.Devices = {}));
})(Windows || (Windows = {}));
var Windows;
(function (Windows) {
    var Networking;
    (function (Networking) {
        var Connectivity;
        (function (Connectivity) {
            class ConnectionProfile {
                static hasInternetAccess() {
                    return navigator.onLine;
                }
            }
            Connectivity.ConnectionProfile = ConnectionProfile;
        })(Connectivity = Networking.Connectivity || (Networking.Connectivity = {}));
    })(Networking = Windows.Networking || (Windows.Networking = {}));
})(Windows || (Windows = {}));
var Windows;
(function (Windows) {
    var Networking;
    (function (Networking) {
        var Connectivity;
        (function (Connectivity) {
            class NetworkInformation {
                static startStatusChanged() {
                    window.addEventListener("online", NetworkInformation.networkStatusChanged);
                    window.addEventListener("offline", NetworkInformation.networkStatusChanged);
                }
                static stopStatusChanged() {
                    window.removeEventListener("online", NetworkInformation.networkStatusChanged);
                    window.removeEventListener("offline", NetworkInformation.networkStatusChanged);
                }
                static networkStatusChanged() {
                    if (NetworkInformation.dispatchStatusChanged == null) {
                        NetworkInformation.dispatchStatusChanged =
                            Module.mono_bind_static_method("[Uno] Windows.Networking.Connectivity.NetworkInformation:DispatchStatusChanged");
                    }
                    NetworkInformation.dispatchStatusChanged();
                }
            }
            Connectivity.NetworkInformation = NetworkInformation;
        })(Connectivity = Networking.Connectivity || (Networking.Connectivity = {}));
    })(Networking = Windows.Networking || (Windows.Networking = {}));
})(Windows || (Windows = {}));
var WakeLockType;
(function (WakeLockType) {
    WakeLockType["screen"] = "screen";
})(WakeLockType || (WakeLockType = {}));
;
;
;
var Windows;
(function (Windows) {
    var System;
    (function (System) {
        var Display;
        (function (Display) {
            class DisplayRequest {
                static activateScreenLock() {
                    if (navigator.wakeLock) {
                        DisplayRequest.activeScreenLockPromise = navigator.wakeLock.request(WakeLockType.screen);
                        DisplayRequest.activeScreenLockPromise.catch(reason => console.log("Could not acquire screen lock (" + reason + ")"));
                    }
                }
                static deactivateScreenLock() {
                    if (DisplayRequest.activeScreenLockPromise) {
                        DisplayRequest.activeScreenLockPromise.then(sentinel => sentinel.release());
                        DisplayRequest.activeScreenLockPromise = null;
                    }
                }
            }
            Display.DisplayRequest = DisplayRequest;
        })(Display = System.Display || (System.Display = {}));
    })(System = Windows.System || (Windows.System = {}));
})(Windows || (Windows = {}));
var Windows;
(function (Windows) {
    var System;
    (function (System) {
        var Profile;
        (function (Profile) {
            class AnalyticsVersionInfo {
                static getUserAgent() {
                    return navigator.userAgent;
                }
                static getBrowserName() {
                    // Opera 8.0+
                    if ((!!window.opr && !!window.opr.addons) || !!window.opera || navigator.userAgent.indexOf(' OPR/') >= 0) {
                        return "Opera";
                    }
                    // Firefox 1.0+
                    if (typeof window.InstallTrigger !== 'undefined') {
                        return "Firefox";
                    }
                    // Safari 3.0+ "[object HTMLElementConstructor]" 
                    if (/constructor/i.test(window.HTMLElement) ||
                        ((p) => p.toString() === "[object SafariRemoteNotification]")(typeof window.safari !== 'undefined' && window.safari.pushNotification)) {
                        return "Safari";
                    }
                    // Edge 20+
                    if (!!window.StyleMedia) {
                        return "Edge";
                    }
                    // Chrome 1 - 71
                    if (!!window.chrome && (!!window.chrome.webstore || !!window.chrome.runtime)) {
                        return "Chrome";
                    }
                }
            }
            Profile.AnalyticsVersionInfo = AnalyticsVersionInfo;
        })(Profile = System.Profile || (System.Profile = {}));
    })(System = Windows.System || (Windows.System = {}));
})(Windows || (Windows = {}));
var Windows;
(function (Windows) {
    var UI;
    (function (UI) {
        var Core;
        (function (Core) {
            class SystemNavigationManager {
                constructor() {
                    var that = this;
                    var dispatchBackRequest = Module.mono_bind_static_method("[Uno] Windows.UI.Core.SystemNavigationManager:DispatchBackRequest");
                    window.history.replaceState(0, document.title, null);
                    window.addEventListener("popstate", function (evt) {
                        if (that._isEnabled) {
                            if (evt.state === 0) {
                                // Push something in the stack only if we know that we reached the first page.
                                // There is no way to track our location in the stack, so we use indexes (in the 'state').
                                window.history.pushState(1, document.title, null);
                            }
                            dispatchBackRequest();
                        }
                        else if (evt.state === 1) {
                            // The manager is disabled, but the user requested to navigate forward to our dummy entry,
                            // but we prefer to keep this dummy entry in the forward stack (is more prompt to be cleared by the browser,
                            // and as it's less commonly used it should be less annoying for the user)
                            window.history.back();
                        }
                    });
                }
                static get current() {
                    if (!this._current) {
                        this._current = new SystemNavigationManager();
                    }
                    return this._current;
                }
                enable() {
                    if (this._isEnabled) {
                        return;
                    }
                    // Clear the back stack, so the only items will be ours (and we won't have any remaining forward item)
                    this.clearStack();
                    window.history.pushState(1, document.title, null);
                    // Then set the enabled flag so the handler will begin its work
                    this._isEnabled = true;
                }
                disable() {
                    if (!this._isEnabled) {
                        return;
                    }
                    // Disable the handler, then clear the history
                    // Note: As a side effect, the forward button will be enabled :(
                    this._isEnabled = false;
                    this.clearStack();
                }
                clearStack() {
                    // There is no way to determine our position in the stack, so we only navigate back if we determine that
                    // we are currently on our dummy target page.
                    if (window.history.state === 1) {
                        window.history.back();
                    }
                    window.history.replaceState(0, document.title, null);
                }
            }
            Core.SystemNavigationManager = SystemNavigationManager;
        })(Core = UI.Core || (UI.Core = {}));
    })(UI = Windows.UI || (Windows.UI = {}));
})(Windows || (Windows = {}));
var Windows;
(function (Windows) {
    var UI;
    (function (UI) {
        var ViewManagement;
        (function (ViewManagement) {
            class ApplicationView {
                static setFullScreenMode(turnOn) {
                    if (turnOn) {
                        if (document.fullscreenEnabled) {
                            document.documentElement.requestFullscreen();
                            return true;
                        }
                        else {
                            return false;
                        }
                    }
                    else {
                        document.exitFullscreen();
                        return true;
                    }
                }
            }
            ViewManagement.ApplicationView = ApplicationView;
        })(ViewManagement = UI.ViewManagement || (UI.ViewManagement = {}));
    })(UI = Windows.UI || (Windows.UI = {}));
})(Windows || (Windows = {}));
var Windows;
(function (Windows) {
    var UI;
    (function (UI) {
        var ViewManagement;
        (function (ViewManagement) {
            class ApplicationViewTitleBar {
                static setBackgroundColor(colorString) {
                    if (colorString == null) {
                        //remove theme-color meta
                        var metaThemeColorEntries = document.querySelectorAll("meta[name='theme-color']");
                        for (let entry of metaThemeColorEntries) {
                            entry.remove();
                        }
                    }
                    else {
                        var metaThemeColorEntries = document.querySelectorAll("meta[name='theme-color']");
                        var metaThemeColor;
                        if (metaThemeColorEntries.length == 0) {
                            //create meta
                            metaThemeColor = document.createElement("meta");
                            metaThemeColor.setAttribute("name", "theme-color");
                            document.head.appendChild(metaThemeColor);
                        }
                        else {
                            metaThemeColor = metaThemeColorEntries[0];
                        }
                        metaThemeColor.setAttribute("content", colorString);
                    }
                }
            }
            ViewManagement.ApplicationViewTitleBar = ApplicationViewTitleBar;
        })(ViewManagement = UI.ViewManagement || (UI.ViewManagement = {}));
    })(UI = Windows.UI || (Windows.UI = {}));
})(Windows || (Windows = {}));
var Windows;
(function (Windows) {
    var UI;
    (function (UI) {
        var Xaml;
        (function (Xaml) {
            class Application {
                static getDefaultSystemTheme() {
                    if (window.matchMedia) {
                        if (window.matchMedia("(prefers-color-scheme: dark)").matches) {
                            return Xaml.ApplicationTheme.Dark;
                        }
                        if (window.matchMedia("(prefers-color-scheme: light)").matches) {
                            return Xaml.ApplicationTheme.Light;
                        }
                    }
                    return null;
                }
                static observeSystemTheme() {
                    if (!this.dispatchThemeChange) {
                        this.dispatchThemeChange = Module.mono_bind_static_method("[Uno.UI] Windows.UI.Xaml.Application:DispatchSystemThemeChange");
                    }
                    if (window.matchMedia) {
                        window.matchMedia('(prefers-color-scheme: dark)').addEventListener("change", () => {
                            Application.dispatchThemeChange();
                        });
                    }
                }
            }
            Xaml.Application = Application;
        })(Xaml = UI.Xaml || (UI.Xaml = {}));
    })(UI = Windows.UI || (Windows.UI = {}));
})(Windows || (Windows = {}));
var Windows;
(function (Windows) {
    var UI;
    (function (UI) {
        var Xaml;
        (function (Xaml) {
            let ApplicationTheme;
            (function (ApplicationTheme) {
                ApplicationTheme["Light"] = "Light";
                ApplicationTheme["Dark"] = "Dark";
            })(ApplicationTheme = Xaml.ApplicationTheme || (Xaml.ApplicationTheme = {}));
        })(Xaml = UI.Xaml || (UI.Xaml = {}));
    })(UI = Windows.UI || (Windows.UI = {}));
})(Windows || (Windows = {}));
var Windows;
(function (Windows) {
    var Phone;
    (function (Phone) {
        var Devices;
        (function (Devices) {
            var Notification;
            (function (Notification) {
                class VibrationDevice {
                    static initialize() {
                        navigator.vibrate = navigator.vibrate || navigator.webkitVibrate || navigator.mozVibrate || navigator.msVibrate;
                        if (navigator.vibrate) {
                            return true;
                        }
                        return false;
                    }
                    static vibrate(duration) {
                        return window.navigator.vibrate(duration);
                    }
                }
                Notification.VibrationDevice = VibrationDevice;
            })(Notification = Devices.Notification || (Devices.Notification = {}));
        })(Devices = Phone.Devices || (Phone.Devices = {}));
    })(Phone = Windows.Phone || (Windows.Phone = {}));
})(Windows || (Windows = {}));
var Windows;
(function (Windows) {
    var UI;
    (function (UI) {
        var Xaml;
        (function (Xaml) {
            var Media;
            (function (Media) {
                var Animation;
                (function (Animation) {
                    class RenderingLoopFloatAnimator {
                        constructor(managedHandle) {
                            this.managedHandle = managedHandle;
                            this._isEnabled = false;
                        }
                        static createInstance(managedHandle, jsHandle) {
                            RenderingLoopFloatAnimator.activeInstances[jsHandle] = new RenderingLoopFloatAnimator(managedHandle);
                        }
                        static getInstance(jsHandle) {
                            return RenderingLoopFloatAnimator.activeInstances[jsHandle];
                        }
                        static destroyInstance(jsHandle) {
                            delete RenderingLoopFloatAnimator.activeInstances[jsHandle];
                        }
                        SetStartFrameDelay(delay) {
                            this.unscheduleFrame();
                            if (this._isEnabled) {
                                this.scheduleDelayedFrame(delay);
                            }
                        }
                        SetAnimationFramesInterval() {
                            this.unscheduleFrame();
                            if (this._isEnabled) {
                                this.onFrame();
                            }
                        }
                        EnableFrameReporting() {
                            if (this._isEnabled) {
                                return;
                            }
                            this._isEnabled = true;
                            this.scheduleAnimationFrame();
                        }
                        DisableFrameReporting() {
                            this._isEnabled = false;
                            this.unscheduleFrame();
                        }
                        onFrame() {
                            Uno.Foundation.Interop.ManagedObject.dispatch(this.managedHandle, "OnFrame", null);
                            // Schedule a new frame only if still enabled and no frame was scheduled by the managed OnFrame
                            if (this._isEnabled && this._frameRequestId == null && this._delayRequestId == null) {
                                this.scheduleAnimationFrame();
                            }
                        }
                        unscheduleFrame() {
                            if (this._delayRequestId != null) {
                                clearTimeout(this._delayRequestId);
                                this._delayRequestId = null;
                            }
                            if (this._frameRequestId != null) {
                                window.cancelAnimationFrame(this._frameRequestId);
                                this._frameRequestId = null;
                            }
                        }
                        scheduleDelayedFrame(delay) {
                            this._delayRequestId = setTimeout(() => {
                                this._delayRequestId = null;
                                this.onFrame();
                            }, delay);
                        }
                        scheduleAnimationFrame() {
                            this._frameRequestId = window.requestAnimationFrame(() => {
                                this._frameRequestId = null;
                                this.onFrame();
                            });
                        }
                    }
                    RenderingLoopFloatAnimator.activeInstances = {};
                    Animation.RenderingLoopFloatAnimator = RenderingLoopFloatAnimator;
                })(Animation = Media.Animation || (Media.Animation = {}));
            })(Media = Xaml.Media || (Xaml.Media = {}));
        })(Xaml = UI.Xaml || (UI.Xaml = {}));
    })(UI = Windows.UI || (Windows.UI = {}));
})(Windows || (Windows = {}));
