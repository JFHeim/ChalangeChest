// using HarmonyLib;
//
// namespace ChallengeChest.Patch;
//
// [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
// [HarmonyWrapSafe]
// public static class MobIcons
// {
//     private static bool _done;
//     public static readonly Dictionary<string, Sprite> Icons = [];
//
//     [HarmonyPostfix, UsedImplicitly]
//     private static void Postfix()
//     {
//         if (_done) return;
//         foreach (var go in ZNetScene.instance.m_namedPrefabs.Values)
//         {
//             if (!go.GetComponent<Character>()) continue;
//             RequestSprite(go);
//         }
//
//         _done = true;
//     }
//
//     private static void RequestSprite(GameObject go)
//     {
//         var path = Path.Combine(Utils_.GetSaveDataPath(FileHelpers.FileSource.Local), "CashedIcons");
//         if (!Directory.Exists(path)) Directory.CreateDirectory(path);
//         path = Path.Combine(path, $"{go.name}.png");
//         Sprite sprite;
//         if (File.Exists(path))
//         {
//             Debug($"Image at {path} already exists, loading...");
//             var texture = new Texture2D(64, 64);
//             texture.LoadImage(File.ReadAllBytes(path));
//             sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
//         }
//         else
//         {
//             sprite = Snapshot(go);
//             File.WriteAllBytes(path, sprite.texture.EncodeToPNG());
//         }
//
//         Icons[$"cc_Temp_MapMobIcon_{go.name}"] = sprite;
//
//         Debug($"Saved image at {path}");
//     }
//
//     public static Sprite Snapshot(GameObject item, float lightIntensity = 1.3f,
//         Quaternion? cameraRotation = null,
//         Quaternion? itemRotation = null)
//     {
//         const int layer = 30;
//
//         var camera = new GameObject("Camera", typeof(Camera)).GetComponent<Camera>();
//         camera.backgroundColor = Color.clear;
//         camera.clearFlags = CameraClearFlags.SolidColor;
//         camera.fieldOfView = 0.5f;
//         camera.farClipPlane = 10000000;
//         camera.cullingMask = 1 << layer;
//         camera.transform.rotation = cameraRotation ?? Quaternion.Euler(90, 0, 45);
//
//         var topLight = new GameObject("Light", typeof(Light)).GetComponent<Light>();
//         topLight.transform.rotation = Quaternion.Euler(150, 0, -5f);
//         topLight.type = LightType.Directional;
//         topLight.cullingMask = 1 << layer;
//         topLight.intensity = lightIntensity;
//
//         Rect rect = new(0, 0, 64, 64);
//
//         GameObject visual;
//         if (item.transform.Find("attach") is { } attach)
//         {
//             visual = Instantiate(attach.gameObject);
//         }
//         else
//         {
//             ZNetView.m_forceDisableInit = true;
//             visual = Instantiate(item.gameObject);
//             ZNetView.m_forceDisableInit = false;
//         }
//
//         if (itemRotation is not null)
//         {
//             visual.transform.rotation = itemRotation.Value;
//         }
//
//         foreach (var child in visual.GetComponentsInChildren<Transform>())
//         {
//             child.gameObject.layer = layer;
//         }
//
//         var renderers = visual.GetComponentsInChildren<Renderer>();
//         var min = renderers.Aggregate(Vector3.positiveInfinity,
//             (cur, renderer) => renderer is ParticleSystemRenderer ? cur : Vector3.Min(cur, renderer.bounds.min));
//         var max = renderers.Aggregate(Vector3.negativeInfinity,
//             (cur, renderer) => renderer is ParticleSystemRenderer ? cur : Vector3.Max(cur, renderer.bounds.max));
//         var size = max - min;
//
//         camera.targetTexture = RenderTexture.GetTemporary((int)rect.width, (int)rect.height);
//         var maxDim = Max(size.x, size.z);
//         var minDim = Min(size.x, size.z);
//         var yDist = (maxDim + minDim) / Sqrt(2) / Tan(camera.fieldOfView * Deg2Rad);
//         var cameraTransform = camera.transform;
//         cameraTransform.position = ((min + max) / 2) with { y = max.y } + new Vector3(0, yDist, 0);
//         topLight.transform.position = cameraTransform.position + new Vector3(-2, 0, 0.2f) / 3 * -yDist;
//
//         camera.Render();
//
//         var currentRenderTexture = RenderTexture.active;
//         RenderTexture.active = camera.targetTexture;
//
//         Texture2D texture = new((int)rect.width, (int)rect.height, TextureFormat.RGBA32, false);
//         texture.ReadPixels(rect, 0, 0);
//         texture.Apply();
//
//         RenderTexture.active = currentRenderTexture;
//
//
//         DestroyImmediate(visual);
//         camera.targetTexture.Release();
//
//         Destroy(camera);
//         Destroy(topLight);
//         return Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f));
//     }
// }