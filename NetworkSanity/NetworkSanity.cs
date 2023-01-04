using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using ExitGames.Client.Photon;
using HarmonyLib;
using MelonLoader;
using MelonLoader.Preferences;
using NetworkSanity.Core;
using Photon.Realtime;
using UnhollowerBaseLib;
using UnityEngine;
using VRC.Core;

namespace NetworkSanity
{
    public static class BuildInfo
    {
        public const string Name = "NetworkSanity EAC-Melon";
        public const string Author = "Made by RequiDev Patched by NONE";
        public const string Company = null;
        public const string Version = "2.0.0";
        public const string DownloadLink = "https://github.com/pocketnone/NetworkSanity";
    }

    public class NetworkSanity : MelonMod
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void EventDelegate(IntPtr thisPtr, IntPtr eventDataPtr, IntPtr nativeMethodInfo);
        private readonly List<object> _ourPinnedDelegates = new();

        private static readonly List<ISanitizer> Sanitizers = new List<ISanitizer>();
        private static MelonPreferences_Category PreferencesCategory;
        private static MelonPreferences_Entry<float> PreferenceFPS;
        private static MelonPreferences_Entry<int> PreferencePing;
        private static float VarianceFPS;
        private static int VariancePing;
        private const string PreferencesIdentifier = "NONEFPS";

        public new static HarmonyLib.Harmony Harmony { get; private set; }

