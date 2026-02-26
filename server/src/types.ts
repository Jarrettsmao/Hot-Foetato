// kept all types in one file for simplicity (also easier imports)
export type Player = {
    id: string;
    name: string;
    connected: boolean;
    isHost: boolean;
    isReady: boolean;
    potatoIndex: number;
};

export type GamePhase = "lobby" | "countdown" | "playing" | "ended";

export type GameRoom = {
    roomId: string;
    players: Player[];
    potatoHolderId: string | null;
    phase: GamePhase;
    endTime: number | null;
    maxPlayers: number;
    hostId: string;
};

export type ClientData = {
    playerId: string;
    roomId: string;
};

