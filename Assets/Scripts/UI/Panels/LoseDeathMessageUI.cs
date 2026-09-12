using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Displays a short randomized death message on the Lose UI.
/// The message stays hidden while the main Lose/result intro finishes, then
/// reveals left-to-right with a typewriter effect.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(TextMeshProUGUI))]
public sealed class LoseDeathMessageUI : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private TextMeshProUGUI messageText;

    [Header("Behaviour")]
    [Tooltip("Prevents the same message from being selected twice in a row for the same death cause.")]
    [SerializeField] private bool avoidImmediateRepeat = true;

    [Header("Typewriter")]
    [Tooltip("Lose ekraninin ana giris animasyonlari bittikten sonra death message'in baslamadan once bekleyecegi sure.")]
    [SerializeField, Min(0f)]
    private float revealDelay = 0.90f;

    [Tooltip("Her gorunur karakter arasindaki sure.")]
    [SerializeField, Min(0.005f)]
    private float secondsPerCharacter = 0.032f;

    [Tooltip("Nokta, virgul, iki nokta vb. sonrasinda eklenecek kisa ekstra bekleme.")]
    [SerializeField, Min(0f)]
    private float punctuationPause = 0.055f;

    private static readonly Dictionary<string, int> LastMessageIndexByCause =
        new Dictionary<string, int>();

    private Coroutine typewriterRoutine;

    private void Awake()
    {
        if (messageText == null)
            messageText = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        Refresh(LastDeathInfo.Cause);
    }

    private void OnDisable()
    {
        StopTypewriter();

        if (messageText != null)
            messageText.maxVisibleCharacters = int.MaxValue;
    }

    public void Refresh()
    {
        Refresh(LastDeathInfo.Cause);
    }

    public void Refresh(string deathCause)
    {
        if (messageText == null)
            return;

        StopTypewriter();

        string normalizedCause = NormalizeCause(deathCause);
        string[] messages = GetMessages(normalizedCause);

        if (messages == null || messages.Length == 0)
        {
            messageText.text = string.Empty;
            messageText.maxVisibleCharacters = 0;
            return;
        }

        int selectedIndex = PickMessageIndex(
            normalizedCause,
            messages.Length
        );

        // Localization runtime can translate this full English source string
        // while it is hidden. The reveal coroutine later uses the CURRENT text,
        // so Turkish and future locales type out correctly as well.
        messageText.text = messages[selectedIndex];
        messageText.maxVisibleCharacters = 0;
        messageText.ForceMeshUpdate();

        LastMessageIndexByCause[normalizedCause] =
            selectedIndex;

        if (isActiveAndEnabled)
        {
            typewriterRoutine =
                StartCoroutine(
                    TypewriterRoutine()
                );
        }
    }

    private IEnumerator TypewriterRoutine()
    {
        // Keep the death message completely hidden until the rest of the Lose
        // presentation has had time to finish.
        float delay = Mathf.Max(0f, revealDelay);

        if (delay > 0f)
        {
            yield return new WaitForSecondsRealtime(
                delay
            );
        }

        if (messageText == null ||
            !isActiveAndEnabled)
        {
            typewriterRoutine = null;
            yield break;
        }

        // Give localization/layout one final frame to settle, then reveal the
        // localized text without changing its layout width.
        yield return null;

        messageText.ForceMeshUpdate(
            true,
            true
        );

        int characterCount =
            messageText.textInfo.characterCount;

        messageText.maxVisibleCharacters = 0;

        for (int visible = 1;
             visible <= characterCount;
             visible++)
        {
            if (messageText == null ||
                !isActiveAndEnabled)
            {
                typewriterRoutine = null;
                yield break;
            }

            messageText.maxVisibleCharacters =
                visible;

            float wait =
                Mathf.Max(
                    0.005f,
                    secondsPerCharacter
                );

            int characterIndex =
                visible - 1;

            if (characterIndex >= 0 &&
                characterIndex <
                messageText.textInfo.characterCount)
            {
                char character =
                    messageText
                        .textInfo
                        .characterInfo[characterIndex]
                        .character;

                if (IsPunctuation(character))
                {
                    wait +=
                        Mathf.Max(
                            0f,
                            punctuationPause
                        );
                }
            }

            yield return new WaitForSecondsRealtime(
                wait
            );
        }

        messageText.maxVisibleCharacters =
            int.MaxValue;

        typewriterRoutine = null;
    }

    private void StopTypewriter()
    {
        if (typewriterRoutine == null)
            return;

        StopCoroutine(typewriterRoutine);
        typewriterRoutine = null;
    }

    private static bool IsPunctuation(char character)
    {
        switch (character)
        {
            case '.':
            case ',':
            case '!':
            case '?':
            case ':':
            case ';':
                return true;

            default:
                return false;
        }
    }

    private int PickMessageIndex(
        string deathCause,
        int messageCount)
    {
        if (messageCount <= 1)
            return 0;

        if (!avoidImmediateRepeat ||
            !LastMessageIndexByCause.TryGetValue(
                deathCause,
                out int previousIndex))
        {
            return Random.Range(0, messageCount);
        }

        int index = Random.Range(0, messageCount - 1);

        if (index >= previousIndex)
            index++;

        return index;
    }

    private static string NormalizeCause(string cause)
    {
        if (string.IsNullOrWhiteSpace(cause))
            return "UNKNOWN";

        return cause
            .Trim()
            .ToUpperInvariant();
    }

    private static string[] GetMessages(string cause)
    {
        switch (cause)
        {
            case "STALKER":
                return new[]
                {
                    "YOU LET IT GET TOO CLOSE.",
                    "THE STALKER FOUND ITS OPENING.",
                    "YOU COULDN'T SHAKE IT.",
                    "IT NEVER STOPPED CHASING."
                };

            case "HUNTER":
                return new[]
                {
                    "THE HUNTER CAUGHT ITS PREY.",
                    "YOU COULDN'T OUTRUN THE HUNT.",
                    "THE HUNTER CLOSED THE DISTANCE.",
                    "THERE WAS NOWHERE LEFT TO RUN."
                };

            case "BLASTER":
                return new[]
                {
                    "YOU CROSSED THE BLASTER'S PATH.",
                    "THE BLASTER HAD YOU LINED UP.",
                    "YOU STAYED TOO CLOSE FOR TOO LONG.",
                    "THE BLASTER WON THE STANDOFF."
                };

            case "LASER BULLET":
                return new[]
                {
                    "ONE SHOT WAS ALL IT TOOK.",
                    "YOU NEVER SAW THAT SHOT COMING.",
                    "THE LASER FOUND ITS MARK.",
                    "ONE HIT ENDED THE RUN."
                };

            case "LASER WALL":
                return new[]
                {
                    "THERE WAS NO GAP TO ESCAPE.",
                    "THE LASER WALL CLOSED YOU IN.",
                    "YOU RAN OUT OF ROOM.",
                    "THE WALL LEFT NO WAY THROUGH."
                };

            case "BOSS":
                return new[]
                {
                    "THE VOID CLAIMED ANOTHER RUN.",
                    "THE BOSS OVERWHELMED YOU.",
                    "YOU COULDN'T SURVIVE ITS ATTACK.",
                    "THE BOSS ENDED YOUR RUN."
                };

            case "MINI BOSS":
                return new[]
                {
                    "YOU UNDERESTIMATED THE THREAT.",
                    "THE MINI-BOSS CAUGHT YOU OFF GUARD.",
                    "YOU DIDN'T CLEAR THE DANGER ZONE.",
                    "THE THREAT WAS SMALLER. NOT WEAKER."
                };

            case "SPACE BOMB":
                return new[]
                {
                    "YOU WERE CAUGHT IN THE BLAST.",
                    "THE EXPLOSION LEFT NO ESCAPE.",
                    "YOU STAYED TOO CLOSE TO THE BOMB.",
                    "THE BLAST RADIUS GOT YOU."
                };

            case "TIME EXPIRED":
                return new[]
                {
                    "TIME RAN OUT.",
                    "YOU NEEDED A FEW MORE SECONDS.",
                    "THE CLOCK WON THIS ROUND.",
                    "YOU RAN OUT OF TIME."
                };

            case "UNKNOWN":
            default:
                return new[]
                {
                    "THE RUN ENDED HERE.",
                    "SOMETHING WENT VERY WRONG.",
                    "THE VOID HAD OTHER PLANS.",
                    "THIS RUN WASN'T MEANT TO LAST."
                };
        }
    }

    private void OnValidate()
    {
        revealDelay =
            Mathf.Max(0f, revealDelay);

        secondsPerCharacter =
            Mathf.Max(
                0.005f,
                secondsPerCharacter
            );

        punctuationPause =
            Mathf.Max(
                0f,
                punctuationPause
            );
    }
}
