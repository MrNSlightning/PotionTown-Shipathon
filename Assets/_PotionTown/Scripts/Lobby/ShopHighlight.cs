using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class ShopHighlight : MonoBehaviour
{
    [Header("Dükkan Bilgisi")]
    public string shopName = "Ýksir Dükkaný";

    [Header("Yazý Objesi")]
    [SerializeField] private GameObject shopTextContainer;
    [SerializeField] private TextMeshPro shopTextMesh;

    [Header("Parlaklýk Rengi")]
    public Color hoverColor = new Color(1.2f, 1.2f, 0.8f, 1f); // Hafif tatlý sarýmsý parlaklýk

    private List<SpriteRenderer> targetRenderers = new List<SpriteRenderer>();
    private List<Color> originalColors = new List<Color>();

    void Start()
    {
        if (shopTextContainer != null)
            shopTextContainer.SetActive(false);

        if (shopTextMesh != null)
            shopTextMesh.text = shopName;

        // Iþýk/efekt objeleri dýþýndaki görselleri topla
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in renderers)
        {
            // Ýsim içinde light, ef, glow geçen efekt görsellerini boyama listesine alma
            string objName = sr.gameObject.name.ToLower();
            if (!objName.Contains("light") && !objName.Contains("glow") && !objName.Contains("ef"))
            {
                targetRenderers.Add(sr);
                originalColors.Add(sr.color);
            }
        }
    }

    private void OnMouseEnter()
    {
        if (shopTextContainer != null)
            shopTextContainer.SetActive(true);

        for (int i = 0; i < targetRenderers.Count; i++)
        {
            if (targetRenderers[i] != null)
                targetRenderers[i].color = hoverColor;
        }
    }

    private void OnMouseExit()
    {
        if (shopTextContainer != null)
            shopTextContainer.SetActive(false);

        for (int i = 0; i < targetRenderers.Count; i++)
        {
            if (targetRenderers[i] != null)
                targetRenderers[i].color = originalColors[i];
        }
    }
}