var first = true;

var hubStarted = false;
var sysInfoFirst = true;
var cpuFirst = true;
var servicesFirst = true;

var pendingServiceElement = null;
var pendingServicePrevState = null;
var serviceTimeoutId = null;

function setServiceToggle(element, isChecked) {
    if (!element) {
        return;
    }
    var $el = $(element);
    $el.unbind("change");
    $el.bootstrapToggle("destroy");
    $el.prop("checked", isChecked === true);
    $el.bootstrapToggle();
    if (defaults.canStopStartServices) {
        $el.change(serviceChange);
    }
}

function revertServiceToggle() {
    if (pendingServiceElement !== null) {
        if (serviceTimeoutId) {
            clearTimeout(serviceTimeoutId);
            serviceTimeoutId = null;
        }
        var element = pendingServiceElement;
        var prevState = pendingServicePrevState;
        pendingServiceElement = null;
        pendingServicePrevState = null;
        $("#tabsPane").hideLoading();
        setServiceToggle(element, prevState);
    }
}
var memoryFirst = true;
var dioFirst = true;
var bioFirst = true;
var networkFirst = true;
var adsFirst = true;
var remoteFirst = true;

var currentTab = null;

var myCpus;
var counter = 1;
var webSocket;
var newWindow;

var TabEnum = {
    SystemInformation: 0,
    CPU: 1,
    Memory: 2,
    Dio: 3,
    Bio: 4,
    Services: 5,
    ADS: 6,
    Network: 7,
    Remote: 8
}

sysInfoResponse = function (deviceId, response) {
    sysInfoFirst = false;
    $("#tabsPane").hideLoading();
    var entry = typeof response === 'string' ? JSON.parse(response) : response;
    $("#computerType").text(entry.computerType);
    $("#osVersion").text(entry.osVersion);
    $("#cpuCount").text(entry.cpus);
    $("#uptime").text(entry.upTime);
    $("#monitorVersion").text(entry.monitorVersion);
    $("#temperature").text(entry.temperature);

    var html = $("#tmplPS").render(entry.powerSupplies);
    $("#psBody").html(html);

    html = $("#tmplPS").render(entry.fans);
    $("#fansBody").html(html);
};

serviceResponse = function (deviceId, response) {
    if (serviceTimeoutId) {
        clearTimeout(serviceTimeoutId);
        serviceTimeoutId = null;
    }
    pendingServiceElement = null;
    pendingServicePrevState = null;

    $("#tabsPane").hideLoading();
    var code = response.Code;
    setServiceToggle(document.getElementById("service_id_" + code), response.Checked === true);
    var statusCell = document.getElementById("service_td_" + code);
    if (statusCell) {
        $(statusCell).html(response.StatusDesc);
    }

}

if (defaults.canStopStartServices) {
    serviceChange = function () {
        pendingServiceElement = this;
        pendingServicePrevState = !this.checked;
        if (serviceTimeoutId) clearTimeout(serviceTimeoutId);
        serviceTimeoutId = setTimeout(function () {
            revertServiceToggle();
        }, 10000);

        $("#tabsPane").showLoading();
        var serviceName = $(this).attr("data-name");
        var serviceCode = $(this).attr("data-code");
        var action = "stop";
        if (this.checked) {
            action = "start";
        }
        var statusCell = document.getElementById("service_td_" + serviceCode);
        if (statusCell) {
            $(statusCell).html("...");
        }
        webSocket.send(defaults.deviceId, "service|" + serviceCode + "|" + serviceName + "|" + action);
    }
}

servicesResponse = function (deviceId, response) {
    servicesFirst = false;
    $("#tabsPane").hideLoading();
    var html = $("#servicesTemplate").render(response);
    $("#myServicesBody").html(html);

    $(".bootstrapToggleHere").bootstrapToggle();
    if (defaults.canStopStartServices) {
        $(".bootstrapToggleHere").change(serviceChange);
    }
}

