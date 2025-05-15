import { createRoom, addSegments, spinWheel, getRoom } from "./api.js";

let currentRoomId = null;
let theWheel = null;
let isSpinning = false;

const spinBtn = document.getElementById("spin-button");
const addBtn = document.getElementById("add-segment");
const delBtn = document.getElementById("delete-segment");
const pickSelect = document.getElementById("pick-winner-select");
const pickBtn = document.getElementById("pick-winner-btn");
const historyDiv = document.getElementById("history");
const roomIdValueSpan = document.getElementById("roomIdValue");

const modal = document.getElementById("resultModal");
const modalText = document.getElementById("modal-result-text");
const modalClose = document.querySelector(".modal .close");

const SEGMENT_COLORS = [
  "#8A2BE2",
  "#5F9EA0",
  "#D2691E",
  "#FF7F50",
  "#6495ED",
  "#DC143C",
  "#00FFFF",
  "#00008B",
  "#008B8B",
  "#B8860B",
  "#A9A9A9",
  "#006400",
  "#BDB76B",
  "#8B008B",
  "#556B2F",
  "#FF8C00",
  "#9932CC",
  "#8B0000",
  "#E9967A",
  "#9400D3",
];
let colorIndex = 0;

function getNextFillStyle() {
  const color = SEGMENT_COLORS[colorIndex % SEGMENT_COLORS.length];
  colorIndex++;
  return color;
}

function resetColorIndex() {
  colorIndex = 0;
}

function initializeWheel(backendSegments = []) {
  resetColorIndex();
  const wheelContainer = document.getElementById("wheel-container");
  if (theWheel) {
    wheelContainer.innerHTML =
      '<div class="pointer"></div><canvas id="wheel" width="500" height="500"></canvas>';
  }

  let winwheelSegments = backendSegments.map((s) => ({
    text: String(s.name),
    fillStyle: getNextFillStyle(),
    data: { weight: s.weight },
  }));

  if (winwheelSegments.length === 0) {
    winwheelSegments = [{ text: "Add Segments", fillStyle: "#cccccc" }];
  }

  theWheel = new Winwheel({
    canvasId: "wheel",
    outerRadius: 200,
    centerX: 250,
    centerY: 250,
    lineWidth: 1,
    strokeStyle: "silver",
    textAlignment: "center",
    textFontFamily: "Arial",
    textFontSize: 16,
    numSegments: winwheelSegments.length,
    segments: winwheelSegments,
    animation: {
      type: "spinToStop",
      duration: 8,
      spins: 10,
      callbackFinished: handleFrontendAnimationComplete,
    },
  });
  refreshPickList();
}

function updateWheelDisplayFromServer() {
  if (!currentRoomId) return;
  getRoom(currentRoomId)
    .then((roomInfo) => {
      initializeWheel(roomInfo.segments || []);
      updateHistory(roomInfo.history || []);
    })
    .catch((error) => {
      console.error("Error fetching room details to update wheel:", error);
    });
}

function refreshPickList() {
  pickSelect.innerHTML = "";
  if (
    !theWheel ||
    !theWheel.segments ||
    theWheel.segments.length === 0 ||
    (theWheel.segments.length === 1 &&
      theWheel.segments[0].text === "Add Segments")
  ) {
    const opt = document.createElement("option");
    opt.text = "No segments available";
    opt.disabled = true;
    pickSelect.add(opt);
    return;
  }
  theWheel.segments.forEach((seg, idx) => {
    if (seg && seg.text !== "Add Segments") {
      const opt = document.createElement("option");
      opt.value = idx + 1;
      opt.text = `${idx + 1}: ${seg.text}`;
      pickSelect.add(opt);
    }
  });
}

function updateHistory(historyArray) {
  const historyContentH2 = historyDiv.querySelector("h2");
  historyDiv.innerHTML = "";
  if (historyContentH2) historyDiv.appendChild(historyContentH2);
  else {
    const h2 = document.createElement("h2");
    h2.textContent = "History";
    historyDiv.appendChild(h2);
  }

  if (historyArray && historyArray.length > 0) {
    historyArray
      .slice()
      .reverse()
      .forEach((item) => {
        const entry = document.createElement("div");
        entry.textContent = String(item);
        historyDiv.appendChild(entry);
      });
  } else {
    const entry = document.createElement("div");
    entry.textContent = "No spin history yet.";
    historyDiv.appendChild(entry);
  }
}

