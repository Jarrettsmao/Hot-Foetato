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

wss.on("connection", (ws: WebSocket) => {
  console.log("Client connected");

  ws.on("message", (data) => {
    const message = JSON.parse(data.toString());

    if (message.type === "JOIN_ROOM") {
      const { roomId, playerName } = message;
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
      if (playerName.length < 2 || playerName.length > 20) {
        ws.send(
          JSON.stringify({
            type: "ERROR",
            message: "Player name must be between 1 and 20 characters",
          }),
        );
        return;
      }

      //create room if doesn't exist
      if (!rooms.has(roomId)) {
        rooms.set(roomId, {
          id: roomId,
          players: [],
          phase: "lobby",
          potatoHolderId: null,
          endTime: null,
          maxPlayers: MAX_PLAYERS,
        });
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
            message:
              "Player name already taken in this room. Please change it and try again.",
          }),
        );
        return;
      }

      room.players.push({
        id: playerId,
        name: playerName,
        connected: true,
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
          roomId,
        }),
      );
    } else if (message.type === "START_GAME") {
      const clientData = clients.get(ws);

      if (!clientData) {
        return;
      }

      const room = rooms.get(clientData.roomId);
      if (!room) {
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

      //start game
      room.phase = "playing";

      //pick random person to start with potato
      const randomIndex = Math.floor(Math.random() * room.players.length);
      room.potatoHolderId = room.players[randomIndex].id;

      //set randomtimer
      const randomDelay =
        Math.floor(Math.random() * (MAX_TIMER - MIN_TIMER)) + MIN_TIMER;
      room.endTime = Date.now() + randomDelay;

      //broadcast game start
      broadcast(clientData.roomId, {
        type: "GAME_STARTED",
        room: room,
        message: `Game started! ${room.players.find((p) => p.id === room.potatoHolderId)?.name} has the potato!`,
      });

      broadcast(clientData.roomId, {
        type: "ROOM_UPDATE",
        room: room,
        message: "Sending room update",
      });

      console.log(
        `Game started in room ${clientData.roomId}, timer: ${randomDelay / 1000} seconds`,
      );
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

      broadcast(clientData.roomId, {
        type: "ROOM_UPDATE",
        room: room,
        message: "Sending room update",
      });

      console.log(
        `Potato passed to ${targetPlayer.name} in room ${clientData.roomId}`,
      );
    } else if (message.type === "PLAY_AGAIN") {
      const clientData = clients.get(ws);
      if (!clientData) return;

      const room = rooms.get(clientData.roomId);
      if (!room) return;

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

      broadcast(clientData.roomId, {
        type: "ROOM_UPDATE",
        room: room,
        message: "Room reset! Ready for another round?",
      });
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

    // Start a grace period timer (e.g., 5 seconds)
    const timer = setTimeout(() => {
      // Only remove player if they haven't reconnected
      room.players = room.players.filter((p) => p.id !== clientData.playerId);

      if (room.players.length === 0) {
        rooms.delete(clientData.roomId);
        console.log(`Room ${clientData.roomId} deleted (empty)`);
      } else {
        broadcast(clientData.roomId, {
          type: "ROOM_UPDATE",
          room: room,
          message: playerName + " disconnected",
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

console.log("🚀 WebSocket server running on ws://localhost:8080");
