if (MakaroSoft === undefined) {
    var MakaroSoft = {};
}
MakaroSoft.Uploader = function (selector, dropZone) {
    var self = this;

    var myData;
    var jqXhr;
    var fileName;
    var jqSelector = $(selector);

    this.formData = function (formData) {
        jqSelector.find(".ms-fileUpload").fileupload("option","formData",formData);
    }

    this.onStartClicked = function () { }
    this.onSuccess = function () { }

    try {

        // Use the server-injected app root (window.appRoot set by _Layout.cshtml) so the
        // correct path is used whether the app is at / or a sub-application like /mymonitorhub.
        var root = (window.appRoot || "").replace(/\/$/, "");
        jqSelector.loadTemplate(root + "/scripts/uploader/template.html", null,
            {
                overwriteCache: true,
                complete: function () {
                    var progressBar = jqSelector.find(".ms-progressBar");
                    var infoBar = jqSelector.find(".ms-infoBar");
                    var upload = jqSelector.find(".ms-fileUpload");

                    var startButton = jqSelector.find(".ms-startUpload");
                    var cancelButton = jqSelector.find(".ms-cancelUpload");

                    startButton.prop('disabled', true);
                    cancelButton.prop('disabled', true);


                    upload.fileupload({
                        dataType: "json",
                        url: root + "/File/Upload",
                        maxNumberOfFiles: 1,
                        dropZone: $(dropZone),
                        add: function (e, data) {
                            startButton.prop('disabled', false);
                            fileName = data.files[0].name;

                            progressBar.css("width", 0);
                            progressBar.removeClass("progress-bar-danger");
                            progressBar.addClass("progress-bar-success");

                            infoBar.text("File Selected = " + fileName);
                            myData = data;
                            self.show();
                        },
                        submit: function () {
                            startButton.prop('disabled', true);
                            cancelButton.prop('disabled', false);
                            infoBar.text(fileName);
                        },
                        done: function (e, data) {
                            startButton.prop('disabled', true);
                            cancelButton.prop('disabled', true);

                            if (data.result.status === "Success") {
                                infoBar.text(fileName + " - finished uploading");
                                self.onSuccess();
                            } else {
                                infoBar.text(fileName + " failed. (" + data.result.message + ")");
                                progressBar.removeClass("progress-bar-success");
                                progressBar.addClass("progress-bar-danger");
                            }
                        },
                        fail: function (e, data) {
                            startButton.prop('disabled', true);
                            cancelButton.prop('disabled', true);

                            infoBar.text(fileName + " failed. (" + data.errorThrown + ")");
                            progressBar.removeClass("progress-bar-success");
                            progressBar.addClass("progress-bar-danger");
                        },
                        progress: function (e, data) {
                            try {
                                var progress = parseInt(data.loaded / data.total * 100, 10);
                                progressBar.css(
                                    "width",
                                    progress + "%"
                                );
                                if (progress === 100) {
                                    infoBar.text(fileName + " " + formatBytes(data.loaded) + " (copying temporary file)");
                                } else {
                                    infoBar.text(fileName + " " + formatBytes(data.loaded) + " (" + progress + "%)");
                                }
                            } catch (ex) {
                                alert(ex.message);
                            }
                        }
                    });
                    if (dropZone != null) {
                        upload.fileupload("option", "dropZone", dropZone);
                    }

                    jqSelector.find(".ms-startUpload").click(function () {
                        try {
                            if (myData != null) {
                                this.disabled = true;
                                self.onStartClicked();
                                jqXhr = myData.submit();
                            }
                        } catch (ex) {
                            alert(ex.message);
                        }
                    });

                    jqSelector.find(".ms-cancelUpload").click(function () {
                        try {
                            jqXhr.abort();
                        } catch (ex) {
                            alert(ex.message);
                        }
                    });
                }
            });


    } catch (ex) {
        alert(ex.message);
    }

    this.show = function () {
        jqSelector.find(".ms-uploadModal").modal({ backdrop: "static", keyboard: false });
    }

    function formatBytes(a, b) { if (0 === a) return "0 Bytes"; var c = 1024, d = b || 2, e = ["Bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB"], f = Math.floor(Math.log(a) / Math.log(c)); return parseFloat((a / Math.pow(c, f)).toFixed(d)) + " " + e[f] }
}
