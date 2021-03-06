﻿using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UWE;

namespace NitroxClient.MonoBehaviours.Gui.InGame
{
    // TODO: Prevent closing of window when clicking outside of it.
    // TODO: Refactor to a standalone modal so that it can be used in the main menu.
    /// <summary>
    ///     Extends the IngameMenu with a player kicked popup.
    /// </summary>
    public class PlayerKickedModal : MonoBehaviour
    {
        public const string SUB_WINDOW_NAME = "PlayerKicked";
        private static GameObject playerKickedSubWindow;
        public static PlayerKickedModal Instance { get; private set; }

        public void Show()
        {
            FreezeTime.Begin("NitroxDisconnected");
            StartCoroutine(Show_Impl());
        }

        private static void OnLanguageChanged()
        {
            if (!playerKickedSubWindow)
            {
                return;
            }

            GameObject header = playerKickedSubWindow.FindChild("Header"); //Message Object
            Text messageText = header.GetComponent<Text>();

            // TODO: Implement a NitroxLang wrapper on Subnautica Language.
            switch (Language.main.GetCurrentLanguage())
            {
                case "Spanish":
                    messageText.text = "Has sido expulsado del servidor";
                    break;
                default:
                    messageText.text = "You have been kicked from the server";
                    break;
            }
        }

        private static void InitSubWindow()
        {
            if (!IngameMenu.main)
            {
                throw new NotSupportedException($"Cannot show ingame subwindow {SUB_WINDOW_NAME} because the ingame window does not exist.");
            }

            if (!playerKickedSubWindow)
            {
                GameObject derivedSubWindow = IngameMenu.main.transform.Find("QuitConfirmation").gameObject;
                playerKickedSubWindow = Instantiate(derivedSubWindow, IngameMenu.main.transform, false);
                playerKickedSubWindow.name = SUB_WINDOW_NAME;

                // Styling.
                RectTransform main = playerKickedSubWindow.GetComponent<RectTransform>();
                main.sizeDelta = new Vector2(700, 195);

                DestroyImmediate(playerKickedSubWindow.FindChild("ButtonNo")); // Delete Button No

                GameObject header = playerKickedSubWindow.FindChild("Header"); //Message Object

                RectTransform messageTransform = header.GetComponent<RectTransform>();
                messageTransform.sizeDelta = new Vector2(700, 195);

                GameObject buttonYes = playerKickedSubWindow.FindChild("ButtonYes"); //Button Yes Object
                buttonYes.transform.position = new Vector3(playerKickedSubWindow.transform.position.x / 2, buttonYes.transform.position.y, buttonYes.transform.position.z); // Center Button

                Text messageTextbutton = buttonYes.GetComponentInChildren<Text>(); //Get Button Text Component
                messageTextbutton.text = "OK";

                // Init to current language.
                OnLanguageChanged();
            }
        }

        private void Start()
        {
            if (Instance)
            {
                throw new NotSupportedException($"Only one {nameof(PlayerKickedModal)} must be active at any time.");
            }

            Instance = this;
        }

        private void OnEnable()
        {
            Language.main.OnLanguageChanged += OnLanguageChanged;
        }

        private void OnDisable()
        {
            Language.main.OnLanguageChanged -= OnLanguageChanged;
        }

        private void OnDestroy()
        {
            Instance = null;
        }

        private IEnumerator Show_Impl()
        {
            // Execute frame-by-frame to allow UI scripts to initialize.
            InitSubWindow();
            yield return null;
            IngameMenu.main.Open();
            yield return null;
            IngameMenu.main.ChangeSubscreen(SUB_WINDOW_NAME);
        }
    }
}
