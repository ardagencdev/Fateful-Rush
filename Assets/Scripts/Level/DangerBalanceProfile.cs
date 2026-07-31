using System;
using UnityEngine;

public enum DangerLevel
{
    Danger1 = 1,
    Danger2 = 2,
    Danger3 = 3,
    Danger4 = 4,
    Danger5 = 5
}

public static class DangerLevelUtility
{
    public const int TierCount = 5;

    public static int ToIndex(DangerLevel level)
    {
        return Mathf.Clamp((int)level - 1, 0, TierCount - 1);
    }

    public static DangerLevel Sanitize(DangerLevel level)
    {
        return (DangerLevel)Mathf.Clamp((int)level, 1, TierCount);
    }

    public static string GetShortLabel(DangerLevel level)
    {
        return $"D{(int)Sanitize(level)}";
    }

    public static string GetDisplayName(DangerLevel level)
    {
        switch (Sanitize(level))
        {
            case DangerLevel.Danger1:
                return "D1 - INTRODUCTION";
            case DangerLevel.Danger2:
                return "D2 - STANDARD";
            case DangerLevel.Danger3:
                return "D3 - PRESSURE";
            case DangerLevel.Danger4:
                return "D4 - SEVERE";
            case DangerLevel.Danger5:
                return "D5 - EXTREME";
            default:
                return "D2 - STANDARD";
        }
    }

    public static string GetDescription(DangerLevel level)
    {
        switch (Sanitize(level))
        {
            case DangerLevel.Danger1:
                return "Introduces the mechanic with generous reaction time.";
            case DangerLevel.Danger2:
                return "The original balanced gameplay baseline.";
            case DangerLevel.Danger3:
                return "Creates sustained pressure without becoming unfair.";
            case DangerLevel.Danger4:
                return "High-intensity tuning for late-game combinations.";
            case DangerLevel.Danger5:
                return "Extreme endgame tuning. Use with low counts and careful testing.";
            default:
                return string.Empty;
        }
    }
}

[Serializable]
public class NormalEnemyDangerSettings
{
    [Min(0f)] public float minStartSpeed = 1.5f;
    [Min(0f)] public float maxStartSpeed = 2.5f;
    [Min(0f)] public float maxSpeed = 7f;
    [Min(0f)] public float speedIncreaseRate = 0.1f;

    public bool predictionEnabled = true;
    [Min(0f)] public float predictionDistanceThreshold = 2.5f;
    [Min(0f)] public float predictionTime = 0.25f;
    [Min(0f)] public float maxPredictionDistance = 1.5f;

    public bool separationEnabled = true;
    [Min(0f)] public float separationRadius = 0.75f;
    [Min(0f)] public float separationStrength = 0.65f;

    public NormalEnemyDangerSettings Clone()
    {
        return (NormalEnemyDangerSettings)MemberwiseClone();
    }

    public void Sanitize()
    {
        minStartSpeed = Mathf.Max(0f, minStartSpeed);
        maxStartSpeed = Mathf.Max(minStartSpeed, maxStartSpeed);
        maxSpeed = Mathf.Max(maxStartSpeed, maxSpeed);
        speedIncreaseRate = Mathf.Max(0f, speedIncreaseRate);
        predictionDistanceThreshold = Mathf.Max(0f, predictionDistanceThreshold);
        predictionTime = Mathf.Max(0f, predictionTime);
        maxPredictionDistance = Mathf.Max(0f, maxPredictionDistance);
        separationRadius = Mathf.Max(0f, separationRadius);
        separationStrength = Mathf.Max(0f, separationStrength);
    }
}

[Serializable]
public class ProjectileEnemyDangerSettings
{
    [Min(0f)] public float moveSpeed = 3f;
    [Min(0f)] public float stoppingDistance = 7f;
    [Min(0f)] public float retreatDistance = 4f;
    [Min(0.01f)] public float fireRate = 1.5f;
    [Min(0f)] public float projectileSpeed = 6f;

