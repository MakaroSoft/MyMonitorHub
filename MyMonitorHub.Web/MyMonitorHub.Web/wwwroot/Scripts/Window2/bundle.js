var isMobile = false; //initiate as false
// device detection
if (/(android|bb\d+|meego).+mobile|avantgo|bada\/|blackberry|blazer|compal|elaine|fennec|hiptop|iemobile|ip(hone|od)|ipad|iris|kindle|Android|Silk|lge |maemo|midp|mmp|netfront|opera m(ob|in)i|palm( os)?|phone|p(ixi|re)\/|plucker|pocket|psp|series(4|6)0|symbian|treo|up\.(browser|link)|vodafone|wap|windows (ce|phone)|xda|xiino/i.test(navigator.userAgent)
    || /1207|6310|6590|3gso|4thp|50[1-6]i|770s|802s|a wa|abac|ac(er|oo|s\-)|ai(ko|rn)|al(av|ca|co)|amoi|an(ex|ny|yw)|aptu|ar(ch|go)|as(te|us)|attw|au(di|\-m|r |s )|avan|be(ck|ll|nq)|bi(lb|rd)|bl(ac|az)|br(e|v)w|bumb|bw\-(n|u)|c55\/|capi|ccwa|cdm\-|cell|chtm|cldc|cmd\-|co(mp|nd)|craw|da(it|ll|ng)|dbte|dc\-s|devi|dica|dmob|do(c|p)o|ds(12|\-d)|el(49|ai)|em(l2|ul)|er(ic|k0)|esl8|ez([4-7]0|os|wa|ze)|fetc|fly(\-|_)|g1 u|g560|gene|gf\-5|g\-mo|go(\.w|od)|gr(ad|un)|haie|hcit|hd\-(m|p|t)|hei\-|hi(pt|ta)|hp( i|ip)|hs\-c|ht(c(\-| |_|a|g|p|s|t)|tp)|hu(aw|tc)|i\-(20|go|ma)|i230|iac( |\-|\/)|ibro|idea|ig01|ikom|im1k|inno|ipaq|iris|ja(t|v)a|jbro|jemu|jigs|kddi|keji|kgt( |\/)|klon|kpt |kwc\-|kyo(c|k)|le(no|xi)|lg( g|\/(k|l|u)|50|54|\-[a-w])|libw|lynx|m1\-w|m3ga|m50\/|ma(te|ui|xo)|mc(01|21|ca)|m\-cr|me(rc|ri)|mi(o8|oa|ts)|mmef|mo(01|02|bi|de|do|t(\-| |o|v)|zz)|mt(50|p1|v )|mwbp|mywa|n10[0-2]|n20[2-3]|n30(0|2)|n50(0|2|5)|n7(0(0|1)|10)|ne((c|m)\-|on|tf|wf|wg|wt)|nok(6|i)|nzph|o2im|op(ti|wv)|oran|owg1|p800|pan(a|d|t)|pdxg|pg(13|\-([1-8]|c))|phil|pire|pl(ay|uc)|pn\-2|po(ck|rt|se)|prox|psio|pt\-g|qa\-a|qc(07|12|21|32|60|\-[2-7]|i\-)|qtek|r380|r600|raks|rim9|ro(ve|zo)|s55\/|sa(ge|ma|mm|ms|ny|va)|sc(01|h\-|oo|p\-)|sdk\/|se(c(\-|0|1)|47|mc|nd|ri)|sgh\-|shar|sie(\-|m)|sk\-0|sl(45|id)|sm(al|ar|b3|it|t5)|so(ft|ny)|sp(01|h\-|v\-|v )|sy(01|mb)|t2(18|50)|t6(00|10|18)|ta(gt|lk)|tcl\-|tdg\-|tel(i|m)|tim\-|t\-mo|to(pl|sh)|ts(70|m\-|m3|m5)|tx\-9|up(\.b|g1|si)|utst|v400|v750|veri|vi(rg|te)|vk(40|5[0-3]|\-v)|vm40|voda|vulc|vx(52|53|60|61|70|80|81|83|85|98)|w3c(\-| )|webc|whit|wi(g |nc|nw)|wmlb|wonu|x700|yas\-|your|zeto|zte\-/i.test(navigator.userAgent.substr(0, 4))) isMobile = true;

