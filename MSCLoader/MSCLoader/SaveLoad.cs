#if !Mini
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace MSCLoader;

/// <summary>
///     Save and Load Class for gameobject and custom class
/// </summary>
public class SaveLoad
{
    internal static ES2Data saveFileData;
    internal static Dictionary<string, ES2Header> headers;

    internal static void ResetSaveFile()
    {
        saveFileData = null;
        ES2.Delete("Mods.txt");
        ES2.Save(new byte[1] { 0x02 }, "Mods.txt?tag=MSCLoaderInternalStuff");
    }

    internal static void LoadModsSaveData()
    {
        try
        {
            saveFileData = null;
            if (ES2.Exists("Mods.txt"))
            {
                saveFileData = ES2.LoadAll("Mods.txt");
                var settings = new ES2Settings("Mods.txt");
                var es2r = new ES2Reader(settings);
                headers = es2r.ReadAllHeaders();
                if (!saveFileData.TagExists("MSCLoaderInternalStuff")) ConvertSeparators();
            }
            else
            {
                ES2.Save(new byte[1] { 0x02 }, "Mods.txt?tag=MSCLoaderInternalStuff");
            }
        }
        catch (Exception e)
        {
            ModUI.ShowMessage(
                $"Fatal error:{Environment.NewLine}<color=orange>{e.Message}</color>{Environment.NewLine}{Environment.NewLine}Make sure your save folder is not read-only or is open in another application.",
                "Fatal Error");
        }
    }

    internal static void ResetSaveForMod(Mod mod)
    {
        var saveTags = saveFileData.GetTags().Where(x => x.StartsWith($"{mod.ID}||")).ToArray();
        foreach (var tag in saveTags)
            if (ES2.Exists($"Mods.txt?tag={tag}"))
                ES2.Delete($"Mods.txt?tag={tag}");
    }

    //Convert separator from _ to ||
    //One time, safe to delete this in next update, since it's ea
    internal static void ConvertSeparators()
    {
        if (saveFileData == null) return;
        try
        {
            var oldTags = saveFileData.GetTags();
            var regex = new Regex(Regex.Escape("_"));

            var settings = new ES2Settings("Mods.txt");
            var settings2 = new ES2Settings("Mods2.txt");
            var es2r = new ES2Reader(settings);
            var hdr = es2r.ReadAllHeaders();
            ModConsole.Print("One time save format conversion...");
            foreach (var tag in oldTags)
            {
                var header = new ES2Header();
                if (hdr.ContainsKey(tag))
                    hdr.TryGetValue(tag, out header);

                ModConsole.Print($"{tag} -> {regex.Replace(tag, "||", 1)}");
                var w = new ES2Writer(settings2);
                switch (header.collectionType)
                {
                    case ES2Keys.Key._NativeArray:
                        saveFileData.loadedData.TryGetValue(tag, out var value2);
                        var stuff3 = value2 as object[];
                        w.WriteHeader(regex.Replace(tag, "||", 1), ES2Keys.Key._NativeArray,
                            ES2TypeManager.GetES2Type(header.valueType), null);
                        w.Write(stuff3, ES2TypeManager.GetES2Type(header.valueType));
                        w.WriteTerminator();
                        w.WriteLength();
                        w.Save();
                        break;
                    case ES2Keys.Key._List:
                        saveFileData.loadedData.TryGetValue(tag, out var value);
                        var stuff2 = value as List<object>;
                        w.WriteHeader(regex.Replace(tag, "||", 1), ES2Keys.Key._List,
                            ES2TypeManager.GetES2Type(header.valueType), null);
                        w.Write(stuff2, ES2TypeManager.GetES2Type(header.valueType));
                        w.WriteTerminator();
                        w.WriteLength();
                        w.Save();
                        break;
                    case ES2Keys.Key._Null:
                        saveFileData.loadedData.TryGetValue(tag, out var stuff);
                        w.WriteHeader(regex.Replace(tag, "||", 1), ES2Keys.Key._Null,
                            ES2TypeManager.GetES2Type(header.valueType), null);
                        w.Write(stuff, ES2TypeManager.GetES2Type(header.valueType));
                        w.WriteTerminator();
                        w.WriteLength();
                        w.Save();
                        break;
                }
            }

            ES2.Save(new byte[1] { 0x02 }, "Mods2.txt?tag=MSCLoaderInternalStuff");
            ES2.Delete("Mods.txt");
            ES2.Rename("Mods2.txt", "Mods.txt");
            ModConsole.Print("Conversion done!");
            LoadModsSaveData();
        }
        catch (Exception e)
        {
            ModConsole.Error(e.Message);
            Console.WriteLine(e);
        }
    }

