using TMPro;
using UnityEngine;
using System.Collections;

public class MenuFloatingText : MonoBehaviour
{
    [Header("Reference")]
    public TMP_Text textUI;

    [Header("Float")]
    public float floatAmount = 10f;
    public float floatSpeed = 2f;

    [Header("Pulse")]
    public float pulseAmount = 0.1f;
    public float pulseSpeed = 2f;

    [Header("Rainbow")]
    public bool rainbow;
    public float rainbowSpeed = 0.2f;

    [Header("Shake")]
    public bool shake;
    public float shakeAmount = 4f;
    public float shakeSpeed = 25f;

    [Header("Fade Loop")]
    public bool fadeLoop;
    public float minAlpha = 0.25f;
    public float maxAlpha = 1f;
    public float fadeSpeed = 2f;

    [Header("Rotate")]
    public bool rotate;
    public float rotateAmount = 8f;
    public float rotateSpeed = 2f;

    [Header("Wave")]
    public bool waveMove;
    public float waveXAmount = 8f;
    public float waveYAmount = 4f;
    public float waveSpeed = 2f;

    [Header("Color Pulse")]
    public bool colorPulse;
    public Color pulseColor = Color.magenta;
    public float colorPulseSpeed = 2f;

    [Header("Typewriter")]
    public bool typewriter;
    [TextArea] public string typewriterMessage = "";
    public float typeSpeed = 0.06f;
    public bool showCursor = true;
    public string cursor = "_";
    public bool eraseAfterType;
    public float eraseSpeed = 0.04f;
    public bool loopTypewriter;
    public float waitAfterType = 1.5f;
    public float waitAfterErase = 0.5f;

    [Header("Glitch")]
    public bool glitch;
    public float glitchIntervalMin = 4f;
    public float glitchIntervalMax = 9f;
    public float glitchDuration = 0.15f;
    public float glitchPositionAmount = 4f;
    public int glitchCharacterChanges = 2;
    public string glitchCharacters = "#@$%&!?<>/\\|";

    [Header("Cursor Blink")]
    public bool cursorBlink = true;
    public float cursorBlinkSpeed = 0.5f;

    [Header("Letter Spacing")]
    public bool letterSpacingAnim;
    public float letterSpacingAmount = 8f;
    public float letterSpacingSpeed = 1.5f;

    private bool cursorVisible = true;
    private float cursorTimer;

    private bool initialized;
    private bool isGlitching;
    private Vector3 startPos;
    private Vector3 startScale;
    private Quaternion startRot;
    private Color originalColor;
    private string originalText;
    private Coroutine typeRoutine;
    private Coroutine glitchRoutine;

    private IEnumerator Start()
    {
        if (textUI == null)
            textUI = GetComponent<TMP_Text>();

        yield return null;

        startPos = transform.localPosition;
        startScale = transform.localScale;
        startRot = transform.localRotation;

        if (textUI != null)
        {
            originalColor = textUI.color;
            originalText = textUI.text;

            if (!string.IsNullOrEmpty(typewriterMessage))
                originalText = typewriterMessage;
        }

        initialized = true;

        if (typewriter && textUI != null)
            typeRoutine = StartCoroutine(TypewriterRoutine());

        if (glitch && textUI != null)
            glitchRoutine = StartCoroutine(GlitchRoutine());
    }

    private void Update()
    {
        if (!initialized) return;

        ApplyPositionEffect();
        ApplyScaleEffect();
        ApplyRotationEffect();
        ApplyColorEffect();
        ApplyLetterSpacingEffect();
        ApplyCursorBlink();
    }

    private void ApplyPositionEffect()
    {
        Vector3 finalPos = startPos;

        if (floatAmount != 0f)
            finalPos += Vector3.up * (Mathf.Sin(Time.time * floatSpeed) * floatAmount);

        if (waveMove)
        {
            float x = Mathf.Sin(Time.time * waveSpeed) * waveXAmount;
            float y = Mathf.Cos(Time.time * waveSpeed * 1.25f) * waveYAmount;
            finalPos += new Vector3(x, y, 0f);
        }

        if (shake)
        {
            float x = Mathf.Sin(Time.time * shakeSpeed) * shakeAmount;
            float y = Mathf.Cos(Time.time * shakeSpeed * 1.3f) * shakeAmount;
            finalPos += new Vector3(x, y, 0f);
        }

        if (isGlitching)
        {
            finalPos += new Vector3(
                Random.Range(-glitchPositionAmount, glitchPositionAmount),
                Random.Range(-glitchPositionAmount, glitchPositionAmount),
                0f
            );
        }

        transform.localPosition = finalPos;
    }

