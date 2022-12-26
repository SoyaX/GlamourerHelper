using System;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;

namespace GlamourerHelper;

public sealed class Plugin : IDalamudPlugin {
    public string Name => "Glamourer Helper";
    
    private ICallGateSubscriber<string, GameObject?, object>? applyAll;
    private ICallGateSubscriber<GameObject?, string>? getAll;

    private string tempCustomization = string.Empty;
    
    [PluginService] public static DalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] public static Condition Condition { get; private set; } = null!;
    [PluginService] public static ClientState ClientState { get; private set; } = null!;
    
    public Plugin() {
        Condition.ConditionChange += OnConditionChange;
    }

    private bool SetupGlamourerIpc() {
        if (applyAll != null && getAll != null) return true;
        applyAll = PluginInterface.GetIpcSubscriber<string, GameObject?, object>("Glamourer.ApplyAllToCharacter");
        getAll = PluginInterface.GetIpcSubscriber<GameObject?, string>("Glamourer.GetAllCustomizationFromCharacter");
        return applyAll != null && getAll != null;
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

    public void Dispose() {
        Condition.ConditionChange -= OnConditionChange;
    }
}
