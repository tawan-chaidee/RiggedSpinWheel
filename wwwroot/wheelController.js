import { createRoom, addSegments, spinWheel, getRoom, deleteSegment } from "./api.js";

// --- Constants ---
const SEGMENT_COLORS = [
    "#8A2BE2", "#5F9EA0", "#D2691E", "#FF7F50", "#6495ED",
    "#DC143C", "#00FFFF", "#00008B", "#008B8B", "#B8860B",
    "#A9A9A9", "#006400", "#BDB76B", "#8B008B", "#556B2F",
    "#FF8C00", "#9932CC", "#8B0000", "#E9967A", "#9400D3",
];

const PLACEHOLDER_TEXT = "Add Segments";

// --- Elements ---
const SPIN_BUTTON = document.getElementById("spin-button");
const ADD_BUTTON = document.getElementById("add-segment");
const DELETE_BUTTON = document.getElementById("delete-segment");
const PICK_SELECT = document.getElementById("pick-winner-select");
const PICK_BUTTON = document.getElementById("pick-winner-btn");
const HISTORY_DIV = document.getElementById("history");
const ROOM_LINK = document.getElementById("roomLink");
const MODAL = document.getElementById("resultModal");
const MODAL_TEXT = document.getElementById("modal-result-text");
const MODAL_CLOSE = document.querySelector(".modal .close");
const WHEEL_CONTAINER = document.getElementById("wheel-container");

// --- State ---
const state = {
    roomId: null,
    wheel: null,
    isSpinning: false,
    colorIndex: 0,
};

// --- Functions ---
function getNextColor() {
    return SEGMENT_COLORS[state.colorIndex++ % SEGMENT_COLORS.length];
}

function renderWheel(segments, onComplete) {
    state.colorIndex = 0;
    WHEEL_CONTAINER.innerHTML = `
        <div class="pointer"></div>
        <canvas id="wheel" width="500" height="500"></canvas>
    `;

    const canvasId = "wheel";
    const wheelSegments = segments.length
        ? segments.map(s => ({
            text: s.name,
            fillStyle: getNextColor(),
            data: { weight: s.weight },
        }))
        : [{ text: PLACEHOLDER_TEXT, fillStyle: "#cccccc" }];

    const wheel = new Winwheel({
        canvasId,
        outerRadius: 200,
        centerX: 250,
        centerY: 250,
        lineWidth: 1,
        strokeStyle: "silver",
        textAlignment: "center",
        textFontFamily: "Arial",
        textFontSize: 16,
        numSegments: wheelSegments.length,
        segments: wheelSegments,
        animation: {
            type: "spinToStop",
            duration: 8,
            spins: 10,
            callbackFinished: onComplete,
        },
    });

    renderPickList(wheel.segments);
    return wheel;
}

function renderPickList(segments) {
    PICK_SELECT.innerHTML = "";
    const validSegments = segments.slice(1).filter(s => s && s.text !== PLACEHOLDER_TEXT);

    if (!validSegments.length) {
        const opt = new Option("No segments available", "", true, false);
        opt.disabled = true;
        PICK_SELECT.add(opt);
        return;
    }

    validSegments.forEach((seg, i) => {
        PICK_SELECT.add(new Option(`${i + 1}: ${seg.text}`, i + 1));
    });
}

function renderHistory(history) {
    HISTORY_DIV.innerHTML = "<h2>History</h2>";
    if (!history.length) {
        HISTORY_DIV.appendChild(Object.assign(document.createElement("div"), { textContent: "No spin history yet." }));
        return;
    }
    history.slice().reverse().forEach(item => {
        HISTORY_DIV.appendChild(Object.assign(document.createElement("div"), { textContent: item }));
    });
}

function updateRoomLink(text, url = "#") {
    ROOM_LINK.textContent = text;
    ROOM_LINK.href = url;
    ROOM_LINK.addEventListener('click', e => {
      const url = ROOM_LINK.href;
      if (url && url !== '#') {
        window.open(url, '_blank');
        e.preventDefault(); 
      }
    });    
}

function toggleControls(disabled) {
    [SPIN_BUTTON, PICK_BUTTON, ADD_BUTTON, DELETE_BUTTON].forEach(btn => btn.disabled = disabled);
}

function showModal(text) {
    MODAL_TEXT.textContent = text;
    MODAL.style.display = "block";
}

function hideModal() {
    MODAL.style.display = "none";
}

