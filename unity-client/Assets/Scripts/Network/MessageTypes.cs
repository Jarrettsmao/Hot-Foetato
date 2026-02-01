using System;
using System.Collections.Generic;

[Serializable]
public class Player
{
    public string id;
    public string name;
    public bool connected;
}

[Serializable]
public class GameRoom
{
    public string roomId;
    public List<Player> players;
    public string potatoHolderId;
    public string phase;
    public long endTime;
    public int maxPlayers;
}

[Serializable]
public class ServerMessage
{
    public string type;
    public string message;
    public GameRoom room;
    public Player loser;
    public string playerId;
    public string fromPlayerId;
    public string toPlayerId;
}

//Outgoing message classes
[Serializable]
public class JoinRoomMessage
{
    public string type = "JOIN_ROOM";
    public string roomId;
    public string playerName;
}
[Serializable]
public class StartGameMessage
{
    public string type = "START_GAME";
}
[Serializable]
public class PassPotatoMessage
{
    public string type = "PASS_POTATO";
    public string targetPlayerId;
}
[Serializable]
public class PlayAgainMessage
{
    public string type = "PLAY_AGAIN";
}
