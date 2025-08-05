const API_HOST = window.location.origin;

const BASE_URL = `${API_HOST}/api/Rooms`;

export async function createRoom() {
  const response = await fetch(BASE_URL, { method: "POST" });
  if (!response.ok)
    throw new Error(`Failed to create room: ${response.statusText}`);
  return response.json();
}

export async function getRoom(roomId) {
  const response = await fetch(`${BASE_URL}/${roomId}`);
  if (!response.ok)
    throw new Error(`Failed to get room ${roomId}: ${response.statusText}`);
  return response.json();
}

export async function addSegments(roomId, segments) {
  const url = `${BASE_URL}/${roomId}/segments/batch`;
  const response = await fetch(url, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(segments),
  });
  return response.ok;
}

export async function spinWheel(roomId, winnerName = null) {
  const url = `${BASE_URL}/${roomId}/spin`;
  const requestBody = winnerName ? JSON.stringify([winnerName]) : JSON.stringify([]);
  const response = await fetch(url, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: requestBody,
  });
  if (!response.ok) {
    const errorData = await response.text();
    throw new Error(`Spin wheel failed: ${response.statusText} - ${errorData}`);
  }
  return response.json();
}

export async function deleteSegment(roomId, segmentName) {
  const url = `${BASE_URL}/${roomId}/segments/${segmentName}`;
  const response = await fetch(url, { method: "DELETE" });
  return response.ok;
}


// Poor man unit test
async function runDemo() {
  const segments = [
    { name: "Alice", weight: 1 },
    { name: "Bob", weight: 2 },
    { name: "Charlie", weight: 3 },
  ];

  try {
    // --- Test: Create Room ---
    const { roomId } = await createRoom();
    if (!roomId) throw new Error("❌ Failed to create room");
    console.log("✅ Room created:", roomId);

    // --- Test: Add Segments ---
    const added = await addSegments(roomId, segments);
    if (!added) throw new Error("❌ Failed to add segments");
    console.log("✅ Segments added");

    // --- Test: Get Room Info ---
    const room = await getRoom(roomId);
    if (!room || !room.segments || room.segments.length !== segments.length)
      throw new Error("❌ Room info incorrect after adding segments");
    console.log("✅ Room info fetched correctly");

    // --- Test: Random Spin ---
    const randomSpin = await spinWheel(roomId);
    const randomWinner = randomSpin?.result?.newState?.[0]?.name;
    if (!randomWinner) throw new Error("❌ Random spin failed");
    console.log("✅ Random spin successful:", randomWinner);

    // --- Test: Pick Winner Spin ---
    const pickedSpin = await spinWheel(roomId, randomWinner);
    const pickedWinner = pickedSpin?.result?.newState?.[0]?.name;
    if (pickedWinner !== randomWinner)
      throw new Error("❌ Picked spin did not match expected winner");
    console.log("✅ Picked spin successful:", pickedWinner);

    // --- Test: Delete Segment ---
    const deleted = await deleteSegment(roomId, randomWinner);
    if (!deleted) throw new Error("❌ Failed to delete segment");
    const updatedRoom = await getRoom(roomId);
    const stillExists = updatedRoom.segments.find(s => s.name === randomWinner);
    if (stillExists) throw new Error("❌ Segment still exists after deletion");
    console.log("✅ Segment deleted successfully:", randomWinner);

    console.log("🎉 All tests passed!");
  } catch (err) {
    console.error("❌ Test failed:", err.message);
  }
}

runDemo();
