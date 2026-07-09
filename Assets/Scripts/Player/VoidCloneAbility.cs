using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class VoidCloneAbility : MonoBehaviour
{
    [Header("Clone")]
    public GameObject clonePrefab;
    public float cloneDuration = 3f;
    public int enemiesToDistract = 2;

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

    private readonly List<TargetData> targetBuffer = new List<TargetData>();

    private class TargetData
    {
        public EnemyFollow enemy;
        public ProjectileEnemyFollow projectileEnemy;
        public MiniBossFollow miniBoss;
        public HunterEnemyFollow hunter;
        public Transform originalTarget;
    }

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

    private void Update()
    {
        if (!GameStateManager.IsGameplayStarted)
            return;

        if (playerMovement != null && playerMovement.IsGameOver)
        {
            if (!gameOverHandled)
            {
                gameOverHandled = true;

                if (cloneButton != null)
                    cloneButton.interactable = false;

                HideCooldownUI();
            }

            return;
        }

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            UseClone();

        if (!canUseClone)
            UpdateCooldownUI();
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
        if (!GameStateManager.IsGameplayStarted) return;
        if (!canUseClone) return;
        if (cloneActive) return;
        if (playerMovement != null && playerMovement.IsGameOver) return;
        if (clonePrefab == null) return;

        canUseClone = false;
        cooldownTimer = cloneCooldown;
        textRefreshTimer = 0f;

        StatsManager.AddCloneUse();

        if (soundManager != null)
            soundManager.PlayVoidCloneSound();

        ShowCooldownUI();
        UpdateUI();

        StartCoroutine(CloneRoutine());
    }

    private IEnumerator CloneRoutine()
    {
        cloneActive = true;

        GameObject clone = Instantiate(clonePrefab, transform.position, Quaternion.identity);

        VoidClone cloneScript = clone.GetComponent<VoidClone>();
        if (cloneScript != null)
            cloneScript.StartClone(cloneDuration);

        List<TargetData> selectedTargets = GetRandomEnemies();

        foreach (TargetData data in selectedTargets)
            SetEnemyTarget(data, clone.transform);

        yield return new WaitForSeconds(cloneDuration);

        foreach (TargetData data in selectedTargets)
            RestoreEnemyTarget(data);

        if (clone != null)
            Destroy(clone);

        cloneActive = false;
        UpdateUI();
    }

    private void UpdateCooldownUI()
    {
        cooldownTimer -= Time.deltaTime;
        cooldownTimer = Mathf.Max(cooldownTimer, 0f);

        if (cooldownFill != null)
            cooldownFill.fillAmount = cloneCooldown <= 0f ? 0f : cooldownTimer / cloneCooldown;

        textRefreshTimer -= Time.deltaTime;

        if (textRefreshTimer <= 0f)
        {
            textRefreshTimer = TextRefreshInterval;

            if (cooldownText != null)
                cooldownText.text = cooldownTimer > 0f ? cooldownTimer.ToString("F1") : "";
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
        bool usable = canUseClone && !cloneActive;

        if (cloneButton != null)
            cloneButton.interactable = usable;

        if (cloneButtonImage != null)
            cloneButtonImage.sprite = usable ? readySprite : usedSprite;
    }

    private List<TargetData> GetRandomEnemies()
    {
        targetBuffer.Clear();

        List<TargetData> allTargets = new List<TargetData>();
        HashSet<GameObject> addedObjects = new HashSet<GameObject>();

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        foreach (GameObject enemyObj in enemies)
            AddEnemyTarget(enemyObj, allTargets, addedObjects);

        HunterEnemyFollow[] hunters = FindObjectsByType<HunterEnemyFollow>(FindObjectsInactive.Exclude);

        foreach (HunterEnemyFollow hunter in hunters)
        {
            if (hunter != null)
                AddEnemyTarget(hunter.gameObject, allTargets, addedObjects);
        }

        Shuffle(allTargets);

        int count = Mathf.Min(enemiesToDistract, allTargets.Count);
        return allTargets.GetRange(0, count);
    }

    private void AddEnemyTarget(GameObject enemyObj, List<TargetData> allTargets, HashSet<GameObject> addedObjects)
    {
        if (enemyObj == null) return;
        if (addedObjects.Contains(enemyObj)) return;

        if (enemyObj.GetComponent<BossEnemyFollow>() != null)
            return;

        EnemyFollow enemy = enemyObj.GetComponent<EnemyFollow>();
        ProjectileEnemyFollow projectileEnemy = enemyObj.GetComponent<ProjectileEnemyFollow>();
        MiniBossFollow miniBoss = enemyObj.GetComponent<MiniBossFollow>();
        HunterEnemyFollow hunter = enemyObj.GetComponent<HunterEnemyFollow>();

        if (enemy == null && projectileEnemy == null && miniBoss == null && hunter == null)
            return;

        TargetData data = new TargetData
        {
            enemy = enemy,
            projectileEnemy = projectileEnemy,
            miniBoss = miniBoss,
            hunter = hunter
        };

        if (enemy != null)
            data.originalTarget = enemy.player;
        else if (projectileEnemy != null)
            data.originalTarget = projectileEnemy.player;
        else if (miniBoss != null)
            data.originalTarget = miniBoss.player;
        else if (hunter != null)
            data.originalTarget = hunter.player;

        addedObjects.Add(enemyObj);
        allTargets.Add(data);
    }

    private void SetEnemyTarget(TargetData data, Transform target)
    {
        if (data.enemy != null) data.enemy.player = target;
        if (data.projectileEnemy != null) data.projectileEnemy.player = target;
        if (data.miniBoss != null) data.miniBoss.player = target;
        if (data.hunter != null) data.hunter.player = target;
    }

    private void RestoreEnemyTarget(TargetData data)
    {
        if (data.originalTarget == null) return;

        if (data.enemy != null) data.enemy.player = data.originalTarget;
        if (data.projectileEnemy != null) data.projectileEnemy.player = data.originalTarget;
        if (data.miniBoss != null) data.miniBoss.player = data.originalTarget;
        if (data.hunter != null) data.hunter.player = data.originalTarget;
    }

    private void Shuffle(List<TargetData> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);
            TargetData temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}