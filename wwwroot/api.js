const BASE_URL = "http://localhost:5252/api/Rooms";

export async function createRoom() {
  const response = await fetch(BASE_URL, { method: "POST" });
  if (!response.ok)
    throw new Error(`Failed to create room: ${response.statusText}`);
  const data = await response.json();
  return data;
}

export async function getRoom(roomId) {
  const response = await fetch(`${BASE_URL}/${roomId}`);
  if (!response.ok)
    throw new Error(`Failed to get room ${roomId}: ${response.statusText}`);
  const data = await response.json();
  return data;
}

export async function addSegments(roomId, segments) {
  const url = `http://localhost:5252/api/Rooms/${roomId}/segments/batch`;
  const response = await fetch(url, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(segments),
  });
  return response.ok;
}

export async function spinWheel(roomId, winnerName = null) {
  // winnerName is optional
  const url = `${BASE_URL}/${roomId}/spin`;
  let requestBody;

  if (winnerName) {
    requestBody = JSON.stringify([winnerName]); // Send winner name in an array as per your example
  } else {
    requestBody = JSON.stringify([]); // Empty array for a random spin
  }

  const response = await fetch(url, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: requestBody,
  });
  if (!response.ok) {
    // Attempt to get error message from backend if available
    const errorData = await response.text(); // Use .text() first in case it's not JSON
    throw new Error(`Spin wheel failed: ${response.statusText} - ${errorData}`);
  }
  const data = await response.json();
  return data;
}

export async function deleteTestSegment(roomId) {
  // This API seems specific, retained as is
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

runDemo();
