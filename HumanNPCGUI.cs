//#define DEBUG
using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using System.Globalization;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;
using System.Text;
using System.Linq;
using Oxide.Core.Plugins;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    [Info("HumanNPC Editor GUI", "RFC1920", "1.0.1")]
    [Description("Oxide Plugin")]
    class HumanNPCGUI : RustPlugin
    {
        #region vars
        [PluginReference]
        Plugin HumanNPC;

        private const string permNPCGuiUse = "humannpcgui.use";
        const string NPCGUI = "npcgui.editor";
        const string NPCGUK = "npcgui.kitselect";
        const string NPCGUN = "npcgui.kitsetnum";
        const string NPCGUS = "npcgui.select";
        const string NPCGUV = "npcgui.setval";
        #endregion

        #region Message
        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);
        private void Message(IPlayer player, string key, params object[] args) => player.Reply(Lang(key, player.Id, args));
        #endregion

        #region init
        void Init()
        {
            AddCovalenceCommand("npcgui", "npcEdit");

            permission.RegisterPermission(permNPCGuiUse, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["npcgui"] = "HumanNPC GUI",
                ["npcguisel"] = "HumanNPCGUI NPC Select ",
                ["npcguikit"] = "HumanNPCGUI Kit Select",
                ["close"] = "Close",
                ["none"] = "None",
                ["needselect"] = "Select NPC",
                ["select"] = "Select",
                ["editing"] = "Editing",
                ["mustselect"] = "Please press 'Select' to choose an NPC.",
                ["guihelp1"] = "For blue buttons, click to toggle true/false.",
                ["guihelp2"] = "For all values above in gray, you may type a new value and press enter.",
                ["guihelp3"] = "For kit, press the button to select a kit.",
                ["add"] = "Add",
                ["new"] = "Create New",
                ["spawnhere"] = "Spawn Here",
                ["name"] = "Name",
                ["online"] = "Online",
                ["offline"] = "Offline",
                ["deauthall"] = "DeAuthAll",
                ["remove"] = "Remove"
            }, this);
        }

        void Loaded()
        {
        }

        void Unload()
        {
            foreach(BasePlayer player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, NPCGUI);
                CuiHelper.DestroyUi(player, NPCGUK);
                CuiHelper.DestroyUi(player, NPCGUN);
                CuiHelper.DestroyUi(player, NPCGUS);
                CuiHelper.DestroyUi(player, NPCGUV);
            }
        }

        protected override void LoadDefaultConfig()
        {
        }
        #endregion

        #region Main
        [Command("npcgui")]
        void npcEdit(IPlayer iplayer, string command, string[] args)
        {
            if(!iplayer.HasPermission(permNPCGuiUse)) return;
            var player = iplayer.Object as BasePlayer;
            ulong npc = 0;

            if(args.Length > 0)
            {
                switch(args[0])
                {
                    case "selkitclose":
                        CuiHelper.DestroyUi(player, NPCGUK);
                        break;
                    case "npcselkit":
                        Puts($"selkit {args[1]} {args[2]}");
                        NPCKitGUI(player, ulong.Parse(args[1]), args[2]);
                        break;
                    case "kitsel":
                        Puts($"kitsel {args[1]} {args[2]}");
                        CuiHelper.DestroyUi(player, NPCGUK);
                        npc = ulong.Parse(args[1]);
                        Interface.CallHook("SetHumanNPCInfo", npc, "spawnkit", args[2]);
                        npcEditGUI(player, npc);
                        break;
                    case "npctoggle":
                        if(args.Length > 3)
                        {
                            npc = ulong.Parse(args[1]);
                            string toset = args[2];
                            string newval = args[3] == "True" ? "false" : "true";
                            Interface.CallHook("SetHumanNPCInfo", npc, toset, newval);
                        }
                        npcEditGUI(player, npc);
                        break;
                    case "spawn":
                        Puts($"spawn {args[1]}");
                        npc = ulong.Parse(args[1]);
                        string pos = args[3];
                        pos = pos.Replace("(","").Replace(")","");
                        Interface.CallHook("SetHumanNPCInfo", npc, "spawn", pos);
                        npcEditGUI(player, npc);
                        break;
                    case "spawnhere":
                        Puts($"spawnhere {args[1]}");
                        npc = ulong.Parse(args[1]);
                        string newSpawn = player.transform.position.x.ToString() + "," + player.transform.position.y + "," + player.transform.position.z.ToString();
                        Interface.CallHook("SetHumanNPCInfo", npc, "spawn", newSpawn);
                        npcEditGUI(player, npc);
                        break;
                    case "new":
                        CuiHelper.DestroyUi(player, NPCGUS);
                        Quaternion currentRot;
                        TryGetPlayerView(player, out currentRot);
                        npc = (ulong)Interface.CallHook("SpawnHumanNPC", player.transform.position, currentRot, "HumanNPCGUI");
                        npcEditGUI(player, npc);
                        break;
                    case "npcset":
                        Puts($"npcset {args[1]} {args[2]} {args[3]} {args[4]}");
                        npc = ulong.Parse(args[1]);
                        Interface.CallHook("SetHumanNPCInfo", npc, args[2], args[4]);
                        npcEditGUI(player, npc);
                        break;
                    case "close":
                        CuiHelper.DestroyUi(player, NPCGUI);
                        break;
                    case "select":
                        NPCSelectGUI(player);
                        break;
                    case "selclose":
                        CuiHelper.DestroyUi(player, NPCGUS);
                        break;
                    case "npc":
                    default:
                        if(args.Length > 1)
                        {
                            CuiHelper.DestroyUi(player, NPCGUS);
                            npc = ulong.Parse(args[1]);
                            npcEditGUI(player, npc);
                        }
                        break;
                }
            }
            else
            {
                npcEditGUI(player);
            }
        }

        void npcEditGUI(BasePlayer player, ulong npc = 0)
        {
            if(player == null) return;
            CuiHelper.DestroyUi(player, NPCGUI);

            string npcname = Lang("needselect");
            if(npc > 0)
            {
                npcname = Lang("editing") + " " + (string)HumanNPC?.Call("HumanNPCname", npc);
            }

            CuiElementContainer container = UI.Container(NPCGUI, UI.Color("2b2b2b", 1f), "0.05 0.05", "0.95 0.95", true, "Overlay");
            UI.Button(ref container, NPCGUI, UI.Color("#d85540", 1f), Lang("select"), 12, "0.86 0.95", "0.92 0.98", $"npcgui select");
            UI.Button(ref container, NPCGUI, UI.Color("#d85540", 1f), Lang("close"), 12, "0.93 0.95", "0.99 0.98", $"npcgui close");
            UI.Label(ref container, NPCGUI, UI.Color("#ffffff", 1f), Lang("npcgui") + ": " + npcname, 24, "0.2 0.92", "0.7 1");

            int col = 0;
            int row = 0;

            if(npc == 0)
            {
                UI.Label(ref container, NPCGUI, UI.Color("#ffffff", 1f), Lang("mustselect"), 24, "0.2 0.47", "0.7 0.53");
            }
            else
            {
                Dictionary<string, string> npcinfo = new Dictionary<string, string>
                {
                    { "displayName", (string) HumanNPC?.Call("GetHumanNPCInfo", npc, "displayName") },
                    { "kit", (string) HumanNPC?.Call("GetHumanNPCInfo", npc, "spawnkit") },
                    { "invulnerable", (string) HumanNPC?.Call("GetHumanNPCInfo", npc, "invulnerable") },
                    { "lootable", (string) HumanNPC?.Call("GetHumanNPCInfo", npc, "lootable") },
                    { "hostile", (string) HumanNPC?.Call("GetHumanNPCInfo", npc, "hostile") },
                    { "ahostile", (string) HumanNPC?.Call("GetHumanNPCInfo", npc, "ahostile") },
                    { "defend", (string) HumanNPC?.Call("GetHumanNPCInfo", npc, "defend") },
                    { "evade", (string) HumanNPC?.Call("GetHumanNPCInfo", npc, "evade") },
                    { "follow", (string) HumanNPC?.Call("GetHumanNPCInfo", npc, "follow") },
                    { "followtime", (string) HumanNPC?.Call("GetHumanNPCInfo", npc, "followtime") },
                    { "allowsit", (string) HumanNPC?.Call("GetHumanNPCInfo", npc, "allowsit") },
                    { "allowride", (string) HumanNPC?.Call("GetHumanNPCInfo", npc, "allowride") },
                    { "needsAmmo", (string) HumanNPC?.Call("GetHumanNPCInfo", npc, "needsAmmo") },
                    { "health", (string) HumanNPC?.Call("GetHumanNPCInfo", npc, "health") },
                    { "attackDistance", (string) HumanNPC?.Call("GetHumanNPCInfo", npc, "attackDistance") },
                    { "damageAmount", (string) HumanNPC?.Call("GetHumanNPCInfo", npc, "damageAmount") },
                    { "damageInterval", (string) HumanNPC?.Call("GetHumanNPCInfo", npc, "damageInterval") },
                    { "maxDistance", (string) HumanNPC?.Call("GetHumanNPCInfo", npc, "maxDistance") },
                    { "damageDistance", (string) HumanNPC?.Call("GetHumanNPCInfo", npc, "damageDistance") },
                    { "collisionRadius", (string) HumanNPC?.Call("GetHumanNPCInfo", npc, "collisionRadius") },
                    { "respawnSeconds", (string) HumanNPC?.Call("GetHumanNPCInfo", npc, "respawnSeconds") },
                    { "speed", (string) HumanNPC?.Call("GetHumanNPCInfo", npc, "speed") },
                    { "stopandtalk", (string) HumanNPC?.Call("GetHumanNPCInfo", npc, "stopandtalk") },
                    { "stopandtalkSeconds", (string) HumanNPC?.Call("GetHumanNPCInfo", npc, "stopandtalkSeconds") },
                    { "hitchance", (string) HumanNPC?.Call("GetHumanNPCInfo", npc, "hitchance") },
                    { "reloadDuration", (string) HumanNPC?.Call("GetHumanNPCInfo", npc, "reloadDuration") },
                    { "band", (string) HumanNPC?.Call("GetHumanNPCInfo", npc, "band") },
                    { "hostileTowardsArmed", (string) HumanNPC?.Call("GetHumanNPCInfo", npc, "hostileTowardsArmed") },
                    { "hostileTowardsArmedHard", (string) HumanNPC?.Call("GetHumanNPCInfo", npc, "hostileTowardsArmedHard") },
                    { "raiseAlarm", (string) HumanNPC?.Call("GetHumanNPCInfo", npc, "raiseAlarm") },
                    { "spawnInfo", (string) HumanNPC?.Call("GetHumanNPCInfo", npc, "spawnInfo") },
                    { "hello", (string) HumanNPC?.Call("GetHumanNPCInfo", npc, "hello") },
                    { "bye", (string) HumanNPC?.Call("GetHumanNPCInfo", npc, "bye") },
                    { "hurt", (string) HumanNPC?.Call("GetHumanNPCInfo", npc, "hurt") },
                    { "use", (string) HumanNPC?.Call("GetHumanNPCInfo", npc, "use") },
                    { "kill", (string) HumanNPC?.Call("GetHumanNPCInfo", npc, "kill") }
                };
                Dictionary<string, bool> isBool = new Dictionary<string, bool>
                {
                    { "enable", true },
                    { "invulnerable", true },
                    { "lootable", true },
                    { "hostile", true },
                    { "ahostile", true },
                    { "defend", true },
                    { "evade", true },
                    { "follow", true },
                    { "allowsit", true },
                    { "allowride", true },
                    { "needsAmmo", true },
                    { "stopandtalk", true },
                    { "hostileTowardsArmed", true },
                    { "hostileTowardsArmedHard", true },
                    { "raiseAlarm", true }
                };
                Dictionary<string, bool> isLarge = new Dictionary<string, bool>
                {
                    { "hello", true },
                    { "bye", true },
                    { "hurt", true },
                    { "use", true },
                    { "kill", true }
                };

                foreach(KeyValuePair<string,string> info in npcinfo)
                {
                    if(row > 11)
                    {
                        row = 0;
                        col++;
                        col++;
                    }
                    float[] posl = GetButtonPositionP(row, col);
                    float[] posb = GetButtonPositionP(row, col + 1);

                    if(!isLarge.ContainsKey(info.Key))
                    {
                        UI.Label(ref container, NPCGUI, UI.Color("#ffffff", 1f), info.Key, 12, $"{posl[0]} {posl[1]}", $"{posl[0] + ((posl[2] - posl[0]) / 2)} {posl[3]}");
                    }
                    if(info.Key == "kit")
                    {
                        if(plugins.Exists("Kits"))
                        {
                            string kitname = info.Value != null ? info.Value : Lang("none");
                            UI.Button(ref container, NPCGUI, UI.Color("#d85540", 1f), kitname, 12, $"{posb[0]} {posb[1]}", $"{posb[0] + ((posb[2] - posb[0]) / 2)} {posb[3]}", $"npcgui npcselkit {npc.ToString()} {kitname}");
                        }
                        else
                        {
                            UI.Label(ref container, NPCGUI, UI.Color("#ffffff", 1f), Lang("none"), 12, $"{posb[0]} {posb[1]}", $"{posb[0] + ((posb[2] - posb[0]) / 2)} {posb[3]}");
                        }
                    }
                    else if(info.Key == "spawnInfo")
                    {
                        UI.Label(ref container, NPCGUI, UI.Color("#535353", 1f), info.Value, 12, $"{posb[0]} {posb[1]}", $"{posb[0] + ((posb[2] - posb[0]) / 2)} {posb[3]}");
                        UI.Input(ref container, NPCGUI, UI.Color("#ffffff", 1f), info.Value, 12, $"{posb[0]} {posb[1]}", $"{posb[0] + ((posb[2] - posb[0]) / 2)} {posb[3]}", $"npcgui spawn {npc.ToString()} {info.Key} ");
                        posb = GetButtonPositionP(row, col + 2);
                        UI.Button(ref container, NPCGUI, UI.Color("#d85540", 1f), Lang("spawnhere"), 12, $"{posb[0]} {posb[1]}", $"{posb[0] + ((posb[2] - posb[0]) / 2)} {posb[3]}", $"npcgui spawnhere {npc.ToString()} ");
                    }
                    else if(isLarge.ContainsKey(info.Key))
                    {
//                        string oldval = info.Value != null ? info.Value : Lang("unset");
//                        UI.Label(ref container, NPCGUI, UI.Color("#535353", 1f), oldval, 12, $"{posb[0]} {posb[1]}", $"{posb[0] + ((posb[2] - posb[0]))} {posb[3]}");
//                        UI.Input(ref container, NPCGUI, UI.Color("#ffffff", 1f), oldval, 12, $"{posb[0]} {posb[1]}", $"{posb[0] + ((posb[2] - posb[0]))} {posb[3]}", $"npcgui npcset {npc.ToString()} {info.Key} {oldval} ");
                    }
                    else if(isBool.ContainsKey(info.Key))
                    {
                        UI.Button(ref container, NPCGUI, UI.Color("#222255", 1f), info.Value, 12, $"{posb[0]} {posb[1]}", $"{posb[0] + ((posb[2] - posb[0]) / 2)} {posb[3]}", $"npcgui npctoggle {npc.ToString()} {info.Key} {info.Value}");
                    }
                    else
                    {
                        string oldval = info.Value != null ? info.Value : Lang("unset");
                        UI.Label(ref container, NPCGUI, UI.Color("#535353", 1f), oldval, 12, $"{posb[0]} {posb[1]}", $"{posb[0] + ((posb[2] - posb[0]) / 2)} {posb[3]}");
                        UI.Input(ref container, NPCGUI, UI.Color("#ffffff", 1f), oldval, 12, $"{posb[0]} {posb[1]}", $"{posb[0] + ((posb[2] - posb[0]) / 2)} {posb[3]}", $"npcgui npcset {npc.ToString()} {info.Key} ");
                    }
                    row++;
                }
                UI.Label(ref container, NPCGUI, UI.Color("#ffffff", 1f), Lang("guihelp1"), 12, "0.02 0.08", "0.9 0.11");
                UI.Label(ref container, NPCGUI, UI.Color("#ffffff", 1f), Lang("guihelp2"), 12, "0.02 0.04", "0.9 0.07");
                UI.Label(ref container, NPCGUI, UI.Color("#ffffff", 1f), Lang("guihelp3"), 12, "0.02 0", "0.9 0.03");
            }

            CuiHelper.AddUi(player, container);
        }

        void NPCMessageGUI(BasePlayer player, ulong npc, string field, string message)
        {
        }

        void NPCSelectGUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, NPCGUS);

            string description = Lang("npcguisel");
            CuiElementContainer container = UI.Container(NPCGUS, UI.Color("242424", 1f), "0.1 0.1", "0.9 0.9", true, "Overlay");
            UI.Label(ref container, NPCGUS, UI.Color("#ffffff", 1f), description, 18, "0.23 0.92", "0.7 1");
            UI.Label(ref container, NPCGUS, UI.Color("#22cc44", 1f), Lang("musician"), 12, "0.72 0.92", "0.77 1");
            UI.Label(ref container, NPCGUS, UI.Color("#2244cc", 1f), Lang("standard"), 12, "0.79 0.92", "0.86 1");
            UI.Button(ref container, NPCGUS, UI.Color("#d85540", 1f), Lang("close"), 12, "0.92 0.93", "0.985 0.98", $"npcgui selclose");
            int col = 0;
            int row = 0;
            bool found = false;

            List<ulong> npcs = (List<ulong>)HumanNPC?.Call("HumanNPCs");
            foreach(ulong npc in npcs)
            {
                found = true;
                if(row > 10)
                {
                    row = 0;
                    col++;
                }
                var hBand = (string)HumanNPC?.Call("GetHumanNPCInfo", npc, "band");
                if(hBand == "99") continue;
                string color = "#2244cc";
                if(hBand != "0") color = "#22cc44";

                var hName = (string)HumanNPC?.Call("HumanNPCname", npc);
                float[] posb = GetButtonPositionP(row, col);
                UI.Button(ref container, NPCGUS, UI.Color(color, 1f), hName, 12, $"{posb[0]} {posb[1]}", $"{posb[0] + ((posb[2] - posb[0]) / 2)} {posb[3]}", $"npcgui npc {npc.ToString()}");
                row++;
            }
            float[] posn = GetButtonPositionP(row, col);
            UI.Button(ref container, NPCGUS, UI.Color("#cc3333", 1f), Lang("new"), 12, $"{posn[0]} {posn[1]}", $"{posn[0] + ((posn[2] - posn[0]) / 2)} {posn[3]}", $"npcgui new");

            CuiHelper.AddUi(player, container);
        }

        void NPCKitGUI(BasePlayer player, ulong npc, string kit)
        {
            CuiHelper.DestroyUi(player, NPCGUK);

            string description = Lang("npcguikit");
            CuiElementContainer container = UI.Container(NPCGUK, UI.Color("242424", 1f), "0.1 0.1", "0.9 0.9", true, "Overlay");
            UI.Label(ref container, NPCGUK, UI.Color("#ffffff", 1f), description, 18, "0.23 0.92", "0.7 1");
            UI.Button(ref container, NPCGUK, UI.Color("#d85540", 1f), Lang("close"), 12, "0.92 0.93", "0.985 0.98", $"npcgui selkitclose");

            int col = 0;
            int row = 0;

            var kits = Interface.Oxide.DataFileSystem.GetFile("Kits");
            kits.Settings.NullValueHandling = NullValueHandling.Ignore;
            StoredData storedData = kits.ReadObject<StoredData>();
            foreach(var kitinfo in storedData.Kits)
            {
                if(row > 10)
                {
                    row = 0;
                    col++;
                }
                float[] posb = GetButtonPositionP(row, col);

                if(kit == null) kit = Lang("none");
                if(kitinfo.Key == kit)
                {
                       UI.Button(ref container, NPCGUK, UI.Color("#d85540", 1f), kitinfo.Key, 12, $"{posb[0]} {posb[1]}", $"{posb[0] + ((posb[2] - posb[0]) / 2)} {posb[3]}", $"npcgui kitsel {npc.ToString()} {kitinfo.Key}");
                }
                else
                {
                    UI.Button(ref container, NPCGUK, UI.Color("#424242", 1f), kitinfo.Key, 12, $"{posb[0]} {posb[1]}", $"{posb[0] + ((posb[2] - posb[0]) / 2)} {posb[3]}", $"npcgui kitsel {npc.ToString()} {kitinfo.Key}");
                }
                row++;
            }

            CuiHelper.AddUi(player, container);
        }

        private int RowNumber(int max, int count) => Mathf.FloorToInt(count / max);
        private float[] GetButtonPosition(int rowNumber, int columnNumber)
        {
            float offsetX = 0.05f + (0.096f * columnNumber);
            float offsetY = (0.80f - (rowNumber * 0.064f));

            return new float[] { offsetX, offsetY, offsetX + 0.196f, offsetY + 0.03f };
        }
        private float[] GetButtonPositionP(int rowNumber, int columnNumber)
        {
            float offsetX = 0.05f + (0.126f * columnNumber);
            float offsetY = (0.87f - (rowNumber * 0.064f));

            return new float[] { offsetX, offsetY, offsetX + 0.226f, offsetY + 0.03f };
        }

        private bool TryGetPlayerView(BasePlayer player, out Quaternion viewAngle)
        {
            viewAngle = new Quaternion(0f, 0f, 0f, 0f);
            if(player.serverInput?.current == null) return false;
            viewAngle = Quaternion.Euler(player.serverInput.current.aimAngles);
            return true;
        }
        #endregion

        #region classes
        class StoredData
        {
            public Dictionary<string, Kit> Kits = new Dictionary<string, Kit>();
        }
        class Kit
        {
            public string name;
            public string description;
            public int max;
            public double cooldown;
            public int authlevel;
            public bool hide;
            public bool npconly;
            public string permission;
            public string image;
            public string building;
            public List<KitItem> items = new List<KitItem>();
        }
        class KitItem
        {
            public int itemid;
            public string container;
            public int amount;
            public ulong skinid;
            public bool weapon;
            public int blueprintTarget;
            public List<int> mods = new List<int>();
        }

        public static class UI
        {
            public static CuiElementContainer Container(string panel, string color, string min, string max, bool useCursor = false, string parent = "Overlay")
            {
                CuiElementContainer container = new CuiElementContainer()
                {
                    {
                        new CuiPanel
                        {
                            Image = { Color = color },
                            RectTransform = {AnchorMin = min, AnchorMax = max},
                            CursorEnabled = useCursor
                        },
                        new CuiElement().Parent = parent,
                        panel
                    }
                };
                return container;
            }
            public static void Panel(ref CuiElementContainer container, string panel, string color, string min, string max, bool cursor = false)
            {
                container.Add(new CuiPanel
                {
                    Image = { Color = color },
                    RectTransform = { AnchorMin = min, AnchorMax = max },
                    CursorEnabled = cursor
                },
                panel);
            }
            public static void Label(ref CuiElementContainer container, string panel, string color, string text, int size, string min, string max, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiLabel
                {
                    Text = { Color = color, FontSize = size, Align = align, Text = text },
                    RectTransform = { AnchorMin = min, AnchorMax = max }
                },
                panel);

            }
            public static void Button(ref CuiElementContainer container, string panel, string color, string text, int size, string min, string max, string command, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiButton
                {
                    Button = { Color = color, Command = command, FadeIn = 0f },
                    RectTransform = { AnchorMin = min, AnchorMax = max },
                    Text = { Text = text, FontSize = size, Align = align }
                },
                panel);
            }
            public static void Input(ref CuiElementContainer container, string panel, string color, string text, int size, string min, string max, string command, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = panel,
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            Align = align,
                            CharsLimit = 30,
                            Color = color,
                            Command = command + text,
                            FontSize = size,
                            IsPassword = false,
                            Text = text
                        },
                        new CuiRectTransformComponent { AnchorMin = min, AnchorMax = max },
                        new CuiNeedsCursorComponent()
                    }
                });
            }
            public static string Color(string hexColor, float alpha)
            {
                if(hexColor.StartsWith("#"))
                {
                    hexColor = hexColor.Substring(1);
                }
                int red = int.Parse(hexColor.Substring(0, 2), NumberStyles.AllowHexSpecifier);
                int green = int.Parse(hexColor.Substring(2, 2), NumberStyles.AllowHexSpecifier);
                int blue = int.Parse(hexColor.Substring(4, 2), NumberStyles.AllowHexSpecifier);
                return $"{(double)red / 255} {(double)green / 255} {(double)blue / 255} {alpha}";
            }
        }
        #endregion
    }
}
