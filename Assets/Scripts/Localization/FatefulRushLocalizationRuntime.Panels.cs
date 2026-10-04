using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;

// Same Unity component. Inspector data and lifecycle entry points remain in FatefulRushLocalizationRuntime.cs.
public sealed partial class FatefulRushLocalizationRuntime
{
    private void ApplyMainMenus()
    {
        for (int i = 0; i < mainMenus.Length; i++)
        {
            MainMenu menu = mainMenus[i];
            if (menu == null)
                continue;

            TMP_Text signal = GetPrivate<TMP_Text>(menu, "signalStatusText");
            if (signal != null)
            {
                string key = SignalStatusState.IsStable
                    ? "menu.signal_stable"
                    : "menu.signal_unstable";

                string fallback = SignalStatusState.IsStable
                    ? "SIGNAL // STABLE"
                    : "SIGNAL // UNSTABLE";

                SetText(signal, FatefulRushLocalization.Text(key, fallback));
            }

            TMP_Text continueText = GetPrivate<TMP_Text>(menu, "continueLevelText");
            LevelConfig target = GetPrivate<LevelConfig>(menu, "continueTargetLevel");

            if (continueText != null && target != null && continueText.gameObject.activeSelf)
            {
                SetText(
                    continueText,
                    FatefulRushLocalization.Text("menu.level", "LEVEL {0}", target.levelNumber)
                );

                // This label intentionally previews the target level's own
                // NearStars color. Localization/theme refreshes must never
                // replace it with the selected skin UI theme color.
                menu.RefreshContinueLevelColor();
            }
        }
    }

    private void ApplyPauseObjectives()
    {
        foreach (PausePanelTransition panel in pausePanels)
            if (panel != null) panel.RefreshLocalizedText();
    }

    private static string BuildPauseObjective(LevelConfig level)
    {
        return MissionTextFormatter.PauseObjective(level);
    }

    private void ApplySkinPanels()
    {
        for (int i = 0; i < skinPanels.Length; i++)
        {
            PlayerSkinPanelUI panel = skinPanels[i];
            if (panel == null)
                continue;

            PlayerSkinCatalog catalog = GetPrivate<PlayerSkinCatalog>(panel, "skinCatalog");
            if (catalog == null || catalog.Skins == null || catalog.Skins.Count == 0)
                continue;

            int index = GetPrivate<int>(panel, "currentSkinIndex");
            index = Mathf.Clamp(index, 0, catalog.Skins.Count - 1);
            PlayerSkinCatalog.SkinEntry skin = catalog.Skins[index];
            if (skin == null)
                continue;

            bool unlocked = catalog.IsUnlocked(skin);
            bool selected = catalog.IsSelected(skin);
            string skinKey = GetSkinKey(skin.id);

            TMP_Text name = GetPrivate<TMP_Text>(panel, "skinNameText");
            TMP_Text status = GetPrivate<TMP_Text>(panel, "skinStatusText");
            TMP_Text requirement = GetPrivate<TMP_Text>(panel, "skinRequirementText");
            TMP_Text equip = GetPrivate<TMP_Text>(panel, "equipButtonText");

            if (name != null)
            {
                string fallbackName = string.IsNullOrWhiteSpace(skin.displayName)
                    ? skin.id.ToUpperInvariant()
                    : skin.displayName.ToUpperInvariant();

                SetText(name, FatefulRushLocalization.Text(skinKey, fallbackName));
            }

            if (status != null)
            {
                string statusKey = selected
                    ? "skin.status.equipped"
                    : unlocked
                        ? "skin.status.available"
                        : "skin.status.locked";

                string fallback = selected ? "EQUIPPED" : unlocked ? "AVAILABLE" : "LOCKED";
                SetText(status, FatefulRushLocalization.Text(statusKey, fallback));
            }

            if (requirement != null)
            {
                string requirementText = unlocked
                    ? string.Empty
                    : FatefulRushLocalization.Text(
                        "skin.requirement.complete_level_to_unlock",
                        "COMPLETE LEVEL {0} TO UNLOCK",
                        skin.requiredCompletedLevel
                    );

                SetText(requirement, requirementText);
            }

            if (equip != null)
            {
                string equipKey = selected
                    ? "skin.status.equipped"
                    : unlocked
                        ? "skin.status.equip"
                        : "skin.status.locked";

                string fallback = selected ? "EQUIPPED" : unlocked ? "EQUIP" : "LOCKED";
                SetText(equip, FatefulRushLocalization.Text(equipKey, fallback));
            }
        }
    }

    private void ApplyMissionBriefings()
    {
        foreach (MissionBriefingPanelUI panel in briefingPanels)
            if (panel != null) panel.RefreshLocalizedText();
    }

