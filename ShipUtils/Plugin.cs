using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using JetBrains.Annotations;
using LethalCompanyInputUtils.Api;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShipUtils;

[BepInPlugin("dev.starflight.shiputils", "Ship Utils", PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private static readonly InputActions Inputs = new();
    private static ManualCameraRenderer _map;
    private static List<int> _targets;
    private int _target;
    private bool _allTargets = true;
    private void Awake()
    {
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is loaded!");
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        Inputs.PreviousTarget.performed += _ =>
        {
            _target = (_target + _targets.Count - 1) % _targets.Count;
            Logger.LogInfo(_target);
            _map.SwitchRadarTargetAndSync(_targets[_target]);
        };
        Inputs.NextTarget.performed += _ =>
        {
            _target = (_target + 1) % _targets.Count;
            Logger.LogInfo(_target);
            _map.SwitchRadarTargetAndSync(_targets[_target]);
        };
        Inputs.AddToList.performed += _ =>
        {
            if (_allTargets)
            {
                _targets = [_target];
                _allTargets = false;
            }
            else
            {
                _targets.Add(_map.targetTransformIndex);
                _targets = _targets.Distinct().ToList();
            }
            var terminal = FindAnyObjectByType<Terminal>();
            terminal.screenText.text = terminal.currentText.TrimEnd('+');
        };
        Inputs.RemoveFromList.performed += _ =>
        {
            _targets.Remove(_map.targetTransformIndex);
            _allTargets = false;
            var terminal = FindAnyObjectByType<Terminal>();
            terminal.screenText.text = terminal.currentText.TrimEnd('-');
        };
        Inputs.Teleport.performed += _ =>
        {
            if (!ShipTeleporter.hasBeenSpawnedThisSession) return;
            var shipTeleporters = FindObjectsByType<ShipTeleporter>(FindObjectsSortMode.None);
            ShipTeleporter shipTeleporter = null;
            foreach (var teleporter in shipTeleporters)
            {
                if (!teleporter.isInverseTeleporter)
                {
                    shipTeleporter = teleporter;
                }
            }
            if (shipTeleporter != null)
            {
                shipTeleporter.PressTeleportButtonOnLocalClient();
            }
        };

    }
    private sealed class InputActions : LcInputActions
    {
        [InputAction("<Keyboard>/leftArrow", Name = "Previous Target")]
        internal InputAction PreviousTarget { get; [UsedImplicitly] set; }
        [InputAction("<Keyboard>/rightArrow", Name = "Next Target")]
        internal InputAction NextTarget { get; [UsedImplicitly] set; }
        [InputAction("<Keyboard>/numpadPlus", Name = "Add To List")]
        internal InputAction AddToList { get; [UsedImplicitly] set; }
        [InputAction("<Keyboard>/numpadMinus", Name = "Remove From List")]
        internal InputAction RemoveFromList { get; [UsedImplicitly] set; }
        [InputAction("<Keyboard>/home", Name = "Teleport")]
        internal InputAction Teleport { get; [UsedImplicitly] set; }

    }

    [HarmonyPatch(typeof(StartOfRound))]
    public static class Patches
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void StartPatches([SuppressMessage("ReSharper", "InconsistentNaming")] ref Terminal __instance)
        {
            _map = StartOfRound.Instance.mapScreen;
            _targets = Enumerable.Range(0, _map.radarTargets.Count).ToList();
        }
    }
}