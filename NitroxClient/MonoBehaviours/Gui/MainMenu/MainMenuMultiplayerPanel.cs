﻿using System.Collections.Generic;
using System.IO;
using NitroxClient.Unity.Helper;
using NitroxModel.Core;
using UnityEngine;
using UnityEngine.UI;

namespace NitroxClient.MonoBehaviours.Gui.MainMenu
{
    public class MainMenuMultiplayerPanel : MonoBehaviour
    {
        public const string SERVER_LIST_PATH = @".\servers";
        public GameObject SavedGamesRef;
        public GameObject LoadedMultiplayerRef;

        private bool shouldFocus;
        private bool showingAddServer;
        private string serverNameInput;
        private string serverHostInput;
        private Rect addServerWindowRect = new Rect(Screen.width / 2 - 250, 200, 500, 200);
        private GameObject joinServerGameObject;

        GameObject multiplayerButton;
        Transform savedGameAreaContent;

        public void Awake()
        {
            //This sucks, but the only way around it is to establish a Subnautica resources cache and reference it everywhere we need it.
            //Given recent push-back on elaborate designs, I've just crammed it here until we can all get on the same page as far as code-quality standars are concerned.
            JoinServer.SaveGameMenuPrototype = SavedGamesRef;

            multiplayerButton = SavedGamesRef.RequireGameObject("Scroll View/Viewport/SavedGameAreaContent/NewGame");
            savedGameAreaContent = LoadedMultiplayerRef.RequireTransform("Scroll View/Viewport/SavedGameAreaContent");

            if (!File.Exists(SERVER_LIST_PATH))
            {
                AddServer("local", "127.0.0.1");
            }

            CreateButton("Add a server", ShowAddServerWindow);
            using (StreamReader sr = new StreamReader(SERVER_LIST_PATH))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    string[] lineData = line.Split('|');
                    string serverName = lineData[0];
                    string serverIp = lineData[1];
                    CreateServerButton($"<b>{serverName}</b>\n{serverIp}", serverIp);
                }
            }
        }

        public void CreateButton(string text, UnityEngine.Events.UnityAction clickEvent)
        {
            GameObject multiplayerButtonInst = Instantiate(multiplayerButton);
            Transform txt = multiplayerButtonInst.RequireTransform("NewGameButton/Text");
            txt.GetComponent<Text>().text = text;
            DestroyObject(txt.GetComponent<TranslationLiveUpdate>());
            Button multiplayerButtonButton = multiplayerButtonInst.RequireTransform("NewGameButton").GetComponent<Button>();
            multiplayerButtonButton.onClick = new Button.ButtonClickedEvent();
            multiplayerButtonButton.onClick.AddListener(clickEvent);
            multiplayerButtonInst.transform.SetParent(savedGameAreaContent, false);
        }

        public void CreateServerButton(string text, string joinIp)
        {
            GameObject multiplayerButtonInst = Instantiate(multiplayerButton);
            multiplayerButtonInst.name = (savedGameAreaContent.childCount - 1).ToString();
            Transform txt = multiplayerButtonInst.RequireTransform("NewGameButton/Text");
            txt.GetComponent<Text>().text = text;
            DestroyObject(txt.GetComponent<TranslationLiveUpdate>());
            Button multiplayerButtonButton = multiplayerButtonInst.RequireTransform("NewGameButton").GetComponent<Button>();
            multiplayerButtonButton.onClick = new Button.ButtonClickedEvent();
            multiplayerButtonButton.onClick.AddListener(() => OpenJoinServerMenu(joinIp));
            multiplayerButtonInst.transform.SetParent(savedGameAreaContent, false);

            GameObject delete = Instantiate(SavedGamesRef.GetComponent<MainMenuLoadPanel>().saveInstance.GetComponent<MainMenuLoadButton>().deleteButton);
            Button deleteButtonButton = delete.GetComponent<Button>();
            deleteButtonButton.onClick = new Button.ButtonClickedEvent();
            deleteButtonButton.onClick.AddListener(() =>
            {
                RemoveServer(multiplayerButtonInst.transform.GetSiblingIndex() - 1);
                Destroy(multiplayerButtonInst);
            });
            delete.transform.SetParent(multiplayerButtonInst.transform, false);
        }

        public void AddServer(string name, string ip)
        {
            using (StreamWriter sw = new StreamWriter(SERVER_LIST_PATH, true))
            {
                sw.WriteLine($"{name}|{ip}");
            }
        }

        public void RemoveServer(int index)
        {
            List<string> serverLines = new List<string>(File.ReadAllLines(SERVER_LIST_PATH));
            serverLines.RemoveAt(index);
            File.WriteAllLines(SERVER_LIST_PATH, serverLines.ToArray());
        }

        public void OpenJoinServerMenu(string serverIp)
        {
            NitroxServiceLocator.BeginNewLifetimeScope();

            if (joinServerGameObject != null)
            {
                Destroy(joinServerGameObject);
            }

            joinServerGameObject = new GameObject();
            JoinServer joinServerComponent = joinServerGameObject.AddComponent<JoinServer>();
            string seperator = ":";
            if (serverIp.Contains(seperator))
            {
                char splitter = ':';
                string[] splitIP = serverIp.Split(splitter);
                joinServerComponent.ServerIp = splitIP[0];
                joinServerComponent.serverPort = int.Parse(splitIP[1]);
            }
            else
            {
                joinServerComponent.ServerIp = serverIp;
                joinServerComponent.serverPort = 11000;
            }
            
        }

        public void ShowAddServerWindow()
        {
            serverNameInput = "local";
            serverHostInput = "127.0.0.1";
            showingAddServer = true;
            shouldFocus = true;
        }

        public void HideAddServerWindow()
        {
            showingAddServer = false;
            shouldFocus = true;
        }

        public void OnGUI()
        {
            if (!showingAddServer)
            {
                return;
            }

            addServerWindowRect = GUILayout.Window(GUIUtility.GetControlID(FocusType.Keyboard), addServerWindowRect, DoAddServerWindow, "Add server");
        }

        private void OnAddServerButtonClicked()
        {
            AddServer(serverNameInput, serverHostInput);
            CreateServerButton($"<b>{serverNameInput}</b>\n{serverHostInput}", serverHostInput);
            HideAddServerWindow();
        }

        private void OnCancelButtonClicked()
        {
            HideAddServerWindow();
        }

        private GUISkin GetGUISkin()
        {
            return GUISkinUtils.RegisterDerivedOnce("menus.server", s =>
            {
                s.textField.fontSize = 14;
                s.textField.richText = false;
                s.textField.alignment = TextAnchor.MiddleLeft;
                s.textField.wordWrap = true;
                s.textField.stretchHeight = true;
                s.textField.padding = new RectOffset(10, 10, 5, 5);

                s.label.fontSize = 14;
                s.label.alignment = TextAnchor.MiddleRight;
                s.label.stretchHeight = true;
                s.label.fixedWidth = 80; //change this when adding new labels that need more space.

                s.button.fontSize = 14;
                s.button.stretchHeight = true;
            });
        }

        private void DoAddServerWindow(int windowId)
        {
            Event e = Event.current;
            if (e.isKey)
            {
                switch (e.keyCode)
                {
                    case KeyCode.Return:
                        OnAddServerButtonClicked();
                        break;
                    case KeyCode.Escape:
                        OnCancelButtonClicked();
                        break;
                }
            }

            GUISkinUtils.RenderWithSkin(GetGUISkin(), () =>
            {
                using (new GUILayout.VerticalScope("Box"))
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label("Name:");
                        GUI.SetNextControlName("serverNameField");
                        // 120 so users can't go too crazy.
                        serverNameInput = GUILayout.TextField(serverNameInput, 120);
                    }

                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label("Host:");
                        GUI.SetNextControlName("serverHostField");
                        // 120 so users can't go too crazy.
                        serverHostInput = GUILayout.TextField(serverHostInput, 120);
                    }

                    if (GUILayout.Button("Add server"))
                    {
                        OnAddServerButtonClicked();
                    }

                    if (GUILayout.Button("Cancel"))
                    {
                        OnCancelButtonClicked();
                    }
                }
            });

            if (shouldFocus)
            {
                GUI.FocusControl("serverNameField");
                shouldFocus = false;
            }
        }
    }
}
