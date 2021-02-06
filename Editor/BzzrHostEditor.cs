using System;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace Bzzr
{
    [CustomEditor(typeof(BzzrHost))]
    public class BzzrHostEditor : Editor
    {
        private BzzrHost _obj = null;

        private void OnEnable()
        {
            _obj = target as BzzrHost;
        }

        private void OnDisable()
        {
            _obj = null;
        }

        public override bool RequiresConstantRepaint()
        {
            return true;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.LabelField("Room Info", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Room Code", _obj.RoomCode);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Room Options", EditorStyles.boldLabel);
            bool buzzersArmed = EditorGUILayout.Toggle("Buzzers Armed?", _obj.BuzzArmed);
            if (buzzersArmed != _obj.BuzzArmed)
            {
                if (buzzersArmed)
                {
                    _obj.ArmBuzzers();
                }
                else
                {
                    _obj.DisarmBuzzers();
                }
            }
            EditorGUILayout.Space();

            string[] colorNames = BzzrPlayer.ColorNames.ToArray();

            EditorGUILayout.LabelField("Players", EditorStyles.boldLabel);
            if (_obj.HasPlayers)
            {
                foreach (BzzrPlayer player in _obj.GetAllPlayers())
                {
                    EditorGUILayout.BeginHorizontal();

                    Color oldColor = GUI.color;
                    GUI.color = player.Color;
                    if (player.Connected)
                    {
                        EditorGUILayout.LabelField(player.Name, GUILayout.ExpandWidth(true));
                    }
                    else
                    {
                        EditorGUILayout.LabelField($"{player.Name} (Disconnected)", GUILayout.ExpandWidth(true));
                    }
                    GUI.color = oldColor;

                    int oldColorIndex = Array.IndexOf(colorNames, player.ColorName);
                    int colorIndex = EditorGUILayout.Popup(oldColorIndex, colorNames);
                    if (colorIndex != oldColorIndex)
                    {
                        _obj.UpdatePlayer(player, newColorName: colorNames[colorIndex]);
                    }

                    if (GUILayout.Button("Kick", GUILayout.Width(40.0f)))
                    {
                        _obj.KickPlayer(player);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.LabelField("<No players>");
            }
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Buzzes", EditorStyles.boldLabel);
            if (_obj.HasBuzzes)
            {
                foreach (Buzz buzz in _obj.GetAllBuzzes())
                {
                    EditorGUILayout.BeginHorizontal();
                    Color oldColor = GUI.color;
                    GUI.color = buzz.Player.Color;
                    EditorGUILayout.LabelField(buzz.Player.Name, GUILayout.ExpandWidth(true));
                    GUI.color = oldColor;
                    EditorGUILayout.LabelField(buzz.ServerTime.TotalSeconds.ToString("0.0000"), GUILayout.Width(100));
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.LabelField("<No buzzes>");
            }
        }
    }
}
