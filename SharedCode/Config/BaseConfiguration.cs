﻿using System;
using System.IO;
using YamlDotNet.Serialization;

namespace EmpyrionModApi
{
    public class BaseConfiguration
    {
        public string GameServerIp { get; set; }
        public int GameServerApiPort { get; set; }

        // Kept in only so older configs won't choke
        public int PlayerUpdateIntervalInSeconds { get; set; }

        public BaseConfiguration()
        {
            GameServerIp = "127.0.0.1";
            GameServerApiPort = 12345;
            PlayerUpdateIntervalInSeconds = 5;
        }

        public static T GetConfiguration<T>(String filePath)
        {
            using (var input = File.OpenText(filePath))
            {
                return (new Deserializer()).Deserialize<T>(input);
            }
        }
    }
}