    public bool strafeEnabled = true;
    [Min(0f)] public float strafeSpeedMultiplier = 0.65f;
    [Min(0f)] public float strafeDirectionChangeMinTime = 1.5f;
    [Min(0f)] public float strafeDirectionChangeMaxTime = 3f;
    [Min(0f)] public float strafeDistanceTolerance = 0.6f;

    public bool predictiveAimEnabled = true;
    [Min(0f)] public float predictionTime = 0.25f;
    [Min(0f)] public float maxPredictionDistance = 1.5f;
    [Min(0f)] public float predictionDistanceThreshold = 2.5f;

    public bool separationEnabled = true;
    [Min(0f)] public float separationRadius = 0.9f;
    [Min(0f)] public float separationStrength = 0.45f;

    public ProjectileEnemyDangerSettings Clone()
    {
        return (ProjectileEnemyDangerSettings)MemberwiseClone();
    }

    public void Sanitize()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);
        stoppingDistance = Mathf.Max(0f, stoppingDistance);
        retreatDistance = Mathf.Clamp(retreatDistance, 0f, stoppingDistance);
        fireRate = Mathf.Max(0.01f, fireRate);
        projectileSpeed = Mathf.Max(0f, projectileSpeed);
        strafeSpeedMultiplier = Mathf.Max(0f, strafeSpeedMultiplier);
        strafeDirectionChangeMinTime = Mathf.Max(0f, strafeDirectionChangeMinTime);
        strafeDirectionChangeMaxTime = Mathf.Max(
            strafeDirectionChangeMinTime,
            strafeDirectionChangeMaxTime
        );
        strafeDistanceTolerance = Mathf.Max(0f, strafeDistanceTolerance);
        predictionTime = Mathf.Max(0f, predictionTime);
        maxPredictionDistance = Mathf.Max(0f, maxPredictionDistance);
        predictionDistanceThreshold = Mathf.Max(0f, predictionDistanceThreshold);
        separationRadius = Mathf.Max(0f, separationRadius);
        separationStrength = Mathf.Max(0f, separationStrength);
    }
}

[Serializable]
public class HunterEnemyDangerSettings
{
    [Min(0f)] public float prepareDistance = 6f;
    [Min(0f)] public float repositionTime = 1.2f;
    [Min(0f)] public float recoveryTime = 1.2f;
    [Min(0f)] public float warningDuration = 1f;
    [Min(0f)] public float chargeSpeed = 15f;
    [Min(0.01f)] public float maxChargeTime = 0.65f;
    [Min(0f)] public float stunDuration = 1f;

    public HunterEnemyDangerSettings Clone()
    {
        return (HunterEnemyDangerSettings)MemberwiseClone();
    }

    public void Sanitize()
    {
        prepareDistance = Mathf.Max(0f, prepareDistance);
        repositionTime = Mathf.Max(0f, repositionTime);
        recoveryTime = Mathf.Max(0f, recoveryTime);
        warningDuration = Mathf.Max(0f, warningDuration);
        chargeSpeed = Mathf.Max(0f, chargeSpeed);
        maxChargeTime = Mathf.Max(0.01f, maxChargeTime);
        stunDuration = Mathf.Max(0f, stunDuration);
    }
}

[Serializable]
public class BossDangerSettings
{
    [Min(0f)] public float speed = 1.2f;
    [Min(0f)] public float directionSmoothness = 7f;
    public bool canSplit = true;
    [Min(0f)] public float splitDelay = 0.8f;
    [Min(0f)] public float splitDistance = 1.2f;
    [Min(0f)] public float miniBossSpeed = 2.5f;

    public BossDangerSettings Clone()
    {
        return (BossDangerSettings)MemberwiseClone();
    }

    public void Sanitize()
    {
        speed = Mathf.Max(0f, speed);
        directionSmoothness = Mathf.Max(0f, directionSmoothness);
        splitDelay = Mathf.Max(0f, splitDelay);
        splitDistance = Mathf.Max(0f, splitDistance);
        miniBossSpeed = Mathf.Max(0f, miniBossSpeed);
    }
}

