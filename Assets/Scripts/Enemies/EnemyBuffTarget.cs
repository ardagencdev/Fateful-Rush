using System.Collections;
using UnityEngine;

public class EnemyBuffTarget : MonoBehaviour
{
    public GameObject buffAura;

    [Header("Buff Duration")]
    public float buffDuration = 15f;

    private bool buffed;
    private Coroutine buffRoutine;

    private EnemyFollow normal;
    private ProjectileEnemyFollow projectile;
    private HunterEnemyFollow hunter;

    private bool canReceiveBeaconBuff = true;

    private Vector3 scaleBeforeBuff;

    private float nSpeed, nMaxSpeed;
    private float pMoveSpeed, pShotSpeed, pFireRate;
    private float hRep, hWarn, hCharge, hStun;

    private float appliedSizeMult = 1f;
    private float appliedNormalSpeedMult = 1f;

    public bool IsBuffed => buffed;
    public bool CanReceiveBeaconBuff => canReceiveBeaconBuff;

    private void Awake()
    {
        FindChildAura();
        SetAura(false);
        RefreshBaseValues();
    }

    public void RefreshBaseValues()
    {
        normal = GetComponent<EnemyFollow>();
        projectile = GetComponent<ProjectileEnemyFollow>();
        hunter = GetComponent<HunterEnemyFollow>();

        canReceiveBeaconBuff =
            GetComponent<BeaconEnemy>() == null &&
            GetComponent<BossEnemyFollow>() == null &&
            GetComponent<MiniBossFollow>() == null &&
            GetComponentInParent<BeaconEnemy>() == null &&
            GetComponentInParent<BossEnemyFollow>() == null &&
            GetComponentInParent<MiniBossFollow>() == null;

        if (normal != null)
        {
            nSpeed = normal.speed;
            nMaxSpeed = normal.maxSpeed;
        }

        if (projectile != null)
        {
            pMoveSpeed = projectile.moveSpeed;
            pShotSpeed = projectile.projectileSpeed;
            pFireRate = projectile.fireRate;
        }

        if (hunter != null)
        {
            hRep = hunter.repositionTime;
            hWarn = hunter.warningDuration;
            hCharge = hunter.chargeSpeed;
            hStun = hunter.stunDuration;
        }
    }

    private void FindChildAura()
    {
        Transform aura = transform.Find("BuffAura");

        if (aura != null)
            buffAura = aura.gameObject;
    }

    private void SetAura(bool state)
    {
        if (buffAura != null && buffAura.activeSelf != state)
            buffAura.SetActive(state);
    }

    public void ApplyBeaconBuff(
        float sizeMult,
        float nSpeedMult,
        float nMaxMult,
        float pMoveMult,
        float pShotMult,
        float pFireMult,
        float hRepMult,
        float hWarnMult,
        float hChargeMult,
        float hStunMult
    )
    {
        if (!canReceiveBeaconBuff) return;
        if (buffed) return;

        RefreshBaseValues();

        if (!canReceiveBeaconBuff) return;

        buffed = true;

        appliedSizeMult = sizeMult;
        appliedNormalSpeedMult = nSpeedMult;
        scaleBeforeBuff = transform.localScale;

        if (buffRoutine != null)
            StopCoroutine(buffRoutine);

        buffRoutine = StartCoroutine(BuffDurationRoutine());

        SetAura(true);

        transform.localScale = scaleBeforeBuff * sizeMult;

        if (normal != null)
        {
            nSpeed = normal.speed;
            nMaxSpeed = normal.maxSpeed;

            normal.speed = nSpeed * nSpeedMult;
            normal.maxSpeed = nMaxSpeed * nMaxMult;
        }

        if (projectile != null)
        {
            pMoveSpeed = projectile.moveSpeed;
            pShotSpeed = projectile.projectileSpeed;
            pFireRate = projectile.fireRate;

            projectile.moveSpeed = pMoveSpeed * pMoveMult;
            projectile.projectileSpeed = pShotSpeed * pShotMult;
            projectile.fireRate = pFireRate / pFireMult;
        }

        if (hunter != null)
        {
            hRep = hunter.repositionTime;
            hWarn = hunter.warningDuration;
            hCharge = hunter.chargeSpeed;
            hStun = hunter.stunDuration;

            hunter.repositionTime = hRep * hRepMult;
            hunter.warningDuration = hWarn * hWarnMult;
            hunter.chargeSpeed = hCharge * hChargeMult;
            hunter.stunDuration = hStun * hStunMult;
        }
    }

    private IEnumerator BuffDurationRoutine()
    {
        yield return new WaitForSeconds(buffDuration);

        RemoveBeaconBuff();
        buffRoutine = null;
    }

    public void RemoveBeaconBuff()
    {
        if (!buffed) return;

        buffed = false;

        SetAura(false);

        if (appliedSizeMult > 0.001f)
            transform.localScale = transform.localScale / appliedSizeMult;
        else
            transform.localScale = scaleBeforeBuff;

        if (normal != null)
        {
            if (appliedNormalSpeedMult > 0.001f)
                normal.speed = normal.speed / appliedNormalSpeedMult;
            else
                normal.speed = nSpeed;

            normal.maxSpeed = nMaxSpeed;

            if (normal.speed > normal.maxSpeed)
                normal.speed = normal.maxSpeed;
        }

        if (projectile != null)
        {
            projectile.moveSpeed = pMoveSpeed;
            projectile.projectileSpeed = pShotSpeed;
            projectile.fireRate = pFireRate;
        }

        if (hunter != null)
        {
            hunter.repositionTime = hRep;
            hunter.warningDuration = hWarn;
            hunter.chargeSpeed = hCharge;
            hunter.stunDuration = hStun;
        }

        appliedSizeMult = 1f;
        appliedNormalSpeedMult = 1f;
    }
}