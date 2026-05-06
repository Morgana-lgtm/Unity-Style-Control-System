using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

//sd接入
public static class SDGenerator
{
    public static IEnumerator Generate(string prompt, System.Action<Texture2D> onDone)
    {
        string url = "http://127.0.0.1:7860/sdapi/v1/txt2img";

        string json = "{\"prompt\":\"" + prompt + "\",\"steps\":20}";

        UnityWebRequest req = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            Texture2D tex = Decode(req.downloadHandler.text);
            onDone?.Invoke(tex);
        }
        else
        {
            Debug.LogError(req.error);
        }
    }

    static Texture2D Decode(string json)
    {
        SDResponse data = JsonUtility.FromJson<SDResponse>(json);
        byte[] bytes = System.Convert.FromBase64String(data.images[0]);

        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(bytes);
        tex.Apply();

        return tex;
    }

    [System.Serializable]
    private class SDResponse
    {
        public string[] images;
    }
}


public class StyleToolWindow : EditorWindow
{
    // 数据

    // 材质列表
    public List<Material> materials = new List<Material>();

    // Preset列表
    public List<StylePresetAsset> presets = new List<StylePresetAsset>();

    // 当前参数
    float stylization = 0f;
    float darkness = 0f;
    Color colorTint = Color.white;
    float rampV = 0.5f;
    public string promptModifier;

    // UI状态
    int selectedPresetIndex = 0;
    string[] presetNames = new string[0];
    public Texture2D styleTexture;
    bool isGenerating = false;
    Texture2D previewTexture;

    string prompt = "anime style, soft lighting";

    // 打开窗口

    [MenuItem("Tools/Style Tool")]
    public static void ShowWindow()
    {
        GetWindow<StyleToolWindow>("Style Tool");
    }

    Color GetAverageColor(Texture2D tex)
    {
        Color[] pixels = tex.GetPixels();
        Color sum = Color.black;

        foreach (var p in pixels)
            sum += p;

        return sum / pixels.Length;
    }

    float GetBrightness(Texture2D tex)
    {
        Color[] pixels = tex.GetPixels();
        float sum = 0;

        foreach (var p in pixels)
            sum += p.grayscale;

        return sum / pixels.Length;
    }

    float GetContrast(Texture2D tex)
    {
        Color[] pixels = tex.GetPixels();

        float mean = 0;
        foreach (var p in pixels)
            mean += p.grayscale;
        mean /= pixels.Length;

        float variance = 0;
        foreach (var p in pixels)
        {
            float diff = p.grayscale - mean;
            variance += diff * diff;
        }

        variance /= pixels.Length;

        return Mathf.Clamp01(variance * 10f); // 放大一下更明显
    }

    float GetSaturation(Texture2D tex)
    {
        Color[] pixels = tex.GetPixels();
        float sum = 0;

        foreach (var p in pixels)
        {
            float max = Mathf.Max(p.r, Mathf.Max(p.g, p.b));
            float min = Mathf.Min(p.r, Mathf.Min(p.g, p.b));

            float sat = (max == 0) ? 0 : (max - min) / max;
            sum += sat;
        }

        return sum / pixels.Length;
    }

    // UI