var keepAlive;
var started = false;
var ws = null;

var terminalContainer;

var keyboard = null; // keyboard class
var esc = String.fromCharCode(27);

var term = null;
var keypadHidden = true;
var timer;
var arrowKeyElement;


$(document).ready(function () {
    try {
        var identifier = new MakaroSoft.BrowserIdentifier();
        var termElements = "<div class=\"row\"><div class=\"terminal\"></div></div>";

        if (isMobile) {
            if (identifier.iPad && identifier.portrait()) {
                // puts toolbar at the bottom
                $("#screen-container").prepend(termElements);
            } else {
                // puts toolbar at the top
                $("#screen-container").append(termElements);
            }

            $(".toolbar").removeClass("hidden");

            $("#ctrlKey").on("change", function () {
                var value = $(this).prop("checked");
                keyboard.setCtrlKey(value);
                term.focus();
            });


            $("#arrowKeys")
                .on("touchstart",
                    "button",
                    function () {
                        arrowKeyElement = this;
                        sendKey($(arrowKeyElement));
                        timer = setTimeout(mouseDownHandler, 500);
                    });

            $("#arrowKeys")
                .on("touchend",
                    "button",
                    function () {
                        clearTimeout(timer);
                        term.focus();
                    });




            $("#otherKeys").on("click", "li", function () {
                sendKey($(this));
            });
            $("#pfKeys").on("click", "li", function () {
                sendKey($(this));
            });
            $("#fKeys").on("click", "li", function () {
                sendKey($(this));
            });
            $("#keypadButton")
                .click(function () {
                    if (keypadHidden) {
                        keypadHidden = false;

                        $(".keypad").removeClass("hidden");

                        $(".keypad").css({
                            "left": $(this).offset().left - $(".keypad").width() + $(this).width() + 25,
                            "top": $(this).offset().top + $(this).height() + 18
                        });

                    } else {
                        keypadHidden = true;
                        $(".keypad").addClass("hidden");
                    }
                });
            $("table").on("mousedown", "div", function () {
                $(this).css({ "background-color": "gray" });
            });
            $("table").on("mouseup", "div", function () {
                var s = this;
                setTimeout(function() {
                    $(s).css({ "background-color": "" });
                }, 200);
                var data = esc + $(this).data("keyseq");
                if (ws !== null) {
                    ws.send(data);
                }
            });
        }
        else {
            $("#screen-container").append(termElements);
        }

        // ReSharper disable once Html.EventNotResolved
        document.addEventListener("touchmove",
            function (e) {
                e.preventDefault();
                e.stopPropagation();
            },
            false);

        terminalContainer = $(".terminal");

        // set up the keyboard
        keyboard = new Keyboard(terminalContainer[0], defaults.isVms);
        keyboard.init();

        // set up touch
        var touch = new TerminalTouch(terminalContainer);
        touch.onScroll = function (direction) {
            term.scrollDisp(direction, true);
        };
        touch.start();

        // setup the terminal
        term = new Terminal({
            cursorBlink: true
        });

        term.open(terminalContainer[0]);
        term.resize(132, 24);

        term.attachCustomKeydownHandler(function () {
            return false;
        });
        term.on("paste", function (data) {
            term.write(data);
        });

        keepAlive = new MakaroSoft.KeepAlive(300000); // 5 minutes
        connect();

    }
    catch (ex) {
        throw ex;
    }

});

function mouseDownHandler() {
    sendKey($(arrowKeyElement));
    timer = setTimeout(mouseDownHandler, 50);
}

function sendKey(element) {
    try {
        term.focus();
        var value = parseInt(element.data("keycode"));
        if (value === 9) {
            if (ws !== null) ws.send(String.fromCharCode(9));
            return;
        }
        var msg = keyboard.convertNonPrintableKey(value);
        if (ws !== null) ws.send(msg);
    } catch (e) {
        alert("app.send>" + e.message);
    }
}
function connect() {
    try {
        if (window.WebSocket) {
            ws = new window.WebSocket(defaults.wsUrl);
        } else {
            ws = new window.MozWebSocket(defaults.wsUrl);
        }

        ws.onopen = function () { open(); };
        ws.onmessage = function (e) { message(e); };
        ws.onerror = function () { error(); };
        ws.onclose = function (e) { close(e); };

    } catch (ex) {
        alert("error: " + ex.message);
        throw ex;
    }
};

