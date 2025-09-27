﻿declare const config: any;

// eslint-disable-next-line @typescript-eslint/no-namespace
namespace Uno.UI {

	export class WindowManager {

		public static current: WindowManager;

		private static readonly unoRootClassName = "uno-root-element";
		private static readonly unoUnarrangedClassName = "uno-unarranged";
		private static readonly unoCollapsedClassName = "uno-visibility-collapsed";
		private static readonly unoPersistentLoaderClassName = "uno-persistent-loader";
		private static readonly unoKeepLoaderClassName = "uno-keep-loader";

		/**
			* Initialize the WindowManager
			* @param containerElementId The ID of the container element for the Xaml UI
			* @param loadingElementId The ID of the loading element to remove once ready
			*/
		public static async init(containerElementId: string = "uno-body", loadingElementId: string = "uno-loading") {

			HtmlDom.initPolyfills();

			await WindowManager.initMethods();

			Uno.UI.Dispatching.NativeDispatcher.init(WindowManager.buildReadyPromise());

			this.current = new WindowManager(containerElementId, loadingElementId);
			MonoSupport.jsCallDispatcher.registerScope("Uno", this.current);
			this.current.init();
		}

		/**
		 * Builds a promise that will signal the ability for the dispatcher
		 * to initiate work.
		 * */
		private static buildReadyPromise(): Promise<boolean> {
			return new Promise<boolean>(resolve => {
				Promise.all(
					[WindowManager.buildSplashScreen()]
				).then(() => resolve(true))
			});
		}

