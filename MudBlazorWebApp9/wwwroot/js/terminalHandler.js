var term = null;

window.initializeTerminal = function (elementId) {
    term = new Terminal();
    term.open(document.getElementById(elementId));
};

window.writeToTerminal = function (text) {
    if (term) {
        term.write(text);
    }
};

window.onTerminalData = function (dotNetHelper) {
    if (term) {
        term.onData(function (data) {
            dotNetHelper.invokeMethodAsync('OnTerminalInput', data);
        });
    }
};

window.clearTerminal = function () {
    if (term) {
        term.clear();
    }
};