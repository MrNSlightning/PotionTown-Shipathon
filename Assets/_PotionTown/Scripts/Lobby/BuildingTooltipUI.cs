using UnityEngine;
using TMPro;
using PotionShop;

/// <summary>
/// Mouse ile binaların üzerine gelindiğinde ismi ekranda gösteren UI Yöneticisi.
/// (Artık fareyi takip etmez, UI'da nereye koyarsanız orada çıkar, örneğin ekranın ortası)
/// </summary>
public class BuildingTooltipUI : MonoBehaviour
{
    public static BuildingTooltipUI Instance { get; private set; }

    [Header("UI Referansları")]
    [Tooltip("Tooltip arka planı (Panel)")]
    public RectTransform tooltipBackground;
    [Tooltip("Binanın isminin yazılacağı Text")]
    public TextMeshProUGUI buildingNameText;

    private string _currentRawBuildingName;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        var graphics = GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
        foreach (var g in graphics)
        {
            g.raycastTarget = false;
        }

        HideTooltip(); // Başlangıçta gizle
    }

    private void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
    }

    private void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
    }

    private void HandleLanguageChanged(GameLanguage lang)
    {
        UpdateLocalizedText();
    }

    private void UpdateLocalizedText()
    {
        if (buildingNameText != null && !string.IsNullOrEmpty(_currentRawBuildingName))
        {
            buildingNameText.text = LocalizationManager.Get(_currentRawBuildingName);
            UIThemeHelper.ApplyNewRocker(buildingNameText);
        }
    }

    /// <summary>
    /// Tooltip'i gösterir ve içeriğini ayarlar.
    /// </summary>
    public void ShowTooltip(string buildingName)
    {
        _currentRawBuildingName = buildingName;
        UpdateLocalizedText();

        if (tooltipBackground != null)
        {
            tooltipBackground.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Tooltip'i gizler.
    /// </summary>
    public void HideTooltip()
    {
        _currentRawBuildingName = null;
        if (tooltipBackground != null)
        {
            tooltipBackground.gameObject.SetActive(false);
        }
    }
}