[Serializable]
public class BeaconEnemyDangerSettings
{
    [Min(0f)] public float activationDelay = 3f;
    [Min(0.05f)] public float pulseInterval = 2f;
    [Min(0.05f)] public float retargetInterval = 0.5f;
    [Min(0f)] public float targetStopDistance = 1.5f;

    [Min(0.1f)] public float buffDuration = 15f;
    [Min(0.1f)] public float buffSizeMultiplier = 1.25f;
    [Min(0.1f)] public float normalSpeedMultiplier = 1.35f;
    [Min(0.1f)] public float normalMaxSpeedMultiplier = 1.25f;
    [Min(0.1f)] public float projectileMoveMultiplier = 1.2f;
    [Min(0.1f)] public float projectileShotMultiplier = 1.25f;
    [Min(0.1f)] public float projectileFireMultiplier = 1.25f;
    [Min(0.1f)] public float hunterRepositionMultiplier = 0.8f;
    [Min(0.1f)] public float hunterWarningMultiplier = 0.8f;
    [Min(0.1f)] public float hunterChargeMultiplier = 1.25f;
    [Min(0.1f)] public float hunterStunMultiplier = 0.8f;

    [Min(0f)] public float moveSpeed = 3f;
    [Min(0f)] public float safeDistanceFromPlayer = 5f;
    [Min(0f)] public float wanderStrength = 0.6f;

    public BeaconEnemyDangerSettings Clone()
    {
        return (BeaconEnemyDangerSettings)MemberwiseClone();
    }

    public void Sanitize()
    {
        activationDelay = Mathf.Max(0f, activationDelay);
        pulseInterval = Mathf.Max(0.05f, pulseInterval);
        retargetInterval = Mathf.Max(0.05f, retargetInterval);
        targetStopDistance = Mathf.Max(0f, targetStopDistance);
        buffDuration = Mathf.Max(0.1f, buffDuration);
        buffSizeMultiplier = Mathf.Max(0.1f, buffSizeMultiplier);
        normalSpeedMultiplier = Mathf.Max(0.1f, normalSpeedMultiplier);
        normalMaxSpeedMultiplier = Mathf.Max(0.1f, normalMaxSpeedMultiplier);
        projectileMoveMultiplier = Mathf.Max(0.1f, projectileMoveMultiplier);
        projectileShotMultiplier = Mathf.Max(0.1f, projectileShotMultiplier);
        projectileFireMultiplier = Mathf.Max(0.1f, projectileFireMultiplier);
        hunterRepositionMultiplier = Mathf.Max(0.1f, hunterRepositionMultiplier);
        hunterWarningMultiplier = Mathf.Max(0.1f, hunterWarningMultiplier);
        hunterChargeMultiplier = Mathf.Max(0.1f, hunterChargeMultiplier);
        hunterStunMultiplier = Mathf.Max(0.1f, hunterStunMultiplier);
        moveSpeed = Mathf.Max(0f, moveSpeed);
        safeDistanceFromPlayer = Mathf.Max(0f, safeDistanceFromPlayer);
        wanderStrength = Mathf.Max(0f, wanderStrength);
    }
}

[Serializable]
public class LaserDangerSettings
{
    [Min(0f)] public float minSpawnTime = 8f;
    [Min(0f)] public float maxSpawnTime = 25f;
    [Min(0f)] public float warningDuration = 2f;
    [Min(0.1f)] public float lifeTime = 1.5f;
    [Min(0.01f)] public float width = 0.5f;
    [Min(0f)] public float sizeExtra = 1f;

    public LaserDangerSettings Clone()
    {
        return (LaserDangerSettings)MemberwiseClone();
    }

    public void Sanitize()
    {
        minSpawnTime = Mathf.Max(0f, minSpawnTime);
        maxSpawnTime = Mathf.Max(minSpawnTime, maxSpawnTime);
        warningDuration = Mathf.Max(0f, warningDuration);
        lifeTime = Mathf.Max(0.1f, lifeTime);
        width = Mathf.Max(0.01f, width);
        sizeExtra = Mathf.Max(0f, sizeExtra);
    }
}

