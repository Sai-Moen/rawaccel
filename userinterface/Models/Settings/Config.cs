using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RawAccel.Models.Settings
{
    public class Config
    {
        #region Properties

        [JsonPropertyOrder(0)]
        public int DPI { get; set; }

        [JsonPropertyOrder(1)]
        public int PollRate { get; set; }

        [JsonPropertyOrder(2)]
        public bool ShowLastMouseMove { get; set; }

        [JsonPropertyOrder(3)]
        public bool ShowVelocityAndGain { get; set; }

        [JsonPropertyOrder(4)]
        public bool AutoWriteToDriverOnStartup { get; set; }

        [JsonPropertyOrder(5)]
        public string CurrentColorScheme { get; set; } = "Light Theme"; // Fix this when Themes are implemented

        #endregion Properties

        #region Methods

        public void Save()
        {
            File.WriteAllText(Constants.ConfigFileName, JsonSerializer.Serialize(this));
        }

        public static Config LoadOrThrow()
        {
            Config? config;

            try
            {
                config = JsonSerializer.Deserialize<Config>(File.ReadAllText(Constants.ConfigFileName));
                if (config == null)
                {
                    // Deserialize throws if null but return type is nullable?
                    throw new Exception("This is not possible!");
                }
            }
            catch (JsonException)
            {
                throw; // Maybe tell the user their config file is messed up
            }
            catch (Exception)
            {
                throw;
            }

            return config;
        }

        #endregion Methods
    }
}
