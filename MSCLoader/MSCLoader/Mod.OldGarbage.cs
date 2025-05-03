#if !Mini
using System;
using System.ComponentModel;

namespace MSCLoader;

public partial class Mod
{
    /// <summary>
    ///     [DON'T USE] It's useless
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete("This doesn't do anything")]
    public ModSettings modSettings;

    /// <summary>
    ///     Constructor DON'T USE
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool proSettings;

    /// <summary>
    ///     pro BS
    /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
    public Mod() => modSettings = new ModSettings(this);
#pragma warning restore CS0618 // Type or member is obsolete
    //Here is old pre 1.2 functions used here only for backwards compatibility
    //Obsoleted (internal doesn't break old code)

    internal virtual bool LoadInMenu => false;
    internal virtual void OnNewGame() { }

    /// <summary>
    /// [DON'T USE] 
    /// </summary>
    public virtual void OnNewGame()
    {
    }

    /// <summary>
    ///     Compatibility only: same as OnMenuLoad()
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public virtual void MenuOnLoad()
    {
    }

    /// <summary>
    ///     Compatibility only: same as SecondPassOnLoad()
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public virtual void PostLoad()
    {
    }

    /// <summary>
    ///     Called once, when save and quit.
    /// </summary>
    public virtual void OnSave()
    {
    }

    /// <summary>
    ///     Standard unity OnGUI().
    /// </summary>
    /// <example>
    ///     See: https://docs.unity3d.com/500/Documentation/Manual/GUIScriptingGuide.html
    /// </example>
    public virtual void OnGUI()
    {
    }

    /// <summary>
    ///     Called once every frame
    ///     (standard unity Update()).
    /// </summary>
    public virtual void Update()
    {
    }

    /// <summary>
    ///     Called once every fixed frame
    ///     (standard unity FixedUpdate()).
    /// </summary>
    public virtual void FixedUpdate()
    {
    }

    /// <summary>
    ///     Called once in main menu (only when LoadInMenu is true).
    /// </summary>
    public virtual void OnMenuLoad()
    {
        if (LoadInMenu)
            ModConsole.Error(
                $"<b>LoadInMenu</b> is set to <b>true</b> for mod: <b>{ID}</b> but <b>OnMenuLoad()</b> is empty.");
    }

    /// <summary>
    ///     Called once as soon as GAME scene is loaded.
    /// </summary>
    public virtual void PreLoad()
    {
    }

    /// <summary>
    ///     Called once, after GAME scene is fully loaded.
    /// </summary>
    public virtual void OnLoad()
    {
    }

    /// <summary>
    ///     Called once, after ALL mods has finished OnLoad() and when SecondPass is set to true
    ///     (Executed still before first pass of Update(), but NOT exectued if OnLoad() failed with error)
    /// </summary>
    public virtual void SecondPassOnLoad()
    {
        PostLoad();
    }

    /// <summary>
    /// Constructor only for compatibiltiy for pro settings.
    /// </summary>
    public virtual void OnModEnabled()
    {
    }

    /// <summary>
    ///     Called once when mod has been disabled in settings
    /// </summary>
    public virtual void OnModDisabled()
    {
    }

    /// <summary>
    ///     All settings should be created here.
    /// </summary>
    public virtual void ModSettings()
    {
    }

    /// <summary>
    ///     Called after saved settings have been loaded from file.
    /// </summary>
    public virtual void ModSettingsLoaded()
    {
    }
}
#endif