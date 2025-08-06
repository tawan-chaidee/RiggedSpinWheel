import { getRoom } from "/api.js";

const SEGMENT_COLORS = [
  "#8A2BE2", "#5F9EA0", "#D2691E", "#FF7F50", "#6495ED",
  "#DC143C", "#00FFFF", "#00008B", "#008B8B", "#B8860B"
];
const PLACEHOLDER_TEXT = "Waiting for Segments...";
const state = { wheel: null };

const HISTORY_DIV = document.getElementById("history");
const MODAL = document.getElementById("resultModal");
const MODAL_TEXT = document.getElementById("modal-result-text");
const MODAL_CLOSE = document.querySelector(".modal .close");

function renderWheel(segments) {
  let colorIndex = 0;
  const getNextColor = () => SEGMENT_COLORS[colorIndex++ % SEGMENT_COLORS.length];

  const wheelSegments = segments && segments.length
    ? segments.map(s => ({
        text: s.Name || s.name,
        fillStyle: getNextColor(),
      }))
    : [{ text: PLACEHOLDER_TEXT, fillStyle: "#cccccc" }];

  return new Winwheel({
    'canvasId': 'wheel',
    'outerRadius': 200,
    'centerX': 250,
    'centerY': 250,
    'lineWidth': 1,
    'strokeStyle': 'silver',
    'textAlignment': 'center',
    'textFontFamily': 'Arial',
    'textFontSize': 16,
    'numSegments': wheelSegments.length,
    'segments': wheelSegments,
    'animation': {
      'type': 'spinToStop',
      'duration': 4,
      'spins': 5,
    },
  });
}

function renderHistory(history) {
  HISTORY_DIV.innerHTML = "<h2>History</h2>";
  if (!history || !history.length) {
    HISTORY_DIV.appendChild(Object.assign(document.createElement("div"), { textContent: "No spin history yet." }));
    return;
  }
  history.slice().reverse().forEach(item => {
    const historyItem = document.createElement("div");
    historyItem.textContent = item;
    HISTORY_DIV.appendChild(historyItem);
  });
}

function showModal(text) {
  MODAL_TEXT.textContent = text;
  MODAL.style.display = "block";
}

function hideModal() {
  MODAL.style.display = "none";
}

MODAL_CLOSE.onclick = hideModal;
window.onclick = e => { if (e.target === MODAL) hideModal(); };

const urlParams = new URLSearchParams(window.location.search);
const roomId = urlParams.get("roomId");

if (!roomId) {
  document.getElementById("layout").innerHTML = "<h1>Error: Room ID is required.</h1><p>Please use a URL like /observe.html?roomId=YOUR_ROOM_ID</p>";
} else {
  const rootUrl = `${window.location.protocol}//${window.location.host}`;
  const hubUrl = `${rootUrl}/room`;

  const connection = new signalR.HubConnectionBuilder()
    .withUrl(hubUrl, {
      skipNegotiation: true,
      transport: signalR.HttpTransportType.WebSockets,
    })
    .configureLogging(signalR.LogLevel.Information)
    .build();

  connection.on("SpinResult", (msg) => {
    try {
      const data = JSON.parse(msg);
      const winner = data.Current;
      const winningSegment = state.wheel.segments.find(s => s && s.text === winner);

      if (winningSegment) {
        const index = state.wheel.segments.indexOf(winningSegment);
        const stopAt = state.wheel.getRandomForSegment(index);

        state.wheel.animation.stopAngle = stopAt;
        state.wheel.animation.callbackFinished = () => {
          showModal(`The winner is: ${winner}!`);
          state.wheel = renderWheel(data.NewState);
          renderHistory(data.History);
        };
        state.wheel.startAnimation();
      } else {
        showModal(`Winner: ${winner}. Wheel state has been resynced.`);
        state.wheel = renderWheel(data.NewState);
        renderHistory(data.History);
      }
    } catch (e) {
      console.error("Failed to parse SpinResult message:", e);
    }
  });

  connection.on("SegmentAdded", (json) => {
    try {
      const data = JSON.parse(json);
      state.wheel = renderWheel(data.Segments);
      renderHistory(data.History);
    } catch (e) {
      console.error("Failed to parse SegmentAdded message:", e);
    }
  });

  connection.on("SegmentDeleted", (json) => {
    try {
      const data = JSON.parse(json);
      state.wheel = renderWheel(data.Segments);
      renderHistory(data.History);
      console.log("Segment deleted and UI updated.");
    } catch (e) {
      console.error("Failed to parse SegmentDeleted message:", e);
    }
  });

  connection.on("CloseConnection", (reason) => {
    console.log("Connection closed by server:", reason);
    document.getElementById("layout").innerHTML += `<p style="color:red;">Connection Closed: ${reason}</p>`;
    connection.stop();
  });

  connection.onclose((error) => {
    console.error("SignalR connection closed unexpectedly:", error);
    document.getElementById("layout").innerHTML += `<p style="color:red;">Connection lost. Please refresh.</p>`;
  });

  async function initialize() {
    try {
      const room = await getRoom(roomId);
      state.wheel = renderWheel(room.segments);
      renderHistory(room.history);
      await connection.start();
      console.log("Connected to SignalR.");
      await connection.invoke("Register", "observer", roomId);
      console.log(`Registered as observer for room: ${roomId}`);
    } catch (err) {
      console.error("Initialization failed:", err);
      document.getElementById("layout").innerHTML = `<h1>Error</h1><p>Could not load room data or connect to the hub. The room may not exist or the server may be down.</p><p><em>${err.message}</em></p>`;
    }
  }

  initialize();
}
