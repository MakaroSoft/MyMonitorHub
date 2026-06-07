if (MakaroSoft === undefined) {
    var MakaroSoft = {};
}
MakaroSoft.KeepAlive = function (milli) {

    var parts = window.location.href.split("/");
    var url = window.location.protocol + "//" + window.location.hostname + '/' + parts[3] + '/Home/KeepSessionAlive';

    checkToKeepSessionAlive();

    function checkToKeepSessionAlive() {
        setTimeout(keepSessionAlive, milli);
    }

    function keepSessionAlive() {
        $.ajax({
            type: "POST",
            url: url,
            success: function () { }
        });
        checkToKeepSessionAlive();
    }
}
