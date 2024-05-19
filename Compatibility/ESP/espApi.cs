using System.Text;
using BepInEx.Bootstrap;

namespace ChalangeChest.Compatibility.ESP;

[PublicAPI]
public class espApi
{
    private static Dictionary<EventData, GameObject> visuals = new();
    public static bool IsLoaded() => Chainloader.PluginInfos.Keys.Contains("esp");

    public static void DrawEventData(GameObject go, EventData data)
    {
        if (!IsLoaded() || go == null || data == null) return;
        if (visuals.TryGetValue(data, out var go2))
        {
            Destroy(go2);
            visuals.Remove(data);
        }

        espApi_RAW.DrawEventData(go, data);
    }
}

static class espApi_RAW
{
    public static GameObject DrawEventData(GameObject go, EventData data)
    {
        var sphere = Visualization.Draw.DrawSphere("EffectAreaPrivateArea", go, data.range);
        var sb = new StringBuilder();
        sb.AppendLine($"ID: {data.id}");
        sb.AppendLine($"Difficulty: {data.difficulty}");
        sb.AppendLine($"Range: {data.range}");
        sb.AppendLine($"Time left: {data.time}");

        Visualization.Draw.AddText(sphere, "ChalangeChest", sb.ToString());

        return sphere;
    }
}