    /// <summary>
    ///     Serialize custom save class to custom file (see example)
    ///     Call Only in <see cref="Mod.OnSave" />
    /// </summary>
    /// <typeparam name="T">Your class</typeparam>
    /// <param name="mod">Mod Instance</param>
    /// <param name="saveDataClass">Your class</param>
    /// <param name="fileName">Name of the save file</param>
    public static void SerializeSaveFile<T>(Mod mod, T saveDataClass, string fileName)
    {
        var config = new JsonSerializerSettings();
        config.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        config.Formatting = Formatting.Indented;
        var path = Path.Combine(ModLoader.GetModSettingsFolder(mod), fileName);
        var serializedData = JsonConvert.SerializeObject(saveDataClass, config);
        File.WriteAllText(path, serializedData);
    }

    /// <summary>
    ///     Deserialize custom save class to custom file (see example)
    /// </summary>
    /// <typeparam name="T">Your save class</typeparam>
    /// <param name="mod">Mod Instance</param>
    /// <param name="fileName">Name of the save file</param>
    /// <returns>Deserialized class</returns>
    public static T DeserializeSaveFile<T>(Mod mod, string fileName) where T : new()
    {
        var path = Path.Combine(ModLoader.GetModSettingsFolder(mod), fileName);
        if (File.Exists(path))
        {
            var serializedData = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<T>(serializedData);
        }

        return default;
    }

    /// <summary>
    ///     Serialize custom class under custom ID in Unified save system
    /// </summary>
    /// <typeparam name="T">Your class</typeparam>
    /// <param name="mod">Mod Instance</param>
    /// <param name="saveDataClass">Your class</param>
    /// <param name="valueID">ID of saved class</param>
    /// <param name="encrypt">encrypt data</param>
    public static void SerializeClass<T>(Mod mod, T saveDataClass, string valueID, bool encrypt = false)
    {
        var config = new JsonSerializerSettings();
        config.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        config.Formatting = Formatting.None;
        var serializedData = JsonConvert.SerializeObject(saveDataClass, config);
        if (encrypt)
        {
            var bytes = Encoding.UTF8.GetBytes(serializedData);
            WriteValue(mod, valueID, bytes);
            return;
        }

        WriteValue(mod, valueID, serializedData);
    }

    /// <summary>
    ///     Deserialize custom class from Unified save system
    /// </summary>
    /// <typeparam name="T">Your class</typeparam>
    /// <param name="mod">Mod Instance</param>
    /// <param name="valueID">ID of saved class</param>
    /// <param name="encrypted">Was the data encrypted [Important!]</param>
    /// <returns>Your class</returns>
    public static T DeserializeClass<T>(Mod mod, string valueID, bool encrypted = false) where T : new()
    {
        string serializedData;
        if (encrypted)
        {
            var bytes = ReadValueAsArray<byte>(mod, valueID);
            serializedData = Encoding.UTF8.GetString(bytes);
        }
        else
        {
            serializedData = ReadValue<string>(mod, valueID);
        }

        if (serializedData != null)
            return JsonConvert.DeserializeObject<T>(serializedData);

        return default;
    }

