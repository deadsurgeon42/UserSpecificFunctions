using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data;
using Newtonsoft.Json;

namespace UserSpecificFunctions
{
    public class Config
    {
		public int PrefixLength = 20;
		public int SuffixLength = 20;
		public string[] UnAllowedWords = new string[] { "Ass", "Asshole", "Fuck", "Fucktard", "Shit", "Shithead", "Fucker", "Motherfucker" };

		public static Config Read(string path)
		{
			if (!File.Exists(path))
			{
				Config config = new Config();
				File.WriteAllText(path, JsonConvert.SerializeObject(config, Formatting.Indented));
				return config;
			}

			return JsonConvert.DeserializeObject<Config>(File.ReadAllText(path));
		}
	}
}
