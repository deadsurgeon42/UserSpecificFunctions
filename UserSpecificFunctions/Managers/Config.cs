using System.IO;
using Newtonsoft.Json;

namespace UserSpecificFunctions
{
    public class Config
    {
		/// <summary>
		/// The maximum prefix length.
		/// </summary>
		public int PrefixLength = 20;

		/// <summary>
		/// The maximum suffix length.
		/// </summary>
		public int SuffixLength = 20;

		/// <summary>
		/// The array of words players are not allowed to use in their prefixes and suffixes.
		/// </summary>
		public string[] UnAllowedWords = new string[] { "Ass", "Asshole", "Fuck", "Fucktard", "Shit", "Shithead", "Fucker", "Motherfucker" };

		/// <summary>
		/// Attempts to read the configuration file from the given path.
		/// If the file does not exist a new one will be generated.
		/// </summary>
		/// <param name="path">The path to read from.</param>
		/// <returns>A <see cref="Config"/> object.</returns>
		public static Config TryRead(string path)
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
