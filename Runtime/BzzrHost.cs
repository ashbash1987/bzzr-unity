using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json.Linq;
using SocketIOClient;

namespace Bzzr
{
    public class BzzrHost : MonoBehaviour
    {
        #region Constants
        public const string URL = "https://bzzr.dev";
        #endregion

        #region Events
        [Header("Events")]
        public RoomCodeEvent OnRoomCreated = new RoomCodeEvent();
        public BzzrPlayerEvent OnPlayerJoined = new BzzrPlayerEvent();
        public BzzrPlayerEvent OnPlayerKicked = new BzzrPlayerEvent();
        public BzzrPlayerEvent OnPlayerDisconnected = new BzzrPlayerEvent();
        public BzzrPlayerEvent OnPlayerReconnected = new BzzrPlayerEvent();
        public BuzzEvent OnBuzz = new BuzzEvent();
        public BuzzEvent OnClearBuzz = new BuzzEvent();
        #endregion

        #region Public Properties        
        public string RoomCode
        {
            get;
            private set;
        }

        public string UserID
        {
            get;
            private set;
        }

        public bool BuzzArmed
        {
            get;
            private set;
        }

        public bool HasPlayers => Players.Any();
        public bool HasBuzzes => Buzzes.Any();
        public Buzz CurrentBuzz => Buzzes.FirstOrDefault();
        public Buzz[] AllBuzzes => Buzzes.ToArray();
        #endregion

        #region Private Fields
        private readonly ConcurrentDictionary<string, BzzrPlayer> Players = new ConcurrentDictionary<string, BzzrPlayer>();
        private readonly ConcurrentQueue<Action> ThreadSafeActions = new ConcurrentQueue<Action>();
        private readonly List<Buzz> Buzzes = new List<Buzz>();
        private SocketIO _socket = null;
        #endregion

        #region Unity Events
        private void Start()
        {
            CreateNewRoom();
        }

        private void Update()
        {
            while (!ThreadSafeActions.IsEmpty)
            {
                if (ThreadSafeActions.TryDequeue(out Action action))
                {
                    action?.Invoke();
                }
            }
        }

        private void OnDestroy()
        {
            CloseRoom();
        }
        #endregion

        #region Public Methods
        public void CreateNewRoom()
        {
            _ = RestartSocket();
        }

        public void CloseRoom()
        {
            _ = EndSocket();
        }

        public BzzrPlayer[] GetAllPlayers()
        {
            return Players.Values.ToArray();
        }

        public BzzrPlayer GetPlayer(string name)
        {
            return Players.Values.FirstOrDefault(x => x.Name.Equals(name, System.StringComparison.InvariantCultureIgnoreCase));
        }

        public void UpdatePlayer(BzzrPlayer player, string newName = null, string newColorName = null)
        {
            JObject obj = new JObject(
                new JProperty("userId", player.UserID),
                new JProperty("name", newName != null ? newName : player.Name)
            );

            if (newColorName != null || player.ColorName != null)
            {
                obj.Add("color", newColorName != null ? newColorName : player.ColorName);
            }

            _ = _socket.EmitAsync("player.update", obj);
        }

        public void ArmBuzzers()
        {
            _ = _socket.EmitAsync("buzzer.arm");
        }

        public void DisarmBuzzers()
        {
            _ = _socket.EmitAsync("buzzer.disarm");
        }

        public void LockPlayer(BzzrPlayer player)
        {
            _ = _socket.EmitAsync("buzzer.player.lock", player.UserID);
        }

        public void UnlockPlayer(BzzrPlayer player)
        {
            _ = _socket.EmitAsync("buzzer.player.unlock", player.UserID);
        }

        public void KickPlayer(BzzrPlayer player)
        {
            _ = _socket.EmitAsync("player.kick", player.UserID);
        }

        public Buzz GetCurrentBuzz()
        {
            return CurrentBuzz;
        }

        public Buzz[] GetAllBuzzes()
        {
            return AllBuzzes;
        }
        #endregion

        #region Private Methods
        private void DoUnityAction(Action action)
        {
            ThreadSafeActions.Enqueue(action);
        }

        private void Log(string message)
        {
            DoUnityAction(() => Debug.Log(message));
        }

