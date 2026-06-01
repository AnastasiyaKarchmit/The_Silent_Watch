using UnityEngine;

namespace Features.MainMenu.Networking.Rooms
{
    [CreateAssetMenu(
        fileName = "BackendConnectionConfig",
        menuName = "Configs/Networking/Backend Connection Config")]
    public sealed class BackendConnectionConfig : ScriptableObject
    {
        [SerializeField] private string baseUrl = "http://localhost:5000";

        public string BaseUrl => baseUrl.TrimEnd('/');
    }
}