// Copyright (C)
// See LICENSE file for extended copyright information.
// This file is part of the repository from .

using System;
using System.IO;
using ModShardLauncher;
using ModShardLauncher.Mods;

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

        ExportTable(table, "ModSources/Rollback/tmp/test");
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
}
