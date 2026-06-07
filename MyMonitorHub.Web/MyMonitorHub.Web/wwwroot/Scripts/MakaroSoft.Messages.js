    function displayMessage(message, messageType) {
        var myDiv = "";

        if (messageType === "success") {
            myDiv = $(
                "<div class='alert status-message status-message-success d-flex align-items-center'>" +
                "<span class='bi bi-check-circle status-message-glyph'></span>" +
                "<span class='flex-grow-1'>" + message + "</span>" +
                "<button type='button' class='btn-close ms-2' data-bs-dismiss='alert' aria-label='Close'></button>" +
                "</div>");
        } else if (messageType === "fail") {
            myDiv = $(
                "<div class='alert status-message status-message-fail d-flex align-items-center'>" +
                "<span class='bi bi-x-circle status-message-glyph'></span>" +
                "<span class='flex-grow-1'>" + message + "</span>" +
                "<button type='button' class='btn-close ms-2' data-bs-dismiss='alert' aria-label='Close'></button>" +
                "</div>");
        }
        myDiv.hide();
        $("#status-message-container").append(myDiv);
        myDiv.show("slide", { direction: "right" }, 500);

        window.setTimeout(function () {
            myDiv.hide("slide", { direction: "right" }, 500, function () {
                $(this).remove();
            });
        }, 5000);
    }
