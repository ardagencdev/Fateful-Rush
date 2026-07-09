using UnityEngine;

public class ShieldRotate : MonoBehaviour
{
    [SerializeField] private float rotateSpeed = 40f;

    private void Update()
    {
        transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);
    }
}