    void OnGUI()
    {

        GUILayout.Label("Auto Tools", EditorStyles.boldLabel);
        if (previewTexture != null)
        {
            GUILayout.Label("Generated Image Preview", EditorStyles.boldLabel);
            GUILayout.Label(previewTexture, GUILayout.Width(256), GUILayout.Height(256));
        }
        if (GUILayout.Button("Scan Scene Materials"))
        {
            ScanSceneMaterials();
        }

        if (GUILayout.Button("Load Presets From Folder"))
        {
            LoadPresets();
        }

        EditorGUILayout.Space();

        // Preset 下拉

        GUILayout.Label("Presets", EditorStyles.boldLabel);

        // 更新名字
        presetNames = new string[presets.Count];
        for (int i = 0; i < presets.Count; i++)
        {
            presetNames[i] = presets[i] != null ? presets[i].name : "None";
        }

        int newIndex = EditorGUILayout.Popup("Select Style", selectedPresetIndex, presetNames);

        if (newIndex != selectedPresetIndex)
        {
            selectedPresetIndex = newIndex;

            if (presets.Count > 0 && presets[selectedPresetIndex] != null)
            {
                ApplyPreset(presets[selectedPresetIndex]);
            }
        }

        // 当前Preset信息

        if (presets.Count > 0 && selectedPresetIndex < presets.Count)
        {
            var current = presets[selectedPresetIndex];

            if (current != null)
            {
                EditorGUILayout.Space();
                GUILayout.Label("Current Preset Info", EditorStyles.boldLabel);

                EditorGUILayout.LabelField("Stylization", current.stylization.ToString("F2"));
                EditorGUILayout.LabelField("Darkness", current.darkness.ToString("F2"));
                EditorGUILayout.LabelField("Tint", current.tint.ToString());
            }
        }

        EditorGUILayout.Space();

        // 材质列表（只读显示）

        GUILayout.Label("Controlled Materials: " + materials.Count);
        GUILayout.Label("Materials (Auto)", EditorStyles.boldLabel);

        foreach (var mat in materials)
        {
            EditorGUILayout.ObjectField(mat, typeof(Material), false);
        }

        EditorGUILayout.Space();

        // 参数控制（实时）

        GUILayout.Label("Parameters (Realtime)", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();

        stylization = EditorGUILayout.Slider("Stylization", stylization, 0, 1);
        darkness = EditorGUILayout.Slider("Darkness", darkness, 0, 1);
        colorTint = EditorGUILayout.ColorField("Color Tint", colorTint);
        rampV = EditorGUILayout.Slider("Ramp Position", rampV, 0, 1);

        if (EditorGUI.EndChangeCheck())
        {
            ApplyToMaterials(); // 实时更新
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Apply To All Materials"))
        {
            ApplyToMaterials();
        }

        EditorGUILayout.Space();
        GUILayout.Label("Style Input", EditorStyles.boldLabel);
        prompt = EditorGUILayout.TextArea(prompt, GUILayout.Height(40));

        styleTexture = (Texture2D)EditorGUILayout.ObjectField("Style Texture", styleTexture, typeof(Texture2D), false);

        if (GUILayout.Button("Generate Style From Prompt") && !isGenerating)
        {
            string finalPrompt = prompt;

            if (presets.Count > 0 && selectedPresetIndex < presets.Count)
            {
                var preset = presets[selectedPresetIndex];
                if (preset != null)
                {
                    finalPrompt += ", " + preset.promptModifier;
                }
            }

            EditorCoroutineUtility.StartCoroutineOwnerless(
                SDGenerator.Generate(finalPrompt, (tex) =>
                {
                    previewTexture = tex;
                    ExtractFromTexture(tex);
                    ApplyToMaterials();
                    isGenerating = false;
                })
            );

            isGenerating = true;
        }
        if (isGenerating)
        {
            EditorGUILayout.HelpBox("Generating...", MessageType.Info);
        }


        if (GUILayout.Button("Extract Style From Image"))
        {
            if (styleTexture != null)
            {
                ExtractFromTexture(styleTexture);
                ApplyToMaterials();
            }
        }
        if (GUILayout.Button("Preview Extracted Values"))
        {
            Debug.Log($"Stylization: {stylization}");
            Debug.Log($"Darkness: {darkness}");
            Debug.Log($"Tint: {colorTint}");
        }

    }


    // 自动扫描材质

    void ScanSceneMaterials()
    {
        materials.Clear();

        Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);

        foreach (var r in renderers)
        {
            foreach (var mat in r.sharedMaterials)
            {
                if (mat == null) continue;

                // 🔥 核心过滤逻辑
                if (mat.HasProperty("_Stylization") &&
                mat.HasProperty("_Darkness") &&
                mat.HasProperty("_ColorTint") &&
                mat.HasProperty("_RampV"))
                {
                    if (!materials.Contains(mat))
                    {
                        materials.Add(mat);
                    }
                }
            }
        }

        Debug.Log("Filtered Materials: " + materials.Count);
    }



    // 自动加载Preset
    void LoadPresets()
    {
        presets.Clear();

        string[] guids = AssetDatabase.FindAssets("t:StylePresetAsset");

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            StylePresetAsset preset = AssetDatabase.LoadAssetAtPath<StylePresetAsset>(path);

            if (preset != null)
            {
                presets.Add(preset);
            }
        }

        Debug.Log("Loaded Presets: " + presets.Count);
    }

    // 应用Preset

    void ApplyPreset(StylePresetAsset preset)
    {
        stylization = preset.stylization;
        darkness = preset.darkness;
        colorTint = preset.tint;

        ApplyToMaterials();
    }



    // 应用到材质

    void ApplyToMaterials()
    {
        foreach (var mat in materials)
        {
            if (mat == null) continue;

            mat.SetFloat("_Stylization", stylization);
            mat.SetFloat("_Darkness", darkness);
            mat.SetColor("_ColorTint", colorTint);
            mat.SetFloat("_RampV", rampV);
        }
    }
    void OnEnable()
    {
        if (materials.Count == 0)
            ScanSceneMaterials();

        if (presets.Count == 0)
            LoadPresets();
    }



    void ExtractFromTexture(Texture2D tex)
    {
        if (!tex.isReadable)
        {
            Debug.LogError("Texture is not readable! Enable Read/Write in import settings.");
            return;
        }

        Color avg = GetAverageColor(tex);
        float brightness = GetBrightness(tex);
        float contrast = GetContrast(tex);
        float saturation = GetSaturation(tex);
        rampV = Mathf.Lerp(0.2f, 0.8f, saturation);

        // ===== 映射 =====

        colorTint = Color.Lerp(Color.white, avg, saturation);

        // 越亮 → 越不压暗
        darkness = Mathf.Clamp01((1.0f - brightness) * 0.8f);

        // 越饱和 → 越卡通
        stylization = Mathf.Clamp01(saturation);

        // 阴影硬度（shader里有 _ShadowStep 的话可以加）
        //shadowStep = Mathf.Lerp(0.4f, 0.6f, contrast);


        //ApplyShadowStep(shadowStep);
    }

}