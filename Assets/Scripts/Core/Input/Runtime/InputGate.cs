using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core.Input.Runtime
{
    public sealed class InputGate
    {
        private bool _isBlocked;
        private float _blockedUntilTime;

        public bool IsBlocked
        {
            get
            {
                if (_isBlocked)
                    return true;

                return Time.unscaledTime < _blockedUntilTime;
            }
        }

        public bool CanReceiveInput => !IsBlocked;

        public void Block()
        {
            _isBlocked = true;
        }

        public void Unblock()
        {
            _isBlocked = false;
        }

        public void BlockFor(float seconds)
        {
            _blockedUntilTime = Mathf.Max(
                _blockedUntilTime,
                Time.unscaledTime + seconds);
        }
    }
}