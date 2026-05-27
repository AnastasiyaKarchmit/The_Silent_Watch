namespace Core.Audio.Contracts
{
    public interface IUISoundPlayer
    {
        void PlayButtonClick();
        void PlayButtonHover();
        void PlayBack();
        void PlayConfirm();
        void PlayCancel();
        void PlayError();
        void PlayPopupOpen();
        void PlayPopupClose();
    }
}