[Serializable]
public class BombDangerSettings
{
    [Min(0f)] public float minSpawnTime = 6f;
    [Min(0f)] public float maxSpawnTime = 14f;
    [Min(0)] public int maxBombCount = 3;
    [Min(0f)] public float spawnSafeTime = 0.35f;

    public BombDangerSettings Clone()
    {
        return (BombDangerSettings)MemberwiseClone();
    }

    public void Sanitize()
    {
        minSpawnTime = Mathf.Max(0f, minSpawnTime);
        maxSpawnTime = Mathf.Max(minSpawnTime, maxSpawnTime);
        maxBombCount = Mathf.Max(0, maxBombCount);
        spawnSafeTime = Mathf.Max(0f, spawnSafeTime);
    }
}

[CreateAssetMenu(
    fileName = "Default_Danger_Balance",
    menuName = "Void Rush/Balance/Danger Balance Profile"
)]
public class DangerBalanceProfile : ScriptableObject
{
    [SerializeField, Min(1)]
    private int profileVersion = 1;

    [SerializeField]
    private NormalEnemyDangerSettings[] normalEnemyLevels;

    [SerializeField]
    private ProjectileEnemyDangerSettings[] projectileEnemyLevels;

    [SerializeField]
    private HunterEnemyDangerSettings[] hunterEnemyLevels;

    [SerializeField]
    private BossDangerSettings[] bossLevels;

    [SerializeField]
    private BeaconEnemyDangerSettings[] beaconEnemyLevels;

    [SerializeField]
    private LaserDangerSettings[] verticalLaserLevels;

    [SerializeField]
    private LaserDangerSettings[] horizontalLaserLevels;

    [SerializeField]
    private BombDangerSettings[] bombLevels;

    public int ProfileVersion => profileVersion;

    public NormalEnemyDangerSettings GetNormalEnemy(DangerLevel level)
    {
        return GetTier(normalEnemyLevels, level, CreateDefaultNormalEnemyLevels);
    }

    public ProjectileEnemyDangerSettings GetProjectileEnemy(DangerLevel level)
    {
        return GetTier(projectileEnemyLevels, level, CreateDefaultProjectileEnemyLevels);
    }

    public HunterEnemyDangerSettings GetHunterEnemy(DangerLevel level)
    {
        return GetTier(hunterEnemyLevels, level, CreateDefaultHunterEnemyLevels);
    }

    public BossDangerSettings GetBoss(DangerLevel level)
    {
        return GetTier(bossLevels, level, CreateDefaultBossLevels);
    }

    public BeaconEnemyDangerSettings GetBeaconEnemy(DangerLevel level)
    {
        return GetTier(beaconEnemyLevels, level, CreateDefaultBeaconEnemyLevels);
    }

    public LaserDangerSettings GetVerticalLaser(DangerLevel level)
    {
        return GetTier(verticalLaserLevels, level, CreateDefaultVerticalLaserLevels);
    }

    public LaserDangerSettings GetHorizontalLaser(DangerLevel level)
    {
        return GetTier(horizontalLaserLevels, level, CreateDefaultHorizontalLaserLevels);
    }

    public BombDangerSettings GetBomb(DangerLevel level)
    {
        return GetTier(bombLevels, level, CreateDefaultBombLevels);
    }

    public void ResetToBalancedDefaults()
    {
        profileVersion = 1;
        normalEnemyLevels = CreateDefaultNormalEnemyLevels();
        projectileEnemyLevels = CreateDefaultProjectileEnemyLevels();
        hunterEnemyLevels = CreateDefaultHunterEnemyLevels();
        bossLevels = CreateDefaultBossLevels();
        beaconEnemyLevels = CreateDefaultBeaconEnemyLevels();
        verticalLaserLevels = CreateDefaultVerticalLaserLevels();
        horizontalLaserLevels = CreateDefaultHorizontalLaserLevels();
        bombLevels = CreateDefaultBombLevels();
    }