        private async Task StartSocket()
        {
            Log("Attempting socket connection to Bzzr.dev...");
            _socket = new SocketIO(URL, new SocketIOOptions() { EIO = 4 });

            _socket.OnConnected += async (sender, e) =>
            {
                Log("Connected to Bzzr.dev!");

                _socket.On("lobby.join.success", OnJoinLobbySuccess);
                _socket.On("players.update", OnPlayersUpdate);
                _socket.On("buzzer.status", OnBuzzerStatus);
                _socket.On("buzzer.buzzes", OnBuzzesUpdate);
                _socket.On("pm", OnPM);

                await _socket.EmitAsync("lobby.host.new");
            };

            await _socket.ConnectAsync();            
        }

        private async Task EndSocket()
        {
            Log("Closing socket connection to Bzzr.dev...");

            if (_socket != null)
            {
                await _socket.DisconnectAsync();
                _socket = null;
            }

            Log("Closed socket connection.");
        }

        private async Task RestartSocket()
        {
            if (_socket != null)
            {
                await EndSocket();
            }

            await StartSocket();
        }

        private void OnJoinLobbySuccess(SocketIOResponse response)
        {
            JToken obj = response.GetValue();
            RoomCode = (string)obj["roomCode"];
            UserID = (string)obj["userId"];

            Log($"Game created with room code <b>{RoomCode}</b>.");
            DoUnityAction(() => OnRoomCreated.Invoke(RoomCode));
        }

        private void OnPlayersUpdate(SocketIOResponse response)
        {
            List<BzzrPlayer> oldPlayers = new List<BzzrPlayer>(Players.Values);
            foreach (JObject playerData in response.GetValue() as JArray)
            {
                string userID = (string)playerData["userId"];
                if (Players.TryGetValue(userID, out BzzrPlayer player))
                {
                    bool wasConnected = player.Connected;
                    player.Update(playerData);

                    if (player.Connected != wasConnected)
                    {
                        if (player.Connected)
                        {
                            Log($"Player <b>{player.Name}</b> reconnected.");
                            DoUnityAction(() => OnPlayerReconnected.Invoke(player));
                        }
                        else
                        {
                            Log($"Player <b>{player.Name}</b> disconnected.");
                            DoUnityAction(() => OnPlayerDisconnected.Invoke(player));
                        }
                    }

                    oldPlayers.Remove(player);
                }
                else
                {
                    BzzrPlayer newPlayer = new BzzrPlayer(playerData);
                    Players[userID] = newPlayer;

                    Log($"Player <b>{newPlayer.Name}</b> connected.");
                    DoUnityAction(() => OnPlayerJoined.Invoke(newPlayer));
                }
            }

            foreach (BzzrPlayer oldPlayer in oldPlayers)
            {
                Log($"<b>{oldPlayer.Name}</b> has been kicked.");
                if (Players.TryRemove(oldPlayer.UserID, out _))
                {
                    DoUnityAction(() => OnPlayerKicked.Invoke(oldPlayer));
                }                
            }
        }

        private void OnBuzzerStatus(SocketIOResponse response)
        {
            string state = response.GetValue<string>();
            BuzzArmed = state.Equals("armed");
        }

        private void OnBuzzesUpdate(SocketIOResponse response)
        {
            JObject obj = response.GetValue() as JObject;
            List<Buzz> oldBuzzes = new List<Buzz>(Buzzes);

            foreach (JProperty property in obj.Properties())
            {
                if (!Players.TryGetValue(property.Name, out BzzrPlayer player))
                {
                    continue;
                }

                double serverSpeed = (double)property.Value["serverSpeed"];
                double localSpeed = (double)property.Value["localSpeed"];
                Buzz buzzObject = new Buzz(player, serverSpeed, localSpeed);

                int buzzIndex = oldBuzzes.IndexOf(buzzObject);
                if (buzzIndex == -1)
                {
                    Log($"<b>{player.Name}</b> buzzed with {buzzObject.ServerTime.TotalSeconds:0.0000}s.");
                    Buzzes.Add(buzzObject);
                    DoUnityAction(() => OnBuzz.Invoke(buzzObject));
                }
                else
                {
                    oldBuzzes.RemoveAt(buzzIndex);
                }
            }

            foreach (Buzz oldBuzz in oldBuzzes)
            {
                Log($"<b>{oldBuzz.Player.Name}</b>'s buzz has been cleared.");
                Buzzes.Remove(oldBuzz);
                DoUnityAction(() => OnClearBuzz.Invoke(oldBuzz));
            }
        }

        private void OnPM(SocketIOResponse response)
        {
            JObject obj = response.GetValue() as JObject;
            string playerID = (string)obj["playerId"];
            if (Players.TryGetValue(playerID, out BzzrPlayer player))
            {
                player.UpdatePM(obj);
            }
        }
        #endregion
    }
}
