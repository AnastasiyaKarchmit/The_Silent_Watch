using System;
using Core.Patterns.MVP;
using Core.Save;
using Cysharp.Threading.Tasks;

namespace Features.MainMenu
{
    public sealed class MainMenuModel : IModel
    {
        private readonly ISaveSystem _saveSystem;

        public MainMenuModel(
            ISaveSystem saveSystem)
        {
            _saveSystem = saveSystem ?? throw new ArgumentNullException(nameof(saveSystem));
        }

        public async UniTask EnsureSaveLoadedAsync()
        {
            if (!_saveSystem.IsLoaded)
                await _saveSystem.LoadAsync();
        }

        public bool HasPreviousPlaySession()
        {
            PersistentData data = _saveSystem.Data;

            if (data == null)
                return false;
            
            //add needed checks

            return true;
        }

        public UniTask ResetProgressAsync()
        {
            return _saveSystem.ResetAsync();
        }

        public async UniTask SaveBeforeQuit()
        {
            await _saveSystem.SaveAsync();
        }

        public void Dispose()
        {
        }
    }
}