    private void OnEnable()
    {
        EnsureValidArrays();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureValidArrays();
        SanitizeAll();
    }
#endif

    private void EnsureValidArrays()
    {
        normalEnemyLevels = EnsureArray(normalEnemyLevels, CreateDefaultNormalEnemyLevels);
        projectileEnemyLevels = EnsureArray(projectileEnemyLevels, CreateDefaultProjectileEnemyLevels);
        hunterEnemyLevels = EnsureArray(hunterEnemyLevels, CreateDefaultHunterEnemyLevels);
        bossLevels = EnsureArray(bossLevels, CreateDefaultBossLevels);
        beaconEnemyLevels = EnsureArray(beaconEnemyLevels, CreateDefaultBeaconEnemyLevels);
        verticalLaserLevels = EnsureArray(verticalLaserLevels, CreateDefaultVerticalLaserLevels);
        horizontalLaserLevels = EnsureArray(horizontalLaserLevels, CreateDefaultHorizontalLaserLevels);
        bombLevels = EnsureArray(bombLevels, CreateDefaultBombLevels);
    }

    private void SanitizeAll()
    {
        SanitizeArray(normalEnemyLevels, item => item.Sanitize());
        SanitizeArray(projectileEnemyLevels, item => item.Sanitize());
        SanitizeArray(hunterEnemyLevels, item => item.Sanitize());
        SanitizeArray(bossLevels, item => item.Sanitize());
        SanitizeArray(beaconEnemyLevels, item => item.Sanitize());
        SanitizeArray(verticalLaserLevels, item => item.Sanitize());
        SanitizeArray(horizontalLaserLevels, item => item.Sanitize());
        SanitizeArray(bombLevels, item => item.Sanitize());
    }

    private static T GetTier<T>(
        T[] values,
        DangerLevel level,
        Func<T[]> createDefaults)
        where T : class
    {
        int index = DangerLevelUtility.ToIndex(level);

        if (values != null &&
            values.Length == DangerLevelUtility.TierCount &&
            values[index] != null)
        {
            return values[index];
        }

        return createDefaults()[index];
    }

    private static T[] EnsureArray<T>(
        T[] current,
        Func<T[]> createDefaults)
        where T : class
    {
        T[] defaults = createDefaults();

        if (current == null || current.Length != DangerLevelUtility.TierCount)
        {
            T[] rebuilt = defaults;

            if (current != null)
            {
                int copyCount = Mathf.Min(current.Length, rebuilt.Length);

                for (int i = 0; i < copyCount; i++)
                {
                    if (current[i] != null)
                        rebuilt[i] = current[i];
                }
            }

            return rebuilt;
        }

        for (int i = 0; i < current.Length; i++)
        {
            if (current[i] == null)
                current[i] = defaults[i];
        }

        return current;
    }

    private static void SanitizeArray<T>(T[] values, Action<T> sanitize)
        where T : class
    {
        if (values == null)
            return;

        foreach (T value in values)
        {
            if (value != null)
                sanitize(value);
        }
    }

