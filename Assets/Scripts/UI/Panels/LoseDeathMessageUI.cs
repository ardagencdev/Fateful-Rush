using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Displays a short randomized death message on the Lose UI.
/// Place this component on the TMP object that should show the message.
///
/// The text is chosen from LastDeathInfo.Cause whenever the Lose UI becomes active.
/// No GameResultUI reference is required.
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

    private static readonly Dictionary<string, int> LastMessageIndexByCause =
        new Dictionary<string, int>();

    private void Awake()
    {
        if (messageText == null)
            messageText = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        Refresh(LastDeathInfo.Cause);
    }

    public void Refresh()
    {
        Refresh(LastDeathInfo.Cause);
    }

    public void Refresh(string deathCause)
    {
        if (messageText == null)
            return;

        string normalizedCause = NormalizeCause(deathCause);
        string[] messages = GetMessages(normalizedCause);

        if (messages == null || messages.Length == 0)
        {
            messageText.text = string.Empty;
            return;
        }

        int selectedIndex = PickMessageIndex(
            normalizedCause,
            messages.Length
        );

        messageText.text = messages[selectedIndex];

        LastMessageIndexByCause[normalizedCause] =
            selectedIndex;
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

        // Pick from every entry except the previous one without a reroll loop.
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
}
