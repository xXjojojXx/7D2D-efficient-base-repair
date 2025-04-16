using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEngine;


public class ModConfig
{
    private readonly Dictionary<string, string> properties = new Dictionary<string, string>();

    private readonly XmlDocument document;

    public ModConfig(string modName)
    {
        // modName must equals the one defined in ModInfo.xml
        var mod = ModManager.GetMod(modName);
        var modConfig = Path.GetFullPath($"{mod.Path}/ModConfig.xml");

        document = new XmlDocument();

        using (var reader = new StreamReader(modConfig))
        {
            document.LoadXml(reader.ReadToEnd());
        }

        foreach (XmlNode property in document.GetElementsByTagName("property"))
        {
            string name = property.Attributes["name"]?.Value;
            string value = property.Attributes["value"]?.Value;

            if (name != null && value != null)
            {
                properties[name] = value;
            }
        }
    }

    public string GetProperty(string name)
    {
        if (properties.TryGetValue(name, out var value))
        {
            return value;
        }

        throw new KeyNotFoundException(name);
    }

    public float GetFloat(string name)
    {
        return float.Parse(GetProperty(name));
    }

    public int GetInt(string name)
    {
        return int.Parse(GetProperty(name));
    }

    public bool GetBool(string name)
    {
        return bool.Parse(GetProperty(name));
    }

    public Vector2 GetVector2(string name)
    {
        string[] values = GetProperty(name).Split(',');

        return new Vector2(
            float.Parse(values[0].Trim()),
            float.Parse(values[1].Trim())
        );
    }

    public Vector3 GetVector3(string name)
    {
        string[] values = GetProperty(name).Split(',');

        return new Vector3(
            float.Parse(values[0].Trim()),
            float.Parse(values[1].Trim()),
            float.Parse(values[2].Trim())
        );
    }

    public Vector2i GetVector2i(string name)
    {
        string[] values = GetProperty(name).Split(',');

        return new Vector2i(
            int.Parse(values[0].Trim()),
            int.Parse(values[1].Trim())
        );
    }

    public Vector3i GetVector3i(string name)
    {
        string[] values = GetProperty(name).Split(',');

        return new Vector3i(
            int.Parse(values[0].Trim()),
            int.Parse(values[1].Trim()),
            int.Parse(values[2].Trim())
        );
    }
}
