using UnityEngine;

public class Coin : MonoBehaviour
{
    [Min(1)]
    public int value = 1;

    private bool isCollected;

    public bool IsCollected => isCollected;

    /// <summary>
    /// Coin toplama işlemini yalnızca bir kez başlatır.
    /// Aynı fizik karesinde birden fazla trigger çağrısı gelse bile
    /// skorun ikinci kez eklenmesini engeller.
    /// </summary>
    public bool TryBeginCollection()
    {
        if (isCollected)
            return false;

        isCollected = true;
        DisablePhysicsAndCollisions();
        return true;
    }

    private void DisablePhysicsAndCollisions()
    {
        Collider2D[] colliders =
            GetComponentsInChildren<Collider2D>(true);

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].enabled = false;
        }

        Rigidbody2D[] rigidbodies =
            GetComponentsInChildren<Rigidbody2D>(true);

        for (int i = 0; i < rigidbodies.Length; i++)
        {
            Rigidbody2D body = rigidbodies[i];

            if (body != null)
                body.simulated = false;
        }
    }
}