    /// <summary>
    ///     Check if saved value exists in save file.
    /// </summary>
    /// <param name="mod">Mod Instance</param>
    /// <param name="valueID">ID of saved value</param>
    /// <returns>true if value exists in save file</returns>
    public static bool ValueExists(Mod mod, string valueID)
    {
        return ES2.Exists($"Mods.txt?tag={mod.ID}||{valueID}");
    }

    /// <summary>
    ///     Read saved value
    /// </summary>
    /// <typeparam name="T">Type of the saved value</typeparam>
    /// <param name="mod">Mod Instance</param>
    /// <param name="valueID">ID of saved value</param>
    /// <returns>Your saved value</returns>
    public static T ReadValue<T>(Mod mod, string valueID)
    {
        if (ES2.Exists($"Mods.txt?tag={mod.ID}||{valueID}"))
            return ES2.Load<T>($"Mods.txt?tag={mod.ID}||{valueID}");
        return default;
    }

    /// <summary>
    ///     Read saved value as Array
    /// </summary>
    /// <typeparam name="T">Type of the saved array</typeparam>
    /// <param name="mod">Mod Instance</param>
    /// <param name="valueID">ID of saved value</param>
    /// <returns>Your saved value</returns>
    public static T[] ReadValueAsArray<T>(Mod mod, string valueID)
    {
        if (ES2.Exists($"Mods.txt?tag={mod.ID}||{valueID}"))
            return ES2.LoadArray<T>($"Mods.txt?tag={mod.ID}||{valueID}");
        return null;
    }

    /// <summary>
    ///     Read saved value as 2D Array
    /// </summary>
    /// <typeparam name="T">Type of the saved 2darray</typeparam>
    /// <param name="mod">Mod Instance</param>
    /// <param name="valueID">ID of saved value</param>
    /// <returns>Your saved value</returns>
    public static T[,] ReadValueAs2DArray<T>(Mod mod, string valueID)
    {
        if (ES2.Exists($"Mods.txt?tag={mod.ID}||{valueID}"))
            return ES2.Load2DArray<T>($"Mods.txt?tag={mod.ID}||{valueID}");
        return null;
    }

    /// <summary>
    ///     Read saved value as List
    /// </summary>
    /// <typeparam name="T">Type of the saved list</typeparam>
    /// <param name="mod">Mod Instance</param>
    /// <param name="valueID">ID of saved value</param>
    /// <returns>Your saved value</returns>
    public static List<T> ReadValueAsList<T>(Mod mod, string valueID)
    {
        if (ES2.Exists($"Mods.txt?tag={mod.ID}||{valueID}"))
            return ES2.LoadList<T>($"Mods.txt?tag={mod.ID}||{valueID}");
        return null;
    }

    /// <summary>
    ///     Read saved value as HashSet
    /// </summary>
    /// <typeparam name="T">Type of the saved hashset</typeparam>
    /// <param name="mod">Mod Instance</param>
    /// <param name="valueID">ID of saved value</param>
    /// <returns>Your saved value</returns>
    public static HashSet<T> ReadValueAsHashSet<T>(Mod mod, string valueID)
    {
        if (ES2.Exists($"Mods.txt?tag={mod.ID}||{valueID}"))
            return ES2.LoadHashSet<T>($"Mods.txt?tag={mod.ID}||{valueID}");
        return null;
    }

    /// <summary>
    ///     Read saved value as Queue
    /// </summary>
    /// <typeparam name="T">Type of the saved Queue</typeparam>
    /// <param name="mod">Mod Instance</param>
    /// <param name="valueID">ID of saved value</param>
    /// <returns>Your saved value</returns>
    public static Queue<T> ReadValueAsQueue<T>(Mod mod, string valueID)
    {
        if (ES2.Exists($"Mods.txt?tag={mod.ID}||{valueID}"))
            return ES2.LoadQueue<T>($"Mods.txt?tag={mod.ID}||{valueID}");
        return null;
    }

