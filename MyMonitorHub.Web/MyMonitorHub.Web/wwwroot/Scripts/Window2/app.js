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
