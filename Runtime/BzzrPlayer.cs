using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace Bzzr
{
    public class BzzrPlayer
    {
        public BzzrPlayer(JObject data)
        {
            UserID = (string)data["userId"];
            Update(data);
        }

        public string UserID
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public string ColorName
        {
            get => _colorName;
            set
            {
                _colorName = value;
                if (_colorName == null)
                {
                    Color = Color.white;
                    return;
                }

                if (ColorTable.TryGetValue(_colorName, out Color color))
                {
                    Color = color;
                }
                else
                {
                    Color = Color.white;
                }
            }
        }

        public Color Color
        {
            get;
            private set;
        } = Color.white;

        public bool Connected
        {
            get;
            set;
        }

        public string Message
        {
            get;
            set;
        }

        public static IEnumerable<string> ColorNames => ColorTable.Keys;

        private static readonly Dictionary<string, Color> ColorTable = new Dictionary<string, Color>()
        {
            { "red", Color.red },
            { "orange", new Color(1.0f, 0.5f, 0.0f) },
            { "yellow", Color.yellow },
            { "olive", new Color(0.7f, 0.8f, 0.1f) },
            { "green", Color.green },
            { "teal", new Color(0.0f, 0.8f, 0.8f) },
            { "blue", Color.blue },
            { "violet", new Color(0.4f, 0.2f, 0.8f) },
            { "purple", new Color(0.7f, 0.2f, 0.8f) },
            { "pink", new Color(0.9f, 0.2f, 0.6f) },
            { "brown", new Color(0.7f, 0.4f, 0.2f) },
            { "grey", Color.grey }
        };

        private string _colorName = null;

        public void Update(JObject data)
        {
            Name = (string)data["name"];
            Connected = ((string)data["connectionStatus"]).Equals("connected");

            if (data.ContainsKey("color"))
            {
                ColorName = (string)data["color"];
            }
            else
            {
                ColorName = null;
            }
        }

        public void UpdatePM(JObject data)
        {
            Message = (string)data["message"];
        }

        public override int GetHashCode()
        {
            return UserID.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is BzzrPlayer otherPlayer)
            {
                return UserID == otherPlayer.UserID;
            }

            return false;
        }
    }
}
