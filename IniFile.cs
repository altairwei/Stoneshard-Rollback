using System;
using System.Collections.Generic;
using System.IO;

using ModShardLauncher;

namespace Rollback;
public class IniFile
{
    private Dictionary<string, Dictionary<string, string>> data =
        new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
    private string filePath;

    public IniFile(ModFile modFile, string fileName)
    {
        this.filePath = fileName;
        Parse(modFile.GetCode(fileName).Split("\n"));
    }

    public IniFile(string filePath)
    {
        this.filePath = filePath;
        if (File.Exists(filePath))
        {
            Parse(File.ReadAllLines(filePath));
        }
        else
        {
            // 如果文件不存在，则初始化一个默认的 "global" section
            data["global"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private void Parse(string[] lines)
    {
        string currentSection = "global";
        data[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in lines)
        {
            string trimmedLine = line.Trim();

            // 忽略空行和以 ; 或 # 开头的注释行
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith(";") || trimmedLine.StartsWith("#"))
                continue;

            // 如果是 section 行，例如：[SectionName]
            if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
            {
                currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2).Trim();
                if (!data.ContainsKey(currentSection))
                {
                    data[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }
            }
            else
            {
                // 处理 key=value 格式的行
                int index = trimmedLine.IndexOf('=');
                if (index > 0)
                {
                    string key = trimmedLine.Substring(0, index).Trim();
                    string value = trimmedLine.Substring(index + 1).Trim();
                    data[currentSection][key] = value;
                }
            }
        }
    }

    public bool SectionExists(string section)
    {
        return data.ContainsKey(section);
    }

    public List<string> GetSections()
    {
        return new List<string>(data.Keys);
    }

    public bool KeyExists(string section, string key)
    {
        if (SectionExists(section) && data[section].ContainsKey(key))
            return true;
        
        return false;
    }

    public List<string> GetKeys(string section)
    {
        if (data.ContainsKey(section))
        {
            return new List<string>(data[section].Keys);
        }
        return new List<string>();
    }

    public string GetValue(string section, string key)
    {
        if (data.ContainsKey(section) && data[section].ContainsKey(key))
        {
            return data[section][key];
        }
        return null;
    }

    public void SetValue(string section, string key, string value)
    {
        if (!data.ContainsKey(section))
        {
            data[section] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        data[section][key] = value;
    }

    public void Save()
    {
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            foreach (var section in data)
            {
                // 如果不是默认的 global section，则写入 section 行
                if (section.Key != "global")
                {
                    writer.WriteLine($"[{section.Key}]");
                }
                foreach (var kv in section.Value)
                {
                    writer.WriteLine($"{kv.Key}={kv.Value}");
                }
                writer.WriteLine(); // 每个 section 后添加空行
            }
        }
    }
}