function open() {
    ws.send("sendMode=write");
};

function message(e) {
    try {
        if (!started) {
            started = true;
            keyboard.webSocket(ws);
            $(".loading").addClass("hidden");
            term.focus();
            if (e.data === "sendMode=write") return;
        }

        var text = e.data;

        var index = text.indexOf(esc);
        if (index !== -1) {
            var test = text.substring(index);
            if (test === esc + "[c" | test === esc + "[0c") {

                ws.send(esc + "[?62;1c"); // tell server I am a vt200. make the editor work
                return;
            }

            index = test.indexOf(esc + "[6n");
            if (index !== -1) {
                ws.send(esc + "[24;80R");
                return;
            }
            index = test.indexOf(esc + "=");
            if (index !== -1) {
                keyboard.applicationMode(true);
            }
            index = test.indexOf(esc + ">");
            if (index !== -1) {
                keyboard.applicationMode(false);
            }
        }

        term.write(text);
    } catch (ex) {
        alert("message(): " + ex.message);
    }
};

function error() {
    close();
};

function close() {
    try {
        keyboard.webSocket(null);
        $(".disconnected").removeClass("hidden");
    } catch (ex) {
        alert("close() - " + ex.message);
    }
};

TerminalTouch = function (jQueryTerminalContainer) {
    var self = this;

    var drift = 10;

    var terminalContainer = jQueryTerminalContainer[0];

    // starting touch position
    var x;
    var y;

    // base terminal position
    var baseX;
    var baseY;

    var scrollY;

    var dragStarted;

    var touchPointers;

    this.start = function () {
        try {
            // ReSharper disable once Html.EventNotResolved
            terminalContainer.addEventListener("touchstart",
                function (e) {
                    try {
                        touchPointers = e.touches.length;
                        dragStarted = false;

                        var touch = e.touches[0];
                        x = touch.pageX;
                        y = touch.pageY;

                        baseY = parseInt(jQueryTerminalContainer.css("top"));
                        baseX = parseInt(jQueryTerminalContainer.css("left"));

                        scrollY = y;

                        e.preventDefault();
                        e.stopPropagation();
                    } catch (exc) {
                        alert("touchstart> " + exc.message);
                    }
                },
                false);

            // ReSharper disable once Html.EventNotResolved
            terminalContainer.addEventListener("touchmove",
                function (e) {
                    try {
                        var touch = e.changedTouches[0];
                        var newX = touch.pageX;
                        var newY = touch.pageY;
                        var diffX = x - newX;
                        var diffY = y - newY;

                        if (dragStarted || Math.abs(diffX) > drift || Math.abs(diffY) > drift) {
                            if (!dragStarted) dragStarted = true;
                            switch (touchPointers) {
                                case 1:
                                    // move
                                    var top = baseY - diffY;
                                    var left = baseX - diffX;
                                    if (top > 0) top = 0;
                                    if (left > 0) left = 0;
                                    jQueryTerminalContainer.css("top", top);
                                    jQueryTerminalContainer.css("left", left);
                                    break;
                                case 2:
                                    var diffScroll = scrollY - newY;
                                    if (Math.abs(diffScroll) > 16) {
                                        scrollY = newY; // reset
                                        if (diffScroll > 0) {
                                            self.onScroll(1);
                                            term.scrollDisp(1, true);
                                        } else {
                                            term.scrollDisp(-1, true);
                                            self.onScroll(-1);
                                        }
                                    }
                                    // scroll
                                    break;
                            }
                        }
                        e.preventDefault();
                        e.stopPropagation();
                    } catch (ex) {
                        alert("touchmove> " + ex.message);
                    }
                },
                false);

            // ReSharper disable once Html.EventNotResolved
            terminalContainer.addEventListener("touchend",
                function (e) {
                    try {
                        if (!dragStarted) {
                            // tap pressed
                            term.focus();
                        }
                        e.preventDefault();
                        e.stopPropagation();
                    } catch (ex) {
                        alert("touchend> " + ex.message);
                    }
                },
                false);
        } catch (ex) {
            alert("start> " + ex.message);
        }
    }
    this.onScroll = function () { }
}

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