    private static NormalEnemyDangerSettings[] CreateDefaultNormalEnemyLevels()
    {
        return new[]
        {
            new NormalEnemyDangerSettings
            {
                minStartSpeed = 1.2f,
                maxStartSpeed = 1.8f,
                maxSpeed = 5.5f,
                speedIncreaseRate = 0.07f,
                predictionEnabled = false,
                predictionDistanceThreshold = 2f,
                predictionTime = 0.18f,
                maxPredictionDistance = 0.8f,
                separationRadius = 0.7f,
                separationStrength = 0.55f
            },
            new NormalEnemyDangerSettings(),
            new NormalEnemyDangerSettings
            {
                minStartSpeed = 1.9f,
                maxStartSpeed = 2.9f,
                maxSpeed = 8.25f,
                speedIncreaseRate = 0.13f,
                predictionDistanceThreshold = 3f,
                predictionTime = 0.32f,
                maxPredictionDistance = 2.1f,
                separationRadius = 0.8f,
                separationStrength = 0.72f
            },
            new NormalEnemyDangerSettings
            {
                minStartSpeed = 2.3f,
                maxStartSpeed = 3.3f,
                maxSpeed = 9.5f,
                speedIncreaseRate = 0.17f,
                predictionDistanceThreshold = 3.5f,
                predictionTime = 0.4f,
                maxPredictionDistance = 2.8f,
                separationRadius = 0.85f,
                separationStrength = 0.8f
            },
            new NormalEnemyDangerSettings
            {
                minStartSpeed = 2.7f,
                maxStartSpeed = 3.8f,
                maxSpeed = 11f,
                speedIncreaseRate = 0.22f,
                predictionDistanceThreshold = 4f,
                predictionTime = 0.48f,
                maxPredictionDistance = 3.5f,
                separationRadius = 0.9f,
                separationStrength = 0.9f
            }
        };
    }

    private static ProjectileEnemyDangerSettings[] CreateDefaultProjectileEnemyLevels()
    {
        return new[]
        {
            new ProjectileEnemyDangerSettings
            {
                moveSpeed = 2.4f,
                stoppingDistance = 7.5f,
                retreatDistance = 4.2f,
                fireRate = 2.2f,
                projectileSpeed = 5f,
                strafeSpeedMultiplier = 0.45f,
                strafeDirectionChangeMinTime = 2f,
                strafeDirectionChangeMaxTime = 3.5f,
                predictiveAimEnabled = false,
                predictionTime = 0.2f,
                maxPredictionDistance = 1f,
                predictionDistanceThreshold = 2f,
                separationStrength = 0.4f
            },
            new ProjectileEnemyDangerSettings(),
            new ProjectileEnemyDangerSettings
            {
                moveSpeed = 3.4f,
                stoppingDistance = 6.7f,
                retreatDistance = 3.8f,
                fireRate = 1.25f,
                projectileSpeed = 7.2f,
                strafeSpeedMultiplier = 0.75f,
                strafeDirectionChangeMinTime = 1.2f,
                strafeDirectionChangeMaxTime = 2.5f,
                predictionTime = 0.36f,
                maxPredictionDistance = 2.5f,
                predictionDistanceThreshold = 3f,
                separationRadius = 0.95f,
                separationStrength = 0.58f
            },
            new ProjectileEnemyDangerSettings
            {
                moveSpeed = 3.8f,
                stoppingDistance = 6.4f,
                retreatDistance = 3.6f,
                fireRate = 1f,
                projectileSpeed = 8.5f,
                strafeSpeedMultiplier = 0.85f,
                strafeDirectionChangeMinTime = 0.95f,
                strafeDirectionChangeMaxTime = 2f,
                predictionTime = 0.42f,
                maxPredictionDistance = 3.2f,
                predictionDistanceThreshold = 3.5f,
                separationRadius = 1f,
                separationStrength = 0.65f
            },
            new ProjectileEnemyDangerSettings
            {
                moveSpeed = 4.3f,
                stoppingDistance = 6f,
                retreatDistance = 3.4f,
                fireRate = 0.8f,
                projectileSpeed = 10f,
                strafeSpeedMultiplier = 1f,
                strafeDirectionChangeMinTime = 0.7f,
                strafeDirectionChangeMaxTime = 1.5f,
                predictionTime = 0.5f,
                maxPredictionDistance = 4f,
                predictionDistanceThreshold = 4f,
                separationRadius = 1.05f,
                separationStrength = 0.75f
            }
        };
    }

