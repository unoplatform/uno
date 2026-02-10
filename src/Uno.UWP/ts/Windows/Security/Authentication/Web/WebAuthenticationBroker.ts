namespace Windows.Security.Authentication.Web {
	export class WebAuthenticationBroker {

		private static _pendingWindow: Window | null = null;

		public static getReturnUrl() {
			return window.location.origin;
		}

		/**
		 * Pre-opens a blank popup window synchronously.
		 * Must be called from a user-gesture context (e.g., click handler)
		 * to avoid browser popup blockers.
		 */
		public static preparePopupWindow(
			title: string,
			popUpWidth: number,
			popUpHeight: number
		): boolean {
			// Close any existing pending window
			if (WebAuthenticationBroker._pendingWindow && !WebAuthenticationBroker._pendingWindow.closed) {
				WebAuthenticationBroker._pendingWindow.close();
			}
			WebAuthenticationBroker._pendingWindow = null;

			const winLeft = window.screenLeft ? window.screenLeft : window.screenX;
			const winTop = window.screenTop ? window.screenTop : window.screenY;
			const width = window.innerWidth ||
				document.documentElement.clientWidth ||
				document.body.clientWidth;
			const height = window.innerHeight ||
				document.documentElement.clientHeight ||
				document.body.clientHeight;
			const left = ((width / 2) - (popUpWidth / 2)) + winLeft;
			const top = ((height / 2) - (popUpHeight / 2)) + winTop;

			WebAuthenticationBroker._pendingWindow = window.open(
				"about:blank",
				title,
				"width=" + popUpWidth + ", height=" + popUpHeight + ", top=" + top + ", left=" + left
			);

			return WebAuthenticationBroker._pendingWindow != null;
		}

		/**
		 * Closes any pre-opened popup window that was not used for authentication.
		 */
		public static closePendingPopupWindow(): void {
			if (WebAuthenticationBroker._pendingWindow && !WebAuthenticationBroker._pendingWindow.closed) {
				WebAuthenticationBroker._pendingWindow.close();
			}
			WebAuthenticationBroker._pendingWindow = null;
		}

		public static authenticateUsingIframe(
			iframeId: string,
			urlNavigate: string,
			urlRedirect: string,
			timeout: number
		) {
			return new Promise<string>((ok, err) => {
				let iframe: HTMLIFrameElement;

				if (iframeId) {
					iframe = document.getElementById(iframeId) as HTMLIFrameElement;
				}

				if (!iframe) {
					iframe = document.createElement("iframe");
					iframe.style.opacity = "0";
					iframe.style.pointerEvents = "none";
					document.body.append(iframe);
				}

				const terminate = () => {
					iframe.removeEventListener("load", onload);
					iframe.src = "about:blank";
					if (!iframeId) {
						iframe.remove();
					}
				}

				const onload = () => {
					if (!iframe.contentDocument) {
						return; // can't access right now
					}

					const currentUrl = iframe.contentDocument.URL;
					console.log("iframe src=" + currentUrl);

					if (currentUrl.indexOf(urlRedirect) === 0) {
						terminate();
						ok(`success|${currentUrl}`);
					}
				};

				iframe.addEventListener("load", onload);

				iframe.src = urlNavigate;
			})
				;
		}

		public static authenticateUsingWindow(
			urlNavigate: string,
			urlRedirect: string,
			title: string,
			popUpWidth: number,
			popUpHeight: number,
			timeout: number): Promise<string> {

			let win: Window = null;
			let timerSubscription: number = null;

			return new Promise<string>((ok, err) => {

				let finished = false;
				const close = () => {
					if (win) {
						win.close();
						win = null;
					}

					if (timerSubscription) {
						window.clearInterval(timerSubscription);
						timerSubscription = null;
					}
					if (!finished) {
						err("Incomplete");
					}
				};

				const completeSuccessfully = (url: string) => {
					if (!finished) {
						ok(`success|${url}`);
						finished = true;
						close();
					}
				};

				const completeUserCancel = () => {
					if (!finished) {
						ok(`cancel`);
						finished = true;
						close();
					}
				}

				const completeTimeout = () => {
					if (!finished) {
						ok(`timeout`);
						finished = true;
						close();
					}
				}

				const completeWithError = (error: string) => {
					if (!finished) {
						err(error);
						finished = true;
						close();
					}
				};

				try {
					// Check if a popup window was pre-opened via preparePopupWindow()
					// to avoid browser popup blockers.
					if (WebAuthenticationBroker._pendingWindow && !WebAuthenticationBroker._pendingWindow.closed) {
						win = WebAuthenticationBroker._pendingWindow;
						WebAuthenticationBroker._pendingWindow = null;
						win.location.href = urlNavigate;
					} else {
						WebAuthenticationBroker._pendingWindow = null;

						/**
						 * adding winLeft and winTop to account for dual monitor
						 * using screenLeft and screenTop for IE8 and earlier
						 */
						const winLeft = window.screenLeft ? window.screenLeft : window.screenX;
						const winTop = window.screenTop ? window.screenTop : window.screenY;
						/**
						 * window.innerWidth displays browser window"s height and width excluding toolbars
						 * using document.documentElement.clientWidth for IE8 and earlier
						 */
						const width = window.innerWidth ||
							document.documentElement.clientWidth ||
							document.body.clientWidth;
						const height = window.innerHeight ||
							document.documentElement.clientHeight ||
							document.body.clientHeight;
						const left = ((width / 2) - (popUpWidth / 2)) + winLeft;
						const top = ((height / 2) - (popUpHeight / 2)) + winTop;

						// open the window
						win = window.open(urlNavigate,
							title,
							"width=" + popUpWidth + ", height=" + popUpHeight + ", top=" + top + ", left=" + left);
					}

					if (!win) {
						completeWithError("popup_blocked");
						return;
					}
					if (win.focus) {
						win.focus();
					}
					// Note: beforeunload is not added when reusing a pre-opened window
					// because navigation from about:blank would trigger it prematurely.
					// Window closure is detected by polling in startMonitoringRedirect.

					const onFinalUrlReached = (success: boolean, timedout: boolean, finalUrlOrMessage: string) => {
						if (success) {
							completeSuccessfully(finalUrlOrMessage);
						} else {
							if (timedout) {
								completeTimeout();
							} else {
								completeUserCancel();
							}
						}
					}
					timerSubscription = this.startMonitoringRedirect(win, urlRedirect, timeout, onFinalUrlReached);
				} catch (e) {
					completeWithError(`${e}`);
				}
			});
		}

		private static startMonitoringRedirect(
			win: Window,
			urlRedirect: string,
			timeout: number,
			callback: (success: boolean, timedout: boolean, finalUrlOrMessage: string) => void): number {

			const currentTime = (new Date()).getTime();
			const maxTime = currentTime + timeout;

			const subscription = window.setInterval(() => {
					try {
						if ((new Date()).getTime() > maxTime) {
							callback(false, true, null);
						}

						if (win.closed) {
							callback(false, false, "Popup closed");
							return;
						}
						const url = win.document.URL;
						if (url.indexOf(urlRedirect) === 0) {
							callback(true, false, url);
						}

					} catch (e) {
						// Expected! DOMException / crossed origin until reached correct redirect page
					}
				},
				100);

			return subscription;
		}
	}
}
