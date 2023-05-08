using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using ImGuiNET;
using Newtonsoft.Json;

namespace GlamourerHelper; 

public static class SavedDesignHelper {

    private record GlamourerDesignEntry(string Name, string fullName) {
        public List<GlamourerDesignEntry>? Children = null;
        public string Customization = string.Empty;
    }

    private static List<GlamourerDesignEntry>? glamourerDesigns = null;

    private static void GenerateGlamourerDesignList(bool parseFolders = true) {
        glamourerDesigns = new List<GlamourerDesignEntry>();
        var file = Path.Join(PluginInterface.ConfigDirectory.Parent?.FullName ?? string.Empty, "Glamourer", "Designs.json");
        if (!File.Exists(file)) return;
        var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(file));
        if (dict == null) return;

        foreach (var (k, c) in dict) {
            var owner = glamourerDesigns;
            if (!parseFolders) {
                owner.Add(new GlamourerDesignEntry(k, k) { Customization = c });
                continue;
            }

            var spl = k.Split('/').ToList();

            while (spl.Count > 1) {
                var f = spl[0];
                spl.RemoveAt(0);

                var newOwner = owner.FirstOrDefault(a => a.Name == f);
                if (newOwner == null) {
                    newOwner = new GlamourerDesignEntry(f, k);
                    owner.Add(newOwner);
                }

                newOwner.Children ??= new List<GlamourerDesignEntry>();
                owner = newOwner.Children;
            }
            
            owner.Add(new GlamourerDesignEntry(spl[0], k) { Customization = c });
        }
    }

    private static float minComboWidth = 250f;
    
    public static bool GlamourerDesignPicker(string label, ref string design) {
        var modified = false;

        ImGui.SetNextItemWidth(250 * ImGuiHelpers.GlobalScale);
        modified |= ImGui.InputTextWithHint($"##{label}_input", "No Design Selected", ref design, ushort.MaxValue);
        var s = ImGui.GetItemRectSize();
        ImGui.SameLine();
        ImGui.SetCursorScreenPos(new Vector2(ImGui.GetItemRectMax().X, ImGui.GetItemRectMin().Y));

        var isOpen = ImGui.BeginCombo($"##{label}_combo", design, ImGuiComboFlags.NoPreview | ImGuiComboFlags.PopupAlignLeft);
        if (isOpen) {
            ImGui.Dummy(new Vector2(minComboWidth - ImGui.GetStyle().FramePadding.X * 2, 1));

            if (!string.IsNullOrEmpty(design)) {
                ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudOrange);
                if (ImGui.Selectable("Clear Selection...")) {
                    design = string.Empty;
                    modified = true;
                }
                ImGui.PopStyleColor();
            }
            
            
            if (glamourerDesigns == null || ImGui.IsWindowAppearing()) GenerateGlamourerDesignList();
            
            void RecursiveGlamourDisplay(IEnumerable<GlamourerDesignEntry> l, ref string d) {
                foreach (var k in l.OrderBy(k => k.Children == null)) {

                    if (k.Children != null) {
                        if (ImGui.TreeNode(k.Name)) {
                            RecursiveGlamourDisplay(k.Children, ref d);
                            ImGui.Spacing();
                            ImGui.TreePop();
                        }
                        
                    } else {
                        if (ImGui.Selectable(k.Name)) {
                            d = k.fullName;
                            modified = true;
                        }
                    }
                }
            }

            if ((glamourerDesigns?.Count ?? 0) == 0) {
                ImGui.TextDisabled("No Designs Found");
            }
            
            RecursiveGlamourDisplay(glamourerDesigns ?? new List<GlamourerDesignEntry>(), ref design);
            
            ImGui.EndCombo();
        }
        s.X += ImGui.GetItemRectSize().X;
        minComboWidth = s.X;
        
        ImGui.SameLine();
        ImGui.Text($"{label.Split("##")[0]}");


        return modified;
    }

    public static string GetSavedDesign(string fullName) {
        GenerateGlamourerDesignList(false);
        return glamourerDesigns.FirstOrDefault(k => k.fullName == fullName)?.Customization ?? string.Empty;
    }
    
}
