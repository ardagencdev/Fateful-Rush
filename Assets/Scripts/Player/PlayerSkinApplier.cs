using UnityEngine;

[DefaultExecutionOrder(100)]
public class PlayerSkinApplier : MonoBehaviour
{
    [Header("Skin Data")]
    [SerializeField]
    private PlayerSkinCatalog skinCatalog;

    [Header("References")]
    [SerializeField]
    private SpriteRenderer playerSpriteRenderer;

    [SerializeField]
    private PlayerDash playerDash;

    public PlayerSkinCatalog.SkinEntry CurrentSkin
    {
        get;
        private set;
    }

    public Sprite CurrentSprite =>
        CurrentSkin != null
            ? CurrentSkin.playerSprite
            : playerSpriteRenderer != null
                ? playerSpriteRenderer.sprite
                : null;

    public Color CurrentDashTrailColor =>
        CurrentSkin != null
            ? CurrentSkin.dashTrailColor
            : Color.white;

    private void Awake()
    {
        FindMissingReferences();
        ApplySelectedSkin();
    }

    private void OnEnable()
    {
        ApplySelectedSkin();
    }

    public void ApplySelectedSkin()
    {
        if (skinCatalog == null)
        {
            Debug.LogWarning(
                "PlayerSkinApplier skinCatalog is missing.",
                this
            );
            return;
        }

        ApplySkin(skinCatalog.GetSelectedSkin());
    }

    public void ApplySkin(
        PlayerSkinCatalog.SkinEntry skin)
    {
        if (skin == null)
            return;

        FindMissingReferences();

        CurrentSkin = skin;

        if (playerSpriteRenderer != null &&
            skin.playerSprite != null)
        {
            playerSpriteRenderer.sprite =
                skin.playerSprite;
        }

        if (playerDash != null)
        {
            playerDash.ApplyTrailColor(
                skin.dashTrailColor
            );
        }
    }

    private void FindMissingReferences()
    {
        if (playerSpriteRenderer == null)
        {
            playerSpriteRenderer =
                GetComponentInChildren<SpriteRenderer>(true);
        }

        if (playerDash == null)
            playerDash = GetComponent<PlayerDash>();
    }
}