// callback after WinWheel's animation finishes.
function handleFrontendAnimationComplete() {
  isSpinning = false;
  spinBtn.disabled = false;
  pickBtn.disabled = false;
}

async function initializeApp() {
  try {
    roomIdValueSpan.textContent = "Creating room...";
    const roomData = await createRoom();
    currentRoomId = roomData.roomId;
    roomIdValueSpan.textContent = currentRoomId;
    const roomInfo = await getRoom(currentRoomId);
    initializeWheel(roomInfo.segments || []);
    updateHistory(roomInfo.history || []);
  } catch (error) {
    console.error("Initialization failed:", error);
    roomIdValueSpan.textContent = "Error initializing!";
    alert(`Could not initialize the application: ${error.message}`);
    initializeWheel([]);
  }
}

// Unified function to perform spin (random or picked) and handle outcome
async function performSpinAndHandleOutcome(
  roomId,
  winnerNameToPick = null,
  operationType = "Spin"
) {
  isSpinning = true;
  spinBtn.disabled = true;
  pickBtn.disabled = true;

  theWheel.stopAnimation(false);
  theWheel.rotationAngle = theWheel.rotationAngle % 360;

  try {
    const spinApiResponse = await spinWheel(roomId, winnerNameToPick);
    const backendWinnerName = spinApiResponse.result.current;
    const newState = spinApiResponse.result.newState;
    const newHistory = spinApiResponse.result.history;

    if (winnerNameToPick && backendWinnerName !== winnerNameToPick) {
      console.warn(
        `Backend winner '${backendWinnerName}' differs from requested '${winnerNameToPick}'. Trusting backend.`
      );
    }

    let winningSegmentActualIndex = -1;
    if (theWheel && theWheel.segments) {
      for (let i = 0; i < theWheel.segments.length; i++) {
        if (
          theWheel.segments[i] &&
          theWheel.segments[i].text === backendWinnerName
        ) {
          winningSegmentActualIndex = i + 1;
          break;
        }
      }
    }

    if (winningSegmentActualIndex !== -1) {
      theWheel.animation.stopAngle = theWheel.getRandomForSegment(
        winningSegmentActualIndex
      );
    } else {
      console.warn(
        `Winner '${backendWinnerName}' not found on frontend wheel for animation. Wheel will spin randomly before state update.`
      );
      theWheel.animation.stopAngle = Math.random() * 360; // Visual spin only
    }

    // Define what happens after THIS specific animation sequence (triggered by backend response)
    theWheel.animation.callbackFinished = () => {
      modalText.textContent = `The winner is: ${backendWinnerName}! ${
        operationType === "Pick" ? "(Manually Picked)" : ""
      }`;
      modal.style.display = "block";
      initializeWheel(newState); // Re-initialize wheel with new segments from backend
      updateHistory(newHistory); // Update history from backend
      isSpinning = false;
      spinBtn.disabled = false;
      pickBtn.disabled = false;
    };
    theWheel.startAnimation();
  } catch (error) {
    console.error(`Error during ${operationType.toLowerCase()}:`, error);
    alert(
      `Failed to ${operationType.toLowerCase()} winner via backend: ${
        error.message
      }. Please try again.`
    );
    isSpinning = false;
    spinBtn.disabled = false;
    pickBtn.disabled = false;
    updateWheelDisplayFromServer(); // Resync frontend with server state
  }
}

spinBtn.addEventListener("click", () => {
  if (!currentRoomId || isSpinning) {
    alert(isSpinning ? "Spin in progress..." : "Room not initialized.");
    return;
  }
  if (
    !theWheel ||
    theWheel.numSegments === 0 ||
    (theWheel.numSegments === 1 && theWheel.segments[0].text === "Add Segments")
  ) {
    alert("Please add segments to the wheel before spinning!");
    return;
  }
  performSpinAndHandleOutcome(currentRoomId, null, "Spin");
});

