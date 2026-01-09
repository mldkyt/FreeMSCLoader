#if !Mini 
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace MSCLoader;

internal class MSCLInternal
{
    internal static bool AsyncRequestInProgress = false;
    internal static bool AsyncRequestError = false;
    internal static string AsyncRequestResult = string.Empty;
    internal static bool ValidateVersion(string version)
    {
        try
        {
            new Version(version);
        }
        catch
        {
            ModConsole.Error($"Invalid version: {version}{Environment.NewLine}Please use proper version format: (0.0 or 0.0.0 or 0.0.0.0)");
            return false;
        }
        return true;
    }

    internal static string ProLoaderMagic()
    {
        byte[] magicbyte = { 97, 114, 97, 105, 107, 55, 49 };
        return Encoding.UTF8.GetString(magicbyte);
    }


    internal static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
    {
        DirectoryInfo dir = new DirectoryInfo(sourceDirName);
        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException(
                "Source directory does not exist or could not be found: "
                + sourceDirName);
        }
        DirectoryInfo[] dirs = dir.GetDirectories();

        Directory.CreateDirectory(destDirName);

        FileInfo[] files = dir.GetFiles();
        foreach (FileInfo file in files)
        {
            string tempPath = Path.Combine(destDirName, file.Name);
            file.CopyTo(tempPath, false);
        }

        if (copySubDirs)
        {
            foreach (DirectoryInfo subdir in dirs)
            {
                string tempPath = Path.Combine(destDirName, subdir.Name);
                DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
            }
        }
    }

    internal static void SaveMSCLDataFile()
    {
        if (!ES2.Exists(ModLoader.GetMetadataFolder("MSCLData.bin")))
        {
            ES2.Save(new byte[1] { 0x01 }, $"{ModLoader.GetMetadataFolder("MSCLData.bin")}?tag=MSCLData");
        }
        using (ES2Writer writer = ES2Writer.Create(ModLoader.GetMetadataFolder("MSCLData.bin")))
        {
            JsonSerializerSettings config = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Formatting.None
            };
            for (int i = 0; i < ModLoader.Instance.MetadataUpdateList.Count; i++)
            {
                Mod mod = ModLoader.GetModByID(ModLoader.Instance.MetadataUpdateList[i], true);
                if (mod == null) continue;
                if (mod.metadata == null) continue;
                string serializedData = JsonConvert.SerializeObject(mod.metadata, config);
                byte[] bytes = Encoding.UTF8.GetBytes(serializedData);
                writer.Write(bytes, $"{mod.ID}||metadata");
            }
            writer.Save();
        }
    }
    internal static void SaveMSCLDataFile(Mod mod)
    {
        if (!ES2.Exists(ModLoader.GetMetadataFolder("MSCLData.bin")))
        {
            ES2.Save(new byte[1] { 0x01 }, $"{ModLoader.GetMetadataFolder("MSCLData.bin")}?tag=MSCLData");
        }
        if (mod.metadata == null) return;
        JsonSerializerSettings config = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Formatting = Formatting.None
        };
        string serializedData = JsonConvert.SerializeObject(mod.metadata, config);
        byte[] bytes = Encoding.UTF8.GetBytes(serializedData);

        ES2.Save(bytes, $"{ModLoader.GetMetadataFolder("MSCLData.bin")}?tag={mod.ID}||metadata");
    }
    internal static void LoadMSCLDataFile()
    {
        if (!ES2.Exists(ModLoader.GetMetadataFolder("MSCLData.bin")))
        {
            ES2.Save(new byte[1] { 0x01 }, $"{ModLoader.GetMetadataFolder("MSCLData.bin")}?tag=MSCLData");
            string[] oldm = Directory.GetFiles(ModLoader.GetMetadataFolder(""), "*.json");
            if (oldm.Length > 0)
            {
                for (int i = 0; i < oldm.Length; i++)
                {
                    File.Delete(oldm[i]);
                }
            }
            return;
        }
        using (ES2Reader reader = ES2Reader.Create(ModLoader.GetMetadataFolder("MSCLData.bin")))
        {
            for (int i = 0; i < ModLoader.Instance.actualModList.Length; i++)
            {
                Mod mod = ModLoader.Instance.actualModList[i];
                if (!reader.TagExists($"{mod.ID}||metadata")) continue;
                byte[] bytes = reader.ReadArray<byte>($"{mod.ID}||metadata");
                string serializedData = Encoding.UTF8.GetString(bytes);
                mod.metadata = JsonConvert.DeserializeObject<MSCLData>(serializedData);
            }
        }
    }
    internal static bool MSCLDataExists(string modID)
    {
        return ES2.Exists($"{ModLoader.GetMetadataFolder("MSCLData.bin")}?tag={modID}||metadata");
    }

    internal static bool IsEAFile(string path)
    {
        byte[] bytes = new byte[4];
        try
        {
            using (BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                reader.Read(bytes, 0, 4);
                reader.Close();
                return bytes[0] == 0x45 && bytes[1] == 0x41 && bytes[2] == 0x4D && bytes[3] == 0x33;
            }
        }
        catch (Exception ex)
        {
            ModConsole.Error(ex.Message);
            return false;
        }
    }

}