		/**
		 * Build the splashscreen image eagerly
		 * */
		private static buildSplashScreen(): Promise<boolean> {
			return new Promise<boolean>(resolve => {

				let bootstrapperLoaders = document.getElementsByClassName(WindowManager.unoPersistentLoaderClassName);
				if (bootstrapperLoaders.length > 0) {
					// Bootstrapper supports persistent loader, skip creating local one and keep it displayed
					let bootstrapperLoader = bootstrapperLoaders[0] as HTMLElement;
					bootstrapperLoader.classList.add(WindowManager.unoKeepLoaderClassName);

					resolve(true);
				}
				else {
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
								let canvas = document.createElement("canvas");
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
							} else {
								WindowManager.setupSplashScreen(img);
								resolve(true);
							}
						}
					};

					// Preload the splash screen so the image element
					// created later on 
					img.onload = loadingDone;
					img.onerror = loadingDone;

					const UNO_BOOTSTRAP_APP_BASE = config.environmentVariables["UNO_BOOTSTRAP_APP_BASE"] || "";
					const UNO_BOOTSTRAP_WEBAPP_BASE_PATH = config.environmentVariables["UNO_BOOTSTRAP_WEBAPP_BASE_PATH"] || "";

					let fullImagePath = String(UnoAppManifest.splashScreenImage);

					// If the splashScreenImage image already points to the app base path, use it, otherwise we build it.
					if (UNO_BOOTSTRAP_APP_BASE !== "" && fullImagePath.indexOf(UNO_BOOTSTRAP_APP_BASE) == -1) {
						fullImagePath = `${UNO_BOOTSTRAP_WEBAPP_BASE_PATH}${UNO_BOOTSTRAP_APP_BASE}/${UnoAppManifest.splashScreenImage}`;
					}

					img.src = fullImagePath;

					// If there's no response, skip the loading
					setTimeout(loadingDone, 2000);
				}
			});
		}

		private containerElement: HTMLDivElement;
		private rootElement: HTMLElement;

		private cursorStyleRule: CSSStyleRule;

		private allActiveElementsById: { [id: string]: HTMLElement | SVGElement } = {};

		/** Native elements created with the BrowserHtmlElement class */
		private nativeHandlersMap: { [id: string]: any } = {};

		private uiElementRegistrations: {
			[id: string]: {
				typeName: string;
				isFrameworkElement: boolean;
				classNames: string[];
			};
		} = {};

		private static resizeMethod: any;
		private static dispatchEventMethod: any;
		private static dispatchEventNativeElementMethod: any;
		private static focusInMethod: any;
		private static dispatchSuspendingMethod: any;
		private static getDependencyPropertyValueMethod: any;
		private static setDependencyPropertyValueMethod: any;
		private static keyTrackingMethod: any;

		private constructor(private containerElementId: string, private loadingElementId: string) {
			this.initDom();
		}

		/**
			* Creates the UWP-compatible splash screen
			* 
			*/
		static setupSplashScreen(splashImage: HTMLImageElement): void {

			if (UnoAppManifest && UnoAppManifest.splashScreenImage) {

				const unoBody = document.getElementById("uno-body");

				if (unoBody) {
					const unoLoading = document.createElement("div");
					unoLoading.id = "uno-loading";

					if (UnoAppManifest.lightThemeBackgroundColor) {
						unoLoading.style.setProperty("--light-theme-bg-color", UnoAppManifest.lightThemeBackgroundColor);
					}
					if (UnoAppManifest.darkThemeBackgroundColor) {
						unoLoading.style.setProperty("--dark-theme-bg-color", UnoAppManifest.darkThemeBackgroundColor);
					}

					if (UnoAppManifest.splashScreenColor && UnoAppManifest.splashScreenColor != 'transparent') {
						unoLoading.style.backgroundColor = UnoAppManifest.splashScreenColor;
					}

					splashImage.id = "uno-loading-splash";
					splashImage.classList.add("uno-splash");

					unoLoading.appendChild(splashImage);

					unoBody.appendChild(unoLoading);
				}

				const loading = document.getElementById("loading");

				if (loading) {
					loading.remove();
				}
			}
		}

		static setBodyCursor(value: string): void {
			document.body.style.cursor = value;
		}

		static setSingleLine(htmlId: number): void {
			const element = this.current.getView(htmlId);
			if (element instanceof HTMLTextAreaElement) {
				element.addEventListener("keydown", e => {
					if (e.key === "Enter") {
						e.preventDefault();
					}
				})
			}
		}

		/**
			* Reads the window's search parameters
			* 
			*/
		static beforeLaunch(): string {
			WindowManager.resize();

			if (typeof URLSearchParams === "function") {
				return new URLSearchParams(window.location.search).toString();
			}
			else {
				const queryIndex = document.location.search.indexOf("?");

				if (queryIndex !== -1) {
					return document.location.search.substring(queryIndex + 1);
				}

				return "";
			}
		}

		/**
			* Estimated application startup time
			*/
		public static getBootTime(): number {
			return Date.now() - performance.now();
		}

		public containsPoint(htmlId: number, x: number, y: number, considerFill: boolean, considerStroke: boolean): boolean {
			const view = this.getView(htmlId);
			if (view instanceof SVGGeometryElement) {
				try {
					const point = new DOMPoint(x, y);
					return (considerFill && view.isPointInFill(point)) ||
						(considerStroke && view.isPointInStroke(point));
				}
				catch (e) {
					// SVGPoint is deprecated, but only Firefox and Safari supports DOMPoint
					const svgElement = view.closest("svg");
					const point = svgElement.createSVGPoint();
					point.x = x;
					point.y = y;
					return (considerFill && view.isPointInFill(point)) ||
						(considerStroke && view.isPointInStroke(point));
				}
			}

			return false;
		}

		/**
			* Create a html DOM element representing a Xaml element.
			*
			* You need to call addView to connect it to the DOM.
			*/
		public createContentNativeFast(
			htmlId: number,
			tagName: string,
			uiElementRegistrationId: number,
			isFocusable: boolean,
			isSvg: boolean) {

			this.createContentInternal({
				id: this.handleToString(htmlId),
				handle: htmlId, /* handle is htmlId */
				tagName: tagName,
				uiElementRegistrationId: uiElementRegistrationId,
				isFocusable: isFocusable,
				isSvg: isSvg
			});
		}

		private createContentInternal(contentDefinition: IContentDefinition): void {
			// Create the HTML element
			const element =
				contentDefinition.isSvg
					? document.createElementNS("http://www.w3.org/2000/svg", contentDefinition.tagName)
					: document.createElement(contentDefinition.tagName);

			element.id = contentDefinition.id;

			const uiElementRegistration =
				this.uiElementRegistrations[this.handleToString(contentDefinition.uiElementRegistrationId)];
			if (!uiElementRegistration) {
				throw `UIElement registration id ${contentDefinition.uiElementRegistrationId} is unknown.`;
			}

			element.setAttribute("XamlType", uiElementRegistration.typeName);
			element.setAttribute("XamlHandle", this.handleToString(contentDefinition.handle));
			if (uiElementRegistration.isFrameworkElement) {
				this.setAsUnarranged(element, true);
			}
			if (element.hasOwnProperty("tabindex")) {
				(element as any)["tabindex"] = contentDefinition.isFocusable ? 0 : -1;
			} else {
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

		public registerUIElement(typeName: string, isFrameworkElement: boolean, classNames: string[]): number {

			const registrationId = Object.keys(this.uiElementRegistrations).length;

			this.uiElementRegistrations[this.handleToString(registrationId)] = {
				classNames: classNames,
				isFrameworkElement: isFrameworkElement,
				typeName: typeName,
			};

			return registrationId;
		}

		public getView(elementHandle: number): HTMLElement | SVGElement {
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
		public setNameNative(pParam: number): boolean {
			const params = WindowManagerSetNameParams.unmarshal(pParam);
			this.setNameInternal(params.HtmlId, params.Name);
			return true;
		}

		private setNameInternal(elementId: number, name: string): void {
			this.getView(elementId).setAttribute("xamlname", name);
		}

		/**
			* Set a name for an element.
			*
			* This is mostly for diagnostic purposes.
			*/
		public setXUidNative(pParam: number): boolean {
			const params = WindowManagerSetXUidParams.unmarshal(pParam);
			this.setXUidInternal(params.HtmlId, params.Uid);
			return true;
		}

		private setXUidInternal(elementId: number, name: string): void {
			this.getView(elementId).setAttribute("xuid", name);
		}

		public setVisibilityNativeFast(htmlId: number, visible: boolean) {

			this.setVisibilityInternal(htmlId, visible);
		}

		private setVisibilityInternal(elementId: number, visible: boolean): void {
			const element = this.getView(elementId);

			if (visible) {
				element.classList.remove(WindowManager.unoCollapsedClassName);
			}
			else {
				element.classList.add(WindowManager.unoCollapsedClassName);
			}
		}

		/**
			* Set an attribute for an element.
			*/
		public setAttributesNativeFast(htmlId: number, pairs: string[]) {

			const element = this.getView(htmlId);

			const length = pairs.length;

			for (let i = 0; i < length; i += 2) {
				element.setAttribute(pairs[i], pairs[i + 1]);
			}
		}

		/**
			* Set an attribute for an element.
			*/
		public setAttribute(htmlId: number, name: string, value: string) {
			const element = this.getView(htmlId);
			element.setAttribute(name, value);
		}

		/**
			* Removes an attribute for an element.
			*/
		public removeAttributeNative(pParams: number): boolean {

			const params = WindowManagerRemoveAttributeParams.unmarshal(pParams);
			const element = this.getView(params.HtmlId);
			element.removeAttribute(params.Name);

			return true;
		}

		/**
			* Get an attribute for an element.
			*/
		public getAttribute(elementId: number, name: string): string {

			return this.getView(elementId).getAttribute(name);
		}

		/**
			* Set a property for an element.
			*/
		public setPropertyNativeFast(htmlId: number, pairs: string[]) {

			const element = this.getView(htmlId);

			const length = pairs.length;

			for (let i = 0; i < length; i += 2) {

				const setVal = pairs[i + 1];

				if (setVal === "true") {
					(element as any)[pairs[i]] = true;
				}
				else if (setVal === "false") {
					(element as any)[pairs[i]] = false;
				}
				else {
					(element as any)[pairs[i]] = setVal;
				}
			}
		}

		public setSinglePropertyNativeFast(htmlId: number, name: string, value: string) {

			const element = this.getView(htmlId);
			if (value === "true") {
				(element as any)[name] = true;
			}
			else if (value === "false") {
				(element as any)[name] = false;
			}
			else {
				(element as any)[name] = value;
			}
		}

		/**
			* Get a property for an element.
			*/
		public getProperty(elementId: number, name: string): string {
			const element = <any>this.getView(elementId);

			return (element[name] || "").toString();
		}

		/**
		* Set the CSS style of a html element.
		*
		* To remove a value, set it to empty string.
		* @param styles A dictionary of styles to apply on html element.
		*/
		public setStyleNativeFast(htmlId: number, styles: string[]) {

			const elementStyle = this.getView(htmlId).style;

			const stylesLength = styles.length;

			for (let i = 0; i < stylesLength; i += 2) {
				elementStyle.setProperty(styles[i], styles[i + 1]);
			}
		}

		/**
		* Set a single CSS style of a html element
		*
		*/
		public setStyleDoubleNative(pParams: number): boolean {

			const params = WindowManagerSetStyleDoubleParams.unmarshal(pParams);
			const element = this.getView(params.HtmlId);

			element.style.setProperty(params.Name, this.handleToString(params.Value));

			return true;
		}

		public setStyleStringNativeFast(htmlId: number, name: string, value: string) {

			this.getView(htmlId).style.setProperty(name, value);
		}

		/**
			* Remove the CSS style of a html element.
			*/
		public resetStyle(elementId: number, names: string[]): void {
			const element = this.getView(elementId);

			for (const name of names) {
				element.style.setProperty(name, "");
			}
		}

		public isCssConditionSupported(supportCondition: string): boolean {
			return CSS.supports(supportCondition);
		}

		/**
		 * Set + Unset CSS classes on an element
		 */
		public setUnsetCssClasses(elementId: number, cssClassesToSet: string[], cssClassesToUnset: string[]) {
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

		/**
		 * Set CSS classes on an element from a specified list
		 */
		public setClasses(elementId: number, cssClassesList: string[], classIndex: number) {
			const element = this.getView(elementId);

			for (let i = 0; i < cssClassesList.length; i++) {
				if (i === classIndex) {
					element.classList.add(cssClassesList[i]);
				} else {
					element.classList.remove(cssClassesList[i]);
				}
			}
		}

		/**
		* Arrange and clips a native elements 
		*
		*/
		public arrangeElementNativeFast(
			htmlId: number,
			top: number,
			left: number,
			width: number,
			height: number,
			clip: boolean,
			clipTop: number,
			clipLeft: number,
			clipBottom: number,
			clipRight: number) {

			const element = this.getView(htmlId);

			const style = element.style;

			style.position = "absolute";
			style.top = top + "px";
			style.left = left + "px";
			style.width = width === NaN ? "auto" : width + "px";
			style.height = height === NaN ? "auto" : height + "px";

			if (clip) {
				style.clip = `rect(${clipTop}px, ${clipRight}px, ${clipBottom}px, ${clipLeft}px)`;
			} else {
				style.clip = "";
			}

			this.setAsArranged(element);
		}

		private setAsArranged(element: HTMLElement | SVGElement) {

			if (!(<any>element)._unoIsArranged) {
				(<any>element)._unoIsArranged = true;
				element.classList.remove(WindowManager.unoUnarrangedClassName);
			}
		}

		private setAsUnarranged(element: HTMLElement | SVGElement, force: boolean = false) {
			if ((<any>element)._unoIsArranged || force) {
				(<any>element)._unoIsArranged = false;
				element.classList.add(WindowManager.unoUnarrangedClassName);
			}
		}

		/**
		* Sets the color property of the specified element
		*/
		public setElementColorNative(pParam: number): boolean {
			const params = WindowManagerSetElementColorParams.unmarshal(pParam);
			this.setElementColorInternal(params.HtmlId, params.Color);
			return true;
		}

		private setElementColorInternal(elementId: number, color: number): void {
			const element = this.getView(elementId);

			element.style.setProperty("color", this.numberToCssColor(color));
		}

		/**
		 * Sets the element's selection highlight.
		**/

		public setSelectionHighlight(elementId: number, backgroundColor: number, foregroundColor: number): boolean {
			const element = this.getView(elementId);
			element.classList.add("selection-highlight");
			element.style.setProperty("--selection-background", this.numberToCssColor(backgroundColor));
			element.style.setProperty("--selection-color", this.numberToCssColor(foregroundColor));
			return true;
		}

		public setSelectionHighlightNative(pParam: number): boolean {
			const params = WindowManagerSetSelectionHighlightParams.unmarshal(pParam);
			return this.setSelectionHighlight(params.HtmlId, params.BackgroundColor, params.ForegroundColor);
		}

		/**
		* Sets the background color property of the specified element
		*/
		public setElementBackgroundColor(pParam: number): boolean {
			const params = WindowManagerSetElementBackgroundColorParams.unmarshal(pParam);

			const element = this.getView(params.HtmlId);
			const style = element.style;

			style.setProperty("background-color", this.numberToCssColor(params.Color));
			style.removeProperty("background-image");

			return true;
		}

		/**
		* Sets the background image property of the specified element
		*/
		public setElementBackgroundGradient(pParam: number): boolean {
			const params = WindowManagerSetElementBackgroundGradientParams.unmarshal(pParam);

			const element = this.getView(params.HtmlId);
			const style = element.style;

			style.removeProperty("background-color");
			style.setProperty("background-image", params.CssGradient);

			return true;
		}

		/**
		* Clears the background property of the specified element
		*/
		public resetElementBackground(pParam: number): boolean {
			const params = WindowManagerResetElementBackgroundParams.unmarshal(pParam);

			const element = this.getView(params.HtmlId);
			const style = element.style;

			style.removeProperty("background-color");
			style.removeProperty("background-image");
			style.removeProperty("background-size");

			return true;
		}

		/**
		* Sets the transform matrix of an element
		*
		*/
		public setElementTransformNativeFast(
			htmlId: number,
			m11: number,
			m12: number,
			m21: number,
			m22: number,
			m31: number,
			m32: number) {

			const element = this.getView(htmlId);

			element.style.transform = `matrix(${m11},${m12},${m21},${m22},${m31},${m32})`;

			this.setAsArranged(element);
		}

		public setPointerEvents(htmlId: number, enabled: boolean) {
			this.getView(htmlId).style.pointerEvents = enabled ? "auto" : "none";
		}

		/**
			* Issue a browser alert to user
			* @param message message to display
			*/
		public alert(message: string): string {
			window.alert(message);

			return "ok";
		}

		/**
			* Add an event handler to a html element.
			*
			* @param eventName The name of the event
			* @param onCapturePhase true means "on trickle down", false means "on bubble up". Default is false.
			*/
		public registerEventOnViewNative(pParams: number): boolean {
			const params = WindowManagerRegisterEventOnViewParams.unmarshal(pParams);

			this.registerEventOnViewInternal(
				params.HtmlId,
				params.EventName,
				params.OnCapturePhase,
				params.EventExtractorId);

			return true;
		}

		/**
			* Add an event handler to a html element.
			*
			* @param eventName The name of the event
			* @param onCapturePhase true means "on trickle down", false means "on bubble up". Default is false.
			*/
		private registerEventOnViewInternal(
			elementId: number,
			eventName: string,
			onCapturePhase: boolean = false,
			eventExtractorId: number
		): void {
			const element = this.getView(elementId);
			const eventExtractor = this.getEventExtractor(eventExtractorId);
			const eventHandler = (event: Event) => {
				const eventPayload = eventExtractor
					? `${eventExtractor(event)}`
					: "";

				const result = this.dispatchEvent(element, eventName, eventPayload, onCapturePhase);
				if (result & HtmlEventDispatchResult.StopPropagation) {
					event.stopPropagation();
				}
				if (result & HtmlEventDispatchResult.PreventDefault) {
					event.preventDefault();
				}
			};

			element.addEventListener(eventName, eventHandler, onCapturePhase);
		}

		/**
		 * keyboard event extractor to be used with registerEventOnView
		 * @param evt
		 */
		private keyboardEventExtractor(evt: Event): string {
			return (evt instanceof KeyboardEvent) ? `${(evt.ctrlKey ? "1" : "0")}${(evt.altKey ? "1" : "0")}${(evt.metaKey ? "1" : "0")}${(evt.shiftKey ? "1" : "0")}${evt.key}` : "0";
		}

		/**
		 * tapped (mouse clicked / double clicked) event extractor to be used with registerEventOnView
		 * @param evt
		 */
		private tappedEventExtractor(evt: MouseEvent): string {
			return evt
				? `0;${evt.clientX};${evt.clientY};${(evt.ctrlKey ? "1" : "0")};${(evt.shiftKey ? "1" : "0")};${evt.button};mouse`
				: "";
		}

		/**
		 * focus event extractor to be used with registerEventOnView
		 * @param evt
		 */
		private focusEventExtractor(evt: FocusEvent): string {

			if (evt) {
				const targetElement: HTMLElement | SVGElement = <any>evt.target;

				if (targetElement) {
					const targetXamlHandle = targetElement.getAttribute("XamlHandle");

					if (targetXamlHandle) {
						return `${targetXamlHandle}`;
					}
				}
			}

			return "";
		}

		private customEventDetailExtractor(evt: CustomEvent): string {
			if (evt) {
				const detail = evt.detail;
				if (detail) {
					return JSON.stringify(detail);
				}
			}

			return "";
		}

		private customEventDetailStringExtractor(evt: CustomEvent): string {
			return evt ? `${evt.detail}` : "";
		}

		/**
		 * Gets the event extractor function. See UIElement.HtmlEventExtractor
		 * @param eventExtractorName an event extractor name.
		 */
		private getEventExtractor(eventExtractorId: number): (evt: Event) => string {

			if (eventExtractorId) {
				//
				// NOTE TO MAINTAINERS: Keep in sync with Microsoft.UI.Xaml.UIElement.HtmlEventExtractor
				//

				switch (eventExtractorId) {
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
			* Set or replace the root element.
			*/
		public setRootElement(elementId?: number): void {
			if (this.rootElement && Number(this.rootElement.id) === elementId) {
				return null; // nothing to do
			}

			if (this.rootElement) {
				// Remove existing
				this.containerElement.removeChild(this.rootElement);

				this.rootElement.classList.remove(WindowManager.unoRootClassName);
			}

			if (!elementId) {
				return null;
			}

			// set new root
			const newRootElement = this.getView(elementId) as HTMLElement;
			newRootElement.classList.add(WindowManager.unoRootClassName);

			this.rootElement = newRootElement;

			this.containerElement.appendChild(this.rootElement);

			this.setAsArranged(newRootElement); // patch because root is not measured/arranged
		}

		/**
			* Set a view as a child of another one.
			* @param pParams Pointer to a WindowManagerAddViewParams native structure.
			*/
		public addViewNative(pParams: number): boolean {
			const params = WindowManagerAddViewParams.unmarshal(pParams);

			this.addViewInternal(
				params.HtmlId,
				params.ChildView,
				params.Index != -1 ? params.Index : null
			);

			return true;
		}

		public addViewInternal(parentId: number, childId: number, index?: number): void {
			const parentElement = this.getView(parentId);
			const childElement = this.getView(childId);

			if (index != null && index < parentElement.childElementCount) {
				const insertBeforeElement = parentElement.children[index];
				parentElement.insertBefore(childElement, insertBeforeElement);

			} else {
				parentElement.appendChild(childElement);
			}
		}

		/**
			* Remove a child from a parent element.
			*/
		public removeViewNative(pParams: number): boolean {
			const params = WindowManagerRemoveViewParams.unmarshal(pParams);
			this.removeViewInternal(params.HtmlId, params.ChildView);
			return true;
		}

		private removeViewInternal(parentId: number, childId: number): void {
			const parentElement = this.getView(parentId);
			const childElement = this.getView(childId);

			parentElement.removeChild(childElement);

			// Mark the element as unarranged, so if it gets measured while being
			// disconnected from the root element, it won't be visible.
			this.setAsUnarranged(childElement);
		}

		public destroyViewNativeFast(htmlId: number) {

			this.destroyViewInternal(htmlId);
		}

		private destroyViewInternal(elementId: number): void {
			const element = this.getView(elementId);

			if (element.parentElement) {
				element.parentElement.removeChild(element);
			}

			delete this.allActiveElementsById[elementId];
		}

		public getBBox(elementId: number): any {

			const element = this.getView(elementId) as SVGGraphicsElement;
			let unconnectedRoot: HTMLElement | SVGGraphicsElement = null;

			const cleanupUnconnectedRoot = (owner: HTMLDivElement) => {
				if (unconnectedRoot !== null) {
					owner.removeChild(unconnectedRoot);
				}
			}

			try {

				// On FireFox, the element needs to be connected to the DOM
				// or the getBBox() will crash.
				if (!element.isConnected) {

					unconnectedRoot = element;
					while (unconnectedRoot.parentElement) {
						// Need to find the top most "unconnected" parent
						// of this element
						unconnectedRoot = unconnectedRoot.parentElement as HTMLElement;
					}

					this.containerElement.appendChild(unconnectedRoot);
				}

				let bbox = element.getBBox();

				return [
					bbox.x,
					bbox.y,
					bbox.width,
					bbox.height];
			}
			finally {
				cleanupUnconnectedRoot(this.containerElement);
			}
		}

		/**
			* Use the Html engine to measure the element using specified constraints.
			*
			* @param maxWidth string containing width in pixels. Empty string means infinite.
			* @param maxHeight string containing height in pixels. Empty string means infinite.
			*/
		public measureViewNativeFast(htmlId: number, availableWidth: number, availableHeight: number, measureContent: boolean, pReturn: number) {

			const result = this.measureViewInternal(htmlId, availableWidth, availableHeight, measureContent);

			const desiredSize = new WindowManagerMeasureViewReturn();
			desiredSize.DesiredWidth = result[0];
			desiredSize.DesiredHeight = result[1];

			desiredSize.marshal(pReturn);
		}

		private static MAX_WIDTH = `${Number.MAX_SAFE_INTEGER}vw`;
		private static MAX_HEIGHT = `${Number.MAX_SAFE_INTEGER}vh`;

		private measureElement(element: HTMLElement): [number, number] {

			const offsetWidth = element.offsetWidth;
			const offsetHeight = element.offsetHeight;

			const resultWidth = offsetWidth ? offsetWidth : element.clientWidth;
			const resultHeight = offsetHeight ? offsetHeight : element.clientHeight;

			// +1 is added to take rounding/flooring into account
			return [resultWidth + 1, resultHeight];
		}

		private measureViewInternal(viewId: number, maxWidth: number, maxHeight: number, measureContent: boolean): [number, number] {
			const element = this.getView(viewId) as HTMLElement;

			const elementStyle = element.style;
			const elementClasses = element.className;
			const originalStyleCssText = elementStyle.cssText;
			const unconstrainedStyleCssText = this.createUnconstrainedStyle(elementStyle, maxWidth, maxHeight);

			let parentElement: HTMLElement = null;
			let parentElementWidthHeight: { width: string, height: string } = null;
			let unconnectedRoot: HTMLElement = null;

			const cleanupUnconnectedRoot = (owner: HTMLDivElement) => {
				if (unconnectedRoot !== null) {
					owner.removeChild(unconnectedRoot);
				}
			}

			try {
				if (!element.isConnected) {
					// If the element is not connected to the DOM, we need it
					// to be connected for the measure to provide a meaningful value.

					unconnectedRoot = element;
					while (unconnectedRoot.parentElement) {
						// Need to find the top most "unconnected" parent
						// of this element
						unconnectedRoot = unconnectedRoot.parentElement as HTMLElement;
					}

					this.containerElement.appendChild(unconnectedRoot);
				}

				if (measureContent && element instanceof HTMLImageElement) {
					elementStyle.cssText = unconstrainedStyleCssText;
					const imgElement = element as HTMLImageElement;
					return [imgElement.naturalWidth, imgElement.naturalHeight];
				} else if (measureContent && element instanceof HTMLInputElement) {
					elementStyle.cssText = unconstrainedStyleCssText;
					const inputElement = element as HTMLInputElement;

					cleanupUnconnectedRoot(this.containerElement);

					// Create a temporary element that will contain the input's content
					const textOnlyElement = document.createElement("p") as HTMLParagraphElement;
					textOnlyElement.style.cssText = unconstrainedStyleCssText;
					textOnlyElement.innerText = inputElement.value;
					textOnlyElement.className = elementClasses;

					unconnectedRoot = textOnlyElement;
					this.containerElement.appendChild(unconnectedRoot);

					const textSize = this.measureElement(textOnlyElement);
					const inputSize = this.measureElement(element);

					// Take the width of the inner text, but keep the height of the input element.
					return [textSize[0], inputSize[1]];
				} else if (measureContent && element instanceof HTMLTextAreaElement) {
					const inputElement = element;

					cleanupUnconnectedRoot(this.containerElement);

					// Create a temporary element that will contain the input's content
					const textOnlyElement = document.createElement("p") as HTMLParagraphElement;
					textOnlyElement.style.cssText = unconstrainedStyleCssText;

					// If the input is null or empty, add a no-width character to force the paragraph to take up one line height
					// The trailing new lines are going to be ignored for measure, so we also append no-width char at the end.
					textOnlyElement.innerText = inputElement.value ? (inputElement.value + "\u200B") : "\u200B";
					textOnlyElement.className = elementClasses; // Note: Here we will have the uno-textBoxView class name

					unconnectedRoot = textOnlyElement;
					this.containerElement.appendChild(unconnectedRoot);

					const textSize = this.measureElement(textOnlyElement);

					// For TextAreas, take the width and height of the inner text
					const width = Math.min(textSize[0], maxWidth);
					const height = Math.min(textSize[1], maxHeight);
					return [width, height];
				} else {
					elementStyle.cssText = unconstrainedStyleCssText;

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

		private createUnconstrainedStyle(elementStyle: CSSStyleDeclaration, maxWidth: number, maxHeight: number): string {
			const updatedStyles = <any>{};

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

			// This is necessary because in Safari 17 "white-space" is not selected by index (i.e. elementStyle[i])
			// This is important to implement the Wrap/NoWrap of Controls
			if (elementStyle.cssText.includes("white-space") && !updatedStyleString.includes("white-space"))
				updatedStyleString += "white-space: " + elementStyle.whiteSpace + "; ";

			// We use a string to prevent the browser to update the element between
			// each style assignation. This way, the browser will update the element only once.
			return updatedStyleString;
		}

		public scrollTo(pParams: number): boolean {

			const params = WindowManagerScrollToOptionsParams.unmarshal(pParams);
			const elt = this.getView(params.HtmlId);
			const opts = <ScrollToOptions>({
				left: params.HasLeft ? params.Left : undefined,
				top: params.HasTop ? params.Top : undefined,
				behavior: <ScrollBehavior>(params.DisableAnimation ? "instant" : "smooth")
			});

			elt.scrollTo(opts);

			return true;
		}

		public rawPixelsToBase64EncodeImage(dataPtr: number, width: number, height: number): string {
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
		public setImageAsMonochrome(viewId: number, url: string, color: string): void {
			const element = this.getView(viewId);

			if (element.tagName.toUpperCase() === "IMG") {

				const imgElement = element as HTMLImageElement;
				const img = new Image();
				img.onload = buildMonochromeImage;
				img.src = url;

				function buildMonochromeImage() {

					// create a colored version of img
					const c = document.createElement("canvas");
					const ctx = c.getContext("2d");

					c.width = img.width;
					c.height = img.height;

					ctx.drawImage(img, 0, 0);
					ctx.globalCompositeOperation = "source-atop";
					ctx.fillStyle = color;
					ctx.fillRect(0, 0, img.width, img.height);
					ctx.globalCompositeOperation = "source-over";

					imgElement.src = c.toDataURL();
				}
			}
			else {
				throw `setImageAsMonochrome: Element id ${viewId} is not an Img.`;
			}
		}

		public setCornerRadius(viewId: number, topLeftX: number, topLeftY: number, topRightX: number, topRightY: number, bottomRightX: number, bottomRightY: number, bottomLeftX: number, bottomLeftY: number) {
			const element = this.getView(viewId);
			element.style.borderRadius = `${topLeftX}px ${topRightX}px ${bottomRightX}px ${bottomLeftX}px / ${topLeftY}px ${topRightY}px ${bottomRightY}px ${bottomLeftY}px`;
			element.style.overflow = "hidden"; // overflow: hidden is required here because the clipping can't do its job when it's non-rectangular.
		}

		public focusView(elementId: number): void {
			const element = this.getView(elementId);

			if (!(element instanceof HTMLElement)) {
				throw `Element id ${elementId} is not focusable.`;
			}

			element.focus({ preventScroll: true });
		}

		/**
			* Set the Html content for an element.
			*
			* Those html elements won't be available as XamlElement in managed code.
			* WARNING: you should avoid mixing this and `addView` for the same element.
			*/
		public setHtmlContentNative(pParams: number): boolean {
			const params = WindowManagerSetContentHtmlParams.unmarshal(pParams);

			this.setHtmlContentInternal(params.HtmlId, params.Html);
			return true;
		}

		private setHtmlContentInternal(viewId: number, html: string): void {

			this.getView(viewId).innerHTML = html;
		}

		/**
		 * Gets the Client and Offset size of the specified element
		 *
		 * This method is used to determine the size of the scroll bars, to
		 * mask the events coming from that zone.
		 */
		public getClientViewSizeNative(pParams: number, pReturn: number): boolean {
			const params = WindowManagerGetClientViewSizeParams.unmarshal(pParams);

			const element = this.getView(params.HtmlId) as HTMLElement;

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
		public GetDependencyPropertyValue(elementId: number, propertyName: string): string {
			if (!WindowManager.getDependencyPropertyValueMethod) {
				if ((<any>globalThis).DotnetExports !== undefined) {
					WindowManager.getDependencyPropertyValueMethod = (<any>globalThis).DotnetExports.UnoUI.Uno.UI.Helpers.Automation.GetDependencyPropertyValue;
				} else {
					WindowManager.getDependencyPropertyValueMethod = (<any>Module).mono_bind_static_method("[Uno.UI] Uno.UI.Helpers.Automation:GetDependencyPropertyValue");
				}
			}

			const element = this.getView(elementId) as HTMLElement;
			const htmlId = Number(element.getAttribute("XamlHandle"));

			return WindowManager.getDependencyPropertyValueMethod(htmlId, propertyName);
		}

		/**
		 * Sets a dependency property value.
		 *
		 * Note that the casing of this method is intentionally Pascal for platform alignment.
		 */
		public SetDependencyPropertyValue(elementId: number, propertyNameAndValue: string): string {
			if (!WindowManager.setDependencyPropertyValueMethod) {
				if ((<any>globalThis).DotnetExports !== undefined) {
					WindowManager.setDependencyPropertyValueMethod = (<any>globalThis).DotnetExports.UnoUI.Uno.UI.Helpers.Automation.SetDependencyPropertyValue;
				} else {
					throw `SetDependencyPropertyValue: Unable to find dotnet exports`;
				}
			}

			const element = this.getView(elementId) as HTMLElement;
			const htmlId = Number(element.getAttribute("XamlHandle"));

			return WindowManager.setDependencyPropertyValueMethod(htmlId, propertyNameAndValue);
		}

		/**
			* Remove the loading indicator.
			*
			* In a future version it will also handle the splashscreen.
			*/
		public activate(): void {
			this.removeLoading();
		}

		/**
		 * Creates a native element from BrowserHttpElement.
		 */
		public createNativeElement(elementId: string, unoElementId: number, tagname: string): void {
			const element = document.createElement(tagname);
			element.id = elementId;
			(<any>element).unoId = unoElementId;

			// Add the html element to list of elements
			this.allActiveElementsById[this.handleToString(unoElementId)] = element;
		}

		/**
		 * Dispose a native element
		 */
		public disposeNativeElement(unoElementId: number): void {
			this.destroyViewInternal(unoElementId);
		}

		/**
		 * Attaches a native element to a known UIElement-backed element.
		 */
		public attachNativeElement(ownerId: number, unoElementId: number): void {
			var ownerView = this.getView(ownerId);
			var elementView = this.getView(unoElementId);

			ownerView.appendChild(elementView);
		}

		/**
		 * Detaches a native element to a known UIElement-backed element.
		 */
		public detachNativeElement(unoElementId: number): void {
			var view = this.getView(unoElementId);

			view.parentElement.removeChild(view);
		}

		/**
		 * Registers a managed event handler
		 */
		public registerNativeHtmlEvent(
			owner: any,
			unoElementId: any,
			eventName: string,
			managedHandler: any
		) {
			const element = this.getView(unoElementId);

			const eventHandler = (event: Event) => {
				WindowManager.dispatchEventNativeElementMethod(owner, eventName, managedHandler, event);
			};

			// Register the handler using a string representation of the managed handler
			// the managed representation assumes that the string contains a unique id.
			this.nativeHandlersMap["" + managedHandler] = eventHandler;

			element.addEventListener(eventName, eventHandler);
		}

		/**
		 * Unregisters a managed handler from its element
		 */
		public unregisterNativeHtmlEvent(
			unoElementId: any,
			eventName: string,
			managedHandler: any) {
			const element = this.getView(unoElementId);
			const key = "" + managedHandler;
			const eventHandler = this.nativeHandlersMap[key];
			if (eventHandler) {
				element.removeEventListener(eventName, eventHandler);
				delete this.nativeHandlersMap[key];
			}
		}

		private init() {

			if (UnoAppManifest.displayName) {
				document.title = UnoAppManifest.displayName;
			}

			window.addEventListener(
				"beforeunload",
				() => WindowManager.dispatchSuspendingMethod()
			);
		}

		private static async initMethods() {

			await ExportManager.initialize();

			if ((<any>globalThis).DotnetExports !== undefined) {
				const exports = (<any>globalThis).DotnetExports.UnoUI;

				WindowManager.resizeMethod = exports.Microsoft.UI.Xaml.Window.Resize;
				WindowManager.dispatchEventMethod = exports.Microsoft.UI.Xaml.UIElement.DispatchEvent;
				WindowManager.dispatchEventNativeElementMethod = exports.Uno.UI.NativeElementHosting.BrowserHtmlElement.DispatchEventNativeElementMethod;
				WindowManager.focusInMethod = exports.Microsoft.UI.Xaml.Input.FocusManager.ReceiveFocusNative;
				WindowManager.dispatchSuspendingMethod = exports.Microsoft.UI.Xaml.Application.DispatchSuspending;
				WindowManager.keyTrackingMethod = (<any>globalThis).DotnetExports.Uno.Uno.UI.Core.KeyboardStateTracker.UpdateKeyStateNative;
			} else {
				throw `WindowManager: Unable to find dotnet exports`;
			}
		}

		private initDom() {
			this.containerElement = (document.getElementById(this.containerElementId) as HTMLDivElement);
			if (!this.containerElement) {
				// If not found, we simply create a new one.
				this.containerElement = document.createElement("div");
			}
			document.body.addEventListener("focusin", this.onfocusin);
			document.body.appendChild(this.containerElement);

			// On WASM, if no one subscribes to key<Down|Up>, not only will the event not fire on any UIElement,
			// but the browser won't even notify us that a key was pressed/released, and this breaks KeyboardStateTracker
			// key tracking, which depends on RaiseEvent being called even if no one is subscribing. Instead, we
			// subscribe on the body and make sure to call KeyboardStateTracker ourselves here.
			document.body.addEventListener("keydown", this.onBodyKeyDown);
			document.body.addEventListener("keyup", this.onBodyKeyUp);

			window.addEventListener("resize", x => WindowManager.resize());
			window.addEventListener("contextmenu", x => {
				if (!(x.target instanceof HTMLInputElement) ||
					x.target.classList.contains("context-menu-disabled")) {
					x.preventDefault();
				}
			})
			window.addEventListener("blur", this.onWindowBlur);
		}

		private removeLoading() {
			const element = document.getElementById(this.loadingElementId);
			if (element) {
				element.parentElement.removeChild(element);
			}

			let bootstrapperLoaders = document.getElementsByClassName(WindowManager.unoPersistentLoaderClassName);
			if (bootstrapperLoaders.length > 0) {
				let bootstrapperLoader = bootstrapperLoaders[0] as HTMLElement;
				bootstrapperLoader.parentElement.removeChild(bootstrapperLoader);
			}
		}

		private static resize() {
			WindowManager.resizeMethod(document.documentElement.clientWidth, document.documentElement.clientHeight);
		}

		private onfocusin(event: Event) {
			const newFocus = event.target;
			const handle = (newFocus as HTMLElement).getAttribute("XamlHandle");
			const htmlId = handle ? Number(handle) : -1; // newFocus may not be an Uno element
			WindowManager.focusInMethod(htmlId);
		}

		private onWindowBlur() {
			// Unset managed focus when Window loses focus
			WindowManager.focusInMethod(-1);
		}

		private dispatchEvent(element: HTMLElement | SVGElement, eventName: string, eventPayload: string = null, onCapturePhase: boolean = false): HtmlEventDispatchResult {
			const htmlId = Number(element.getAttribute("XamlHandle"));

			// console.debug(`${element.getAttribute("id")}: Raising event ${eventName}.`);

			if (!htmlId) {
				throw `No attribute XamlHandle on element ${element}. Can't raise event.`;
			}

			return WindowManager.dispatchEventMethod(htmlId, eventName, eventPayload || "", onCapturePhase);
		}

		private getIsConnectedToRootElement(element: HTMLElement | SVGElement): boolean {
			const rootElement = this.rootElement;

			if (!rootElement) {
				return false;
			}
			return rootElement === element || rootElement.contains(element);
		}

		private handleToString(handle: number): string {

			// Fastest conversion as of 2020-03-25 (when compared to String(handle) or handle.toString())
			return handle + "";
		}

		private numberToCssColor(color: number): string {
			return "#" + color.toString(16).padStart(8, "0");
		}

		public getElementInCoordinate(x: number, y: number): number {
			const element = document.elementFromPoint(x, y) as HTMLElement;
			return Number(element.getAttribute("XamlHandle"));
		}

		public setCursor(cssCursor: string): string {
			const unoBody = document.getElementById(this.containerElementId);

			if (unoBody) {

				if (this.cursorStyleRule === undefined) {
					const styleSheet = document.styleSheets[document.styleSheets.length - 1];

					const ruleId = styleSheet.insertRule(".uno-buttonbase { }", styleSheet.cssRules.length);

					this.cursorStyleRule = <CSSStyleRule>styleSheet.cssRules[ruleId];
				}

				this.cursorStyleRule.style.cursor = cssCursor !== "auto" ? cssCursor : null;

				unoBody.style.cursor = cssCursor;
			}
			return "ok";
		}

		public getNaturalImageSize(imageUrl: string): Promise<string> {
			return new Promise<string>((resolve, reject) => {
				const img = new Image();

				let loadingDone = () => {
					this.containerElement.removeChild(img);
					resolve(`${img.width};${img.height}`);
				};
				let loadingError = (e: Event) => {
					this.containerElement.removeChild(img);
					reject(e);
				}

				img.style.pointerEvents = "none";
				img.style.opacity = "0";
				img.onload = loadingDone;
				img.onerror = loadingError;
				img.src = imageUrl;

				this.containerElement.appendChild(img);

			});
		}

		public selectInputRange(elementId: number, start: number, length: number) {
			(this.getView(elementId) as HTMLInputElement).setSelectionRange(start, start + length);
		}

		public getIsOverflowing(elementId: number): boolean {
			const element = this.getView(elementId) as HTMLElement;

			return element.clientWidth < element.scrollWidth || element.clientHeight < element.scrollHeight;
		}

		public setIsFocusable(elementId: number, isFocusable: boolean) {
			const element = this.getView(elementId) as HTMLElement;

			element.setAttribute("tabindex", isFocusable ? "0" : "-1");
		}

		public resizeWindow(width: number, height: number) {
			window.resizeTo(width, height);
		}

		public moveWindow(x: number, y: number) {
			window.moveTo(x, y);
		}

		private onBodyKeyDown(event: KeyboardEvent) {
			WindowManager.keyTrackingMethod(event.key, true);
		}

		private onBodyKeyUp(event: KeyboardEvent) {
			WindowManager.keyTrackingMethod(event.key, false);
		}

		private getCssColorOrUrlRef(color: number, paintRef: number): string {
			if (paintRef != null) {
				return `url(#${paintRef})`;
			}
			else if (color != null) {
				// JSInvoke doesnt allow passing of uint, so we had to deal with int's "sign-ness" here
				// (-1 >>> 0) is a quick hack to turn signed negative into "unsigned" positive
				// padded to 8-digits 'RRGGBBAA', so the value doesnt get processed as 'RRGGBB' or 'RGB'.
				return `#${(color >>> 0).toString(16).padStart(8, '0')}`;
			}
			else {
				return '';
			}
		}

		public setShapeFillStyle(elementId: number, color: number, paintRef: number): void {
			const e = this.getView(elementId);
			if (e instanceof SVGElement) {
				e.style.fill = this.getCssColorOrUrlRef(color, paintRef);
			}
		}

		public setShapeStrokeStyle(elementId: number, color: number, paintRef: number): void {
			const  e = this.getView(elementId);
			if (e instanceof SVGElement) {
				e.style.stroke = this.getCssColorOrUrlRef(color, paintRef);
			}
		}

		public setShapeStrokeWidthStyle(elementId: number, strokeWidth: number): void {
			const  e = this.getView(elementId);
			if (e instanceof SVGElement) {
				e.style.strokeWidth = `${strokeWidth}px`;
			}
		}

		public setShapeStrokeDashArrayStyle(elementId: number, strokeDashArray: number[]): void {
			const  e = this.getView(elementId);
			if (e instanceof SVGElement) {

				e.style.strokeDasharray = strokeDashArray.join(',');
			}
		}

		public setShapeStylesFast1(elementId: number, fillColor: number, fillPaintRef: number, strokeColor: number, strokePaintRef: number): void {
			const  e = this.getView(elementId);
			if (e instanceof SVGElement) {

				e.style.fill = this.getCssColorOrUrlRef(fillColor, fillPaintRef);
				e.style.stroke = this.getCssColorOrUrlRef(strokeColor, strokePaintRef);
			}
		}

		public setShapeStylesFast2(elementId: number, fillColor: number, fillPaintRef: number, strokeColor: number, strokePaintRef: number, strokeWidth: number, strokeDashArray: any[]): void {
			const  e = this.getView(elementId);
			if (e instanceof SVGElement) {

				e.style.fill = this.getCssColorOrUrlRef(fillColor, fillPaintRef);
				e.style.stroke = this.getCssColorOrUrlRef(strokeColor, strokePaintRef);
				e.style.strokeWidth = `${strokeWidth}px`;
				e.style.strokeDasharray = strokeDashArray.join(',');
			}
		}

		public setSvgFillRule(htmlId: number, nonzero: boolean): void {
			const e = this.getView(htmlId);
			if (e instanceof SVGPathElement) {
				e.setAttribute('fill-rule', nonzero ? 'nonzero' : 'evenodd');
			}
		}

		public setSvgEllipseAttributes(htmlId: number, cx: number, cy: number, rx: number, ry: number): void {
			const e = this.getView(htmlId);
			if (e instanceof SVGEllipseElement) {
				e.cx.baseVal.value = cx;
				e.cy.baseVal.value = cy;
				e.rx.baseVal.value = rx;
				e.ry.baseVal.value = ry;
			}
		}

		public setSvgLineAttributes(htmlId: number, x1: number, x2: number, y1: number, y2: number): void {
			const e = this.getView(htmlId);
			if (e instanceof SVGLineElement) {
				e.x1.baseVal.value = x1;
				e.x2.baseVal.value = x2;
				e.y1.baseVal.value = y1;
				e.y2.baseVal.value = y2;
			}
		}

		public setSvgPathAttributes(htmlId: number, nonzero: boolean, data: string): void {
			const e = this.getView(htmlId);
			if (e instanceof SVGPathElement) {
				e.setAttribute('fill-rule', nonzero ? 'nonzero' : 'evenodd');
				e.setAttribute('d', data);
			}
		}

		public setSvgPolyPoints(htmlId: number, points: number[]): void {
			const e = this.getView(htmlId);
			if (e instanceof SVGPolygonElement || e instanceof SVGPolylineElement) {
				if (points != null) {
					const delimiters = [' ', ','];
					// interwave to produce: x0,y0 x1,y1 ...
					// i start at 1
					e.setAttribute('points', points.reduce((acc, x, i) => acc + delimiters[i % delimiters.length] + x, ''));
				}
				else {
					e.removeAttribute('points');
				}
			}
		}

		public setSvgRectangleAttributes(htmlId: number, x: number, y: number, width: number, height: number, rx: number, ry: number): void {
			const e = this.getView(htmlId);
			if (e instanceof SVGRectElement) {
				e.x.baseVal.value = x;
				e.y.baseVal.value = y;
				e.width.baseVal.value = width;
				e.height.baseVal.value = height;
				e.rx.baseVal.value = rx;
				e.ry.baseVal.value = ry;
			}
		}
	}

	if (typeof define === "function") {
		define(
			[`./AppManifest.js`],
			() => {
			}
		);
	}
	else {
		throw `The Uno.Wasm.Boostrap is not up to date, please upgrade to a later version`;
	}
}
