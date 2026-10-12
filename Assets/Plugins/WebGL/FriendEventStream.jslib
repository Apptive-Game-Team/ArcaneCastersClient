mergeInto(LibraryManager.library, {
  ConnectFriendEventStream: function (objectNamePtr, urlPtr, tokenPtr) {
    var objectName = UTF8ToString(objectNamePtr);
    var url = UTF8ToString(urlPtr);
    var token = UTF8ToString(tokenPtr);

    if (window.wordOnlineFriendEventAbortController) {
      window.wordOnlineFriendEventAbortController.abort();
    }

    var controller = new AbortController();
    window.wordOnlineFriendEventAbortController = controller;

    fetch(url, {
      headers: {
        "Accept": "text/event-stream",
        "Authorization": "Bearer " + token
      },
      signal: controller.signal
    }).then(function (response) {
      if (!response.ok || !response.body) throw new Error("SSE HTTP " + response.status);
      var reader = response.body.getReader();
      var decoder = new TextDecoder();
      var buffer = "";

      function read() {
        return reader.read().then(function (result) {
          if (result.done) throw new Error("SSE stream closed");
          buffer += decoder.decode(result.value, { stream: true }).replace(/\r\n/g, "\n");
          var boundary;
          while ((boundary = buffer.indexOf("\n\n")) >= 0) {
            var block = buffer.substring(0, boundary);
            buffer = buffer.substring(boundary + 2);
            var data = [];
            block.split("\n").forEach(function (line) {
              if (line.indexOf("data:") === 0) data.push(line.substring(5).trimStart());
            });
            if (data.length) {
              SendMessage(objectName, "OnFriendSseEvent", JSON.stringify({ data: data.join("\n") }));
            }
          }
          return read();
        });
      }
      return read();
    }).catch(function (error) {
      if (error.name !== "AbortError") SendMessage(objectName, "OnFriendSseDisconnected", error.message);
    });
  },

  DisconnectFriendEventStream: function () {
    if (window.wordOnlineFriendEventAbortController) {
      window.wordOnlineFriendEventAbortController.abort();
      window.wordOnlineFriendEventAbortController = null;
    }
  }
});
