#if !Mini
using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace MSCLoader;
internal class RequiredList
{
    public List<string> mods = new List<string>();
    public List<string> references = new List<string>();
}

internal class ModVersions
{
    public List<MetaVersion> versions = new List<MetaVersion>();
    public string notFound = string.Empty;
}
internal class MetaVersion
{
    public string mod_id;
    public string mod_version;
    public byte mod_type;
    public int mod_rev;
    public string cached_date;
}
internal class RefVersions
{
    public List<RefVersion> versions = new List<RefVersion>();
}
internal class RefVersion
{
    public string ref_id;
    public string ref_version;
    public byte ref_type;
    public string cached_date;

}

//Metadata V3
internal class MSCLData
{
    public string modID;
    public string description;
    public List<string> links = new List<string>();
    public string icon;
    public MinimumRequirements minimumRequirements = new MinimumRequirements();
    public ModConflicts modConflicts = new ModConflicts();
    public string sign = string.Empty;
    public int type = 1;
    public string msg;
    public int rev = 0;
}
internal class MinimumRequirements
{
    public string MSCLoaderVer;
    public int MSCbuildID = 0;
    public bool disableIfVer;
}

internal class ModConflicts
{
    public List<string> modIDs = new List<string>();
    public string customMessage;
    public bool disableIfConflict = false;
}

#endif