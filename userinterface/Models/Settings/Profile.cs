using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RawAccel.Models.Settings
{
    public class Profile
    {
        #region Properties

        [JsonPropertyOrder(0)]
        public string Name { get; set; } = "Default";

        [JsonPropertyOrder(1)]
        public GeneralParams? Params { get; set; }

        [JsonPropertyOrder(2)]
        public DirectionalParams? ParamsX { get; set; }

        [JsonPropertyOrder(3)]
        public DirectionalParams? ParamsY { get; set; }

        #endregion Properties

        #region Methods

        public void Save()
        {
            string path = Path.Combine(Constants.ProfileFolderName, Name);
            File.WriteAllText(path, JsonSerializer.Serialize(this));
        }

        public static Profile LoadOrThrow(string name)
        {
            string path = Path.Combine(Constants.ProfileFolderName, name);

            Profile? profile;

            try
            {
                profile = JsonSerializer.Deserialize<Profile>(File.ReadAllText(path));
                if (profile == null)
                {
                    // Deserialize throws if null but return type is nullable?
                    throw new Exception("This is not possible!");
                }
            }
            catch (JsonException)
            {
                throw; // Maybe tell the user their profile is messed up
            }
            catch (Exception)
            {
                throw;
            }

            return profile;
        }

        #endregion Methods
    }
}
