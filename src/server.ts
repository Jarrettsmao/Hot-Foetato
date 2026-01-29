import { WebSocketServer, WebSocket } from "ws";

const wss = new WebSocketServer({ port: 8080 });

//in-memory storage (replace with database later)
const rooms = new Map<string, any>();
const clients = new Map<WebSocket, { playerId: string; roomId: string }>();

wss.on("connection", (ws) => {
  console.log("Client connected");

  ws.on("message", (data) => {
    const message = JSON.parse(data.toString());

    if (message.type === "JOIN_ROOM") {
      const { roomId, playerName } = message;
      const playerId = crypto.randomUUID();

      //create room if doesn't exist
      if (!rooms.has(roomId)) {
        rooms.set(roomId, {
          id: roomId,
          players: [],
          phase: "lobby",
          potatoHolderId: null,
          endTime: null,
        });
      }

      //add player
      const room = rooms.get(roomId);
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

      //set randomtimer 10-30 sec
      const randomDelay = Math.floor(Math.random() * 20000) + 10000;
      room.endTime = Date.now() + randomDelay;

      //broadcast game start
      broadcast(clientData.roomId, {
        type: "GAME_STARTED",
        room: room,
        message: `Game started! ${room.players.find((p) => p.id === room.potatoHolderId)?.name} has the potato!`,
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

      console.log(
        `Potato passed to ${targetPlayer.name} in room ${clientData.roomId}`,
      );
    }
  });

  function broadcast(roomId: string, message: any) {
    clients.forEach((clientData, clientWs) => {
      if (
        clientData.roomId === roomId &&
        clientWs.readyState === WebSocket.OPEN
      ) {
        clientWs.send(JSON.stringify(message));
      }
    });
  }

  ws.on("close", () => {
    console.log("Client disconnected");

    const clientData = clients.get(ws);

    if (clientData) {
      const room = rooms.get(clientData.roomId);
      if (room) {
        const disconnectedPlayer = room.players.find(
          (p) => p.id === clientData.playerId,
        );
        const playerName = disconnectedPlayer
          ? disconnectedPlayer.name
          : "Unknown Player";

        room.players = room.players.filter((p) => p.id !== clientData.playerId);
        //Broadcast update
        broadcast(clientData.roomId, {
          type: "ROOM_UPDATE",
          room: room,
          message: playerName + " disconnected",
        });
      }

      //remove from clients map
      clients.delete(ws);
    }
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
});
