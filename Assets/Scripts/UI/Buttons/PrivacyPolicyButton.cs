using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class PrivacyPolicyButton : MonoBehaviour
{
    private const string DefaultPrivacyPolicyUrl =
        "https://ardagencdev.github.io/fateful-rush-privacy/";

    [SerializeField]
    private string privacyPolicyUrl = DefaultPrivacyPolicyUrl;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (button == null)
            button = GetComponent<Button>();

        button.onClick.RemoveListener(OpenPrivacyPolicy);
        button.onClick.AddListener(OpenPrivacyPolicy);
    }

    private void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(OpenPrivacyPolicy);
    }

    public void OpenPrivacyPolicy()
    {
        if (!Uri.TryCreate(
                privacyPolicyUrl,
                UriKind.Absolute,
                out Uri uri) ||
            (uri.Scheme != Uri.UriSchemeHttp &&
             uri.Scheme != Uri.UriSchemeHttps))
        {
            Debug.LogError(
                "PrivacyPolicyButton has an invalid HTTP/HTTPS URL.",
                this
            );
            return;
        }

        Application.OpenURL(uri.AbsoluteUri);
    }
}