        public override void OnApplicationStart()
        {
            Harmony = HarmonyInstance;

            
            IEnumerable<Type> types;
            try
            {
                types = Assembly.GetExecutingAssembly().GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                types = e.Types.Where(t => t != null);
            }

            foreach (var t in types)
            {
                if (t.IsAbstract)
                    continue;
                if (!typeof(ISanitizer).IsAssignableFrom(t))
                    continue;

                var sanitizer = Activator.CreateInstance(t) as ISanitizer;
                Sanitizers.Add(sanitizer);
                MelonLogger.Msg($"Added new Sanitizer: {t.Name}");
            }
            MelonLogger.Msg("\n");
            MelonLogger.Msg("POST EAC! THanks to EAC Melon for having Mods back <3");
            MelonLogger.Msg("Patched by NONE#0777");
            MelonLogger.Msg("Join my Discord: https://discord.gg/CTdUbBGQq3");
            MelonLogger.Msg("\n");
            unsafe
            {
                var originalMethodPtr = *(IntPtr*)(IntPtr)UnhollowerUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(typeof(LoadBalancingClient).GetMethod(nameof(LoadBalancingClient.OnEvent))).GetValue(null);

                EventDelegate originalDelegate = null;

                void OnEventDelegate(IntPtr thisPtr, IntPtr eventDataPtr, IntPtr nativeMethodInfo)
                {
                    if (eventDataPtr == IntPtr.Zero)
                    {
                        originalDelegate(thisPtr, eventDataPtr, nativeMethodInfo);
                        return;
                    }

                    try
                    {
                        var eventData = new EventData(eventDataPtr);
                        if (OnEventPatch(new LoadBalancingClient(thisPtr), eventData))
                            originalDelegate(thisPtr, eventDataPtr, nativeMethodInfo);
                    }
                    catch (Exception ex)
                    {
                        originalDelegate(thisPtr, eventDataPtr, nativeMethodInfo);
                        MelonLogger.Error(ex.Message);
                    }
                }

                var patchDelegate = new EventDelegate(OnEventDelegate);
                _ourPinnedDelegates.Add(patchDelegate);

                MelonUtils.NativeHookAttach((IntPtr)(&originalMethodPtr), Marshal.GetFunctionPointerForDelegate(patchDelegate));
                originalDelegate = Marshal.GetDelegateForFunctionPointer<EventDelegate>(originalMethodPtr);
            }

            unsafe
            {
                var originalMethodPtr = *(IntPtr*)(IntPtr)UnhollowerUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(typeof(VRCNetworkingClient).GetMethod(nameof(VRCNetworkingClient.OnEvent))).GetValue(null);

                EventDelegate originalDelegate = null;

                void OnEventDelegate(IntPtr thisPtr, IntPtr eventDataPtr, IntPtr nativeMethodInfo)
                {
                    if (eventDataPtr == IntPtr.Zero)
                    {
                        originalDelegate(thisPtr, eventDataPtr, nativeMethodInfo);
                        return;
                    }

                    var eventData = new EventData(eventDataPtr);
                    if (VRCNetworkingClientOnPhotonEvent(eventData))
                        originalDelegate(thisPtr, eventDataPtr, nativeMethodInfo);
                }

                var patchDelegate = new EventDelegate(OnEventDelegate);
                _ourPinnedDelegates.Add(patchDelegate);

                MelonUtils.NativeHookAttach((IntPtr)(&originalMethodPtr), Marshal.GetFunctionPointerForDelegate(patchDelegate));
                originalDelegate = Marshal.GetDelegateForFunctionPointer<EventDelegate>(originalMethodPtr);
            }
          
            MelonLogger.Msg("Start Ping and FPS Spoofer Animation...");
            
            //PatchFPS & Ping
            PreferencesCategory = MelonPreferences.CreateCategory("NONEFPS", nameof(NetworkSanity));
            PreferenceFPS = PreferencesCategory.CreateEntry<float>("SpoofFPS", -1f, "FPS to spoof", (string)null, false, false, (ValueValidator)null, (string)null);
            PreferencePing = PreferencesCategory.CreateEntry<int>("SpoofPing", -1, "Ping to spoof", (string)null, false, false, (ValueValidator)null, (string)null);
            try
            {
                PreferencesCategory.DeleteEntry("SpoofFPS");
                PreferenceFPS = PreferencesCategory.CreateEntry<float>("SpoofFPS", 0.0f, (string)null, (string)null, false, false, (ValueValidator)null, (string)null);
                ((MelonBase)this).HarmonyInstance.Patch((MethodBase)typeof(Time).GetProperty("smoothDeltaTime").GetGetMethod(), new HarmonyMethod(typeof(NetworkSanity).GetMethod("PatchFPS", BindingFlags.Static | BindingFlags.NonPublic)), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);

                new Thread((ThreadStart)(() =>
                {
                    try
                    {
                        while (true)
                        {
                            Thread.Sleep(1000);
                            VarianceFPS = 10;
                            PatchFPS(ref VarianceFPS);
                            Thread.Sleep(1000);
                            VarianceFPS = 20;
                            PatchFPS(ref VarianceFPS);
                            Thread.Sleep(1000);
                            VarianceFPS = 30;
                            PatchFPS(ref VarianceFPS);
                            Thread.Sleep(1000);
                            VarianceFPS = 40f;
                            PatchFPS(ref VarianceFPS);
                            Thread.Sleep(1000);
                            VarianceFPS = 50f;
                            PatchFPS(ref VarianceFPS);
                            Thread.Sleep(1000);
                            VarianceFPS = 40f;
                            PatchFPS(ref VarianceFPS);
                            Thread.Sleep(1000);
                            VarianceFPS = 30;
                            PatchFPS(ref VarianceFPS);
                            Thread.Sleep(1000);
                            VarianceFPS = 2;
                            PatchFPS(ref VarianceFPS);
                        }
                        MelonLogger.Msg("FPS Spoof on");
                    }
                    catch (Exception ex)
                    { ((MelonBase)this).LoggerInstance.Error(string.Format("Failed to patch FPS: {0}", (object)ex)); }
                })).Start();
            }
            catch (Exception ex) { ((MelonBase)this).LoggerInstance.Error(string.Format("Failed to patch FPS: {0}", (object)ex)); }
            try
            {
                PreferencesCategory.DeleteEntry("SpoofPing");
                PreferencePing = PreferencesCategory.CreateEntry<int>("SpoofPing", 0, (string)null, (string)null, false, false, (ValueValidator)null, (string)null);
                ((MelonBase)this).HarmonyInstance.Patch((MethodBase)typeof(PhotonPeer).GetProperty("RoundTripTime").GetGetMethod(), new HarmonyMethod(typeof(NetworkSanity).GetMethod("PatchPing", BindingFlags.Static | BindingFlags.NonPublic)), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
                ((MelonBase)this).LoggerInstance.Msg("Start Ping Animation");
                new Thread((ThreadStart)(() =>
                {
                    try
                    {
                        PreferencesCategory.DeleteEntry("SpoofPing");
                        while (true)
                        {
                            Thread.Sleep(1000);
                            VariancePing = 777;
                            PatchPing(ref VariancePing);                    
                            Thread.Sleep(1000);
                            VariancePing = 77;
                            PatchPing(ref VariancePing);
                            Thread.Sleep(1000);
                            VariancePing = 7;
                            PatchPing(ref VariancePing);
                            Thread.Sleep(1000);
                            VariancePing = 77;
                            PatchPing(ref VariancePing);
                        }
                        MelonLogger.Msg("Ping Spoof on");
                    }
                    catch (Exception ex)
                    {
                        ((MelonBase)this).LoggerInstance.Error(string.Format("Failed to patch Ping: {0}", (object)ex));
                    }
                })).Start();
            } catch { }
        }

        private static bool OnEventPatch(LoadBalancingClient loadBalancingClient, EventData eventData)
        {
            foreach (var sanitizer in Sanitizers)
            {
                if (sanitizer.OnPhotonEvent(loadBalancingClient, eventData))
                    return false;
            }
            return true;
        }

        private static bool VRCNetworkingClientOnPhotonEvent(EventData eventData)
        {
            foreach (var sanitizer in Sanitizers)
            {
                if (sanitizer.VRCNetworkingClientOnPhotonEvent(eventData))
                    return false;
            }
            return true;
        }

        private static bool PatchFPS(ref float __result)
        {
            __result = 1f / VarianceFPS;
            return false;
        }

        private static bool PatchPing(ref int __result)
        {
            __result = VariancePing;
            return false;
        }
    }
}
