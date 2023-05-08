using System;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace GlamourerHelper;

public sealed class Plugin : IDalamudPlugin {
    public string Name => "Glamourer Helper";
    
    private static ICallGateSubscriber<string, GameObject?, object>? applyAll;
    private static ICallGateSubscriber<GameObject?, string>? getAll;

    private string tempCustomization = string.Empty;
    
    [PluginService] public static DalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] public static Condition Condition { get; private set; } = null!;
    [PluginService] public static ClientState ClientState { get; private set; } = null!;
    [PluginService] public static Framework Framework { get; private set; } = null!;
    [PluginService] public static DataManager DataManager { get; private set; } = null!;

    private readonly WindowSystem windowSystem = new("GlamourerHelper");
    private readonly ConfigWindow configWindow = new() {
#if DEBUG
        IsOpen = true
#endif
    };
    
    public Plugin() {
        windowSystem.AddWindow(configWindow);
        Condition.ConditionChange += OnConditionChange;
        ClientState.Login += OnLogin;
        ClientState.Logout += OnLogout;
        PluginInterface.UiBuilder.Draw += windowSystem.Draw;
        Framework.Update += FrameworkOnUpdate;

        lastClassJob = ClientState.LocalPlayer?.ClassJob?.Id ?? 0U;
        
        if (ClientState.LocalContentId != 0) {
            PluginInterface.UiBuilder.OpenConfigUi += configWindow.Toggle;
        }
    }

    private unsafe void ClassJobUpdate() {
        var cj = 0U;
        try {
            cj = UIState.Instance()->PlayerState.CurrentClassJobId;
            if (lastClassJob == cj) return;
            PluginLog.Debug($"ClassJob Changed: {cj}");
            
            var config = Config;
            if (config == null) return;

            var usingDefaultDesign = false;
            
            if (!(config.ClassJob.TryGetValue(cj, out var design) && !string.IsNullOrEmpty(design))) {
                design = config.DefaultDesign;
                usingDefaultDesign = true;
            }
            
            if (!string.IsNullOrEmpty(design)) {
                var customization = SavedDesignHelper.GetSavedDesign(design);
                if (!string.IsNullOrEmpty(customization)) {
                    ApplyCustomization(customization);
                } else {
                    if (usingDefaultDesign) {
                        config.DefaultDesign = string.Empty;
                    } else {
                        config.ClassJob.Remove(cj);
                    }
                }
            }
            
        } finally {
            lastClassJob = cj;
        }
    }
    
    private void FrameworkOnUpdate(Framework framework) {
        if (ClientState.LocalContentId == 0) return;
        ClassJobUpdate();
    }

    private static bool SetupGlamourerIpc() {
        if (applyAll != null && getAll != null) return true;
        applyAll = PluginInterface.GetIpcSubscriber<string, GameObject?, object>("Glamourer.ApplyAllToCharacter");
        getAll = PluginInterface.GetIpcSubscriber<GameObject?, string>("Glamourer.GetAllCustomizationFromCharacter");
        return applyAll != null && getAll != null;
    }
    

    private uint lastClassJob = 0;

    private void OnLogin(object? sender, EventArgs e) {
        lastClassJob = 0;
        PluginInterface.UiBuilder.OpenConfigUi -= configWindow.Toggle;
        PluginInterface.UiBuilder.OpenConfigUi += configWindow.Toggle;
    }
    
    private void OnLogout(object? sender, EventArgs e) {
        lastClassJob = 0;
        PluginInterface.UiBuilder.OpenConfigUi -= configWindow.Toggle;
    }
    
    private void OnConditionChange(ConditionFlag flag, bool value) {
        if (flag != ConditionFlag.BetweenAreas) return;
        if (!SetupGlamourerIpc()) return;
        try {
            if (value) 
                tempCustomization = getAll?.InvokeFunc(ClientState.LocalPlayer) ?? string.Empty;
            else
                applyAll?.InvokeAction(tempCustomization, ClientState.LocalPlayer);
        } catch {
            applyAll = null;
            getAll = null;
        }
    }

    public static void ApplyCustomization(string customizationString) {
        if (!SetupGlamourerIpc()) return;
        applyAll?.InvokeAction(customizationString, ClientState.LocalPlayer);
    }

    public void Dispose() {
        Condition.ConditionChange -= OnConditionChange;
        ClientState.Login -= OnLogin;
        ClientState.Logout -= OnLogout;
        Framework.Update -= FrameworkOnUpdate;
        Config?.Save();

    }
}