    /// <summary>
    ///     Read saved value as Stack
    /// </summary>
    /// <typeparam name="T">Type of the saved stack</typeparam>
    /// <param name="mod">Mod Instance</param>
    /// <param name="valueID">ID of saved value</param>
    /// <returns>Your saved value</returns>
    public static Stack<T> ReadValueAsStack<T>(Mod mod, string valueID)
    {
        if (ES2.Exists($"Mods.txt?tag={mod.ID}||{valueID}"))
            return ES2.LoadStack<T>($"Mods.txt?tag={mod.ID}||{valueID}");
        return null;
    }

    /// <summary>
    ///     Read saved value as Dictionary
    /// </summary>
    /// <typeparam name="TKey">dictionary key</typeparam>
    /// <typeparam name="TValue">dictionary value</typeparam>
    /// <param name="mod">Mod Instance</param>
    /// <param name="valueID">ID of saved value</param>
    /// <returns>Your saved value</returns>
    public static Dictionary<TKey, TValue> ReadValueAsDictionary<TKey, TValue>(Mod mod, string valueID)
    {
        if (ES2.Exists($"Mods.txt?tag={mod.ID}||{valueID}"))
            return ES2.LoadDictionary<TKey, TValue>($"Mods.txt?tag={mod.ID}||{valueID}");
        return null;
    }

    /// <summary>
    ///     Write value to save file
    /// </summary>
    /// <typeparam name="T">value type</typeparam>
    /// <param name="mod">Mod Instance</param>
    /// <param name="valueID">unique ID under this value will be saved</param>
    /// <param name="value">Value to save</param>
    public static void WriteValue<T>(Mod mod, string valueID, T value)
    {
        var sf = $"Mods.txt?tag={mod.ID}||{valueID}";
        ES2.Save(value, sf);
    }

    /// <summary>
    ///     Write array to save file
    /// </summary>
    /// <typeparam name="T">value type</typeparam>
    /// <param name="mod">Mod Instance</param>
    /// <param name="valueID">unique ID under this value will be saved</param>
    /// <param name="value">Array to save</param>
    public static void WriteValue<T>(Mod mod, string valueID, T[] value)
    {
        var sf = $"Mods.txt?tag={mod.ID}||{valueID}";
        ES2.Save(value, sf);
    }

    /// <summary>
    ///     Write 2D array to save file
    /// </summary>
    /// <typeparam name="T">value type</typeparam>
    /// <param name="mod">Mod Instance</param>
    /// <param name="valueID">unique ID under this value will be saved</param>
    /// <param name="value">2D array to save</param>
    public static void WriteValue<T>(Mod mod, string valueID, T[,] value)
    {
        var sf = $"Mods.txt?tag={mod.ID}||{valueID}";
        ES2.Save(value, sf);
    }

    /// <summary>
    ///     Write List to save file
    /// </summary>
    /// <typeparam name="T">List type</typeparam>
    /// <param name="mod">Mod Instance</param>
    /// <param name="valueID">unique ID under this value will be saved</param>
    /// <param name="value">List to save</param>
    public static void WriteValue<T>(Mod mod, string valueID, List<T> value)
    {
        var sf = $"Mods.txt?tag={mod.ID}||{valueID}";
        ES2.Save(value, sf);
    }

    /// <summary>
    ///     Write HashSet to save file
    /// </summary>
    /// <typeparam name="T">value type</typeparam>
    /// <param name="mod">Mod Instance</param>
    /// <param name="valueID">unique ID under this value will be saved</param>
    /// <param name="value">HashSet to save</param>
    public static void WriteValue<T>(Mod mod, string valueID, HashSet<T> value)
    {
        var sf = $"Mods.txt?tag={mod.ID}||{valueID}";
        ES2.Save(value, sf);
    }

    /// <summary>
    ///     Write Queue to save file
    /// </summary>
    /// <typeparam name="T">value type</typeparam>
    /// <param name="mod">Mod Instance</param>
    /// <param name="valueID">unique ID under this value will be saved</param>
    /// <param name="value">Queue to save</param>
    public static void WriteValue<T>(Mod mod, string valueID, Queue<T> value)
    {
        var sf = $"Mods.txt?tag={mod.ID}||{valueID}";
        ES2.Save(value, sf);
    }