pickBtn.addEventListener("click", () => {
  if (!currentRoomId || isSpinning) {
    alert(isSpinning ? "Operation in progress..." : "Room not initialized.");
    return;
  }
  if (!pickSelect.value) {
    alert("Please select a segment to pick as winner.");
    return;
  }

  const selectedSegmentOneBasedIndex = parseInt(pickSelect.value, 10);
  const pickedSegmentObject =
    theWheel && theWheel.segments
      ? theWheel.segments[selectedSegmentOneBasedIndex - 1]
      : null;

  if (!pickedSegmentObject || pickedSegmentObject.text === "Add Segments") {
    alert("Cannot pick an invalid or placeholder segment.");
    return;
  }
  const winnerNameToPick = pickedSegmentObject.text;
  performSpinAndHandleOutcome(currentRoomId, winnerNameToPick, "Pick");
});

addBtn.addEventListener("click", async () => {
  if (!currentRoomId) {
    alert("Room not initialized.");
    return;
  }

  const inputString = prompt(
    "Enter segments as Name1:Weight1, Name2:Weight2, ... (e.g., Alice:2, Bob, Charlie:3)\nWeight defaults to 1 if omitted.",
    "ParticipantA:1, ParticipantB:2"
  );

  if (inputString) {
    const segmentEntries = inputString.split(",");
    const newSegments = segmentEntries
      .map((entry) => {
        const parts = entry.trim().split(":");
        const name = parts[0].trim();
        let weight = 1;
        if (parts.length > 1) {
          const parsedWeight = parseInt(parts[1].trim(), 10);
          if (!isNaN(parsedWeight) && parsedWeight > 0) weight = parsedWeight;
          else if (name)
            console.warn(`Invalid weight for '${name}', defaulting to 1.`);
          else return null;
        }
        return name ? { name, weight } : null;
      })
      .filter((segment) => segment !== null && segment.name !== "");

    if (newSegments.length > 0) {
      try {
        addBtn.disabled = true;
        const success = await addSegments(currentRoomId, newSegments);
        if (success) updateWheelDisplayFromServer();
        else alert("Failed to add segments via backend.");
      } catch (error) {
        console.error("Error adding segments:", error);
        alert(`Error adding segments: ${error.message}`);
      } finally {
        addBtn.disabled = false;
      }
    } else alert("No valid segments entered. Format: Name:Weight or Name.");
  }
});

delBtn.addEventListener("click", async () => {
  if (!currentRoomId || !pickSelect.value) {
    alert("Room not initialized or no segment selected.");
    return;
  }

  const selectedIndex = parseInt(pickSelect.value, 10) - 1;
  const segmentNameToDelete =
    theWheel && theWheel.segments[selectedIndex]
      ? theWheel.segments[selectedIndex].text
      : null;

  if (!segmentNameToDelete || segmentNameToDelete === "Add Segments") {
    alert("Invalid segment selected for deletion.");
    return;
  }
  if (!confirm(`Delete segment: "${segmentNameToDelete}"?`)) return;

  const currentBackendSegments = theWheel.segments
    .filter(
      (seg, idx) => idx !== selectedIndex && seg && seg.text !== "Add Segments"
    )
    .map((seg) => ({
      name: seg.text,
      weight: seg.data && seg.data.weight ? seg.data.weight : 1,
    }));

  try {
    delBtn.disabled = true;
    const success = await addSegments(currentRoomId, currentBackendSegments); // Overwrite with new list
    if (success) updateWheelDisplayFromServer();
    else
      alert(
        `Failed to update backend after attempting to delete "${segmentNameToDelete}".`
      );
  } catch (error) {
    console.error("Error deleting segment:", error);
    alert(`Error deleting segment: ${error.message}`);
  } finally {
    delBtn.disabled = false;
  }
});

modalClose.onclick = () => {
  modal.style.display = "none";
};
window.onclick = (event) => {
  if (event.target === modal) modal.style.display = "none";
};

initializeApp();
