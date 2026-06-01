namespace Features.MainMenu.Networking.Rooms.Data
{
    public sealed class RoomSessionData
    {
        public string RoomCode { get; set; }
        public string PlayerId { get; set; }
        public string Status { get; set; }
        public string Host { get; set; }
        public ushort Port { get; set; }

        public bool HasServerEndpoint =>
            !string.IsNullOrWhiteSpace(Host) && Port > 0;
    }
}