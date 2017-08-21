using System;
using System.Reflection;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace CM3D2.HandmaidsTale.Plugin
{
    public static class GlobalComponentPicker
    {
        static GlobalComponentPicker() {
            color = new ComponentWindow(309);
            gsWin = new GUIStyle("box");
            gsWin.fontSize = Util.GetPix(12);
            gsWin.alignment = TextAnchor.UpperRight;

        }

        public static void Update()
        {
            if(color.show)
            {
                color.rect = GUI.Window(color.WINDOW_ID, color.rect, color.GuiFunc, string.Empty, gsWin);
            }
        }

        public static bool Visible
        {
            get
            {
                return color.show;
            }
        }

        public static void Set(Vector2 p, float fWidth, int iFontSize, Action<GameObject, Component> f)
        {
            color.Set(p, fWidth, iFontSize, f);
        }

        private static GUIStyle gsWin;
        private static ComponentWindow color;

        internal class ComponentWindow
        {
            public readonly int WINDOW_ID;

            public Rect rect { get; set; }
            private float fMargin { get; set; }
            private float fRightPos { get; set; }
            private float fUpPos { get; set; }

            public bool show { get; private set; }

            public Action<GameObject, Component> func { get; private set; }

            private GUIStyle gsLabel { get; set; }
            private GUIStyle gsButton { get; set; }

            private List<GameObject> gameObjects;
            private List<Component> components;

            private CustomComboBox objectBox;
            private CustomComboBox componentBox;
            private CustomButton okButton;
            private CustomButton cancelButton;

            private GameObject selectedObject
            {
                get
                {
                    return this.gameObjects[objectBox.SelectedIndex];
                }
            }

            private Component selectedComponent
            {
                get
                {
                    return this.components[componentBox.SelectedIndex];
                }
            }

            public ComponentWindow(int iWIndowID)
            {
                WINDOW_ID = iWIndowID;

                this.objectBox = new CustomComboBox();
                this.objectBox.SelectedIndexChanged += this.SelectObject;
                this.componentBox = new CustomComboBox();
                this.componentBox.SelectedIndexChanged += this.SelectComponent;

                this.okButton = new CustomButton();
                this.okButton.Text = "OK";
                this.okButton.Click = this.Ok;
                this.cancelButton = new CustomButton();
                this.cancelButton.Text = "Cancel";
                this.cancelButton.Click = this.Cancel;
            }

            public void Set(Vector2 p, float fWidth, int iFontSize, Action<GameObject, Component> f)
            {
                rect = new Rect(p.x - fWidth, p.y, fWidth, 0f);
                fRightPos = p.x + fWidth;
                fUpPos = p.y;

                gsLabel = new GUIStyle("label");
                gsLabel.fontSize = iFontSize;
                gsLabel.alignment = TextAnchor.MiddleLeft;

                gsButton = new GUIStyle("button");
                gsButton.fontSize = iFontSize;
                gsButton.alignment = TextAnchor.MiddleCenter;

                fMargin = iFontSize * 0.3f;

                func = f;

                show = true;

                this.LoadObjects();
            }

            private bool IsPermittedGameObject(GameObject go)
            {
                return go.activeInHierarchy &&
                    !go.GetComponents<Component>().Any(c => c.GetType().Name.StartsWith("UI"));
            }

            private void LoadObjects()
            {
                this.gameObjects = UnityEngine.Object.FindObjectsOfType<GameObject>().Where(this.IsPermittedGameObject).ToList();
                this.objectBox.Items = this.gameObjects.Select(go => new GUIContent(go.name)).ToList();
                this.objectBox.SelectedIndex = 0;
            }

            private void SelectObject(object sender, EventArgs args)
            {
                int index = this.objectBox.SelectedIndex;
                if (!this.gameObjects.Any() || index < 0 || index > this.gameObjects.Count)
                    return;

                this.components = this.gameObjects[index].GetComponents<Component>().ToList();
                this.componentBox.Items = this.components.Select(co => new GUIContent(co.GetType().Name)).ToList();
                this.componentBox.SelectedIndex = 0;
            }

            private void SelectComponent(object sender, EventArgs args)
            {

            }

            private void Ok(object sender, EventArgs args)
            {
                func(this.selectedObject, this.selectedComponent);
                this.show = false;
            }

            private void Cancel(object sender, EventArgs args)
            {
                this.show = false;
            }

            public void GuiFunc(int winId)
            {
                int iFontSize = gsLabel.fontSize;
                Rect rectItem = new Rect(iFontSize * 0.5f, iFontSize * 0.5f, rect.width - iFontSize * 0.5f, iFontSize * 1.5f);

                this.objectBox.SetFromRect(rectItem);
                this.objectBox.ScreenPos = new Rect(rect.x, rect.y, 0, 0);
                this.objectBox.OnGUI();

                rectItem.y += rectItem.height;
                this.componentBox.SetFromRect(rectItem);
                this.componentBox.ScreenPos = new Rect(rect.x, rect.y, 0, 0);
                this.componentBox.OnGUI();

                rectItem.y += rectItem.height;
                rectItem.width = (rect.width - iFontSize * 0.5f) / 2;
                this.okButton.SetFromRect(rectItem);
                this.okButton.ScreenPos = new Rect(rect.x, rect.y, 0, 0);
                this.okButton.OnGUI();

                rectItem.x += rectItem.width;
                this.cancelButton.SetFromRect(rectItem);
                this.cancelButton.ScreenPos = new Rect(rect.x, rect.y, 0, 0);
                this.cancelButton.OnGUI();

                float fHeight = rectItem.y + rectItem.height + fMargin;
                if (rect.height != fHeight)
                {
                    Rect rectTmp = new Rect(rect.x, rect.y - fHeight, rect.width, fHeight);
                    rect = rectTmp;
                }
                else if (rect.x < 0f)
                {
                    Rect rectTmp = new Rect(fRightPos, rect.y, rect.width, rect.height);
                    rect = rectTmp;
                }
                else if (rect.y < 0f)
                {
                    Rect rectTmp = new Rect(rect.x, fUpPos, rect.width, rect.height);
                    rect = rectTmp;
                }

                GUI.DragWindow();
            }

            private bool GetAnyMouseButtonDown()
            {
                return Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2);
            }

        }
    }
}
