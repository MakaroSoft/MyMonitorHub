if (MakaroSoft === undefined) {
    var MakaroSoft = {};
}

MakaroSoft.WebSocket = function(url, ticket) {
    var websocket = new WebSocket(url + "/api/Hub/Get");
    var self = this;
    websocket.onmessage = function(event) {
        if (event.data.toString().indexOf("Registered: ") === 0) {
            self.onConnected(event.data);
            return;
        }
        var message = event.data.toString();
        var index = message.indexOf("|");
        if (index == -1) {
            self.onMessage(message);
        } else {
            var jsonString = message.substring(index + 1);
            var obj = JSON.parse(jsonString);
            var from = message.substring(0, index);

            if (obj.Success === true) {
                if (obj.Command === "Connection") {
                    if (obj.Data === "Connected") {
                        self.onAgentConnected(from);
                    } else {
                        self.onAgentDisconnected(from);
                    }
                    return;
                }
            }

            self.onMessage(from, obj);
        }
    }
    websocket.onopen = function(event) {
        websocket.send("RegisterBrowser|" + ticket);
    }
    websocket.onerror = function(event) {
        self.onError(event);
    }
    websocket.onclose = function(event) {
        self.onClosed(event);
    }

    this.send = function(to, message) {
        websocket.send("Request|" + to + "|" + message);
    }
    this.onConnected = function(message) {}
    this.onClosed = function(event) {}
    this.onMessage = function(from, message) {}
    this.onAgentConnected = function(from) {}
    this.onAgentDisconnected = function(from) {}
    this.onError = function(event) {}
}

