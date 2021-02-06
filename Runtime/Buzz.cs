using System;

namespace Bzzr
{
    public class Buzz
    {
        public Buzz(BzzrPlayer player, double serverTime, double localTime)
        {
            Player = player;
            ServerTime = TimeSpan.FromMilliseconds(serverTime);
            LocalTime = TimeSpan.FromMilliseconds(localTime);
            AverageTime = TimeSpan.FromMilliseconds((serverTime + localTime) * 0.5);
        }

        public readonly BzzrPlayer Player;
        public readonly TimeSpan ServerTime;
        public readonly TimeSpan LocalTime;
        public readonly TimeSpan AverageTime;

        public override int GetHashCode()
        {
            return Player.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is Buzz otherBuzz)
            {
                return Player == otherBuzz.Player && ServerTime == otherBuzz.ServerTime;
            }

            return false;
        }
    }
}
