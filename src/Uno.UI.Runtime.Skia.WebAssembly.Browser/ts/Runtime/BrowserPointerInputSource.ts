namespace Uno.UI.Runtime.Skia {
	//import PointerDeviceType = Windows.Devices.Input.PointerDeviceType;

	export enum HtmlPointerEvent {
		pointerover = 1,
		pointerout = 1 << 1,
		pointerdown = 1 << 2,
		pointerup = 1 << 3,
		pointercancel = 1 << 4,

		// Optional pointer events
		pointermove = 1 << 5,
		lostpointercapture = 1 << 6,
		wheel = 1 << 7,
	}

	// TODO: Duplicate of Uno.UI.HtmlEventDispatchResult to merge!
	export enum HtmlEventDispatchResult {
		Ok = 0,
		StopPropagation = 1,
		PreventDefault = 2,
		NotDispatched = 128
	}

	// TODO: Duplicate of Windows.Devices.Input.PointerDeviceType to import instead of duplicate!
	export enum PointerDeviceType {
		Touch = 0,
		Pen = 1,
		Mouse = 2,
	}

	export class BrowserPointerInputSource {

		private static _exports: any;
		
		public static async initialize(inputSource: any): Promise<any> {

			const module = <any>window.Module;
			if (BrowserPointerInputSource._exports == undefined
				&& module.getAssemblyExports !== undefined) {
					
				const browserExports = (await module.getAssemblyExports("Uno.UI.Runtime.Skia.WebAssembly.Browser"));

				BrowserPointerInputSource._exports = browserExports.Uno.UI.Runtime.Skia.BrowserPointerInputSource;
			}

			return new BrowserPointerInputSource(inputSource);
		}

		public static setPointerCapture(pointerId: number): void {
			// Capture disabled for now on skia for wasm
			//document.body.setPointerCapture(pointerId);
		}

		public static releasePointerCapture(pointerId: number): void {
			// Capture disabled for now on skia for wasm
			//document.body.releasePointerCapture(pointerId);
		}

		private _source: any;
		private _bootTime: Number;

		private constructor(manageSource: any) {
			this._bootTime = Date.now() - performance.now();
			this._source = manageSource;

			BrowserPointerInputSource._exports.OnInitialized(manageSource, this._bootTime);
			this.subscribePointerEvents(); // Subscribe only after the managed initialization is done
		}

		private subscribePointerEvents() {
			const element = document.body;

			element.addEventListener("pointerover", this.onPointerEventReceived.bind(this), { capture: true });
			element.addEventListener("pointerout", this.onPointerEventReceived.bind(this), { capture: true });
			element.addEventListener("pointerdown", this.onPointerEventReceived.bind(this), { capture: true });
			element.addEventListener("pointerup", this.onPointerEventReceived.bind(this), { capture: true });
			//element.addEventListener("lostpointercapture", this.onPointerEventReceived.bind(this), { capture: true });
			element.addEventListener("pointercancel", this.onPointerEventReceived.bind(this), { capture: true });
			element.addEventListener("pointermove", this.onPointerEventReceived.bind(this), { capture: true });
			element.addEventListener("wheel", this.onPointerEventReceived.bind(this), { capture: true, passive: false });
		}

		private onPointerEventReceived(evt: PointerEvent): void {
			let id = (evt.target as HTMLElement)?.id;
			if (id === "uno-enable-accessibility") {
				// We have a div to enable accessibility (see enableA11y in WebAssemblyWindowWrapper).
				// Pressing space on keyboard to click it will trigger pointer event which we want to ignore.
				return;
			}

			const event = BrowserPointerInputSource.toHtmlPointerEvent(evt.type);

			let pointerId: number, pointerType: PointerDeviceType, pressure: number;
			let wheelDeltaX: number, wheelDeltaY: number;
			if (evt instanceof WheelEvent) {
				pointerId = (evt as any).mozInputSource ? 0 : 1; // Try to match the mouse pointer ID 0 for FF, 1 for others
				pointerType = PointerDeviceType.Mouse;
				pressure = 0.5; // like WinUI
				wheelDeltaX = evt.deltaX;
				wheelDeltaY = evt.deltaY;

				switch (evt.deltaMode) {
					case WheelEvent.DOM_DELTA_LINE: // Actually this is supported only by FF
						const lineSize = BrowserPointerInputSource.wheelLineSize;
						wheelDeltaX *= lineSize;
						wheelDeltaY *= lineSize;
						break;
					case WheelEvent.DOM_DELTA_PAGE:
						wheelDeltaX *= document.documentElement.clientWidth;
						wheelDeltaY *= document.documentElement.clientHeight;
						break;
				}
			} else {
				pointerId = evt.pointerId;
				pointerType = BrowserPointerInputSource.toPointerDeviceType(evt.pointerType);
				pressure = evt.pressure;
				wheelDeltaX = 0;
				wheelDeltaY = 0;
			}

			const result = BrowserPointerInputSource._exports.OnNativeEvent(
				this._source,
				event, //byte @event, // ONE of NativePointerEvent
				evt.timeStamp, //double timestamp,
				pointerType, //int deviceType, // ONE of _PointerDeviceType
				pointerId, //double pointerId, // Warning: This is a Number in JS, and it might be negative on safari for iOS
				evt.clientX, //double x,
				evt.clientY, //double y,
				evt.ctrlKey, //bool ctrl,
				evt.shiftKey, //bool shift,
				evt.buttons, //int buttons,
				evt.button, //int buttonUpdate,
				pressure, //double pressure,
				wheelDeltaX, //double wheelDeltaX,
				wheelDeltaY, //double wheelDeltaY,
				evt.relatedTarget !== null //bool hasRelatedTarget)
			);

			// pointer events may have some side effects (like changing focus or opening a context menu on right clicking)
			// We blanket-disable all the native behaviour so we don't have to whack-a-mole all the edge cases.
			// We only allow wheel events with ctrl key pressed to allow zooming in/out.
			const isZooming = evt instanceof WheelEvent && evt.ctrlKey;
			if (result == HtmlEventDispatchResult.PreventDefault ||
				!isZooming) {
				evt.preventDefault();
			}
		}

		//#region WheelLineSize
		private static _wheelLineSize: number = undefined;
		private static get wheelLineSize(): number {
			// In web browsers, scroll might happen by pixels, line or page.
			// But WinUI works only with pixels, so we have to convert it before send the value to the managed code.
			// The issue is that there is no easy way get the "size of a line", instead we have to determine the CSS "line-height"
			// defined in the browser settings. 
			// https://stackoverflow.com/questions/20110224/what-is-the-height-of-a-line-in-a-wheel-event-deltamode-dom-delta-line
			if (this._wheelLineSize == undefined) {
				const el = document.createElement("div");
				el.style.fontSize = "initial";
				el.style.display = "none";
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
		//#endregion

		//#region Helpers
		private static toHtmlPointerEvent(eventName: string): HtmlPointerEvent {
			switch (eventName) {
				case "pointerover":
					return HtmlPointerEvent.pointerover;
				case "pointerout":
					return HtmlPointerEvent.pointerout;
				case "pointerdown"	 :
					return HtmlPointerEvent.pointerdown;
				case "pointerup"	 :
					return HtmlPointerEvent.pointerup;
				case "pointercancel" :
					return HtmlPointerEvent.pointercancel;
				case "pointermove"	 :
					return HtmlPointerEvent.pointermove;
				case "wheel":
					return HtmlPointerEvent.wheel;
				default:
					return undefined;
			}
		}

		private static toPointerDeviceType(type: string): PointerDeviceType {
			switch (type) {
				case "touch":
					return PointerDeviceType.Touch;
				case "pen":
					// Note: As of 2019-11-28, once pen pressed events pressed/move/released are reported as TOUCH on Firefox
					//		 https://bugzilla.mozilla.org/show_bug.cgi?id=1449660
					return PointerDeviceType.Pen;
				case "mouse":
				default:
					return PointerDeviceType.Mouse;
			}
		}
		//#endregion
	}
}
