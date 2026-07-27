using System.Collections;
using UnityEngine;

public class MainMenuStarColorRandomizer : MonoBehaviour
{
    public static MainMenuStarColorRandomizer Instance
    {
        get;
        private set;
    }

    [Header("Reference")]
    [SerializeField]
    private ParticleSystem nearStars;

    [Header("Panel Colors")]
    [SerializeField]
    private Color mainMenuColor =
        new Color(0.65f, 0.2f, 1f, 0.9f);

    [SerializeField]
    private Color levelSelectionColor =
        new Color(0.2f, 0.6f, 1f, 0.9f);

    [SerializeField]
    private Color missionBriefingColor =
        new Color(1f, 0.2f, 0.55f, 0.9f);

    [SerializeField]
    private Color optionsColor =
        new Color(0.2f, 1f, 0.65f, 0.9f);

    [SerializeField]
    private Color statsColor =
        new Color(1f, 0.75f, 0.2f, 0.9f);

    [Header("Transition")]
    [SerializeField, Min(0.01f)]
    private float transitionDuration = 0.45f;

    private ParticleSystem.Particle[] particles;
    private Coroutine colorRoutine;

    private Color currentColor;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (nearStars == null)
            nearStars = GetComponent<ParticleSystem>();

        currentColor = mainMenuColor;
        ApplyColorInstant(currentColor);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void ShowMainMenuColor()
    {
        ChangeColor(mainMenuColor);
    }

    public void ShowLevelSelectionColor()
    {
        ChangeColor(levelSelectionColor);
    }

    public void ShowMissionBriefingColor()
    {
        ChangeColor(missionBriefingColor);
    }

    public void ShowOptionsColor()
    {
        ChangeColor(optionsColor);
    }

    public void ShowStatsColor()
    {
        ChangeColor(statsColor);
    }

    private void ChangeColor(Color targetColor)
    {
        if (nearStars == null)
            return;

        if (colorRoutine != null)
            StopCoroutine(colorRoutine);

        colorRoutine = StartCoroutine(
            ColorTransitionRoutine(targetColor)
        );
    }

    private IEnumerator ColorTransitionRoutine(
        Color targetColor
    )
    {
        Color startColor = currentColor;
        float timer = 0f;

        while (timer < transitionDuration)
        {
            timer += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                timer / transitionDuration
            );

            float easedProgress =
                EaseInOutCubic(progress);

            currentColor = Color.Lerp(
                startColor,
                targetColor,
                easedProgress
            );

            ApplyColorInstant(currentColor);

            yield return null;
        }

        currentColor = targetColor;
        ApplyColorInstant(currentColor);

        colorRoutine = null;
    }

    private void ApplyColorInstant(Color color)
    {
        if (nearStars == null)
            return;

        ParticleSystem.MainModule main =
            nearStars.main;

        main.startColor =
            new ParticleSystem.MinMaxGradient(color);

        int requiredSize =
            Mathf.Max(1, main.maxParticles);

        if (particles == null ||
            particles.Length < requiredSize)
        {
            particles =
                new ParticleSystem.Particle[
                    requiredSize
                ];
        }

        int particleCount =
            nearStars.GetParticles(particles);

        for (int i = 0;
             i < particleCount;
             i++)
        {
            particles[i].startColor = color;
        }

        nearStars.SetParticles(
            particles,
            particleCount
        );
    }

    private void OnValidate()
    {
        if (nearStars == null)
            nearStars = GetComponent<ParticleSystem>();

        transitionDuration =
            Mathf.Max(0.01f, transitionDuration);
    }

    private static float EaseInOutCubic(
        float value
    )
    {
        value = Mathf.Clamp01(value);

        if (value < 0.5f)
            return 4f * value * value * value;

        float inverse =
            -2f * value + 2f;

        return 1f -
               inverse *
               inverse *
               inverse /
               2f;
    }
}