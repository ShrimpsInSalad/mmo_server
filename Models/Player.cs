namespace MMOServer.Models;

using System;
using System.Net.WebSockets;

public class Player
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public WebSocket ClientSocket { get; set; }

    public bool IsDirty { get; set; } = false;


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


    public Player(int id, string name, WebSocket socket, Position startPosition)
    {
        Id = id;
        Name = name;
        ClientSocket = socket;
        _position = startPosition;
    }
}
