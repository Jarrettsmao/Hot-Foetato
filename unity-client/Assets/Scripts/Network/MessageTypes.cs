using System;
using System.Collections.Generic;

public class Player
{
    public string id;
    public string name;
    public bool connected;
}

public class GameRoom
{
    public string roomId;
    public List<Player> players;
    public string potatoHolderId;
    public string phase;
    public long? endtime;
    public int maxPlayers;
}

public class ServerMessage
{
    public string type;
    public string message;
    public Player loser;
    public string playerId;
    public string fromPlayerId;
    public string toPlayerId;
}

//Outgoing message classes
public class JoinRoomMessage
{
    public string type = "JOIN_ROOM";
    public string roomId;
    public string playerName;
}

public class StartGameMessage
{
    public string type = "START_GAME";
}

public class PassPotatoMessage
{
    public string type = "PASS_POTATO";
    public string targetPlayerId;
}

public class PlayAgainMessage
{
    public string type = "PLAY_AGAIN";
}

public class RoomUpdateMessage
{
    public string type = "ROOM_UPDATE";
    public List<Player> players;
    public string phase;
    public string potatoHolderId;
}