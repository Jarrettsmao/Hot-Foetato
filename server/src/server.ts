import { WebSocketServer, WebSocket } from "ws";
import { GameRoom, Player, ClientData } from "./types";

const wss = new WebSocketServer({ port: 8080 });

//in-memory storage (replace with database later)
const rooms = new Map<string, GameRoom>();
const clients = new Map<WebSocket, ClientData>();

const MIN_PLAYERS = 2;
const MAX_PLAYERS = 4;
const MIN_TIMER = 10000; //10 sec
const MAX_TIMER = 30000; //30 sec

function broadcast(roomId: string, message: unknown) {
  clients.forEach((clientData, clientWs) => {
    if (
      clientData.roomId === roomId &&
      clientWs.readyState === WebSocket.OPEN
    ) {
      clientWs.send(JSON.stringify(message));
    }
  });
}

function startGame(roomId: string, room: GameRoom) {
  room.phase = "playing";

  // pick random potato holder
  const randomIndex = Math.floor(Math.random() * room.players.length);
  room.potatoHolderId = room.players[randomIndex].id;

  // set random timer
  const randomDelay =
    Math.floor(Math.random() * (MAX_TIMER - MIN_TIMER)) + MIN_TIMER;

  room.endTime = Date.now() + randomDelay;

  broadcast(roomId, {
    type: "GAME_STARTED",
    room: room,
    message: `Game started! ${
      room.players.find((p) => p.id === room.potatoHolderId)?.name
    } has the potato!`,
  });

  console.log(`Game started in room ${roomId}, timer: ${randomDelay / 1000}s`);
}

