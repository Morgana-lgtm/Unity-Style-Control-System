using UnityEngine;

[CreateAssetMenu(fileName = "StylePreset", menuName = "Style/Preset")]
public class StylePresetAsset : ScriptableObject
{
    public float stylization;
    public float darkness;
    public Color tint = Color.white;
    public string promptModifier;
}