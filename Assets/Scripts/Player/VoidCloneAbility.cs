using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class VoidCloneAbility : MonoBehaviour
{
    public static Transform ActiveCloneTarget { get; private set; }
    public static bool HasActiveClone => ActiveCloneTarget != null;

    [Header("Clone")]
    public GameObject clonePrefab;
    public float cloneDuration = 3f;

    [Header("Cooldown")]
    [Tooltip("Starts only after the clone disappears.")]
    public float cloneCooldown = 12f;

    [Header("UI")]
    public Button cloneButton;
    public Image cloneButtonImage;
    public Image cooldownFill;
    public TMP_Text cooldownText;
    public Sprite readySprite;
    public Sprite usedSprite;

    [Header("References")]
    public PlayerMovement playerMovement;
    public SoundManager soundManager;
    public PlayerSkinApplier playerSkinApplier;

    private bool canUseClone = true;
    private bool cloneActive;
    private bool gameOverHandled;
    private bool cooldownActive;
    private float cooldownTimer;
    private float textRefreshTimer;
    private const float TextRefreshInterval = 0.1f;
    private GameObject activeCloneObject;
    private Coroutine cloneRoutine;

    private void Awake()
    {
        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
        if (playerSkinApplier == null) playerSkinApplier = GetComponent<PlayerSkinApplier>();
        ResetCloneState();
    }

    private void OnEnable() => ResetCloneState();
    private void OnDisable() => ClearAllCloneState();
    private void OnDestroy() => ClearAllCloneState();

    private void Update()
    {
        if (!GameStateManager.IsGameplayStarted)
            return;

        if (playerMovement != null && playerMovement.IsGameOver)
        {
            HandleGameOver();
            return;
        }

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            UseClone();

        if (cooldownActive)
            UpdateCooldown();
    }

    private void HandleGameOver()
    {
        if (gameOverHandled) return;
        gameOverHandled = true;
        ClearAllCloneState();
        if (cloneButton != null) cloneButton.interactable = false;
        HideCooldownUI();
    }

    public void SetCloneCooldown(float cooldown) => cloneCooldown = Mathf.Max(0.1f, cooldown);
    public void SetCloneUses(int uses) { }

    public void UseClone()
    {
        if (!GameStateManager.IsGameplayStarted || !canUseClone || cloneActive || cooldownActive)
            return;
        if (playerMovement != null && playerMovement.IsGameOver)
            return;
        if (clonePrefab == null)
            return;

        canUseClone = false;
        cloneActive = true;
        StatsManager.AddCloneUse();
        if (soundManager != null) soundManager.PlayVoidCloneSound();
        ShowActiveUI();
        UpdateUI();
        cloneRoutine = StartCoroutine(CloneRoutine());
    }

    private IEnumerator CloneRoutine()
    {
        activeCloneObject = Instantiate(clonePrefab, transform.position, Quaternion.identity);
        ActiveCloneTarget = activeCloneObject.transform;

        VoidClone cloneScript = activeCloneObject.GetComponent<VoidClone>();
        if (cloneScript != null)
        {
            cloneScript.SetSkin(GetCurrentPlayerSprite());
            cloneScript.StartClone(cloneDuration, playerMovement);
        }

        yield return new WaitForSeconds(Mathf.Max(0.01f, cloneDuration));

        cloneRoutine = null;
        DestroyCloneObject();
        BeginCooldown();
    }

    private Sprite GetCurrentPlayerSprite()
    {
        if (playerSkinApplier != null && playerSkinApplier.CurrentSprite != null)
            return playerSkinApplier.CurrentSprite;
        SpriteRenderer renderer = GetComponentInChildren<SpriteRenderer>(true);
        return renderer != null ? renderer.sprite : null;
    }

    private void DestroyCloneObject()
    {
        ActiveCloneTarget = null;
        if (activeCloneObject != null) Destroy(activeCloneObject);
        activeCloneObject = null;
        cloneActive = false;
    }

    private void BeginCooldown()
    {
        cooldownActive = true;
        cooldownTimer = cloneCooldown;
        textRefreshTimer = 0f;
        ShowCooldownUI();
        UpdateCooldownVisuals();
        UpdateUI();
    }

    private void UpdateCooldown()
    {
        cooldownTimer = Mathf.Max(0f, cooldownTimer - Time.deltaTime);
        UpdateCooldownVisuals();
        if (cooldownTimer > 0f) return;

        cooldownActive = false;
        canUseClone = true;
        HideCooldownUI();
        UpdateUI();
    }

    private void UpdateCooldownVisuals()
    {
        if (cooldownFill != null)
            cooldownFill.fillAmount = cloneCooldown <= 0f ? 0f : cooldownTimer / cloneCooldown;

        textRefreshTimer -= Time.deltaTime;
        if (textRefreshTimer > 0f) return;
        textRefreshTimer = TextRefreshInterval;
        if (cooldownText != null)
            cooldownText.text = cooldownTimer > 0f ? cooldownTimer.ToString("F1") : "";
    }

    private void ShowActiveUI()
    {
        if (cooldownFill != null)
        {
            cooldownFill.gameObject.SetActive(true);
            cooldownFill.fillAmount = 1f;
        }
        if (cooldownText != null)
        {
            cooldownText.gameObject.SetActive(true);
            cooldownText.text = "ACTIVE";
        }
    }

    private void ShowCooldownUI()
    {
        if (cooldownFill != null) cooldownFill.gameObject.SetActive(true);
        if (cooldownText != null) cooldownText.gameObject.SetActive(true);
    }

    private void HideCooldownUI()
    {
        if (cooldownFill != null)
        {
            cooldownFill.gameObject.SetActive(false);
            cooldownFill.fillAmount = 0f;
        }
        if (cooldownText != null)
        {
            cooldownText.text = "";
            cooldownText.gameObject.SetActive(false);
        }
    }

    private void ClearAllCloneState()
    {
        if (cloneRoutine != null)
        {
            StopCoroutine(cloneRoutine);
            cloneRoutine = null;
        }
        DestroyCloneObject();
        cooldownActive = false;
        cooldownTimer = 0f;
    }

    public void ResetCloneState()
    {
        StopAllCoroutines();
        cloneRoutine = null;
        DestroyCloneObject();
        canUseClone = true;
        cooldownActive = false;
        gameOverHandled = false;
        cooldownTimer = 0f;
        textRefreshTimer = 0f;
        HideCooldownUI();
        UpdateUI();
    }

    private void UpdateUI()
    {
        bool usable = canUseClone && !cloneActive && !cooldownActive && !gameOverHandled;
        if (cloneButton != null) cloneButton.interactable = usable;
        if (cloneButtonImage != null) cloneButtonImage.sprite = usable ? readySprite : usedSprite;
    }
}