    private static HunterEnemyDangerSettings[] CreateDefaultHunterEnemyLevels()
    {
        return new[]
        {
            new HunterEnemyDangerSettings
            {
                prepareDistance = 6.5f,
                repositionTime = 1.5f,
                recoveryTime = 1.5f,
                warningDuration = 1.4f,
                chargeSpeed = 12f,
                maxChargeTime = 0.5f,
                stunDuration = 1.4f
            },
            new HunterEnemyDangerSettings(),
            new HunterEnemyDangerSettings
            {
                prepareDistance = 5.8f,
                repositionTime = 1f,
                recoveryTime = 0.9f,
                warningDuration = 0.85f,
                chargeSpeed = 17f,
                maxChargeTime = 0.75f,
                stunDuration = 0.8f
            },
            new HunterEnemyDangerSettings
            {
                prepareDistance = 5.5f,
                repositionTime = 0.8f,
                recoveryTime = 0.7f,
                warningDuration = 0.7f,
                chargeSpeed = 19f,
                maxChargeTime = 0.85f,
                stunDuration = 0.65f
            },
            new HunterEnemyDangerSettings
            {
                prepareDistance = 5.2f,
                repositionTime = 0.65f,
                recoveryTime = 0.5f,
                warningDuration = 0.55f,
                chargeSpeed = 22f,
                maxChargeTime = 1f,
                stunDuration = 0.5f
            }
        };
    }

    private static BossDangerSettings[] CreateDefaultBossLevels()
    {
        return new[]
        {
            new BossDangerSettings
            {
                speed = 0.9f,
                directionSmoothness = 6f,
                splitDelay = 1.2f,
                splitDistance = 1f,
                miniBossSpeed = 2f
            },
            new BossDangerSettings(),
            new BossDangerSettings
            {
                speed = 1.5f,
                directionSmoothness = 8f,
                splitDelay = 0.65f,
                splitDistance = 1.4f,
                miniBossSpeed = 3.2f
            },
            new BossDangerSettings
            {
                speed = 1.85f,
                directionSmoothness = 9f,
                splitDelay = 0.5f,
                splitDistance = 1.6f,
                miniBossSpeed = 4f
            },
            new BossDangerSettings
            {
                speed = 2.2f,
                directionSmoothness = 10f,
                splitDelay = 0.35f,
                splitDistance = 1.8f,
                miniBossSpeed = 4.8f
            }
        };
    }

    private static BeaconEnemyDangerSettings[] CreateDefaultBeaconEnemyLevels()
    {
        return new[]
        {
            new BeaconEnemyDangerSettings
            {
                activationDelay = 4f,
                pulseInterval = 2.6f,
                retargetInterval = 0.7f,
                targetStopDistance = 1.8f,
                buffDuration = 10f,
                buffSizeMultiplier = 1.15f,
                normalSpeedMultiplier = 1.15f,
                normalMaxSpeedMultiplier = 1.1f,
                projectileMoveMultiplier = 1.1f,
                projectileShotMultiplier = 1.1f,
                projectileFireMultiplier = 1.1f,
                hunterRepositionMultiplier = 0.92f,
                hunterWarningMultiplier = 0.92f,
                hunterChargeMultiplier = 1.1f,
                hunterStunMultiplier = 0.92f,
                moveSpeed = 2.4f,
                safeDistanceFromPlayer = 6f,
                wanderStrength = 0.45f
            },
            new BeaconEnemyDangerSettings(),
            new BeaconEnemyDangerSettings
            {
                activationDelay = 2.5f,
                pulseInterval = 1.5f,
                retargetInterval = 0.4f,
                targetStopDistance = 1.4f,
                buffDuration = 16f,
                buffSizeMultiplier = 1.3f,
                normalSpeedMultiplier = 1.45f,
                normalMaxSpeedMultiplier = 1.32f,
                projectileMoveMultiplier = 1.28f,
                projectileShotMultiplier = 1.35f,
                projectileFireMultiplier = 1.35f,
                hunterRepositionMultiplier = 0.74f,
                hunterWarningMultiplier = 0.74f,
                hunterChargeMultiplier = 1.35f,
                hunterStunMultiplier = 0.74f,
                moveSpeed = 3.3f,
                safeDistanceFromPlayer = 5.2f,
                wanderStrength = 0.7f
            },
            new BeaconEnemyDangerSettings
            {
                activationDelay = 2f,
                pulseInterval = 1.15f,
                retargetInterval = 0.3f,
                targetStopDistance = 1.3f,
                buffDuration = 18f,
                buffSizeMultiplier = 1.35f,
                normalSpeedMultiplier = 1.55f,
                normalMaxSpeedMultiplier = 1.4f,
                projectileMoveMultiplier = 1.35f,
                projectileShotMultiplier = 1.45f,
                projectileFireMultiplier = 1.45f,
                hunterRepositionMultiplier = 0.68f,
                hunterWarningMultiplier = 0.68f,
                hunterChargeMultiplier = 1.45f,
                hunterStunMultiplier = 0.68f,
                moveSpeed = 3.6f,
                safeDistanceFromPlayer = 5.4f,
                wanderStrength = 0.8f
            },
            new BeaconEnemyDangerSettings
            {
                activationDelay = 1.5f,
                pulseInterval = 0.85f,
                retargetInterval = 0.25f,
                targetStopDistance = 1.2f,
                buffDuration = 20f,
                buffSizeMultiplier = 1.4f,
                normalSpeedMultiplier = 1.7f,
                normalMaxSpeedMultiplier = 1.5f,
                projectileMoveMultiplier = 1.45f,
                projectileShotMultiplier = 1.6f,
                projectileFireMultiplier = 1.6f,
                hunterRepositionMultiplier = 0.6f,
                hunterWarningMultiplier = 0.6f,
                hunterChargeMultiplier = 1.6f,
                hunterStunMultiplier = 0.6f,
                moveSpeed = 4f,
                safeDistanceFromPlayer = 5.6f,
                wanderStrength = 0.9f
            }
        };
    }

