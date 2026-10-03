using UnityEngine;

#if UNITY_ANDROID && !UNITY_EDITOR
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

public sealed partial class GooglePlayGamesManager : MonoBehaviour
{
    private enum AchievementKey
    {
        FirstRush,
        WarmingUp,
        HalfwayThere,
        NoTurningBack,
        FateDefied,
        CloseCall,
        ThreadTheNeedle,
        LivingOnTheEdge,
        Untouchable,
        ComboMaster,
        MagneticAttraction,
        FirstContact,
        DivideAndConquer,
        BehindCover,
        DarkFate,
        GoldenFate,
        StillStanding,
        TooStubbornToQuit,
        EchoInitiate,
        DoubleTrouble,
        ShadowArmy,
        QuickReflexes,
        BlinkAndYouMissMe,
        BornToRush,
        Counterattack,
        Payback,
        Reaper,
        PocketChange,
        TreasureHunter,
        FortuneFavorsTheFast,
        SuitUp,
        IronResolve,
        TimeBender,
        MasterOfTime,
        BadStep,
        BombMagnet,
        GroundZero,
        BurnedOnce,
        LightShowCasualty,
        Laserproof
    }

    private const int NearMiss10Target = 10;
    private const int NearMiss50Target = 50;
    private const int NearMiss250Target = 250;
    private const int MagnetCoinTarget = 100;

    private const int Death50Target = 50;
    private const int Death100Target = 100;

    private const int Ability10Target = 10;
    private const int Ability50Target = 50;
    private const int Ability250Target = 250;

    private const int ArmorKill10Target = 10;
    private const int ArmorKill50Target = 50;
    private const int ArmorKill100Target = 100;

    private const int Coin1000Target = 1000;
    private const int Coin5000Target = 5000;
    private const int Coin10000Target = 10000;

    private const int ArmorUse50Target = 50;
    private const int ArmorUse100Target = 100;
    private const int SlowUse50Target = 50;
    private const int SlowUse100Target = 100;

    private const int BombDeath10Target = 10;
    private const int BombDeath25Target = 25;
    private const int LaserDeath10Target = 10;
    private const int LaserDeath25Target = 25;

    private static readonly bool DiagnosticsEnabled = false;

    private static GooglePlayGamesManager instance;

    private bool authenticationStarted;
    private bool authenticated;

    private string lastAuthenticationStatus = "NotStarted";
    private string lastAchievementsUiStatus = "NotRequested";

    public static bool IsAuthenticated =>
        instance != null && instance.authenticated;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        // Run save-generation migration before authentication. This prevents
        // closed-beta progress from being pushed back to Google Play on the
        // first production launch.
        ReleaseSaveMigration.ApplyIfNeeded();