topCpuResponse = function (deviceId, response) {
    cpuFirst = false;
    $("#tabsPane").hideLoading();
    var entry = typeof response === 'string' ? JSON.parse(response) : response;

    if (first) {
        myCpus = new Array();
    }
    var cpus = entry.percentages.length;

    var i = 0;
    for (i; i < cpus; i++) {
        if (first)
            $("#cpus").append("<div class='cpu2'>cpu " + i + "<div id='cpu" + i + "' class='cpu'></div></div>");

        var mydata = entry.percentages[i].cpuPercentages;
        var res = [];

        for (var ii = 0; ii < mydata.length; ii++) {
            res.push([ii, mydata[ii]]);
        }

        var dta = [res];
        if (first) {
            myCpus[i] = $.plot("#cpu" + i,
                dta,
                {
                    series: {
                        shadowSize: 0 // Drawing is faster without shadows
                    },
                    yaxis: {
                        min: 0,
                        max: 100
                    },
                    xaxis: {
                        show: false
                    }
                });
        } else {
            myCpus[i].setData(dta);
        }
        myCpus[i].draw();
    } // next

    first = false;

    var html = $("#tmpl2").render(entry.processes);
    $("#myTableBody").html(html);
    $("#footerCpu").html(entry.snapshot);
};

var last = null;

