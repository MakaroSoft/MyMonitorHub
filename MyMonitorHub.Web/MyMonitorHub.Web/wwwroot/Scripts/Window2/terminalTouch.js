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
