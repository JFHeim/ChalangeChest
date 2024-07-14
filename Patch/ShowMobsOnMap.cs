// using HarmonyLib;
//
// namespace ChallengeChest.Patch;
//
// [HarmonyPatch(typeof(Minimap), nameof(Minimap.UpdatePins))]
// [HarmonyWrapSafe]
// file static class ShowMobsOnMap
// {
//     [HarmonyPrefix, UsedImplicitly]
//     private static void Prefix(Minimap __instance)
//     {
//         var icon = RegisterPrefabs.Sprite("cc_IconChest");
//         List<Minimap.PinData> pins = [];
//         Debug($"ShowMobsOnMap 0");
//         foreach (var pinData in __instance.m_pins)
//         {
//             if (pinData.m_icon?.name.StartsWith("cc_IconChest") != true) continue;
//             __instance.DestroyPinMarker(pinData);
//         }
//
//         Debug($"ShowMobsOnMap 1");
//
//         __instance.m_pins.RemoveAll(pinData => pinData.m_icon?.name.StartsWith("cc_IconChest") == true);
//         Debug($"ShowMobsOnMap 2");
//
//         foreach (var eventPin in __instance.m_pins.Where(p => EventSetup.Icons.Values.Contains(p.m_icon)))
//         {
//             //Is player close enough
//             Debug($"ShowMobsOnMap 3");
//             if (eventPin.m_pos.DistanceXZ(m_localPlayer.transform.position) > EventSetup.Range + 80) continue;
//             Debug($"ShowMobsOnMap 4");
//
//             foreach (var mob in Character.s_characters) 
//             {
//                 Debug($"ShowMobsOnMap 5");
//                 if (mob.m_nview.GetZDO().GetVec3("ChallengeChestPos", Vector3.zero) == Vector3.zero) continue;
//                 Debug($"ShowMobsOnMap 6");
//                 var mobPin = new Minimap.PinData
//                 {
//                     m_pos = mob.transform.position,
//                     m_icon = icon,
//                     m_animate = true,
//                     m_save = false
//                 };
//                 pins.Add(mobPin);
//                 Debug($"ShowMobsOnMap 7");
//             }
//
//             Debug($"ShowMobsOnMap 8");
//         }
//
//         Debug($"ShowMobsOnMap 9");
//
//         __instance.m_pins.AddRange(pins);
//         Debug($"ShowMobsOnMap 10");
//     }
//
//     private static readonly bool _debug = true;
//
//     private static void Debug(object s)
//     {
//         if (!_debug) return;
//         ModBase.Debug(s);
//     }
// }