$(document)
    .ready(function() {
        try {

            $("#deviceList").on("click", "li", function () {
                var me = $(this);
                if (me.hasClass("disabled")) return;
                var where = me.data("url");
                window.open(where, "ilo" + defaults.deviceId, "width=1000,height=510");
            });
            $("a[ajaxdialog]").ajaxdialog();

            webSocket = new MakaroSoft.WebSocket(defaults.where, defaults.webSocketTicket);
            webSocket.onConnected = function() {
                hubStarted = true;
                $("#tabsPane").showLoading();
                webSocket.send(defaults.deviceId, "sysInfo");
            }
            webSocket.onError = function(event) {
                displayMessage("Error!" + event, "fail");
                $("#tabsPane").hideLoading();
            }
            webSocket.onClosed = function() {
                // must not send alerts when going to another page with window.open
                //alert("Closed!");
            }

            webSocket.onAgentConnected = function (from) {
                if (parseInt(from) === defaults.deviceId) {
                    $(".connectIcon").removeClass("ms-icon16-disconnected").addClass("ms-icon16-connected");
                }
            }
            webSocket.onAgentDisconnected = function(from) {
                if (parseInt(from) === defaults.deviceId) {
                    $(".connectIcon").removeClass("ms-icon16-connected").addClass("ms-icon16-disconnected");
                }
            }

            webSocket.onMessage = function (from, message) {
                if (message.Success === true) {
                    switch (message.Command) {
                    case "sysInfo":
                        sysInfoResponse(from, message.Data);
                        break;
                    case "topCpu":
                        topCpuResponse(from, message.Data);
                        break;
                    case "service":
                        serviceResponse(from, message.Data);
                        break;
                    case "services":
                        servicesResponse(from, message.Data);
                        break;
                    default:
                        displayMessage(from + "<>" + message.Command + "|" + message.Data, "fail");
                        $("#tabsPane").hideLoading();
                    }
                } else {
                    displayMessage(message.FailureReason, "fail");
                    revertServiceToggle();
                    $("#tabsPane").hideLoading();
                }
            }

            $.views.helpers({
                format: function(val) {
                    return val.toString(16);
                }
            });
            $("#cpuRefresh")
                .click(function() {
                    $("#tabsPane").showLoading();
                    webSocket.send(defaults.deviceId, "topCpu|" + $("#topTake").val());
                });
            $("#servicesRefresh")
                .click(function() {
                    $("#tabsPane").showLoading();
                    webSocket.send(defaults.deviceId, "services|favorites");
                });
            $("#btnReport")
                .click(function(event) {
                    event.preventDefault();

                    if (defaults.isCpuFilter) {
                        //drawCpuChartRequest();
                    } else {
                        var itemId = $("#ddlName").val();
                        var report = $("#ddlType").val();

                        if (report === "month") {
                            var test = $("#txtDate").datepicker("getDate");
                            if (test == null) test = new Date();

                            test = new Date(test.getFullYear(), test.getMonth(), 1);
                            $("#txtDate").datepicker("setDate", test);
                        }

                        var chart = defaults.chartUrl + itemId + "?report=" + report + "&date=" + $("#txtDate").val();
                        $("#imgChart").attr("src", chart).css("display", "");
                        $("#btnLeft").css("display", "");
                        $("#btnRight").css("display", "");
                    }
                });

            $("#btnLeft")
                .click(function(event) {
                    event.preventDefault();

                    var itemId = $("#ddlName").val();
                    var report = $("#ddlType").val();

                    var test = $("#txtDate").datepicker("getDate");
                    if (test == null) test = new Date();

                    if (report === "day") {
                        test.setDate(test.getDate() - 1);
                    } else if (report === "week") {
                        test.setDate(test.getDate() - 7);
                    } else if (report === "month") {
                        test.setMonth(test.getMonth() - 1);
                    }
                    $("#txtDate").datepicker("setDate", test);

                    var chart = defaults.chartUrl + itemId + "?report=" + report + "&date=" + $("#txtDate").val();
                    $("#imgChart").attr("src", chart).css("display", "");
                });
            $("#btnRight")
                .click(function(event) {
                    event.preventDefault();

                    var itemId = $("#ddlName").val();
                    var report = $("#ddlType").val();

                    var test = $("#txtDate").datepicker("getDate");
                    if (test == null) test = new Date();

                    if (report === "day") {
                        test.setDate(test.getDate() + 1);
                    } else if (report === "week") {
                        test.setDate(test.getDate() + 7);
                    } else if (report === "month") {
                        test.setMonth(test.getMonth() + 1);
                    }
                    $("#txtDate").datepicker("setDate", test);

                    var chart = defaults.chartUrl + itemId + "?report=" + report + "&date=" + $("#txtDate").val();
                    $("#imgChart").attr("src", chart).css("display", "");
                });

            if (defaults.isVms) {
                $("#RemoteButtonVms")
                    .click(function() {
                        window.open(defaults.sshWindowUrl, "Ssh" + defaults.deviceId, "width=1000,height=510");
                    });
            }

            $("#webButton")
                .click(function() {
                    window.open(defaults.webUrl, "mywWindow" + defaults.deviceId, "width=800,height=600");

                });
            $("#webPingButton")
                .click(function() {
                    window.open(defaults.webPingUrl, "myPing" + defaults.deviceId, "width=800,height=600");

                });

            $("#txtDate")
                .datepicker({
                    defaultDate: "+1w",
                    changeMonth: true,
                    numberOfMonths: 1,
                    onClose: function(selectedDate) {
                        $("#txtDateTo").datepicker("option", "minDate", selectedDate);
                    }
                });
            $("#sysInfTab")
                .on("shown.bs.tab",
                    function () {
                        currentTab = TabEnum.SystemInformation;
                        if (hubStarted === true && sysInfoFirst === true) {
                            $("#tabsPane").showLoading();
                            webSocket.send(defaults.deviceId, "sysInfo");
                        }
                    });
            $("#cpuTab")
                .on("shown.bs.tab",
                    function() {
                        currentTab = TabEnum.CPU;
                        if (hubStarted === true && cpuFirst === true) {
                            $("#tabsPane").showLoading();
                            webSocket.send(defaults.deviceId, "topCpu|" + $("#topTake").val());
                        }
                    });
            $("#memoryTab")
                .on("shown.bs.tab",
                    function() {
                        currentTab = TabEnum.Memory;
                        if (hubStarted === true && memoryFirst === true) {
                        }
                    });
            $("#dioTab")
                .on("shown.bs.tab",
                    function() {
                        currentTab = TabEnum.Dio;
                        if (hubStarted === true && dioFirst === true) {
                        }
                    });
            $("#bioTab")
                .on("shown.bs.tab",
                    function() {
                        currentTab = TabEnum.Bio;
                        if (hubStarted === true && bioFirst === true) {
                        }
                    });
            $("#networkTab")
                .on("shown.bs.tab",
                    function() {
                        currentTab = TabEnum.Network;
                        if (hubStarted === true && networkFirst === true) {
                        }
                    });
            $("#servicesTab")
                .on("shown.bs.tab",
                    function() {
                        currentTab = TabEnum.Services;
                        if (hubStarted === true && servicesFirst === true) {
                            $("#tabsPane").showLoading();
                            webSocket.send(defaults.deviceId, "services|favorites");
                        }
                    });
            $("#adsTab")
                .on("shown.bs.tab",
                    function() {
                        currentTab = TabEnum.ADS;
                        if (hubStarted === true && adsFirst === true) {
                        }
                    });
            $("#remoteTab")
                .on("shown.bs.tab",
                    function() {
                        currentTab = TabEnum.Remote;
                        if (hubStarted === true && remoteFirst === true) {
                        }
                    });

        } catch (ex) {
            displayMessage("deviceDetail> " + ex.message, "fail");
            $("#tabsPane").hideLoading();
        }
    });
