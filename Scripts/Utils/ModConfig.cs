using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEngine;


public class ModConfig
{
    private readonly Dictionary<string, string> properties = new Dictionary<string, string>();

    private readonly XmlDocument document;

    private readonly string modName;

    private readonly int version;

    public ModConfig(string modName, int version = 0, bool save = false)
    {
        this.modName = modName;
        this.version = version;

        if (modName == "")
            throw new InvalidDataException("modname must not be empty");

        if (!ExistsFromModFolder(modName))
            throw new FileNotFoundException($"Can't find ModConfig.xml for mod '{modName}'");

        if (ExistsFromUserData(modName))
            document = ReadFromUserData(modName);

        if (document is null || GetVersion(document) < version)
        {
            document = ReadFromModFolder(modName);
            TryRemoveFromUserdata(modName);
        }

        if (save)
        {
            SaveToUserData();
        }

        properties = ParseProperties(document);
    }

    public int GetVersion(XmlDocument document)
    {
        if (document.DocumentElement.TryGetAttribute("version", out var version))
        {
            return int.Parse(version);
        }

        return 0;
    }

    public Dictionary<string, string> ParseProperties(XmlDocument document)
    {
        var properties = new Dictionary<string, string>();

        foreach (XmlNode property in document.GetElementsByTagName("property"))
        {
            string name = property.Attributes["name"]?.Value;
            string value = property.Attributes["value"]?.Value;

            if (name != null && value != null)
            {
                properties[name] = value;
            }
        }

        return properties;
    }

    public string GetProperty(string name)
    {
        if (properties.TryGetValue(name, out var value))
        {
            return value;
        }

        throw new KeyNotFoundException(name);
    }

    public void SetProperty(string name, object value)
    {
        properties[name] = value.ToString();
    }

    public void SetProperty(string name, IEnumerable<object> values)
    {
        properties[name] = string.Join(",", values.ToList());
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

    public LoggingLevel GetLoggingLevel(string name)
    {
        var loggingLevel = GetProperty(name);

        switch (loggingLevel.ToLower())
        {
            case "debug":
                return LoggingLevel.DEBUG;

            case "info":
                return LoggingLevel.INFO;

            case "warning":
                return LoggingLevel.WARNING;

            case "error":
                return LoggingLevel.ERROR;

            case "none":
                return LoggingLevel.NONE;

            default:
                throw new KeyNotFoundException(loggingLevel);
        }
    }

    private string GetPathFromUserData(string modName)
    {
        return $"{GameIO.GetUserGameDataDir()}/{modName}.ModConfig.xml";
    }

    private string GetPathFromModFolder(string modName)
    {
        var mod = ModManager.GetMod(modName);

        return Path.GetFullPath($"{mod.Path}/ModConfig.xml");
    }

    private bool ExistsFromUserData(string modName)
    {
        var path = GetPathFromUserData(modName);

        return File.Exists(path);
    }

    private bool ExistsFromModFolder(string modName)
    {
        var path = GetPathFromModFolder(modName);

        return File.Exists(path);
    }

    public XmlDocument ReadFromPath(string path)
    {
        var xmlDocument = new XmlDocument();

        using (var reader = new StreamReader(path))
        {
            xmlDocument.LoadXml(reader.ReadToEnd());
        }

        Logging.Info($"read '{path}', version={GetVersion(xmlDocument)}");

        return xmlDocument;
    }

    public XmlDocument ReadFromModFolder(string modName)
    {
        var path = GetPathFromModFolder(modName);

        return ReadFromPath(path);
    }

    public XmlDocument ReadFromUserData(string modName)
    {
        var path = $"{GameIO.GetUserGameDataDir()}/{modName}.ModConfig.xml";

        return ReadFromPath(path);
    }

    public void TryRemoveFromUserdata(string modName)
    {
        var path = GetPathFromUserData(modName);

        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    public void SaveToPath(string path)
    {
        document.DocumentElement.SetAttribute("version", version.ToString());

        using (var writer = new StreamWriter(path))
        {
            document.Save(writer);
        }

        Logging.Info($"'{Path.GetFileName(path)}' saved");
    }

    public void SaveToUserData()
    {
        SaveToPath(GetPathFromUserData(modName));
    }

}
