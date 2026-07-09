using UnityEngine;

public class CameraWorldBounds : MonoBehaviour
{
    public static CameraWorldBounds Instance;

    public Camera cam;
    public float padding = 0.5f;

    public float MinX { get; private set; }
    public float MaxX { get; private set; }
    public float MinY { get; private set; }
    public float MaxY { get; private set; }

    public float Width => MaxX - MinX;
    public float Height => MaxY - MinY;
    public Vector2 Center => new Vector2((MinX + MaxX) / 2f, (MinY + MaxY) / 2f);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (cam == null)
            cam = Camera.main;

        UpdateBounds();
    }

    private void LateUpdate()
    {
        UpdateBounds();
    }

    public void UpdateBounds()
    {
        float zDist = Mathf.Abs(cam.transform.position.z);

        Vector3 bottomLeft = cam.ViewportToWorldPoint(new Vector3(0, 0, zDist));
        Vector3 topRight = cam.ViewportToWorldPoint(new Vector3(1, 1, zDist));

        MinX = bottomLeft.x + padding;
        MaxX = topRight.x - padding;
        MinY = bottomLeft.y + padding;
        MaxY = topRight.y - padding;
    }

    public Vector2 RandomPointInside(float extraPadding)
    {
        return new Vector2(
            Random.Range(MinX + extraPadding, MaxX - extraPadding),
            Random.Range(MinY + extraPadding, MaxY - extraPadding)
        );
    }
}