using System;
using System.Net.WebSockets;

namespace MMOServer.Models;

public class Player
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public WebSocket ClientSocket { get; set; }

    public bool IsDirty { get; set; } = false;

    public readonly object BufferLock = new object();

    public List<object> OutgoingPackets { get; } = new List<object>();


    private Position _position;
    private int _health = 10;
    private int _maxHealth = 10;


    public DateTime LastActionTime { get; set; } = DateTime.UtcNow;


    public Position Position
    {
        get => _position;
        set
        {
            if(_position.X != value.X || _position.Y != value.Y)
            {
                _position = value;
                IsDirty = true;
            }
        }
    }

    public int Health 
    {
        get => _health;
        set
        {
            if(_health != value)
            {
                _health = Math.Clamp(value, 0, _maxHealth);
                IsDirty = true;
            }
        }
    }

    public int MaxHealth
    {
        get => _maxHealth;
        set => _maxHealth = value;
    }

    public int ChunkX { get; set; } = -1;
    public int ChunkY { get; set; } = -1;

    public Player(int id, string name, WebSocket socket, Position startPosition)
    {
        Id = id;
        Name = name;
        ClientSocket = socket;
        _position = startPosition;
    }
}
