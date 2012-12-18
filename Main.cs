using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Data;
using System.IO;
using TShockAPI;
using TShockAPI.DB;
using Terraria;
using Hooks;
using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Vault;

namespace PaidCommands
{
    [APIVersion(1, 12)]
    public class PaidCommands : TerrariaPlugin
    {
        public IDbConnection Database;
        public String SavePath = Path.Combine(TShock.SavePath, "PaidCommands/");
        internal static Config config;
        public override string Name
        {
            get { return "PaidCommands"; }
        }
        public override string Author
        {
            get { return "by InanZen"; }
        }
        public override string Description
        {
            get { return ""; }
        }
        public override Version Version
        {
            get { return new Version("0.1"); }
        }
        public PaidCommands(Main game)
            : base(game)
        {
            Order = -1;
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                GameHooks.Initialize -= OnInitialize;
                ServerHooks.Chat -= OnChat;
                ServerHooks.Command -= OnCommand;

                Database.Dispose();
            }
        }
        public override void Initialize()
        {
            ServerHooks.Chat += OnChat;
            GameHooks.Initialize += OnInitialize;
            ServerHooks.Command += OnCommand;
        }
        void OnInitialize()
        {
            config = new Config();
            if (!Directory.Exists(SavePath))
                Directory.CreateDirectory(SavePath);
            ReadConfig();
        
        }
        void OnCommand(string cmd, HandledEventArgs args)
        {
        }
        void OnChat(messageBuffer buf, int who, string text, HandledEventArgs args)
        {
            try
            {
                if (text[0] == '/')
                {
                    var split = text.Split(' ');
                    string cmd = split[0].TrimStart('/').ToLower();
                   
                    var tscmd = Commands.ChatCommands.Find(c => c.HasAlias(cmd));
                    if (tscmd != null)
                    {
                        for (int i = 0; i < config.Commands.Length; i++)
                        {
                            if (config.Commands[i].Command.ToLower() == cmd)
                            {
                                if (config.Commands[i].Permission != "" && !TShock.Players[who].Group.HasPermission(config.Commands[i].Permission))
                                    break;
                                if (!TShock.Players[who].Group.HasPermission(tscmd.Permission) && Vault.Vault.ModifyBalance(TShock.Players[who].Name, -config.Commands[i].Cost))
                                {
                                    TShock.Players[who].SendMessage(String.Format("You have been charged {0} to use /{1}", config.Commands[i].Cost, cmd), Color.DarkOrange);
                                    TShock.Players[who].Group.AddPermission(tscmd.Permission);
                                    TShockAPI.Commands.HandleCommand(TShock.Players[who], text);
                                    TShock.Players[who].Group.RemovePermission(tscmd.Permission);
                                    args.Handled = true;
                                }
                                break;
                            }
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                Log.ConsoleError(ex.ToString());
            }
        }


        // ---------------------------------- CONFIG ---------------------------
        internal struct PCMD
        {
            public string Command;
            public string Permission;
            public int Cost;
        }
        internal class Config
        {            
            public PCMD[] Commands;
        }
        private void CreateConfig()
        {
            string filepath = Path.Combine(SavePath, "PaidCommands.json");
            try
            {
                using (var stream = new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.Write))
                {
                    using (var sr = new StreamWriter(stream))
                    {
                        config = new Config();
                        config.Commands = new PCMD[1];
                        config.Commands[0] = new PCMD() { Command = "tp", Permission = "paidcmd.tp", Cost = 100 };
                        var configString = JsonConvert.SerializeObject(config, Formatting.Indented);
                        sr.Write(configString);
                    }
                    stream.Close();
                }
            }
            catch (Exception ex)
            {
                Log.ConsoleError(ex.Message);
                config = new Config();
            }
        }
        private bool ReadConfig()
        {
            string filepath = Path.Combine(SavePath, "PaidCommands.json");
            try
            {
                if (File.Exists(filepath))
                {
                    using (var stream = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        using (var sr = new StreamReader(stream))
                        {
                            var configString = sr.ReadToEnd();
                            config = JsonConvert.DeserializeObject<Config>(configString);
                        }
                        stream.Close();
                    }
                    return true;
                }
                else
                {
                    Log.ConsoleError("PaidCommands file not found. Creating new one");
                    CreateConfig();
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.ConsoleError(ex.Message);
            }
            return false;
        }


    }


}