    private void ApplyStatsPanels()
    {
        foreach (StatsPanelUI panel in statsPanels)
            if (panel != null) panel.RefreshLocalizedText();
    }

    private void ApplyGameResults()
    {
        foreach (GameResultUI ui in resultUIs)
            if (ui != null) ui.RefreshLocalizedText();
    }

    private void ApplyDeathMessages()
    {
        for (int i = 0; i < deathMessageUIs.Length; i++)
        {
            LoseDeathMessageUI ui = deathMessageUIs[i];
            if (ui == null)
                continue;

            TMP_Text text = GetPrivate<TMP_Text>(ui, "messageText");
            if (text == null || string.IsNullOrWhiteSpace(text.text))
                continue;

            EntityId id = text.GetEntityId();
            string current = text.text.Trim();

            if (DeathEnglishToKey.TryGetValue(current, out string detectedKey))
                deathMessageKeys[id] = detectedKey;

            if (!deathMessageKeys.TryGetValue(id, out string key))
                continue;

            string englishFallback = current;
            foreach (KeyValuePair<string, string> pair in DeathEnglishToKey)
            {
                if (pair.Value == key)
                {
                    englishFallback = pair.Key;
                    break;
                }
            }

            SetText(text, FatefulRushLocalization.Text(key, englishFallback));
        }
    }

    private void ApplyFloatingTexts()
    {
        for (int i = 0; i < floatingTexts.Length; i++)
        {
            MenuFloatingText floating = floatingTexts[i];
            if (floating == null || !floating.IsInitialized || floating.ExternallyControlledMessages)
                continue;

            EntityId id = floating.GetEntityId();

            if (!floatingSourceMessages.TryGetValue(id, out string[] sourceMessages))
            {
                string[] serializedMessages = GetPrivate<string[]>(floating, "messages");
                if (serializedMessages != null && serializedMessages.Length > 0)
                {
                    sourceMessages = (string[])serializedMessages.Clone();
                    floatingSourceMessages[id] = sourceMessages;
                }
            }

            // Keep MenuFloatingText's own cycle localized too. Otherwise the
            // component would briefly write its original English array every
            // time it advances to a new message.
            List<string> usableMessages = GetPrivate<List<string>>(floating, "usableMessages");
            if (usableMessages != null && sourceMessages != null)
            {
                usableMessages.Clear();

                for (int messageIndex = 0; messageIndex < sourceMessages.Length; messageIndex++)
                {
                    string english = sourceMessages[messageIndex];
                    if (string.IsNullOrWhiteSpace(english))
                        continue;

                    if (AmbientEnglishToKey.TryGetValue(english.Trim(), out string messageKey))
                        usableMessages.Add(FatefulRushLocalization.Text(messageKey, english.Trim()));
                    else
                        usableMessages.Add(english.Trim());
                }
            }

            string stable = floating.StableText;
            string detectedKey = null;

            bool randomOrder = GetPrivate<bool>(floating, "randomOrder");
            int activeIndex = randomOrder
                ? GetPrivate<int>(floating, "previousRandomIndex")
                : GetPrivate<int>(floating, "currentMessageIndex");

            if (sourceMessages != null &&
                activeIndex >= 0 &&
                activeIndex < sourceMessages.Length &&
                AmbientEnglishToKey.TryGetValue(sourceMessages[activeIndex].Trim(), out string indexedKey))
            {
                detectedKey = indexedKey;
            }
            else if (AmbientEnglishToKey.TryGetValue(stable, out string stableKey))
            {
                detectedKey = stableKey;
            }

            if (!string.IsNullOrEmpty(detectedKey))
                floatingKeys[id] = detectedKey;

            if (!floatingKeys.TryGetValue(id, out string key))
                continue;

            string fallback = stable;
            foreach (KeyValuePair<string, string> pair in AmbientEnglishToKey)
            {
                if (pair.Value == key)
                {
                    fallback = pair.Key;
                    break;
                }
            }

            floating.SetStableText(FatefulRushLocalization.Text(key, fallback));
        }
    }

    private void ApplyGameplayHUD()
    {
        PlayerCoinCollector.Instance?.RefreshLocalizedScoreUI();
    }

    private void ApplyNearMissTexts()
    {
        for (int i = 0; i < nearMissUIs.Length; i++)
        {
            if (nearMissUIs[i] != null)
                nearMissUIs[i].RefreshLocalizedText();
        }
    }

    private void ApplyThemeColorGuards()
    {
        SkinUIButtonThemeController.RefreshCurrentScene();
    }
}
