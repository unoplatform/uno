declare namespace MSAL {
    class WebUI {
        static authenticate(urlNavigate: string, urlRedirect: string, title: string, popUpWidth: number, popUpHeight: number): Promise<string>;
        private static startMonitoringRedirect;
    }
}