//GPL-3 licensed ByteArrayExtensions by Bruno Tabbia
internal static class ByteArrayExtensions
{
    const int bitsinbyte = 8;

    public static byte[] Cry_ScrambleByteRightEnc(this byte[] cleardata, byte[] password)
    {
        long cdlen = cleardata.LongLength;
        byte[] cryptdata = new byte[cdlen];
        // first loop: fill crypt array with bytes from cleardata 
        // corresponding to the '1' in passwords bit
        long ci = 0;
        for (long b = cdlen - 1; b >= 0; b--)
        {
            if (password.GetBitR(b))
            {
                cryptdata[ci] = cleardata[b];
                ci++;
            }
        }
        // second loop: fill crypt array with bytes from cleardata 
        // corresponding to the '0' in passwords bit
        for (long b = cdlen - 1; b >= 0; b--)
        {
            if (!password.GetBitR(b))
            {
                cryptdata[ci] = cleardata[b];
                ci++;
            }
        }
        return cryptdata;
    }

    public static byte[] Cry_ScrambleByteRightDec(this byte[] cryptdata, byte[] password)
    {
        long cdlen = cryptdata.LongLength;
        byte[] cleardata = new byte[cdlen];
        long ci = 0;
        for (long b = cdlen - 1; b >= 0; b--)
        {
            if (password.GetBitR(b))
            {
                cleardata[b] = cryptdata[ci];
                ci++;
            }
        }
        for (long b = cdlen - 1; b >= 0; b--)
        {
            if (!password.GetBitR(b))
            {
                cleardata[b] = cryptdata[ci];
                ci++;
            }
        }
        return cleardata;
    }

    // --------------------------------------------------------------------------------------

    private static byte[] Cry_ScrambleBitRightEnc(this byte[] cleardata, byte[] password)
    {
        long cdlen = cleardata.LongLength;
        byte[] cryptdata = new byte[cdlen];
        // first loop: fill crypt array with bits from cleardata 
        // corresponding to the '1' in passwords bit
        long ci = 0;

        for (long b = cdlen * bitsinbyte - 1; b >= 0; b--)
        {
            if (password.GetBitR(b))
            {
                SetBitR(cryptdata, ci, cleardata.GetBitR(b));
                ci++;
            }
        }
        // second loop: fill crypt array with bits from cleardata 
        // corresponding to the '0' in passwords bit
        for (long b = cdlen * bitsinbyte - 1; b >= 0; b--)
        {
            if (!password.GetBitR(b))
            {
                SetBitR(cryptdata, ci, cleardata.GetBitR(b));
                ci++;
            }
        }
        return cryptdata;
    }
    private static byte[] Cry_ScrambleBitRightDec(this byte[] cryptdata, byte[] password)
    {
        long cdlen = cryptdata.LongLength;
        byte[] cleardata = new byte[cdlen];
        long ci = 0;

        for (long b = cdlen * bitsinbyte - 1; b >= 0; b--)
        {
            if (password.GetBitR(b))
            {
                SetBitR(cleardata, b, cryptdata.GetBitR(ci));
                ci++;
            }
        }
        for (long b = cdlen * bitsinbyte - 1; b >= 0; b--)
        {
            if (!password.GetBitR(b))
            {
                SetBitR(cleardata, b, cryptdata.GetBitR(ci));
                ci++;
            }
        }
        return cleardata;
    }

    // -----------------------------------------------------------------------------------

    private static bool GetBitR(this byte[] bytearray, long bit)
    {
        return ((bytearray[(bit / bitsinbyte) % bytearray.LongLength] >>
                ((int)bit % bitsinbyte)) & 1) == 1;
    }

    private static void SetBitR(byte[] bytearray, long bit, bool set)
    {
        long bytepos = bit / bitsinbyte;
        if (bytepos < bytearray.LongLength)
        {
            int bitpos = (int)bit % bitsinbyte;
            byte adder;
            if (set)
            {
                adder = (byte)(1 << bitpos);
                bytearray[bytepos] = (byte)(bytearray[bytepos] | adder);
            }
            else
            {
                adder = (byte)(byte.MaxValue ^ (byte)(1 << bitpos));
                bytearray[bytepos] = (byte)(bytearray[bytepos] & adder);
            }
        }
    }
}
internal class InvalidMods
{
    public string FileName;
    public bool IsManaged;
    public string ErrorMessage;

    //If isManaged
    public List<string> AdditionalRefs = new List<string>();
    public string AsmGuid = null;

    internal InvalidMods(string fileName, bool isManaged, string errorMessage)
    {
        FileName = fileName;
        IsManaged = isManaged;
        ErrorMessage = errorMessage;
    }
    internal InvalidMods(string fileName, bool isManaged, string errorMessage, List<string> additionalRefs, string asmGuid)
    {
        FileName = fileName;
        IsManaged = isManaged;
        ErrorMessage = errorMessage;
        AdditionalRefs = additionalRefs;
        AsmGuid = asmGuid;
    }
}
#endif