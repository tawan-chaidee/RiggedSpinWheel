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

export async function deleteTestSegment(roomId) {
  const url = `${BASE_URL}/${roomId}/game/segments/test`;
  const response = await fetch(url, { method: "DELETE" });
  return response.ok;
}


// runDemo can be kept for testing api.js, ensure it calls spinWheel appropriately
async function runDemo() {
  const segments = [
    { name: "Alice", weight: 1 },
    { name: "Bob", weight: 2 },
    { name: "Charlie", weight: 3 },
  ];
  try {
    console.log("Creating a new room...");
    const { roomId } = await createRoom();
    console.log("Room ID:", roomId);

    if (!roomId) throw new Error("Room ID not obtained in demo.");

    console.log("Adding segments...");
    const added = await addSegments(roomId, segments);
    console.log("Segments added:", added);

    console.log("Fetching room info...");
    const roomInfo = await getRoom(roomId);
    console.log("Room Info:", roomInfo);

    if (roomInfo && roomInfo.segments && roomInfo.segments.length > 0) {
      console.log("Spinning the wheel (random)...");
      const randomResult = await spinWheel(roomId); // Test random spin
      console.log("Random Spin Result:", JSON.stringify(randomResult, null, 2));

      if (
        randomResult.result &&
        randomResult.result.newState &&
        randomResult.result.newState.length > 0
      ) {
        const pickWinnerName = randomResult.result.newState[0].name; // Pick one of the remaining
        console.log(`Spinning the wheel (picking '${pickWinnerName}')...`);
        const pickedResult = await spinWheel(roomId, pickWinnerName); // Test picked spin
        console.log(
          "Picked Spin Result:",
          JSON.stringify(pickedResult, null, 2)
        );
      } else {
        console.log(
          "Skipping picked spin test as no segments remaining after first spin or error."
        );
      }
    } else {
      console.log(
        "Skipping spin tests due to no segments or error fetching room info."
      );
    }
  } catch (error) {
    console.error("Demo failed:", error.message);
  }
}

// runDemo();
