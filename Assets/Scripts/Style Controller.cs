using UnityEngine;

public class StyleController : MonoBehaviour
{
    public Material targetMaterial;

    [Range(0, 1)] public float stylization = 0;
    [Range(0, 1)] public float darkness = 0;
    public Color colorTint = Color.white;

    void Update()
    {
        if (targetMaterial == null) return;

        targetMaterial.SetFloat("_Stylization", stylization);
        targetMaterial.SetFloat("_Darkness", darkness);
        targetMaterial.SetColor("_ColorTint", colorTint);
    }
    public void SetToon()
    {
        targetMaterial.SetFloat("_Stylization", 1);
        targetMaterial.SetFloat("_Darkness", 0);
    }

    public void SetRealistic()
    {
        targetMaterial.SetFloat("_Stylization", 0);
        targetMaterial.SetFloat("_Darkness", 0);
    }

    public void SetDark()
    {
        targetMaterial.SetFloat("_Stylization", 0.5f);
        targetMaterial.SetFloat("_Darkness", 1);
    }
}