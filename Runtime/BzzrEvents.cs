using System;
using UnityEngine.Events;

namespace Bzzr
{
    [Serializable]
    public class RoomCodeEvent : UnityEvent<string>
    {
        public RoomCodeEvent()
        {
        }
    }

    [Serializable]
    public class BzzrPlayerEvent : UnityEvent<BzzrPlayer>
    {
        public BzzrPlayerEvent()
        {
        }
    }

    [Serializable]
    public class BuzzEvent : UnityEvent<Buzz>
    {
        public BuzzEvent()
        {
        }
    }
}
