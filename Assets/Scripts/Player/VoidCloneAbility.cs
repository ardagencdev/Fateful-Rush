using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class VoidCloneAbility : MonoBehaviour
{
    public static Transform ActiveCloneTarget { get; private set; }

    public static bool HasActiveClone =>
        ActiveCloneTarget != null;

    [Header("Clone")]
    public GameObject clonePrefab;
    public float cloneDuration = 3f;

    [Header("Cooldown")]
    public float cloneCooldown = 8f;

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

    private bool canUseClone = true;
    private bool cloneActive;
    private bool gameOverHandled;

    private float cooldownTimer;
    private float textRefreshTimer;

    private const float TextRefreshInterval = 0.1f;

    private GameObject activeCloneObject;
    private Coroutine cloneRoutine;

    private void Awake()
    {
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        ResetCloneState();
    }

    private void OnEnable()
    {
        ResetCloneState();
    }

    private void OnDisable()
    {
        ClearActiveClone();
    }

    private void OnDestroy()
    {
        ClearActiveClone();
    }

    private void Update()
    {
        if (!GameStateManager.IsGameplayStarted)
            return;

        if (playerMovement != null &&
            playerMovement.IsGameOver)
        {
            HandleGameOver();
            return;
        }

        if (Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            UseClone();
        }

        if (!canUseClone)
            UpdateCooldownUI();
    }

    private void HandleGameOver()
    {
        if (gameOverHandled)
            return;

        gameOverHandled = true;

        ClearActiveClone();

        if (cloneButton != null)
            cloneButton.interactable = false;

        HideCooldownUI();
    }

    public void SetCloneCooldown(float cooldown)
    {
        cloneCooldown = Mathf.Max(0.1f, cooldown);
    }

    public void SetCloneUses(int uses)
    {
        // Eski LevelManager çağrıları hata vermesin diye duruyor.
        ResetCloneState();
    }

    public void UseClone()
    {
        if (!GameStateManager.IsGameplayStarted)
            return;

        if (!canUseClone)
            return;

        if (cloneActive)
            return;

        if (playerMovement != null &&
            playerMovement.IsGameOver)
        {
            return;
        }

        if (clonePrefab == null)
            return;

        canUseClone = false;
        cooldownTimer = cloneCooldown;
        textRefreshTimer = 0f;

        StatsManager.AddCloneUse();

        if (soundManager != null)
            soundManager.PlayVoidCloneSound();

        ShowCooldownUI();
        UpdateUI();

        cloneRoutine =
            StartCoroutine(CloneRoutine());
    }

    private IEnumerator CloneRoutine()
    {
        cloneActive = true;

        activeCloneObject =
            Instantiate(
                clonePrefab,
                transform.position,
                Quaternion.identity
            );

        ActiveCloneTarget =
            activeCloneObject.transform;

        VoidClone cloneScript =
            activeCloneObject.GetComponent<VoidClone>();

        if (cloneScript != null)
        {
            cloneScript.StartClone(
                cloneDuration,
                playerMovement
            );
        }

        yield return new WaitForSeconds(cloneDuration);

        ClearActiveClone();

        cloneRoutine = null;

        UpdateUI();
    }

    private void ClearActiveClone()
    {
        if (cloneRoutine != null)
        {
            StopCoroutine(cloneRoutine);
            cloneRoutine = null;
        }

        ActiveCloneTarget = null;

        if (activeCloneObject != null)
            Destroy(activeCloneObject);

        activeCloneObject = null;
        cloneActive = false;
    }

    private void UpdateCooldownUI()
    {
        cooldownTimer -= Time.deltaTime;
        cooldownTimer = Mathf.Max(cooldownTimer, 0f);

        if (cooldownFill != null)
        {
            cooldownFill.fillAmount =
                cloneCooldown <= 0f
                    ? 0f
                    : cooldownTimer / cloneCooldown;
        }

        textRefreshTimer -= Time.deltaTime;

        if (textRefreshTimer <= 0f)
        {
            textRefreshTimer =
                TextRefreshInterval;

            if (cooldownText != null)
            {
                cooldownText.text =
                    cooldownTimer > 0f
                        ? cooldownTimer.ToString("F1")
                        : "";
            }
        }

        if (cooldownTimer <= 0f)
        {
            canUseClone = true;

            HideCooldownUI();
            UpdateUI();
        }
    }

    private void ShowCooldownUI()
    {
        if (cooldownFill != null)
            cooldownFill.gameObject.SetActive(true);

        if (cooldownText != null)
            cooldownText.gameObject.SetActive(true);
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

    public void ResetCloneState()
    {
        StopAllCoroutines();

        cloneRoutine = null;

        ActiveCloneTarget = null;

        if (activeCloneObject != null)
            Destroy(activeCloneObject);

        activeCloneObject = null;

        canUseClone = true;
        cloneActive = false;
        gameOverHandled = false;

        cooldownTimer = 0f;
        textRefreshTimer = 0f;

        HideCooldownUI();
        UpdateUI();
    }

    private void UpdateUI()
    {
        bool usable =
            canUseClone &&
            !cloneActive &&
            !gameOverHandled;

        if (cloneButton != null)
            cloneButton.interactable = usable;

        if (cloneButtonImage != null)
        {
            cloneButtonImage.sprite =
                usable
                    ? readySprite
                    : usedSprite;
        }
    }
}