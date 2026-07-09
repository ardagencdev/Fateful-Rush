using UnityEngine;

public class CameraBoundsWalls : MonoBehaviour
{
    public BoxCollider2D leftWall, rightWall, topWall, bottomWall;
    public float thickness = 1f;

    private CameraWorldBounds bounds;

    private void Start()
    {
        bounds = CameraWorldBounds.Instance;

        if (bounds == null)
        {
            Debug.LogError("CameraBoundsWalls: Sahnede aktif CameraWorldBounds yok!");
            enabled = false;
            return;
        }

        if (leftWall == null || rightWall == null || topWall == null || bottomWall == null)
        {
            Debug.LogError("CameraBoundsWalls: Wall collider referanslarından biri eksik!");
            enabled = false;
            return;
        }

        UpdateWalls();
    }

    private void LateUpdate()
    {
        if (bounds == null) return;
        UpdateWalls();
    }

    private void UpdateWalls()
    {
        float minX = bounds.MinX;
        float maxX = bounds.MaxX;
        float minY = bounds.MinY;
        float maxY = bounds.MaxY;

        float width = maxX - minX;
        float height = maxY - minY;

        float centerX = (minX + maxX) / 2f;
        float centerY = (minY + maxY) / 2f;

        leftWall.transform.position = new Vector2(minX - thickness / 2f, centerY);
        leftWall.size = new Vector2(thickness, height + thickness * 2f);

        rightWall.transform.position = new Vector2(maxX + thickness / 2f, centerY);
        rightWall.size = new Vector2(thickness, height + thickness * 2f);

        topWall.transform.position = new Vector2(centerX, maxY + thickness / 2f);
        topWall.size = new Vector2(width + thickness * 2f, thickness);

        bottomWall.transform.position = new Vector2(centerX, minY - thickness / 2f);
        bottomWall.size = new Vector2(width + thickness * 2f, thickness);
    }
}