using TMPro;
using UnityEngine;

public class PlayerCoinCollector : MonoBehaviour
{
    [Header("References")]
    public PlayerMovement playerMovement;
    public EnemySpawner enemySpawner;
    public SoundManager soundManager;
    public ScoreUIEffect scoreUIEffect;
    public ComboUI comboUI;
    public GameStateManager gameStateManager;

    [Header("UI")]
    public TextMeshProUGUI scoreText;

    [Header("Combo")]
    public bool comboEnabled = true;

    [Min(0.01f)]
    public float comboTimeLimit = 1.5f;

    [Min(1)]
    public int maxCombo = 3;

    [Min(1)]
    public int coinsForCombo2 = 2;

    [Min(1)]
    public int coinsForCombo3 = 5;

    [Tooltip(
        "LevelConfig'den gelir. Boşsa eski coinsForCombo2/3 sistemi kullanılır."
    )]
    public ComboSpeedStage[] comboSpeedStages;

    private int score;
    private int combo = 1;
    private int comboChain;
    private float comboTimer;

    public int Score => score;
    public int Combo => combo;

    private void Awake()
    {
        if (playerMovement == null)
        {
            playerMovement =
                GetComponent<PlayerMovement>();
        }

        if (gameStateManager == null)
        {
            gameStateManager =
                FindAnyObjectByType<GameStateManager>();
        }

        UpdateScoreUI();

        if (comboUI != null)
            comboUI.UpdateCombo(1);
    }

    private void Update()
    {
        if (IsGameOver())
            return;

        if (!comboEnabled)
            return;

        if (comboChain <= 0)
            return;

        comboTimer += Time.deltaTime;

        float normalizedTime =
            1f - (comboTimer / comboTimeLimit);

        normalizedTime =
            Mathf.Clamp01(normalizedTime);

        if (comboUI != null)
        {
            comboUI.UpdateTimerBar(
                normalizedTime,
                combo
            );
        }

        if (comboTimer >= comboTimeLimit)
            ResetCombo();
    }

    private void OnTriggerEnter2D(
        Collider2D other)
    {
        if (IsGameOver())
            return;

        if (!other.CompareTag("Coin"))
            return;

        CollectCoin(other);
    }

    private void CollectCoin(
        Collider2D coinCollider)
    {
        if (coinCollider == null ||
            !coinCollider.enabled)
        {
            return;
        }

        coinCollider.enabled = false;

        Coin coin =
            coinCollider.GetComponentInParent<Coin>();

        VibrationManager.Instance
            ?.VibrateLight();

        int currentCombo =
            UpdateCombo();

        int coinValue =
            coin != null
                ? Mathf.Max(1, coin.value)
                : 1;

        int gainedScore =
            coinValue * currentCombo;

        score += gainedScore;

        if (coin != null)
        {
            string coinType =
                GetCoinType(coin.value);

            StatsManager.AddCoin(
                gainedScore,
                coinType
            );
        }

        UpdateScoreUI();

        if (enemySpawner != null)
        {
            enemySpawner.TrySpawnBoss(score);
        }

        if (comboUI != null &&
            comboEnabled)
        {
            comboUI.ShowCombo(
                gainedScore,
                currentCombo
            );
        }

        if (scoreUIEffect != null)
            scoreUIEffect.PlayPop();

        PlayCollectEffect(coinCollider);

        if (soundManager != null)
            soundManager.PlayCoinSound();

        gameStateManager
            ?.CheckScoreObjective(score);
    }

    private int UpdateCombo()
    {
        if (!comboEnabled)
        {
            combo = 1;
            comboChain = 0;
            comboTimer = 0f;

            return 1;
        }

        comboTimer = 0f;
        comboChain++;

        combo =
            GetComboFromChain();

        return combo;
    }

    private int GetComboFromChain()
    {
        if (comboSpeedStages != null &&
            comboSpeedStages.Length > 0)
        {
            int result = 1;

            for (int i = 0;
                 i < comboSpeedStages.Length;
                 i++)
            {
                ComboSpeedStage stage =
                    comboSpeedStages[i];

                if (stage == null)
                    continue;

                if (stage.comboMultiplier < 2)
                    continue;

                if (stage.coinsRequired < 1)
                    continue;

                if (comboChain >=
                    stage.coinsRequired)
                {
                    result = Mathf.Max(
                        result,
                        stage.comboMultiplier
                    );
                }
            }

            return Mathf.Clamp(
                result,
                1,
                maxCombo
            );
        }

        int fallbackResult = 1;

        if (comboChain >= coinsForCombo3)
        {
            fallbackResult = 3;
        }
        else if (comboChain >= coinsForCombo2)
        {
            fallbackResult = 2;
        }

        return Mathf.Clamp(
            fallbackResult,
            1,
            maxCombo
        );
    }

    private void ResetCombo()
    {
        combo = 1;
        comboChain = 0;
        comboTimer = 0f;

        if (comboUI == null)
            return;

        comboUI.ResetCombo();

        comboUI.UpdateTimerBar(
            0f,
            combo
        );
    }

    private void PlayCollectEffect(
        Collider2D coinCollider)
    {
        SpawnScaleEffect coinEffect =
            coinCollider
                .GetComponentInParent
                    <SpawnScaleEffect>();

        if (coinEffect != null)
        {
            coinEffect.Collect();
            return;
        }

        Destroy(
            coinCollider
                .transform
                .root
                .gameObject
        );
    }

    private static string GetCoinType(
        int coinValue)
    {
        if (coinValue == 5)
            return "Gold";

        if (coinValue == 10)
            return "Rare";

        return "Normal";
    }

    private bool IsGameOver()
    {
        return playerMovement != null &&
               playerMovement.IsGameOver;
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text =
                $"Score: {score}";
        }
    }

    private void OnValidate()
    {
        comboTimeLimit =
            Mathf.Max(
                0.01f,
                comboTimeLimit
            );

        maxCombo =
            Mathf.Max(
                1,
                maxCombo
            );

        coinsForCombo2 =
            Mathf.Max(
                1,
                coinsForCombo2
            );

        coinsForCombo3 =
            Mathf.Max(
                coinsForCombo2,
                coinsForCombo3
            );
    }
}

