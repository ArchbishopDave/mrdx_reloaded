﻿using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X86;
using Reloaded.Mod.Interfaces;
using System.Diagnostics;
using System.Runtime.InteropServices;
using MRDX.Qol.SkipDrillAnim.Template;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Memory.Sources;

namespace MRDX.Qol.SkipDrillAnim;

/// <summary>
/// Your mod logic goes here.
/// </summary>
public class Mod : ModBase // <= Do not Remove.
{
    /// <summary>
    /// Provides access to the mod loader API.
    /// </summary>
    private readonly IModLoader _modLoader;

    /// <summary>
    /// Provides access to the Reloaded.Hooks API.
    /// </summary>
    /// <remarks>This is null if you remove dependency on Reloaded.SharedLib.Hooks in your mod.</remarks>
    private readonly IReloadedHooks? _hooks;

    /// <summary>
    /// Provides access to the Reloaded logger.
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    /// Entry point into the mod, instance that created this class.
    /// </summary>
    private readonly IMod _owner;

    /// <summary>
    /// The configuration of the currently executing mod.
    /// </summary>
    private readonly IModConfig _modConfig;
    
    private const nint UserInputOffset = 0x3723B0;
    private static nuint _inputPtr;

    private const string ShouldSkipTrainingSignature = "33 C0 39 41 ?? 0F 95 C0";
    // private const nuint TrainingIsDoneOffset = 0xC4EE0;

    private const int SkipButton = 0x10; // Triangle button
    private const int AltSkipButton = 0x40; // X button / Back button

    private static bool _isAutoSkipEnabled;

    private IHook<IsTrainingDone>? _hook;

    public Mod(ModContext context)
    {
        _modLoader = context.ModLoader;
        _hooks = context.Hooks;
        _logger = context.Logger;
        _owner = context.Owner;
        _modConfig = context.ModConfig;
        _isAutoSkipEnabled = context.Configuration.AutoSkip;

        // For more information about this template, please see
        // https://reloaded-project.github.io/Reloaded-II/ModTemplate/

        // If you want to implement e.g. unload support in your mod,
        // and some other neat features, override the methods in ModBase.

        var thisProcess = Process.GetCurrentProcess();
        var baseAddr = thisProcess.MainModule!.BaseAddress;
        var baseAddress = new UIntPtr(BitConverter.ToUInt64(BitConverter.GetBytes(baseAddr.ToInt64()), 0));
        _inputPtr = UIntPtr.Add(baseAddress, UserInputOffset.ToInt32());

        _modLoader.GetController<IStartupScanner>().TryGetTarget(out var startupScanner);
        startupScanner?.AddMainModuleScan(ShouldSkipTrainingSignature, 
            result => _hook = _hooks!.CreateHook<IsTrainingDone>(ShouldSkipTraining, (long)baseAddress + result.Offset).Activate());
    }

    public override void ConfigurationUpdated(Config configuration)
    {
        _isAutoSkipEnabled = configuration.AutoSkip;
        _logger!.WriteLine($"[{_modConfig.ModId}] Config Updated: Applying");
    }
    
    private static bool ShouldSkipTraining([In] nint self)
    {
        if (_isAutoSkipEnabled)
            return true;
        Memory.Instance.Read<int>(_inputPtr, out var input);
        return (input & (SkipButton | AltSkipButton)) != 0;
    }

    [Function(CallingConventions.MicrosoftThiscall)]
    private delegate bool IsTrainingDone([In] nint self);

    #region For Exports, Serialization etc.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Mod() { }
#pragma warning restore CS8618
    #endregion
}
