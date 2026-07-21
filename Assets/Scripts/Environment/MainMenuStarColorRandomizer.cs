using UnityEngine;

public class MainMenuStarColorRandomizer : MonoBehaviour
{
    [Header("Reference")]
    public ParticleSystem nearStars;

    [Header("Random Colors")]
    public Color[] possibleColors =
    {
        new Color(0.2f, 0.6f, 1f, 0.9f), // Blue
        new Color(1f, 0.2f, 0.2f, 0.9f), // Red
        new Color(0.6f, 0.2f, 1f, 0.9f), // Purple
        new Color(0.2f, 1f, 0.6f, 0.9f), // Green
        new Color(1f, 0.8f, 0.2f, 0.9f)  // Yellow
    };

    private ParticleSystem.Particle[] particles;

    private void Start()
    {
        ApplyRandomColor();
    }

    private void ApplyRandomColor()
    {
        if (nearStars == null)
            return;

        if (possibleColors == null || possibleColors.Length == 0)
            return;

        var main = nearStars.main;

        if (particles == null || particles.Length < main.maxParticles)
            particles = new ParticleSystem.Particle[main.maxParticles];

        Color randomColor =
            possibleColors[
                Random.Range(0, possibleColors.Length)
            ];

        main.startColor =
            new ParticleSystem.MinMaxGradient(randomColor);

        int count = nearStars.GetParticles(particles);

        for (int i = 0; i < count; i++)
            particles[i].startColor = randomColor;

        nearStars.SetParticles(particles, count);
    }

    private void OnValidate()
    {
        if (nearStars == null)
            nearStars = GetComponent<ParticleSystem>();
    }
}