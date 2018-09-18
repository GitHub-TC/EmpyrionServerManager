﻿using EmpyrionModApi;
using EmpyrionModApi.ExtensionMethods;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;


namespace VotingRewardMod
{
    [System.ComponentModel.Composition.Export(typeof(IGameMod))]
    public class VotingRewardMod : IGameMod
    {
        static readonly string k_versionString = EmpyrionModApi.Helpers.GetVersionString(typeof(VotingRewardMod));

        private string _voteLogPath;
        private int playerVotes = 0;

        private TraceSource _traceSource = new TraceSource("VotingRewardMod");

        public void Start(IGameServerConnection gameServerConnection)
        {
            _traceSource.TraceInformation("Starting up...");
            var configFilePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + "VotingRewardMod_Settings.yaml";
            _traceSource.TraceEvent(TraceEventType.Verbose, 3, "Loaded configuration.");

            _gameServerConnection = gameServerConnection;
            _config = BaseConfiguration.GetConfiguration<Configuration>(configFilePath);

            _gameServerConnection.AddVersionString(k_versionString);
            _gameServerConnection.Event_ChatMessage += OnEvent_ChatMessage;

            _voteLogPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\VoteLog";
        }

        public void Stop()
        {
            _traceSource.TraceInformation("Stopping...");
            _traceSource.Flush();
            _traceSource.Close();
        }

        private async void OnEvent_ChatMessage(ChatType chatType, string msg, Player player)
        {
            try
            {
                switch (msg)
                {
                    case "/votereward":
                        {
                            _traceSource.TraceInformation("{0} is trying to claim a voting reward.", player);
                            if (await DoesPlayerHaveReward(player))
                            {
                                _traceSource.TraceInformation("{0} has a voting reward to claim; show reward to player.", player);
                                //--- find reward level
                                int rewardLevel = 0;
                                ItemStacks rewardList = new ItemStacks();
                                for( var idx = 0; idx <_config.VotingRewards.Count; idx++)
                                {
                                    var reward = _config.VotingRewards[idx];
                                    if( playerVotes >= reward.MinVotesNeeded )
                                    {
                                        if (!_config.Cumulative) rewardList.Clear();
                                        rewardList.AddStacks(reward.Rewards);
                                    }
                                }
                                var rewardItems = rewardList.ToEleonArray();
                                var itemExchangeInfo = await player.DoItemExchange("Voting Reward", "Remember to vote everyday. Enjoy!", "Close", rewardItems);
                                _traceSource.TraceInformation("{0} has closed the voting reward UI.", player);
                                if (!rewardItems.AreTheSame(itemExchangeInfo.items))
                                {
                                    _traceSource.TraceInformation("{0} took at least some of the voting reward.", player);
                                    await MarkRewardClaimed(player);
                                    _traceSource.TraceInformation("{0} claimed a voting reward.", player);
                                }
                                else
                                {
                                    _traceSource.TraceInformation("{0} didn't claim any reward items.", player);
                                }
                            }
                            else
                            {
                                await MarkRewardClaimed(player);
                                _traceSource.TraceInformation("No unclaimed voting reward found for {0}.", player);
                                await player.SendAlarmMessage("No unclaimed voting reward found.");
                            }
                        }
                        break;
                }
            }
            catch(Exception ex)
            {
                _traceSource.TraceEvent(TraceEventType.Error, 1, ex.ToString());
            }
        }

        private async Task<bool> DoesPlayerHaveReward(Player player)
        {
            var uri = $"https://empyrion-servers.com/api/?object=votes&element=claim&key={_config.VotingApiServerKey}&steamid={player.SteamId}";

            var response = await CallRestMethod("GET", uri);

            string voteLogFile = System.IO.Path.Combine(_voteLogPath, $"{player.SteamId}.txt");
            if (System.IO.File.Exists(voteLogFile) )
            {
                using (var reader = System.IO.File.OpenText(voteLogFile))
                {
                    var voteCount = "";
                    if ((voteCount = reader.ReadLine()) != null)
                    {
                        playerVotes = Convert.ToInt16(voteCount);
                    }
                }
            }

            return (response == "1");
        }

        private async Task MarkRewardClaimed(Player player)
        {
            var uri = $"https://empyrion-servers.com/api/?action=post&object=votes&element=claim&key={_config.VotingApiServerKey}&steamid={player.SteamId}";

            await CallRestMethod("POST", uri);

            string voteLogFile = System.IO.Path.Combine(_voteLogPath, $"{player.SteamId}.txt");
            using (var writer = System.IO.File.CreateText(voteLogFile))
            {
                await writer.WriteLineAsync($"{playerVotes+1}");
            }
        }

        public async static Task<string> CallRestMethod(string method, string url)
        {
            var webrequest = WebRequest.Create(url);
            webrequest.Method = method;
            using (var response = (HttpWebResponse)await Task.Factory.FromAsync<WebResponse>(webrequest.BeginGetResponse, webrequest.EndGetResponse, null))
            {
                var responseStream = new StreamReader(response.GetResponseStream(), System.Text.Encoding.GetEncoding("utf-8"));
                string result = string.Empty;
                result = responseStream.ReadToEnd();

                return result;
            }
        }

        private IGameServerConnection _gameServerConnection;
        private Configuration _config;
    }
}
