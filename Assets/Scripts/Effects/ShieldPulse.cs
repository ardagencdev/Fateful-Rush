using UnityEngine;

public class ShieldPulse : MonoBehaviour
{
    public float pulseSpeed = 2f;
    public float pulseAmount = 0.05f;

    private Vector3 startScale;

    private void Awake()
    {
        startScale = transform.localScale;
    }

    private void Update()
    {
        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;

        transform.localScale = startScale * pulse;
    }
}