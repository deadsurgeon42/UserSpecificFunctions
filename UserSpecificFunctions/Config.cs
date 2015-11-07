using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace UserSpecificFunctions
{
    public class Config
    {
        public int PrefixLength = 20;
        public int SuffixLength = 20;
        public string[] UnAllowedWords = new string[] { "Ass", "Asshole", "Fuck", "Fucktard", "Shit", "Shithead", "Fucker", "Motherfucker" };

        public void Write(string path)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        public Config Read(string path)
        {
            return !File.Exists(path)
                ? new Config()
                : JsonConvert.DeserializeObject<Config>(File.ReadAllText(path));
        }
    }
}
