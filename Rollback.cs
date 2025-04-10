// Copyright (C)
// See LICENSE file for extended copyright information.
// This file is part of the repository from .

using System;
using System.IO;
using ModShardLauncher;
using ModShardLauncher.Mods;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace Rollback;
public class Rollback : Mod
{
    public override string Author => "Altair";
    public override string Name => "Rollback";
    public override string Description => "Rollback game experience to v0.8.2.10";
    public override string Version => "1.0.0.0";
    public override string TargetVersion => "0.8.2.10";

    private Dictionary<string, string> equipment = new Dictionary<string, string>();
    private Dictionary<string, string> attribute = new Dictionary<string, string>();

    public override void PatchMod()
    {
        LoadEquipmentChinese();
        LoadAttributeChinese();
    
        ExportAllTables();

        UpdateWeaponArmorTable("gml_GlobalScript_table_weapons", ModFiles, "gml_GlobalScript_table_weapons.gml");
        UpdateWeaponArmorTable("gml_GlobalScript_table_armor", ModFiles, "gml_GlobalScript_table_armor.gml");
        AddToWeaponTable(ModFiles, "derived_weapons.gml");
        AddToArmorTable(ModFiles, "derived_armor.gml");
    }

    private void ExportAllTables()
    {
        // string dest = "ModSources/Rollback/tmp/0.8";
        string dest = "ModSources/Rollback/tmp/0.9";
        ExportTable("gml_GlobalScript_table_weapons", dest);
        ExportTableToIni("gml_GlobalScript_table_weapons", dest);
        ExportTable("gml_GlobalScript_table_armor", dest);
        ExportTableToIni("gml_GlobalScript_table_armor", dest);
    }

    private void UpdateWeaponArmorTable(string table, ModFile modFile, string iniFile)
    {
        IniFile ini = new IniFile(modFile, iniFile);
        List<string> lines = Msl.ThrowIfNull(ModLoader.GetTable(table));
        string[] header = lines[0].Split(';');

        List<string> newlines = lines.Select(line => {
            string[] record = line.Split(";");
            if (ini.SectionExists(record[0]))
            {
                return string.Join(';', header.Select(key => {
                    if (key == "name")
                        return equipment[ini.GetValue(record[0], key)];

                    // Allow English attributes
                    if (ini.KeyExists(record[0], key))
                        return ini.GetValue(record[0], key);
                    // Allow Chinese attributes
                    else if (attribute.ContainsKey(key) && ini.KeyExists(record[0], attribute[key]))
                        return ini.GetValue(record[0], attribute[key]);

                    return "";
                }));
            }

            return line;
        }).ToList();

        ModLoader.SetTable(newlines, table);

        // ExportTable(table, "ModSources/Rollback/tmp/test");
    }

    private void AddToWeaponTable(ModFile modFile, string iniFile)
    {
        AddToWeaponArmorTable("gml_GlobalScript_table_weapons", modFile, iniFile, "weapon");
    }

    private void AddToArmorTable(ModFile modFile, string iniFile)
    {
        AddToWeaponArmorTable("gml_GlobalScript_table_armor", modFile, iniFile, "armor");
    }

    private void AddToWeaponArmorTable(string table, ModFile modFile, string iniFile, string type)
    {
        IniFile ini = new IniFile(modFile, iniFile);
        List<string> lines = Msl.ThrowIfNull(ModLoader.GetTable(table));
        List<string> header = lines[0].Split(';').ToList();

        List<string> sections = ini.GetSections();
        foreach (string section in sections)
        {
            if (!ini.KeyExists(section, "parent"))
                continue;
            
            string parent = ini.GetValue(section, "parent");
            string line = Msl.ThrowIfNull(lines.Find(li => li.StartsWith(parent + ';')));
            string[] record = line.Split(';');

            List<string> keys = ini.GetKeys(section);
            foreach (string key in keys)
            {
                int idx = header.FindIndex(h => h == key);
                if (idx == -1 && attribute.ContainsKey(key))
                    idx = header.FindIndex(h => h == attribute[key]);
                
                if (idx == -1)
                    continue;

                string value = ini.GetValue(section, key);
                if (key == "name" && value != section)
                {
                    AddEquipmentTranslation(section, value, type, parent);
                    value = section;                    
                }

                record[idx] = value;
            }

            lines.Add(string.Join(';', record));

            CopySpriteFrom(section, parent, "s_char_");
            CopySpriteFrom(section, parent, "s_charleft_");
            CopySpriteFrom(section, parent, "s_loot_");
            CopySpriteFrom(section, parent, "s_inv_");
        }

        ModLoader.SetTable(lines, table);

        ExportTable(table, "ModSources/Rollback/tmp/test");
        ExportTable("gml_GlobalScript_table_equipment", "ModSources/Rollback/tmp/test");
    }

    private static void ExportTable(string table, string folder)
    {
        DirectoryInfo dir = new(folder);
        if (!dir.Exists) dir.Create();
        List<string>? lines = ModLoader.GetTable(table);
        string base_name = Path.Join(dir.FullName, Path.DirectorySeparatorChar.ToString(), table);
        IniFile ini = new IniFile(base_name + ".ini");
        if (lines != null)
        {
            File.WriteAllLines(
                base_name + ".csv",
                lines.Select(x => string.Join(',', x.Split(';')))
            );
        }
    }

