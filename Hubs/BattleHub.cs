using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using BatallaNaval.Models;

namespace BatallaNaval.Hubs
{
    public class MultiplayerRoom
    {
        public string RoomCode { get; set; } = "";
        public string HostConnectionId { get; set; } = "";
        public string HostName { get; set; } = "Jugador 1";
        public Board HostBoard { get; set; } = new Board();

        public string? GuestConnectionId { get; set; } = null;
        public string GuestName { get; set; } = "Jugador 2";
        public Board GuestBoard { get; set; } = new Board();

        public bool HostReady { get; set; } = false;
        public bool GuestReady { get; set; } = false;
        public string CurrentTurnConnectionId { get; set; } = "";
        public bool IsGameStarted { get; set; } = false;
    }

    public class BattleHub : Hub
    {
        private static readonly ConcurrentDictionary<string, MultiplayerRoom> Rooms = new();
        private static readonly Random RandomRng = new();

        public async Task CreateRoom(string playerName)
        {
            string roomCode = GenerateRoomCode();
            var room = new MultiplayerRoom
            {
                RoomCode = roomCode,
                HostConnectionId = Context.ConnectionId,
                HostName = string.IsNullOrWhiteSpace(playerName) ? "Comandante Alfa" : playerName,
                CurrentTurnConnectionId = Context.ConnectionId
            };

            Rooms[roomCode] = room;
            await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);
            await Clients.Caller.SendAsync("RoomCreated", roomCode, room.HostName);
        }

        public async Task JoinRoom(string roomCode, string playerName)
        {
            roomCode = roomCode.Trim().ToUpper();

            if (!Rooms.TryGetValue(roomCode, out var room))
            {
                await Clients.Caller.SendAsync("Error", "La sala especificada no existe.");
                return;
            }

            if (room.GuestConnectionId != null)
            {
                await Clients.Caller.SendAsync("Error", "La sala ya está llena.");
                return;
            }

            room.GuestConnectionId = Context.ConnectionId;
            room.GuestName = string.IsNullOrWhiteSpace(playerName) ? "Comandante Beta" : playerName;

            await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);
            await Clients.Group(roomCode).SendAsync("PlayerJoined", room.HostName, room.GuestName);
        }

        public async Task ReadyFleet(string roomCode, bool autoPlace)
        {
            roomCode = roomCode.Trim().ToUpper();
            if (!Rooms.TryGetValue(roomCode, out var room)) return;

            bool isHost = Context.ConnectionId == room.HostConnectionId;
            Board playerBoard = isHost ? room.HostBoard : room.GuestBoard;

            if (autoPlace || playerBoard.Fleet.Count == 0)
            {
                playerBoard.PlaceFleetRandomly();
            }

            if (isHost) room.HostReady = true;
            else room.GuestReady = true;

            await Clients.Group(roomCode).SendAsync("FleetStatusUpdate", isHost ? room.HostName : room.GuestName);

            if (room.HostReady && room.GuestReady)
            {
                room.IsGameStarted = true;
                await Clients.Group(roomCode).SendAsync("GameStarted", room.HostName, room.CurrentTurnConnectionId);
            }
        }

        public async Task SendShot(string roomCode, int row, int col)
        {
            roomCode = roomCode.Trim().ToUpper();
            if (!Rooms.TryGetValue(roomCode, out var room) || !room.IsGameStarted) return;

            if (Context.ConnectionId != room.CurrentTurnConnectionId)
            {
                await Clients.Caller.SendAsync("Error", "No es tu turno.");
                return;
            }

            bool isHost = Context.ConnectionId == room.HostConnectionId;
            Board targetBoard = isHost ? room.GuestBoard : room.HostBoard;
            string shooterName = isHost ? room.HostName : room.GuestName;
            string nextTurnConnectionId = isHost ? room.GuestConnectionId! : room.HostConnectionId;

            var (result, hitShip) = targetBoard.ReceiveShot(row, col);

            if (result == ShotResult.AlreadyAttacked || result == ShotResult.Invalid)
            {
                await Clients.Caller.SendAsync("Error", "Disparo inválido.");
                return;
            }

            room.CurrentTurnConnectionId = nextTurnConnectionId;

            bool isGameOver = targetBoard.AllShipsSunk();

            await Clients.Group(roomCode).SendAsync("ShotResult", new
            {
                ShooterConnectionId = Context.ConnectionId,
                ShooterName = shooterName,
                Row = row,
                Col = col,
                Result = result.ToString(),
                HitShipName = hitShip?.Name,
                NextTurnConnectionId = nextTurnConnectionId,
                IsGameOver = isGameOver
            });
        }

        private static string GenerateRoomCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            return new string(Enumerable.Repeat(chars, 4).Select(s => s[RandomRng.Next(s.Length)]).ToArray());
        }
    }
}
