function sendWebMessage(backgroundColor) {
    try {
        if (window.hasOwnProperty("chrome") && typeof chrome.webview !== undefined) {
            // Windows
            chrome.webview.postMessage(backgroundColor);
        } else if (window.hasOwnProperty("unoWebView")) {
            // Android
            unoWebView.postMessage(JSON.stringify(backgroundColor));
        } else if (window.hasOwnProperty("webkit") && typeof webkit.messageHandlers !== undefined) {
            // iOS and macOS
            webkit.messageHandlers.unoWebView.postMessage(JSON.stringify(backgroundColor));
        }
    }
    catch (ex) {
        alert("Error occurred: " + ex);
    }
}

window.onload = function() {
    document.querySelector('#second').style.backgroundColor = 'blue';
    document.querySelector('#second').addEventListener('click', function() {
        document.querySelector('#second').style.backgroundColor = 'green';        
    });
    sendWebMessage(getComputedStyle(document.querySelector("#first")).backgroundColor);
};