    /// <summary>
    ///     Write Stack to save file
    /// </summary>
    /// <typeparam name="T">value type</typeparam>
    /// <param name="mod">Mod Instance</param>
    /// <param name="valueID">unique ID under this value will be saved</param>
    /// <param name="value">Stack to save</param>
    public static void WriteValue<T>(Mod mod, string valueID, Stack<T> value)
    {
        var sf = $"Mods.txt?tag={mod.ID}||{valueID}";
        ES2.Save(value, sf);
    }

    /// <summary>
    ///     Write Dictionary to save file
    /// </summary>
    /// <typeparam name="TKey">Dictionary key</typeparam>
    /// <typeparam name="TValue">Dictionary value</typeparam>
    /// <param name="mod">Mod Instance</param>
    /// <param name="valueID">unique ID under this value will be saved</param>
    /// <param name="value">Dictionary to save</param>
    public static void WriteValue<TKey, TValue>(Mod mod, string valueID, Dictionary<TKey, TValue> value)
    {
        var sf = $"Mods.txt?tag={mod.ID}||{valueID}";
        ES2.Save(value, sf);
    }

    /// <summary>
    ///     Delete value from Mods.txt (if exists)
    /// </summary>
    /// <param name="mod">Mod Instance</param>
    /// <param name="valueID">unique ID of saved value</param>
    public static void DeleteValue(Mod mod, string valueID)
    {
        if (!ES2.Exists($"Mods.txt?tag={mod.ID}||{valueID}")) return;
        ES2.Delete($"Mods.txt?tag={mod.ID}||{valueID}");
    }
#if MSC
    /// <summary>
    ///     Save position and rotation of single gameobject to file (DO NOT loop this for multiple gameobjects)
    ///     Call this in <see cref="Mod.OnSave" />  function
    /// </summary>
    /// <param name="mod">Mod instance</param>
    /// <param name="g">Your GameObject to save</param>
    /// <param name="fileName">Name of the save file</param>
    /// <exclude />
    [Obsolete("Consider switching to SaveLoad.WriteValue or serializing custom class.", true)]
    public static void SaveGameObject(Mod mod, GameObject g, string fileName)
    {
        var path = Path.Combine(ModLoader.GetModSettingsFolder(mod), fileName);
        var save = new SaveData();
        var s = new SaveDataList
        {
            name = g.name,
            pos = g.transform.position,
            rotX = g.transform.localEulerAngles.x,
            rotY = g.transform.localEulerAngles.y,
            rotZ = g.transform.localEulerAngles.z
        };
        save.save.Add(s);
        var config = new JsonSerializerSettings();
        config.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        config.Formatting = Formatting.Indented;
        var serializedData = JsonConvert.SerializeObject(save, config);
        File.WriteAllText(path, serializedData);
    }

    /// <summary>
    ///     Load position and rotation of single gameobject from file
    ///     Call this AFTER you load your gameobject
    /// </summary>
    /// <param name="mod">Mod instance</param>
    /// <param name="fileName">Name of the save file</param>
    /// <exclude />
    [Obsolete("Consider switching to SaveLoad.ReadValue or deserializing custom class.", true)]
    public static void LoadGameObject(Mod mod, string fileName)
    {
        var data = DeserializeSaveFile<SaveData>(mod, fileName);
        var go = GameObject.Find(data.save[0].name);
        go.transform.position = data.save[0].pos;
        go.transform.eulerAngles = new Vector3(data.save[0].rotX, data.save[0].rotY, data.save[0].rotZ);
    }
#endif
}
#if MSC
#pragma warning disable CS1591
[Obsolete("Consider switching to serializing custom class.", true)]
public class SaveData
{
    public List<SaveDataList> save = new();
}

[Obsolete("Consider switching to serializing custom class.", true)]
public class SaveDataList
{
    public string name;
    public Vector3 pos;
    public float rotX, rotY, rotZ;
}
#pragma warning restore CS1591
#endif
#endif