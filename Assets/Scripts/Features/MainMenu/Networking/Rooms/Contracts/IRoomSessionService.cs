using System.Threading;
using Cysharp.Threading.Tasks;
using Features.MainMenu.Networking.Rooms.Data;

namespace Features.MainMenu.Networking.Rooms.Contracts
{
    public interface IRoomSessionService
    {
        RoomSessionData CurrentSession { get; }

        UniTask<RoomSessionData> CreateRoomAsync(
            string playerName,
            CancellationToken cancellationToken);

        UniTask<RoomSessionData> JoinRoomAsync(
            string roomCode,
            string playerName,
            CancellationToken cancellationToken);

        UniTask<RoomSessionData> SetReadyAsync(
            bool isReady,
            CancellationToken cancellationToken);

        UniTask<RoomSessionData> StartRoomAsync(
            CancellationToken cancellationToken);

        UniTask<RoomSessionData> RefreshStatusAsync(
            CancellationToken cancellationToken);

        UniTask<RoomSessionData> WaitForServerReadyAsync(
            CancellationToken cancellationToken);
        
        UniTask EndRoomAsync(CancellationToken cancellationToken);
    }
}