// Assets/Plugins/WebSocketPlugin.jslib

var LibraryWebSocket = {
  $webSocketState: {
    instances: {},
    lastId: 0,
    onOpen: null,
    onMessage: null,
    onError: null,
    onClose: null,
    debug: false
  },

  WebSocketSetOnOpen: function(callback) {
    webSocketState.onOpen = callback;
  },

  WebSocketSetOnMessage: function(callback) {
    webSocketState.onMessage = callback;
  },

  WebSocketSetOnError: function(callback) {
    webSocketState.onError = callback;
  },

  WebSocketSetOnClose: function(callback) {
    webSocketState.onClose = callback;
  },

  WebSocketAllocate: function(url) {
    var urlStr = UTF8ToString(url);
    var id = webSocketState.lastId++;

    webSocketState.instances[id] = {
      subprotocols: [],
      url: urlStr,
      ws: null
    };

    return id;
  },

  WebSocketAddSubProtocol: function(instanceId, subprotocol) {
    var subprotocolStr = UTF8ToString(subprotocol);
    webSocketState.instances[instanceId].subprotocols.push(subprotocolStr);
  },

  WebSocketFree: function(instanceId) {
    var instance = webSocketState.instances[instanceId];

    if (!instance) return 0;

    if (instance.ws && instance.ws.readyState < 2) {
      instance.ws.close();
    }

    delete webSocketState.instances[instanceId];
    return 0;
  },

  WebSocketConnect: function(instanceId) {
    var instance = webSocketState.instances[instanceId];
    if (!instance) return -1;

    if (instance.ws !== null) return -2;

    instance.ws = new WebSocket(instance.url, instance.subprotocols);
    instance.ws.binaryType = 'arraybuffer';

    instance.ws.onopen = function() {
      if (webSocketState.debug) console.log("[JSLIB WebSocket] Connected.");

      if (webSocketState.onOpen) {
        dynCall('vi', webSocketState.onOpen, [instanceId]);
      }
    };

    instance.ws.onmessage = function(ev) {
      if (webSocketState.debug) console.log("[JSLIB WebSocket] Received message:", ev.data);

      if (webSocketState.onMessage === null) return;

      if (ev.data instanceof ArrayBuffer) {
        var dataBuffer = new Uint8Array(ev.data);
        var buffer = _malloc(dataBuffer.length);
        HEAPU8.set(dataBuffer, buffer);

        try {
          dynCall('viii', webSocketState.onMessage, [instanceId, buffer, dataBuffer.length]);
        } finally {
          _free(buffer);
        }
      } else {
        var dataBuffer = new TextEncoder().encode(ev.data);
        var buffer = _malloc(dataBuffer.length);
        HEAPU8.set(dataBuffer, buffer);

        try {
          dynCall('viii', webSocketState.onMessage, [instanceId, buffer, dataBuffer.length]);
        } finally {
          _free(buffer);
        }
      }
    };

    instance.ws.onerror = function(ev) {
      if (webSocketState.debug) console.log("[JSLIB WebSocket] Error occurred.");

      if (webSocketState.onError) {
        var msg = "WebSocket error.";
        var length = lengthBytesUTF8(msg) + 1;
        var buffer = _malloc(length);
        stringToUTF8(msg, buffer, length);

        try {
          dynCall('vii', webSocketState.onError, [instanceId, buffer]);
        } finally {
          _free(buffer);
        }
      }
    };

    instance.ws.onclose = function(ev) {
      if (webSocketState.debug) console.log("[JSLIB WebSocket] Closed.");

      if (webSocketState.onClose) {
        dynCall('vii', webSocketState.onClose, [instanceId, ev.code]);
      }

      delete instance.ws;
    };

    return 0;
  },

  WebSocketClose: function(instanceId, code, reasonPtr) {
    var instance = webSocketState.instances[instanceId];
    if (!instance) return -1;

    if (!instance.ws) return -3;

    if (instance.ws.readyState === 2) return -4;

    if (instance.ws.readyState === 3) return -5;

    var reason = reasonPtr ? UTF8ToString(reasonPtr) : undefined;

    try {
      instance.ws.close(code, reason);
    } catch (err) {
      return -7;
    }

    return 0;
  },

  WebSocketSend: function(instanceId, bufferPtr, length) {
    var instance = webSocketState.instances[instanceId];
    if (!instance) return -1;

    if (!instance.ws) return -3;

    if (instance.ws.readyState !== 1) return -6;

    var data = HEAPU8.subarray(bufferPtr, bufferPtr + length);
    instance.ws.send(data);

    return 0;
  },

  WebSocketSendText: function(instanceId, message) {
    var instance = webSocketState.instances[instanceId];
    if (!instance) return -1;

    if (!instance.ws) return -3;

    if (instance.ws.readyState !== 1) return -6;

    var msg = UTF8ToString(message);
    instance.ws.send(msg);

    return 0;
  },

  WebSocketGetState: function(instanceId) {
    var instance = webSocketState.instances[instanceId];
    if (!instance) return -1;

    if (instance.ws) return instance.ws.readyState;
    else return 3;
  }
};

autoAddDeps(LibraryWebSocket, '$webSocketState');
mergeInto(LibraryManager.library, LibraryWebSocket);
