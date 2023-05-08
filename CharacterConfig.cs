using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace GlamourerHelper;

public class CharacterConfig {
    private static CharacterConfig? loadedConfig = null;

    public static CharacterConfig? Config {
        get {
            if (ClientState.LocalContentId == 0) {
                if (loadedConfig != null) {
                    loadedConfig.Save();
                    loadedConfig = null;
                }
                return null;
            }
            
            if (loadedConfig == null) {
                loadedConfig = LoadConfig(ClientState.LocalContentId) ?? new CharacterConfig() {
                    ContentID = ClientState.LocalContentId
                };
            }

            if (loadedConfig.ContentID != ClientState.LocalContentId) {
                loadedConfig.Save();
                loadedConfig = LoadConfig(ClientState.LocalContentId) ?? new CharacterConfig() {
                    ContentID = ClientState.LocalContentId
                };
            }

            return loadedConfig.IsNew ? null : loadedConfig;
        }
    }
    
    public static void CreateConfig() {
        loadedConfig ??= new CharacterConfig();
        loadedConfig.IsNew = false;
        loadedConfig.Save();
    }

    private static CharacterConfig? LoadConfig(ulong contentId) {
        if (contentId == 0) return null;
        var configFile = Path.Join(PluginInterface.ConfigDirectory.FullName, $"{contentId:X16}.json");
        if (!File.Exists(configFile)) return null;
        var json = File.ReadAllText(configFile);
        var config = JsonConvert.DeserializeObject<CharacterConfig>(json);
        if (config == null) return null;
        config.ContentID = contentId;
        config.IsNew = false;
        
        return config;
    }

    public void Save() {
        if (ContentID == 0 || IsNew) return;
        var configFile = Path.Join(PluginInterface.ConfigDirectory.FullName, $"{ContentID:X16}.json");
        var json = JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings() {
            TypeNameHandling = TypeNameHandling.None
        });
        File.WriteAllText(configFile, json);
    }

    [JsonIgnore] public ulong ContentID { get; private set; } = 0;
    [JsonIgnore] public bool IsNew { get; private set; } = true;

    public bool Enabled = true;
    public string DefaultDesign = string.Empty;
    public Dictionary<uint, string> ClassJob = new();
}
