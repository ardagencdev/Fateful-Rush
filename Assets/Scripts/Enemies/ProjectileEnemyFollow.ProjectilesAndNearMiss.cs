using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Same Unity component. Inspector data and lifecycle entry points remain in ProjectileEnemyFollow.cs.
public partial class ProjectileEnemyFollow
{
    private void ResetNearMissTracking()
    {
        nearMissPlayerCollider = null;
        nearMissArmed = false;
        nearMissTriggered = false;
        nearMissTouchedPlayer = false;
        nearMissClosestDistance = float.PositiveInfinity;
        nearMissClosestPoint = transform.position;
    }

    private void CacheNearMissPlayerCollider()
    {
        if (playerMovement == null)
            return;

        nearMissPlayerCollider =
            playerMovement.GetComponent<Collider2D>();

        if (nearMissPlayerCollider == null)
        {
            nearMissPlayerCollider =
                playerMovement.GetComponentInChildren<Collider2D>();
        }
    }

    private void TrackNearMiss()
    {
        if (!enableNearMiss ||
            nearMissTriggered ||
            nearMissTouchedPlayer ||
            playerMovement == null ||
            playerMovement.IsGameOver ||
            col == null ||
            !col.enabled)
        {
            return;
        }

        if (nearMissPlayerCollider == null)
            CacheNearMissPlayerCollider();

        if (nearMissPlayerCollider == null ||
            !nearMissPlayerCollider.enabled)
        {
            return;
        }

        ColliderDistance2D separation =
            col.Distance(nearMissPlayerCollider);

        float surfaceDistance = separation.distance;

        if (surfaceDistance <= 0f)
        {
            nearMissTouchedPlayer = true;
            return;
        }

        if (surfaceDistance <= nearMissDistance)
        {
            nearMissArmed = true;

            if (surfaceDistance < nearMissClosestDistance)
            {
                nearMissClosestDistance = surfaceDistance;
                nearMissClosestPoint = separation.pointA;
            }

            return;
        }

        if (!nearMissArmed)
            return;

        bool released =
            surfaceDistance >=
            nearMissDistance + nearMissReleaseDistance;

        if (released)
            TriggerNearMiss();
    }

    private void TriggerNearMiss()
    {
        if (nearMissTriggered ||
            !nearMissArmed ||
            nearMissTouchedPlayer)
        {
            return;
        }

        float closeness =
            NearMissFeedback.GetCloseness01(
                nearMissClosestDistance,
                nearMissDistance
            );

        nearMissTriggered = NearMissFeedback.TryTrigger(
            nearMissClosestPoint,
            closeness
        );
    }

    private GameObject GetProjectileFromPool(Vector3 position)
    {
        return RuntimeObjectPool.Spawn(
            projectilePrefab,
            position,
            Quaternion.identity
        );
    }

    private void RegisterActiveProjectile(EnemyProjectile projectile)
    {
        if (projectile == null)
            return;

        projectile.SetPoolOwner(this);

        if (!ownedProjectiles.Contains(projectile))
            ownedProjectiles.Add(projectile);
    }

    public void NotifyProjectileReturned(EnemyProjectile projectile)
    {
        if (projectile == null)
            return;

        ownedProjectiles.Remove(projectile);
    }

    public void ReturnProjectileToPool(GameObject projectile)
    {
        if (projectile == null)
            return;

        EnemyProjectile projectileScript =
            projectile.GetComponent<EnemyProjectile>();

        if (projectileScript != null)
        {
            projectileScript.ReturnToPool();
            return;
        }

        RuntimeObjectPool.Release(projectile);
    }

    private void DisableActiveProjectiles()
    {
        while (ownedProjectiles.Count > 0)
        {
            int lastIndex = ownedProjectiles.Count - 1;
            EnemyProjectile projectile = ownedProjectiles[lastIndex];
            ownedProjectiles.RemoveAt(lastIndex);

            if (projectile == null)
                continue;

            projectile.SetPoolOwner(null);

            if (projectile.gameObject.activeSelf)
                projectile.ReturnToPool();
        }
    }
}