wss.on("connection", (ws: WebSocket) => {
  console.log("Client connected");

  ws.on("error", (error) => {
    console.error("❌ WebSocket error:", error);
  });

  ws.on("message", (data) => {
    const message = JSON.parse(data.toString());

    if (message.type === "JOIN_ROOM") {
      const { roomId, playerName, potatoIndex } = message;
      const playerId = crypto.randomUUID();

      //check required fields
      if (!roomId || !playerName) {
        ws.send(
          JSON.stringify({
            type: "ERROR",
            message: "Room ID and player name requried",
          }),
        );
        return;
      }

      //check name length
      if (playerName.length < 2 || playerName.length > 17) {
        ws.send(
          JSON.stringify({
            type: "ERROR",
            message: "Player name must be between 2 and 17 characters",
          }),
        );
        return;
      }

      //create room if doesn't exist
      if (!rooms.has(roomId)) {
        rooms.set(roomId, {
          roomId: roomId,
          players: [],
          phase: "lobby",
          potatoHolderId: null,
          endTime: null,
          maxPlayers: MAX_PLAYERS,
          hostId: playerId,
        });
        console.log(`Room: ${roomId}, host: ${playerName}`);
      }

      //add player
      const room = rooms.get(roomId);

      if (!room) return;

      //check if full
      if (room.players.length >= room.maxPlayers) {
        ws.send(
          JSON.stringify({
            type: "ERROR",
            message: `Room is full (max ${room.maxPlayers} players)`,
          }),
        );
        return;
      }

      //check if game already started
      if (room.phase === "playing") {
        ws.send(
          JSON.stringify({
            type: "ERROR",
            message: "Game in progress! Please wait for the next round.",
          }),
        );
        return;
      }

      //check for duplicate names
      if (room.players.find((p) => p.name === playerName)) {
        ws.send(
          JSON.stringify({
            type: "ERROR",
            code: "DUPLICATE_NAME",
            message:
              "Player name already taken in this room. Please change it and try again.",
          }),
        );
        return;
      }

      const isHost = room.hostId === playerId;

      const assignedIndex = getAvailableSpriteIndex(room);
      room.players.push({
        id: playerId,
        name: playerName,
        connected: true,
        isHost: isHost,
        isReady: false,
        potatoIndex: assignedIndex,
      });

      //track this connection
      clients.set(ws, { playerId, roomId });

      //broadcast to everyone in room
      broadcast(roomId, {
        type: "ROOM_UPDATE",
        room: room,
        message: `${playerName} joined the room!`,
      });

      //send confirmation of room joining
      ws.send(
        JSON.stringify({
          type: "JOIN_SUCCESS",
          playerId,
          room: room,
        }),
      );
    } else if (message.type === "LEAVE_ROOM") {
      const clientData = clients.get(ws);
      if (!clientData) return;

      const room = rooms.get(clientData.roomId);
      if (!room) return;

      const leavingPlayer = room.players.find(
        (p) => p.id === clientData.playerId,
      );
      const playerName = leavingPlayer?.name || "Unknown";
      const wasHost = room.hostId === clientData.playerId;

      console.log(
        `👋 ${playerName} left room ${clientData.roomId} from Game scene`,
      );

      // ✅ Remove the leaving player
      room.players = room.players.filter((p) => p.id !== clientData.playerId);

      // ✅ If there are still players in the room, reset to lobby
      if (room.players.length > 0) {
        // Reset room state
        room.phase = "lobby";
        room.potatoHolderId = null;
        room.endTime = null;

        // ✅ Set all remaining players to NOT ready
        room.players.forEach((p) => {
          p.isReady = false;
        });

        // ✅ Transfer host if needed
        if (wasHost) {
          const newHost = room.players[0];
          room.hostId = newHost.id;
          newHost.isHost = true;
          console.log(`👑 Host transferred to ${newHost.name}`);
        }

        console.log(`🔄 Returning ${room.players.length} players to lobby`);

        // ✅ Tell everyone else to go back to Lobby scene
        broadcast(clientData.roomId, {
          type: "RETURN_TO_LOBBY",
          room: room,
          message: `${playerName} left. Returning to lobby...`,
        });
      } else {
        // ✅ Room is empty, delete it
        rooms.delete(clientData.roomId);
        console.log(`🗑️ Room ${clientData.roomId} deleted (empty)`);
      }

      // ✅ Remove from clients map
      clients.delete(ws);

      // ✅ Send confirmation to the person who left
      ws.send(
        JSON.stringify({
          type: "LEAVE_SUCCESS",
          message: "You left the room",
        }),
      );
    } else if (message.type === "TOGGLE_READY") {
      const clientData = clients.get(ws);
      if (!clientData) return;

      const room = rooms.get(clientData.roomId);
      if (!room) return;

      const player = room.players.find((p) => p.id === clientData.playerId);
      if (!player) return;

      if (player.id === room.hostId) {
        ws.send(
          JSON.stringify({
            type: "ERROR",
            message: "Host doesn't need to ready up",
          }),
        );
        return;
      }

      player.isReady = !player.isReady;

      console.log(
        `${player.name} is now ${player.isReady ? "ready" : "not ready"}`,
      );

      broadcast(clientData.roomId, {
        type: "ROOM_UPDATE",
        room: room,
        message: `${player.name} is ${player.isReady ? "ready" : "not ready"}`,
      });
    } else if (message.type === "GAME_ROOM") {
      const clientData = clients.get(ws);
      if (!clientData) return;

      const room = rooms.get(clientData.roomId);
      if (!room) return;

      if (room.hostId !== clientData.playerId) {
        ws.send(
          JSON.stringify({
            type: "ERROR",
            message: "Only the host can start the game",
          }),
        );
        return;
      }

      broadcast(clientData.roomId, {
        type: "GAME_ROOM",
      });
    } else if (message.type === "START_GAME") {
      const clientData = clients.get(ws);

      if (!clientData) {
        return;
      }

      const room = rooms.get(clientData.roomId);
      if (!room) return;

      //check if sender is host
      if (room.hostId !== clientData.playerId) {
        ws.send(
          JSON.stringify({
            type: "ERROR",
            message: "Only the host can start the game",
          }),
        );
        return;
      }

      // if not enough players throw error
      if (room.players.length < 2) {
        ws.send(
          JSON.stringify({
            type: "ERROR",
            message: "Need at least 2 players to start",
          }),
        );

        return;
      }

      if (room.phase !== "lobby") {
        ws.send(
          JSON.stringify({
            type: "ERROR",
            message: "Game already in progress",
          }),
        );
        return;
      }

      startGame(clientData.roomId, room);
    } else if (message.type === "PASS_POTATO") {
      const clientData = clients.get(ws);
      if (!clientData) return;

      const room = rooms.get(clientData.roomId);
      if (!room) return;

      const { targetPlayerId } = message;

      //check if playing
      if (room.phase !== "playing") {
        ws.send(
          JSON.stringify({
            type: "ERROR",
            message: "Game is not active",
          }),
        );
        return;
      }

      //check if player has potato
      if (room.potatoHolderId !== clientData.playerId) {
        ws.send(
          JSON.stringify({
            type: "ERROR",
            message: "You do not have the potato",
          }),
        );
        return;
      }

      //checks target player exists
      console.log("target id: " + targetPlayerId);
      const targetPlayer = room.players.find((p) => p.id === targetPlayerId);
      if (!targetPlayer) {
        ws.send(
          JSON.stringify({
            type: "ERROR",
            message: "Invalid target player",
          }),
        );
        return;
      }

      //pass potato
      room.potatoHolderId = targetPlayerId;

      //broadcast potato passed
      broadcast(clientData.roomId, {
        type: "POTATO_PASSED",
        room: room,
        message: `Potato passed to ${targetPlayer.name}!`,
      });

      console.log(
        `Potato passed to ${targetPlayer.name} in room ${clientData.roomId}`,
      );
    } else if (message.type === "PLAY_AGAIN") {
      const clientData = clients.get(ws);
      if (!clientData) return;

      const room = rooms.get(clientData.roomId);
      if (!room) return;

      //check if sender is host
      if (room.hostId !== clientData.playerId) {
        ws.send(
          JSON.stringify({
            type: "ERROR",
            message: "Only the host can reset the game",
          }),
        );
        return;
      }

      //only allow reset if game is ended
      if (room.phase !== "ended") {
        ws.send(
          JSON.stringify({
            type: "ERROR",
            message: "Game is still in progress",
          }),
        );
        return;
      }

      //reset room
      room.phase = "lobby";
      room.potatoHolderId = null;
      room.endTime = null;

      console.log(`Room ${clientData.roomId} reset for new game`);

      // broadcast(clientData.roomId, {
      //   type: "ROOM_UPDATE",
      //   room: room,
      //   message: "Room reset! Ready for another round?",
      // });

      startGame(clientData.roomId, room);
    }
  });

  const disconnectTimers = new Map<string, NodeJS.Timeout>();
  const timerDuration: number = 5000; // 5 seconds

  ws.on("close", () => {
    console.log("Client disconnected");

    const clientData = clients.get(ws);
    if (!clientData) return;

    const room = rooms.get(clientData.roomId);
    if (!room) return;

    const disconnectedPlayer = room.players.find(
      (p) => p.id === clientData.playerId,
    );
    const playerName = disconnectedPlayer
      ? disconnectedPlayer.name
      : "Unknown Player";
    const wasHost = room.hostId === clientData.playerId;

    // Start a grace period timer
    const timer = setTimeout(() => {
      const stillInRoom = room.players.find(
        (p) => p.id === clientData.playerId,
      );
      if (stillInRoom) {
        room.players = room.players.filter((p) => p.id !== clientData.playerId);
      }

      if (wasHost && room.players.length > 0) {
        const newHost = room.players[0];
        room.hostId = newHost.id;
        newHost.isHost = true;

        console.log(`New host in room ${clientData.roomId} is ${newHost.name}`);

        broadcast(clientData.roomId, {
          type: "HOST_TRANSFERRED",
          newHostId: newHost.id,
          room: room,
          message: `${playerName} has left. ${newHost.name} is now the host`,
        });
      }

      if (room.players.length === 0) {
        rooms.delete(clientData.roomId);
        console.log(`Room ${clientData.roomId} deleted (empty)`);
      } else if (!wasHost) {
        broadcast(clientData.roomId, {
          type: "ROOM_UPDATE",
          room: room,
          message: `${playerName} has disconnected`,
        });
      }

      disconnectTimers.delete(clientData.playerId);
    }, timerDuration);

    disconnectTimers.set(clientData.playerId, timer);
  });
});

//timer loop - checks every 100ms
setInterval(() => {
  const now = Date.now();

  rooms.forEach((room, roomId) => {
    if (room.phase === "playing" && room.endTime !== null) {
      if (now >= room.endTime) {
        const loser = room.players.find((p) => p.id === room.potatoHolderId);

        //end game
        room.phase = "ended";
        room.endTime = null;

        //broadcast gg
        broadcast(roomId, {
          type: "GAME_ENDED",
          room: room,
          loser: loser,
          message: `💥 BOOM! ${loser?.name || "Someone"} lost!`,
        });

        console.log(`Game ended in room ${roomId}, loser: ${loser?.name}`);
      }
    }
  });
}, 100);

function getAvailableSpriteIndex(room) {
  const used = new Set(room.players.map((p) => p.potatoIndex));

  for (let i = 0; i < MAX_PLAYERS; i++) {
    if (!used.has(i)) {
      return i;
    }
  }

  // Fallback (should never happen if maxPlayers is enforced)
  return 0;
}

console.log("🚀 WebSocket server running on ws://localhost:8080");
