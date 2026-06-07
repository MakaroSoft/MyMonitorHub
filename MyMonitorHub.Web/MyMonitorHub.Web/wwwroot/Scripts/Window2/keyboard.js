function Keyboard(element, isVms) {
    var self = this;
    var esc = String.fromCharCode(27);

    var ctrlKeySetByButton = false;
    var ctrlKey = false;
    var altKey = false;

    var debug = false;
    var debugDiv = null;
    var ws = null;
    var appMode = false;

    var ctrlButton;

    function logDebug(msg) {
        if (debugDiv) {
            debugDiv.append(msg);
        }
    }

    this.webSocket = function (socket) {
        if (socket === undefined) {
            return ws;
        }
        ws = socket;
        return ws;
    }

    this.applicationMode = function(mode) {
        appMode = mode;
    }

    this.setCtrlKey = function(value) {
        ctrlKey = value;
        ctrlKeySetByButton = true;
    }

    this.init = function () {
        ctrlButton = $("#ctrlKey");
        if (debug === true) {
            debugDiv = $("<div style='background-color: white'></div>");
            $("#screen-container").prepend(debugDiv);
        }
        $(element)
            .keypress(function (e) {
                try {
                    var msg = String.fromCharCode(e.which);
                    logDebug("keypress(" + msg + ")|" + e.which);
                    if (ws !== null) {
                        ws.send(msg);
                    }
                } catch (ex) {
                    alert("keypress error: " + ex.message);
                }

                cancelEvent(e);
                return false;
            });
        $(element)
            .keydown(function(e)  {
                try {
                    var keyCode = e.which;
                    logDebug("keydown(" + keyCode + ")");
                    if (setModifiers(keyCode, true)) {
                        cancelEvent(e);
                        return true;
                    }

                    var msg = self.convertNonPrintableKey(keyCode);

                    if (msg !== null) {
                        if (ws !== null) {
                            ws.send(msg);
                        }
                        cancelEvent(e);
                        return false;
                    }


                } catch (ex) {
                    alert("keydown error: " + ex.message);
                }

                return true;
            });
        $(element)
            .keyup(function(e) {
                try {
                    var keyCode = e.which;
                    logDebug("keyup(" + keyCode + ")");
                    setModifiers(keyCode, false);
                } catch (ex) {
                    alert("keyup error: " + ex.message);
                }

                cancelEvent(e);
                return false;
            });

    };

    this.convertNonPrintableKey = function(keyCode) {
        var msg = null;
        if (isInvisibleKey(keyCode) || isVmsKeypad(keyCode)) {
            if (ctrlKey === true && keyCode >= 65 && keyCode <= 90) {
                keyCode = keyCode - 64;
                if (ctrlKeySetByButton) {
                    ctrlButton.bootstrapToggle("off");
                    ctrlKeySetByButton = false;
                    ctrlKey = false;
                }
            }
            if (!isVms) {
                msg = String.fromCharCode(keyCode);
            } else {
                // numeric keypad 0 to 9 for vms
                if (keyCode >= keys.Num0 && keyCode <= keys.Num9) {
                    msg = esc + "O" + String.fromCharCode(keyCode + 16);
                } else {
                    switch (keyCode) {
                        case keys.Backspace:
                            msg = String.fromCharCode(127);
                            break;
                        case keys.UpArrow:
                            msg = esc + "[A";
                            break;
                        case keys.DownArrow:
                            msg = esc + "[B";
                            break;
                        case keys.RightArrow:
                            msg = esc + "[C";
                            break;
                        case keys.LeftArrow:
                            msg = esc + "[D";
                            break;
                        case keys.F2:
                        case keys.F12:
                            msg = esc + "OP"; // pf1
                            break;


                            // pc key/vms key
                        case keys.Insert: // insert
                            msg = esc + "[2~";
                            break;
                        case keys.Home: // find
                            msg = esc + "[1~";
                            break;
                        case keys.PageUp: // previous
                            msg = esc + "[5~";
                            break;

                        case keys.Delete: // remove
                            msg = esc + "[3~";
                            break;
                        case keys.End: // select
                            msg = esc + "[4~";
                            break;
                        case keys.PageDown: // next
                            msg = esc + "[6~";
                            break;




                            // more numeric keypad keys for vms
                        case keys.Multiply:
                        case keys.F4:
                            msg = esc + "OR"; // PF3
                            break;
                        case keys.Add:
                            msg = esc + "Om"; // delete to end of word
                            break;
                        case keys.Subtract:
                        case keys.F5:
                            msg = esc + "OS"; // PF4
                            break;
                        case keys.Decimal:
                            msg = esc + "On"; // decimal
                            break;
                        case keys.Divide:
                        case keys.F3:
                            msg = esc + "OQ"; // PF2
                            break;

                        default:
                            msg = String.fromCharCode(keyCode);
                    }
                }
            }
        }
        return msg;
    };

    this.close = function () {
        try {
            $(element).off("keypress keydown keyup");
        } catch (ex) {
            alert("close> " + ex.message);
        }
    }

    function setModifiers(keyCode, onOff) {
        if (keyCode === 17) {
            ctrlKeySetByButton = false;
            ctrlKey = onOff;
            return true;
        } else if (keyCode === 18) {
            altKey = onOff;
            return true;
        } else if (keyCode === 16) { // shift key
            return true;
        }
        return false;
    }

    function cancelEvent(ev) {
        ev.preventDefault();
        ev.stopPropagation();
    };

    function isInvisibleKey(keyCode) {
        if (ctrlKey || altKey) return true; // control and alt characters are not visible
        return keys.isInvisibleKey(keyCode);
    }

    // if vms then these printable keys need to be detected and converted to vms application
    function isVmsKeypad(keyCode) {
        if (!isVms) return false;

        // for vms I am having these keys defined as pf2 to pf4 regardless of in application mode or numeric mode
        if (keyCode === keys.Divide || keyCode === keys.Multiply || keyCode === keys.Subtract) {
            return true;
        }

        if (!appMode) return false;
        if (keyCode >= 96 && keyCode <= 111) {
            return true;
        }
        return false;
    }
}