    private void ApplyScaleEffect()
    {
        float scale = 1f;

        if (pulseAmount != 0f)
            scale += Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;

        transform.localScale = startScale * scale;
    }

    private void ApplyRotationEffect()
    {
        if (!rotate)
        {
            transform.localRotation = startRot;
            return;
        }

        float z = Mathf.Sin(Time.time * rotateSpeed) * rotateAmount;
        transform.localRotation = startRot * Quaternion.Euler(0f, 0f, z);
    }

    private void ApplyColorEffect()
    {
        if (textUI == null) return;

        Color color = originalColor;

        if (rainbow)
            color = Color.HSVToRGB(Mathf.PingPong(Time.time * rainbowSpeed, 1f), 1f, 1f);

        if (colorPulse)
        {
            float t = (Mathf.Sin(Time.time * colorPulseSpeed) + 1f) / 2f;
            color = Color.Lerp(color, pulseColor, t);
        }

        if (fadeLoop)
        {
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, (Mathf.Sin(Time.time * fadeSpeed) + 1f) / 2f);
            color.a = alpha;
        }

        textUI.color = color;
    }

    private IEnumerator TypewriterRoutine()
    {
        do
        {
            yield return TypeText();

            if (!eraseAfterType)
            {
                yield return new WaitForSecondsRealtime(waitAfterType);
            }
            else
            {
                yield return new WaitForSecondsRealtime(waitAfterType);
                yield return EraseText();
                yield return new WaitForSecondsRealtime(waitAfterErase);
            }

        } while (loopTypewriter);
    }

    private IEnumerator TypeText()
    {
        for (int i = 0; i <= originalText.Length; i++)
        {
            SetTypedText(originalText.Substring(0, i));
            yield return new WaitForSecondsRealtime(typeSpeed);
        }
    }

    private IEnumerator EraseText()
    {
        for (int i = originalText.Length; i >= 0; i--)
        {
            SetTypedText(originalText.Substring(0, i));
            yield return new WaitForSecondsRealtime(eraseSpeed);
        }
    }

    private void SetTypedText(string value)
    {
        if (textUI == null) return;
        textUI.text = value + (showCursor ? cursor : "");
    }

    private IEnumerator GlitchRoutine()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(Random.Range(glitchIntervalMin, glitchIntervalMax));

            isGlitching = true;

            string beforeGlitch = textUI.text;
            float timer = 0f;

            while (timer < glitchDuration)
            {
                timer += Time.unscaledDeltaTime;
                textUI.text = CreateGlitchedText(beforeGlitch);
                yield return null;
            }

            textUI.text = beforeGlitch;
            isGlitching = false;
        }
    }

    private string CreateGlitchedText(string source)
    {
        if (string.IsNullOrEmpty(source))
            return source;

        char[] chars = source.ToCharArray();

        for (int i = 0; i < glitchCharacterChanges; i++)
        {
            int index = Random.Range(0, chars.Length);

            if (chars[index] == ' ')
                continue;

            chars[index] = glitchCharacters[Random.Range(0, glitchCharacters.Length)];
        }

        return new string(chars);
    }

    private void OnDisable()
    {
        if (typeRoutine != null)
            StopCoroutine(typeRoutine);

        if (glitchRoutine != null)
            StopCoroutine(glitchRoutine);
    }

    private void ApplyCursorBlink()
    {
        if (!typewriter || !cursorBlink || textUI == null || !showCursor)
            return;

        cursorTimer += Time.unscaledDeltaTime;

        if (cursorTimer >= cursorBlinkSpeed)
        {
            cursorTimer = 0f;
            cursorVisible = !cursorVisible;

            string cleanText = textUI.text.Replace(cursor, "");

            if (cursorVisible)
                textUI.text = cleanText + cursor;
            else
                textUI.text = cleanText;
        }
    }

    private void ApplyLetterSpacingEffect()
    {
        if (!letterSpacingAnim || textUI == null)
            return;

        float spacing = Mathf.Sin(Time.time * letterSpacingSpeed) * letterSpacingAmount;
        textUI.characterSpacing = spacing;
    }
}