    private void ExportTableToIni(string table, string folder)
    {
        DirectoryInfo dir = new(folder);
        if (!dir.Exists) dir.Create();
        List<string> lines = Msl.ThrowIfNull(ModLoader.GetTable(table));

        FileInfo iniFile = new(Path.Join(dir.FullName, Path.DirectorySeparatorChar.ToString(), table + ".ini"));
        if (iniFile.Exists) iniFile.Delete();

        IniFile ini = new IniFile(iniFile.FullName);

        string[] header = lines[0].Split(';');
        for (int j = 1; j < lines.Count; j++)
        {
            string line = lines[j];
            string[] record = line.Split(';');
            if (record[2] == "")
                continue;
            string section = record[0];
            for (int i = 0; i < record.Length; i++)
            {
                string value = record[i];
                if (value != "")
                {
                    if (header[i] == "name" && equipment.ContainsKey(record[i]))
                        ini.SetValue(section, header[i], equipment[record[i]]);
                    else if (attribute.ContainsKey(header[i]))
                        ini.SetValue(section, attribute[header[i]], record[i]);
                    else
                        ini.SetValue(section, header[i], record[i]);
                }
            }
        }

        ini.Save();   
    }

    private List<string[]> GetTableRange(string table, string start, string end)
    {
        List<string> lines = Msl.ThrowIfNull(ModLoader.GetTable(table));
        int startIndex = lines.FindIndex(item => item.Contains(start));
        int endIndex = lines.FindIndex(item => item.Contains(end));
        return lines
            .Skip(startIndex + 1)
            .Take(endIndex - startIndex - 1)
            .Select(x => x.Split(";")).ToList();
    }

    private void LoadEquipmentChinese()
    {
        List<string[]> weapon_name = GetTableRange("gml_GlobalScript_table_equipment", "weapon_name;weapon_name;", "weapon_name_end;weapon_name_end;");
        List<string[]> armor_name = GetTableRange("gml_GlobalScript_table_equipment", "armor_name;armor_name;", "armor_name_end;armor_name_end;");

        foreach (string[] record in weapon_name.Concat(armor_name))
        {
            string key = record[0];
            if (!string.IsNullOrEmpty(key))
            {
                equipment[key] = record[3];
                equipment[record[3]] = key;
            }
        }
    }

    private void LoadAttributeChinese()
    {
        List<string[]> attrs = GetTableRange(
            "gml_GlobalScript_table_attributes",
            "attribute_text;attribute_text;",
            "attribute_text_end;attribute_text_end;");

        foreach (string[] record in attrs)
        {
            string key = record[0];
            if (!string.IsNullOrEmpty(key))
            {
                attribute[key] = record[3];
                attribute[record[3]] = key;
            }
        }
    }

    private void AddEquipmentTranslation(string id, string name, string type, string parent)
    {
        string nameln = $"{id};{id};{id};{name};" + string.Concat(Enumerable.Repeat($"{id};", 9));
        string locator = ";" + string.Concat(Enumerable.Repeat($"{type}_name_end;", 12));

        List<string> lines = Msl.ThrowIfNull(ModLoader.GetTable("gml_GlobalScript_table_equipment"));
        lines.Insert(lines.IndexOf(locator), nameln);
    
        List<string[]> descs = GetTableRange("gml_GlobalScript_table_equipment", $"{type}_desc;{type}_desc;", $"{type}_desc_end;{type}_desc_end;");
        string[] record = Msl.ThrowIfNull(descs.Find(li => li[0] == parent));
        record[0] = id;

        string locatorDesc = ";" + string.Concat(Enumerable.Repeat($"{type}_desc_end;", 12));
        lines.Insert(lines.IndexOf(locatorDesc), string.Join(';', record));

        ModLoader.SetTable(lines, "gml_GlobalScript_table_equipment");
    }

    private void CopySpriteFrom(string newequip, string oldequip, string prefix)
    {
            string oldspr = GetSpriteName(prefix, oldequip);
            string newspr = GetSpriteName(prefix, newequip);

            UndertaleSprite? parent_spr = DataLoader.data.Sprites.FirstOrDefault(t => t.Name.Content == oldspr.ToLower());
            parent_spr ??= DataLoader.data.Sprites.First(t => t.Name.Content == oldspr);

            var textures = new UndertaleSimpleList<UndertaleSprite.TextureEntry>();
            foreach (var item in parent_spr.Textures)
            {
                UndertaleSprite.TextureEntry newEntry = new()
                {
                    Texture = item.Texture
                };

                textures.Add(newEntry);
            }

            UndertaleSprite newSprite = new()
            {
                Name = DataLoader.data.Strings.MakeString(newspr),

                Width = parent_spr.Width,
                Height = parent_spr.Height,

                MarginLeft = parent_spr.MarginLeft,
                MarginRight = parent_spr.MarginRight,
                MarginTop = parent_spr.MarginTop,
                MarginBottom = parent_spr.MarginBottom,

                Transparent = parent_spr.Transparent,
                Smooth = parent_spr.Smooth,
                Preload = parent_spr.Preload,
                BBoxMode = parent_spr.BBoxMode,
                SepMasks = parent_spr.SepMasks,

                OriginX = parent_spr.OriginX,
                OriginY = parent_spr.OriginX,
                
                Textures = textures,
                CollisionMasks = parent_spr.CollisionMasks,
                IsSpecialType = parent_spr.IsSpecialType,
                SpineVersion = parent_spr.SpineVersion,
                SSpriteType = parent_spr.SSpriteType,
                GMS2PlaybackSpeed = parent_spr.GMS2PlaybackSpeed,
                GMS2PlaybackSpeedType = parent_spr.GMS2PlaybackSpeedType
            };

            DataLoader.data.Sprites.Add(newSprite);
    }

    private string GetSpriteName(string prefix, string id)
    {
        return prefix + id.Replace(" ", "").Replace("'", "");
    }
}
