if (MakaroSoft === undefined) {
    var MakaroSoft = {};
}
MakaroSoft.BrowserIdentifier = function () {
    var self = this;

    function browserType(match) {
        var i = navigator.userAgent;
        return i.match(match) !== null;
    };

    this.iPhone = browserType(/(iPhone)|(iPod)/);
    this.iPad = browserType(/iPad/);
    this.iOS = this.iPhone || this.iPad;

    this.portrait = function () {
        return self.iOS ? Math.abs(window.orientation) !== 90 : screen.width < screen.height;
    };
    this.landscape = function () {
        return !self.portrait();
    };

    this.width = function() {
        if (self.iPhone) {
            return 1308;
        } else {
            return $(window).width();
        }
    };
    this.height = function() {
        if (self.iPhone) {
            return 628;
        } else {
            return $(window).height();
        }
    }
}