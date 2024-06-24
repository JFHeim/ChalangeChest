//TODO: ESP debug



// using System.Text;
// using BepInEx.Bootstrap;
// using Visualization;
//
// namespace ChallengeChest.Compatibility.ESP;
//
// [PublicAPI]
// public class EspApi
// {
//     private static Dictionary<EventData, GameObject> _visuals = new();
//     public static bool IsLoaded() => Chainloader.PluginInfos.Keys.Contains("esp");
//
//     public static void DrawEventData(GameObject go, EventData data)
//     {
//         if (!IsLoaded() || go == null || data == null) return;
//         if (_visuals.TryGetValue(data, out var go2))
//         {
//             Destroy(go2);
//             _visuals.Remove(data);
//         }
//
//         EspApiRaw.DrawEventData(go, data);
//     }
// }
//
// static class EspApiRaw
// {
//     public static GameObject DrawEventData(GameObject go, EventData data)
//     {
//         var sphere = Draw.DrawSphere("EffectAreaPrivateArea", go, data.range);
//         var sb = new StringBuilder();
//         sb.AppendLine($"ID: {data.id}");
//         sb.AppendLine($"Difficulty: {data.difficulty}");
//         sb.AppendLine($"Range: {data.range}");
//         sb.AppendLine($"Time left: {data.time}");
//
//         Draw.AddText(sphere, "ChallengeChest", sb.ToString());
//
//         return sphere;
//     }
// }