        EnsureInstance();
    }

    private static GooglePlayGamesManager EnsureInstance()
    {
        if (instance != null)
            return instance;

        GooglePlayGamesManager existing =
            FindAnyObjectByType<GooglePlayGamesManager>();

        if (existing != null)
        {
            instance = existing;
            return instance;
        }

        GameObject root =
            new GameObject("GooglePlayGamesManager");

        instance =
            root.AddComponent<GooglePlayGamesManager>();

        DontDestroyOnLoad(root);
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        if (authenticationStarted)
            return;

        authenticationStarted = true;

#if UNITY_ANDROID && !UNITY_EDITOR
        // Public release: diagnostic logs/toasts are disabled.
        PlayGamesPlatform.DebugLogEnabled = DiagnosticsEnabled;

        LogDiagnostic(
            "Initialize -> starting automatic authentication."
        );

        PlayGamesPlatform.Instance.Authenticate(
            ProcessAuthentication
        );
#else
        authenticated = false;
        lastAuthenticationStatus = "NotAndroidPlayer";
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private void ProcessAuthentication(
        SignInStatus status)
    {
        lastAuthenticationStatus = status.ToString();
        authenticated =
            status == SignInStatus.Success;

        SaveDiagnosticState();

        LogDiagnostic(
            "Automatic authentication result: " + status +
            " | platformAuthenticated=" +
            PlayGamesPlatform.Instance.IsAuthenticated()
        );

        if (!authenticated)
        {
            Debug.LogWarning(
                "[GooglePlayGames] Automatic authentication failed: " +
                status
            );

            return;
        }

        Debug.Log(
            "[GooglePlayGames] Authenticated successfully."
        );

        BeginCloudSyncThenSyncAchievements();
    }
#endif

    public static void ManualSignIn()
    {
        EnsureInstance().ManualSignInInternal(false);
    }

    public static void ShowAchievementsUI()
    {
        GooglePlayGamesManager manager =
            EnsureInstance();

#if UNITY_ANDROID && !UNITY_EDITOR
        manager.LogDiagnostic(
            "Achievements button pressed. " +
            manager.BuildDiagnosticSummary()
        );

        manager.ShowAchievementsUIInternal();
#else
        manager.LogDiagnostic(
            "Achievements button pressed outside an Android player build."
        );
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private void ShowAchievementsUIInternal()
    {
        bool platformAuthenticated =
            PlayGamesPlatform.Instance.IsAuthenticated();

        LogDiagnostic(
            "ShowAchievementsUIInternal -> " +
            "localAuthenticated=" + authenticated +
            " | platformAuthenticated=" + platformAuthenticated +
            " | authStatus=" + lastAuthenticationStatus
        );

        if (!IsReady())
        {
            ShowDiagnosticToast(
                "PGS not authenticated. Trying manual sign-in..."
            );

            ManualSignInInternal(true);
            return;
        }

        ShowDiagnosticToast(
            "PGS authenticated. Opening achievements..."
        );

        PlayGamesPlatform.Instance.ShowAchievementsUI(
            ProcessAchievementsUiStatus
        );
    }

    private void ProcessAchievementsUiStatus(
        UIStatus status)
    {
        lastAchievementsUiStatus = status.ToString();
        SaveDiagnosticState();

        LogDiagnostic(
            "Achievements UI callback: " + status +
            " | " + BuildDiagnosticSummary()
        );

        ShowDiagnosticToast(
            "Achievements UI: " + status
        );
    }
#endif

    private void ManualSignInInternal(
        bool showAchievementsAfterSignIn)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        bool platformAuthenticated =
            PlayGamesPlatform.Instance.IsAuthenticated();

        LogDiagnostic(
            "ManualSignInInternal requested. " +
            "localAuthenticated=" + authenticated +
            " | platformAuthenticated=" + platformAuthenticated +
            " | showAchievementsAfterSignIn=" +
            showAchievementsAfterSignIn
        );

        if (authenticated || platformAuthenticated)
        {
            authenticated = true;
            lastAuthenticationStatus = SignInStatus.Success.ToString();
            SaveDiagnosticState();
            BeginCloudSyncThenSyncAchievements(
                showAchievementsAfterSignIn
                    ? (System.Action)ShowAchievementsUIInternal
                    : null
            );

            return;
        }

        PlayGamesPlatform.Instance.ManuallyAuthenticate(
            status =>
            {
                lastAuthenticationStatus = status.ToString();
                authenticated =
                    status == SignInStatus.Success;

                SaveDiagnosticState();

                LogDiagnostic(
                    "Manual authentication result: " + status +
                    " | platformAuthenticated=" +
                    PlayGamesPlatform.Instance.IsAuthenticated()
                );

                ShowDiagnosticToast(
                    "PGS sign-in: " + status
                );

                if (authenticated)
                {
                    BeginCloudSyncThenSyncAchievements(
                        showAchievementsAfterSignIn
                            ? (System.Action)ShowAchievementsUIInternal
                            : null
                    );
                }
                else
                {
                    Debug.LogWarning(
                        "[GooglePlayGames] Manual authentication failed: " +
                        status
                    );
                }
            }
        );
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private void ShowDiagnosticToast(
        string message)
    {
        if (!DiagnosticsEnabled ||
            string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        try
        {
            AndroidJavaClass unityPlayer =
                new AndroidJavaClass(
                    "com.unity3d.player.UnityPlayer"
                );

            AndroidJavaObject activity =
                unityPlayer.GetStatic<AndroidJavaObject>(
                    "currentActivity"
                );

            activity.Call(
                "runOnUiThread",
                new AndroidJavaRunnable(
                    () =>
                    {
                        AndroidJavaClass toastClass =
                            new AndroidJavaClass(
                                "android.widget.Toast"
                            );

                        AndroidJavaObject toast =
                            toastClass.CallStatic<AndroidJavaObject>(
                                "makeText",
                                activity,
                                message,
                                1
                            );

                        toast.Call("show");
                    }
                )
            );
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning(
                "[GPGS-DIAG] Android Toast failed: " +
                exception.Message
            );
        }
    }
#endif

}