    private static LaserDangerSettings[] CreateDefaultVerticalLaserLevels()
    {
        return CreateDefaultLaserLevels();
    }

    private static LaserDangerSettings[] CreateDefaultHorizontalLaserLevels()
    {
        return CreateDefaultLaserLevels();
    }

    private static LaserDangerSettings[] CreateDefaultLaserLevels()
    {
        return new[]
        {
            new LaserDangerSettings
            {
                minSpawnTime = 14f,
                maxSpawnTime = 24f,
                warningDuration = 2.6f,
                lifeTime = 1.2f,
                width = 0.4f,
                sizeExtra = 1f
            },
            new LaserDangerSettings(),
            new LaserDangerSettings
            {
                minSpawnTime = 7f,
                maxSpawnTime = 16f,
                warningDuration = 1.6f,
                lifeTime = 1.7f,
                width = 0.6f,
                sizeExtra = 1.1f
            },
            new LaserDangerSettings
            {
                minSpawnTime = 5.5f,
                maxSpawnTime = 12f,
                warningDuration = 1.25f,
                lifeTime = 1.9f,
                width = 0.7f,
                sizeExtra = 1.2f
            },
            new LaserDangerSettings
            {
                minSpawnTime = 4f,
                maxSpawnTime = 9f,
                warningDuration = 1f,
                lifeTime = 2.1f,
                width = 0.8f,
                sizeExtra = 1.3f
            }
        };
    }

    private static BombDangerSettings[] CreateDefaultBombLevels()
    {
        return new[]
        {
            new BombDangerSettings
            {
                minSpawnTime = 10f,
                maxSpawnTime = 18f,
                maxBombCount = 2,
                spawnSafeTime = 0.55f
            },
            new BombDangerSettings(),
            new BombDangerSettings
            {
                minSpawnTime = 5f,
                maxSpawnTime = 11f,
                maxBombCount = 4,
                spawnSafeTime = 0.3f
            },
            new BombDangerSettings
            {
                minSpawnTime = 4f,
                maxSpawnTime = 9f,
                maxBombCount = 5,
                spawnSafeTime = 0.25f
            },
            new BombDangerSettings
            {
                minSpawnTime = 3f,
                maxSpawnTime = 7f,
                maxBombCount = 6,
                spawnSafeTime = 0.2f
            }
        };
    }
}
