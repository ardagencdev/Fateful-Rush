using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

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

    [Tooltip("Bu nesne birden fazla mesaj arasında geçiş yapacaksa aç.")]
    public bool cycleMessages;

    [Tooltip(
        "Cycle Messages kapalıyken kullanılacak tek mesaj. " +
        "Boş bırakılırsa TMP üzerindeki mevcut yazı kullanılır."
    )]
    [TextArea]
    public string typewriterMessage = "";

    [Tooltip("Yalnızca Cycle Messages açıkken kullanılır.")]
    [TextArea]
    public string[] typewriterMessages =
    {
        "Prepare for the Void",
        "The Void Is Watching",
        "Your Signal Is Fading",
        "Something Moves Beyond",
        "The Dark Remembers",
        "The Silence Is Alive",
        "You Were Never Alone",
        "The Stars Won't Answer",
        "There Is No Way Back",
        "Survival Only Delays It"
    };

    public float typeSpeed = 0.06f;

    public bool showCursor = true;
    public string cursor = "_";

    public bool eraseAfterType = true;
    public float eraseSpeed = 0.035f;

    public bool loopTypewriter = true;
    public bool randomMessageOrder = true;

    public float waitAfterType = 10f;
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

    private static readonly string[] DefaultTypewriterMessages =
    {
        "Prepare for the Void",
        "The Void Is Watching",
        "Your Signal Is Fading",
        "Something Moves Beyond",
        "The Dark Remembers",
        "The Silence Is Alive",
        "You Were Never Alone",
        "The Stars Won't Answer",
        "There Is No Way Back",
        "Survival Only Delays It"
    };

    private bool initialized;
    private bool cursorVisible = true;
    private bool isGlitching;

    private float cursorTimer;
    private float cursorPositionCompensation;

    private Vector3 startPos;
    private Vector3 startScale;
    private Quaternion startRot;

    private Color originalColor;
    private string originalText = "";
    private string currentTypedText = "";

    private int currentMessageIndex;
    private int previousRandomMessageIndex = -1;

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

            CalculateCursorCompensation();
        }

        currentMessageIndex = 0;
        previousRandomMessageIndex = -1;

        cursorVisible = true;
        cursorTimer = 0f;
        currentTypedText = "";

        initialized = true;

        if (typewriter && textUI != null)
            typeRoutine = StartCoroutine(TypewriterRoutine());

        if (glitch && textUI != null)
            glitchRoutine = StartCoroutine(GlitchRoutine());
    }

    private void Update()
    {
        if (!initialized)
            return;

        ApplyPositionEffect();
        ApplyScaleEffect();
        ApplyRotationEffect();
        ApplyColorEffect();
        ApplyLetterSpacingEffect();
        ApplyCursorBlink();
    }

    private void CalculateCursorCompensation()
    {
        cursorPositionCompensation = 0f;

        if (textUI == null ||
            string.IsNullOrEmpty(cursor))
        {
            return;
        }

        textUI.ForceMeshUpdate();

        Vector2 cursorSize =
            textUI.GetPreferredValues(cursor);

        /*
         * Cursor görünürken ortalanmış metin,
         * cursor genişliğinin yarısı kadar sola gider.
         * Objeyi aynı miktarda sağa taşıyarak bunu dengeliyoruz.
         */
        cursorPositionCompensation =
            cursorSize.x * 0.5f;
    }

    private void ApplyPositionEffect()
    {
        Vector3 finalPos = startPos;

        if (floatAmount != 0f)
        {
            finalPos += Vector3.up *
                (
                    Mathf.Sin(
                        Time.time * floatSpeed
                    ) *
                    floatAmount
                );
        }

        if (waveMove)
        {
            float x =
                Mathf.Sin(
                    Time.time * waveSpeed
                ) *
                waveXAmount;

            float y =
                Mathf.Cos(
                    Time.time *
                    waveSpeed *
                    1.25f
                ) *
                waveYAmount;

            finalPos += new Vector3(
                x,
                y,
                0f
            );
        }

        if (shake)
        {
            float x =
                Mathf.Sin(
                    Time.time * shakeSpeed
                ) *
                shakeAmount;

            float y =
                Mathf.Cos(
                    Time.time *
                    shakeSpeed *
                    1.3f
                ) *
                shakeAmount;

            finalPos += new Vector3(
                x,
                y,
                0f
            );
        }

        if (isGlitching)
        {
            finalPos += new Vector3(
                Random.Range(
                    -glitchPositionAmount,
                    glitchPositionAmount
                ),
                Random.Range(
                    -glitchPositionAmount,
                    glitchPositionAmount
                ),
                0f
            );
        }

        /*
         * Cursor görünürken metin objesini yarım cursor
         * genişliği kadar sağa kaydırır. Böylece asıl cümle
         * cursor yanıp sönerken yerinden oynamaz.
         */
        if (typewriter &&
            showCursor &&
            cursorVisible &&
            !string.IsNullOrEmpty(cursor))
        {
            finalPos += Vector3.right *
                cursorPositionCompensation;
        }

        transform.localPosition = finalPos;
    }

    private void ApplyScaleEffect()
    {
        float scale = 1f;

        if (pulseAmount != 0f)
        {
            scale +=
                Mathf.Sin(
                    Time.time * pulseSpeed
                ) *
                pulseAmount;
        }

        transform.localScale =
            startScale * scale;
    }

    private void ApplyRotationEffect()
    {
        if (!rotate)
        {
            transform.localRotation = startRot;
            return;
        }

        float z =
            Mathf.Sin(
                Time.time * rotateSpeed
            ) *
            rotateAmount;

        transform.localRotation =
            startRot *
            Quaternion.Euler(
                0f,
                0f,
                z
            );
    }

    private void ApplyColorEffect()
    {
        if (textUI == null)
            return;

        Color color = originalColor;

        if (rainbow)
        {
            color = Color.HSVToRGB(
                Mathf.PingPong(
                    Time.time * rainbowSpeed,
                    1f
                ),
                1f,
                1f
            );
        }

        if (colorPulse)
        {
            float t =
                (
                    Mathf.Sin(
                        Time.time *
                        colorPulseSpeed
                    ) +
                    1f
                ) /
                2f;

            color = Color.Lerp(
                color,
                pulseColor,
                t
            );
        }

        if (fadeLoop)
        {
            color.a = Mathf.Lerp(
                minAlpha,
                maxAlpha,
                (
                    Mathf.Sin(
                        Time.time *
                        fadeSpeed
                    ) +
                    1f
                ) /
                2f
            );
        }

        textUI.color = color;
    }

    private IEnumerator TypewriterRoutine()
    {
        while (true)
        {
            string message =
                GetNextMessage();

            yield return TypeText(message);

            if (waitAfterType > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    waitAfterType
                );
            }

            if (eraseAfterType)
            {
                yield return EraseText(message);

                if (waitAfterErase > 0f)
                {
                    yield return new WaitForSecondsRealtime(
                        waitAfterErase
                    );
                }
            }

            if (!loopTypewriter)
                yield break;

            if (!eraseAfterType)
            {
                currentTypedText = "";
                RefreshTypewriterText();
            }
        }
    }

    private string GetNextMessage()
    {
        if (!cycleMessages)
        {
            if (!string.IsNullOrWhiteSpace(
                typewriterMessage
            ))
            {
                return typewriterMessage.Trim();
            }

            if (!string.IsNullOrWhiteSpace(
                originalText
            ))
            {
                return originalText.Trim();
            }

            return DefaultTypewriterMessages[0];
        }

        string[] usableMessages =
            GetUsableMessages();

        if (usableMessages.Length == 0)
            return DefaultTypewriterMessages[0];

        if (randomMessageOrder)
        {
            int selectedIndex;

            if (usableMessages.Length == 1)
            {
                selectedIndex = 0;
            }
            else
            {
                do
                {
                    selectedIndex = Random.Range(
                        0,
                        usableMessages.Length
                    );
                }
                while (
                    selectedIndex ==
                    previousRandomMessageIndex
                );
            }

            previousRandomMessageIndex =
                selectedIndex;

            return usableMessages[
                selectedIndex
            ];
        }

        if (currentMessageIndex < 0 ||
            currentMessageIndex >=
            usableMessages.Length)
        {
            currentMessageIndex = 0;
        }

        string selectedMessage =
            usableMessages[currentMessageIndex];

        currentMessageIndex =
            (
                currentMessageIndex + 1
            ) %
            usableMessages.Length;

        return selectedMessage;
    }

    private string[] GetUsableMessages()
    {
        if (typewriterMessages == null ||
            typewriterMessages.Length == 0)
        {
            return DefaultTypewriterMessages;
        }

        List<string> validMessages =
            new List<string>();

        for (int i = 0;
             i < typewriterMessages.Length;
             i++)
        {
            string message =
                typewriterMessages[i];

            if (string.IsNullOrWhiteSpace(
                message
            ))
            {
                continue;
            }

            validMessages.Add(
                message.Trim()
            );
        }

        if (validMessages.Count == 0)
            return DefaultTypewriterMessages;

        return validMessages.ToArray();
    }

    private IEnumerator TypeText(
        string message
    )
    {
        if (message == null)
            message = "";

        currentTypedText = "";

        ResetCursorBlink();
        RefreshTypewriterText();

        for (int i = 0;
             i <= message.Length;
             i++)
        {
            currentTypedText =
                message.Substring(0, i);

            RefreshTypewriterText();

            if (typeSpeed > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    typeSpeed
                );
            }
            else
            {
                yield return null;
            }
        }
    }

    private IEnumerator EraseText(
        string message
    )
    {
        if (message == null)
            message = "";

        for (int i = message.Length;
             i >= 0;
             i--)
        {
            currentTypedText =
                message.Substring(0, i);

            RefreshTypewriterText();

            if (eraseSpeed > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    eraseSpeed
                );
            }
            else
            {
                yield return null;
            }
        }

        currentTypedText = "";
        RefreshTypewriterText();
    }

    private void RefreshTypewriterText()
    {
        if (textUI == null ||
            isGlitching)
        {
            return;
        }

        textUI.text =
            BuildDisplayedText(
                currentTypedText
            );
    }

    private string BuildDisplayedText(
        string message
    )
    {
        if (message == null)
            message = "";

        if (!showCursor ||
            string.IsNullOrEmpty(cursor))
        {
            return message;
        }

        return cursorVisible
            ? message + cursor
            : message;
    }

    private IEnumerator GlitchRoutine()
    {
        while (true)
        {
            float minimumInterval =
                Mathf.Max(
                    0f,
                    glitchIntervalMin
                );

            float maximumInterval =
                Mathf.Max(
                    minimumInterval,
                    glitchIntervalMax
                );

            yield return new WaitForSecondsRealtime(
                Random.Range(
                    minimumInterval,
                    maximumInterval
                )
            );

            if (textUI == null)
                continue;

            string sourceText =
                typewriter
                    ? currentTypedText
                    : originalText;

            if (string.IsNullOrEmpty(
                sourceText
            ))
            {
                continue;
            }

            isGlitching = true;

            float timer = 0f;
            float safeDuration =
                Mathf.Max(
                    0f,
                    glitchDuration
                );

            while (timer < safeDuration)
            {
                timer +=
                    Time.unscaledDeltaTime;

                string glitchedText =
                    CreateGlitchedText(
                        sourceText
                    );

                textUI.text =
                    typewriter &&
                    showCursor &&
                    cursorVisible
                        ? glitchedText + cursor
                        : glitchedText;

                yield return null;
            }

            isGlitching = false;

            if (typewriter)
                RefreshTypewriterText();
            else
                textUI.text = originalText;
        }
    }

    private string CreateGlitchedText(
        string source
    )
    {
        if (string.IsNullOrEmpty(source) ||
            string.IsNullOrEmpty(
                glitchCharacters
            ))
        {
            return source;
        }

        char[] characters =
            source.ToCharArray();

        int changes =
            Mathf.Max(
                0,
                glitchCharacterChanges
            );

        for (int i = 0;
             i < changes;
             i++)
        {
            int index = Random.Range(
                0,
                characters.Length
            );

            if (characters[index] == ' ')
                continue;

            characters[index] =
                glitchCharacters[
                    Random.Range(
                        0,
                        glitchCharacters.Length
                    )
                ];
        }

        return new string(characters);
    }

    private void ApplyCursorBlink()
    {
        if (!typewriter ||
            !cursorBlink ||
            textUI == null ||
            !showCursor ||
            string.IsNullOrEmpty(cursor) ||
            isGlitching)
        {
            return;
        }

        cursorTimer +=
            Time.unscaledDeltaTime;

        float safeBlinkSpeed =
            Mathf.Max(
                0.05f,
                cursorBlinkSpeed
            );

        if (cursorTimer <
            safeBlinkSpeed)
        {
            return;
        }

        cursorTimer = 0f;
        cursorVisible =
            !cursorVisible;

        RefreshTypewriterText();
    }

    private void ResetCursorBlink()
    {
        cursorTimer = 0f;
        cursorVisible = true;
    }

    private void ApplyLetterSpacingEffect()
    {
        if (!letterSpacingAnim ||
            textUI == null)
        {
            return;
        }

        textUI.characterSpacing =
            Mathf.Sin(
                Time.time *
                letterSpacingSpeed
            ) *
            letterSpacingAmount;
    }

    private void OnDisable()
    {
        if (typeRoutine != null)
        {
            StopCoroutine(typeRoutine);
            typeRoutine = null;
        }

        if (glitchRoutine != null)
        {
            StopCoroutine(glitchRoutine);
            glitchRoutine = null;
        }

        isGlitching = false;
    }
}