function parseSegmentInput(input) {
    return (input || "").split(",").map(entry => {
        const [nameRaw, weightRaw] = entry.trim().split(":");
        const name = nameRaw?.trim();
        const weight = parseInt(weightRaw, 10);
        return name ? { name, weight: !isNaN(weight) && weight > 0 ? weight : 1 } : null;
    }).filter(Boolean);
}

function onSpinComplete() {
    state.isSpinning = false;
    toggleControls(false);
}

async function syncState() {
    if (!state.roomId) return;
    try {
        const { segments = [], history = [] } = await getRoom(state.roomId);
        state.wheel = renderWheel(segments, onSpinComplete);
        renderHistory(history);
    } catch (err) {
        console.error("Sync error:", err);
    }
}

async function performSpin(pickName = null) {
    if (state.isSpinning) return alert("Already spinning!");
    state.isSpinning = true;
    toggleControls(true);
    state.wheel.stopAnimation(false);
    state.wheel.rotationAngle %= 360;

    try {
        const { result } = await spinWheel(state.roomId, pickName);
        const winner = result.current;
        const match = state.wheel.segments.find(s => s && s.text === winner);

        state.wheel.animation.stopAngle = match
            ? state.wheel.getRandomForSegment(state.wheel.segments.indexOf(match))
            : Math.random() * 360;

        state.wheel.animation.callbackFinished = () => {
            showModal(`The winner is: ${winner}! ${pickName ? "(Picked)" : ""}`);
            state.wheel = renderWheel(result.newState, onSpinComplete);
            renderHistory(result.history);
            state.isSpinning = false;
            toggleControls(false);
        };

        state.wheel.startAnimation();
    } catch (err) {
        console.error("Spin failed:", err);
        alert(`Spin failed: ${err.message}`);
        state.isSpinning = false;
        toggleControls(false);
        await syncState();
    }
}

function handleSpin() {
    if (!state.roomId) return alert("Room not initialized.");
    const hasValidSegments = state.wheel.segments.some(s => s && s.text !== PLACEHOLDER_TEXT);
    if (!hasValidSegments) return alert("Please add segments first.");
    performSpin();
}

function handlePickWinner() {
    const selectedIndex = parseInt(PICK_SELECT.value, 10);
    const seg = state.wheel.segments[selectedIndex];
    if (!seg || seg.text === PLACEHOLDER_TEXT) return alert("Invalid segment selected.");
    performSpin(seg.text);
}

async function handleAddSegments() {
    if (!state.roomId) return alert("Room not initialized.");
    const input = prompt("Enter segments (Name[:Weight], separated by commas)", "Alice:2, Bob, Charlie:3");
    const segments = parseSegmentInput(input);
    if (!segments.length) return alert("No valid segments entered.");
    toggleControls(true);
    try {
        await addSegments(state.roomId, segments);
        await syncState();
    } catch (err) {
        console.error("Add segments failed:", err);
        alert(err.message);
    } finally {
        toggleControls(false);
    }
}

async function handleDeleteSegment() {
    const selectedIndex = parseInt(PICK_SELECT.value, 10);
    const seg = state.wheel.segments[selectedIndex];
    if (!state.roomId || !seg || seg.text === PLACEHOLDER_TEXT) return alert("Invalid selection.");
    if (!confirm(`Delete segment \"${seg.text}\"?`)) return;
    toggleControls(true);
    try {
        const success = await deleteSegment(state.roomId, seg.text);
        if (!success) throw new Error("Delete failed");
        await syncState();
    } catch (err) {
        console.error("Delete failed:", err);
        alert(err.message);
    } finally {
        toggleControls(false);
    }
}

// --- Entry Point ---
(async function init() {
    SPIN_BUTTON.onclick = handleSpin;
    PICK_BUTTON.onclick = handlePickWinner;
    ADD_BUTTON.onclick = handleAddSegments;
    DELETE_BUTTON.onclick = handleDeleteSegment;
    MODAL_CLOSE.onclick = hideModal;
    window.onclick = e => e.target === MODAL && hideModal();

    try {
        updateRoomLink("Creating room...");
        const { roomId } = await createRoom();
        state.roomId = roomId;
        const base = window.location.origin + window.location.pathname.replace("index.html", "");
        const url = `${base}join/?roomId=${roomId}`;
        updateRoomLink(url, url);
        await syncState();
    } catch (err) {
        console.error("Init failed:", err);
        updateRoomLink("Error initializing!");
        alert(err.message);
        state.wheel = renderWheel([], onSpinComplete);
    }
})();
