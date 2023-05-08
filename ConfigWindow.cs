using System.Globalization;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace GlamourerHelper; 

public class ConfigWindow : Window {
    public ConfigWindow() : base("Glamourer Helper Config###glamourerHelperConfig", ImGuiWindowFlags.AlwaysAutoResize) { }

    private ulong cid;
    
    public override bool DrawConditions() {
        if (ClientState.LocalContentId == 0) {
            IsOpen = false;
            return false;
        }
        
        if (cid != ClientState.LocalContentId && ClientState.LocalPlayer != null) {
            WindowName = $"Glamourer Helper Config   -   {ClientState.LocalPlayer.Name.TextValue}###glamourerHelperConfig";
            cid = ClientState.LocalContentId;
        }
        
        return base.DrawConditions();
    }

    public override void Draw() {
        if (Config == null) {
            ImGui.Text("No config for this character.");
            if (ImGui.Button("Create Config", new Vector2(ImGui.GetContentRegionAvail().X, 26 * ImGuiHelpers.GlobalScale))) {
                CreateConfig();
            }
            return;
        }
        
        DrawCharacterConfig(Config);
    }

    private void DrawCharacterConfig(CharacterConfig config) {
        ImGui.TextWrapped("Designs to apply when logging in or switching jobs. The Default design will be used when no matching job is applied.");
        ImGui.Separator();

        var currentClassJob = 0U;
        var currentClassJobFound = false;
        unsafe {
            currentClassJob = UIState.Instance()->PlayerState.CurrentClassJobId;
        }

        var ti = CultureInfo.InvariantCulture.TextInfo;
        foreach (var (cj, cDesign) in config.ClassJob.ToArray()) {
            var classJob = DataManager.GetExcelSheet<ClassJob>()!.GetRow(cj);
            if (cj == 0 || classJob == null) continue;
            if (cj == currentClassJob) currentClassJobFound = true;

            var design = cDesign;
            if (SavedDesignHelper.GlamourerDesignPicker($"{ti.ToTitleCase(classJob.Name.ToDalamudString().TextValue)}##classJob#{cj}", ref design)) {
                if (string.IsNullOrEmpty(design)) {
                    config.ClassJob.Remove(cj);
                } else {
                    config.ClassJob[cj] = design;
                }
                
                config.Save();
            }
        }

        if (!currentClassJobFound && currentClassJob != 0) {
            var classJob = DataManager.GetExcelSheet<ClassJob>()!.GetRow(currentClassJob);
            if (classJob != null) {
                var selected = string.Empty;
                if (SavedDesignHelper.GlamourerDesignPicker($"{ti.ToTitleCase(classJob.Name.ToDalamudString().TextValue)}##classJob#{currentClassJob}", ref selected)) {
                    if (!string.IsNullOrEmpty(selected)) {
                        config.ClassJob.Add(currentClassJob, selected);
                        config.Save();
                    }
                }
            }
            
        }

        ImGui.Separator();
        if (SavedDesignHelper.GlamourerDesignPicker("Default Design", ref config.DefaultDesign)) config.Save();
        ImGui.SameLine();
        if (string.IsNullOrEmpty(config.DefaultDesign)) ImGui.BeginDisabled();
        if (ImGui.SmallButton("Test")) {
            var customization = SavedDesignHelper.GetSavedDesign(config.DefaultDesign);
            PluginLog.Debug(customization);
            if (!string.IsNullOrEmpty(customization)) {
                ApplyCustomization(customization);
            }
        }
        ImGui.EndDisabled();
        
    }
}
