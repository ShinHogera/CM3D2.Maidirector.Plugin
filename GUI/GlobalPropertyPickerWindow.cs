using System;
using System.Reflection;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace CM3D2.Maidirector.Plugin
{
    public static class GlobalPropertyPicker
    {
        static GlobalPropertyPicker() {
            color = new PropertyWindow(309);
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

        public static void Set(Vector2 p, float fWidth, int iFontSize, Component component, Action<PropertyInfo, FieldInfo> f)
        {
            color.Set(p, fWidth, iFontSize, component, f);
        }

        private static GUIStyle gsWin;
        private static PropertyWindow color;

        internal class PropertyWindow
        {
            public readonly int WINDOW_ID;

            public Rect rect { get; set; }
            private float fMargin { get; set; }
            private float fRightPos { get; set; }
            private float fUpPos { get; set; }

            public bool show { get; private set; }

            public Action<PropertyInfo, FieldInfo> func { get; private set; }

            private GUIStyle gsLabel { get; set; }
            private GUIStyle gsButton { get; set; }

            private List<PropertyInfo> properties;
            private List<FieldInfo> fields;

            private CustomComboBox propertyBox;
            private CustomComboBox fieldBox;
            private bool isField = false;
            private CustomButton okButton;
            private CustomButton cancelButton;

            private PropertyInfo selectedProperty
            {
                get
                {
                    if(isField)
                        return null;
                    return this.properties[propertyBox.SelectedIndex];
                }
            }

            private FieldInfo selectedField
            {
                get
                {
                    if(!isField)
                        return null;

                    return this.fields[fieldBox.SelectedIndex];
                }
            }

            public PropertyWindow(int iWIndowID)
            {
                WINDOW_ID = iWIndowID;
            }

            public void Set(Vector2 p, float fWidth, int iFontSize, Component component, Action<PropertyInfo, FieldInfo> f)
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
                isField = false;

                this.propertyBox = new CustomComboBox();
                this.fieldBox = new CustomComboBox();

                this.okButton = new CustomButton();
                this.okButton.Text = Translation.GetText("UI", "ok");
                this.okButton.Click = this.Ok;
                this.cancelButton = new CustomButton();
                this.cancelButton.Text = Translation.GetText("UI", "cancel");
                this.cancelButton.Click = this.Cancel;

                this.LoadProperties(component);
            }

            private void LoadProperties(Component component)
            {
                this.properties = component.GetType().GetProperties().Where(pr => MovieProperty.IsSupportedType(pr.PropertyType)).ToList();
                this.propertyBox.Items = this.properties.Select(pr => new GUIContent(pr.Name)).ToList();
                this.propertyBox.SelectedIndex = 0;

                this.fields = component.GetType().GetFields().Where(fi => MovieProperty.IsSupportedType(fi.FieldType)).ToList();
                this.fieldBox.Items = this.fields.Select(fi => new GUIContent(fi.Name)).ToList();
                this.fieldBox.SelectedIndex = 0;
            }

            private void Ok(object sender, EventArgs args)
            {
                func(this.selectedProperty, this.selectedField);
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

                this.isField = GUI.Toggle(rectItem, this.isField, Translation.GetText("PropertyPicker", "isField"), new GUIStyle("toggle"));

                if(isField)
                {
                    rectItem.y += rectItem.height;
                    this.fieldBox.SetFromRect(rectItem);
                    this.fieldBox.ScreenPos = new Rect(rect.x, rect.y, 0, 0);
                    this.fieldBox.OnGUI();
                    if(this.fieldBox.Items.Count == 0)
                        GUI.enabled = false;
                }
                else
                {
                    rectItem.y += rectItem.height;
                    this.propertyBox.SetFromRect(rectItem);
                    this.propertyBox.ScreenPos = new Rect(rect.x, rect.y, 0, 0);
                    this.propertyBox.OnGUI();
                    if(this.propertyBox.Items.Count == 0)
                        GUI.enabled = false;
                }

                rectItem.y += rectItem.height;
                rectItem.width = (rect.width - iFontSize * 0.5f) / 2;
                this.okButton.SetFromRect(rectItem);
                this.okButton.ScreenPos = new Rect(rect.x, rect.y, 0, 0);
                this.okButton.OnGUI();

                GUI.enabled = true;

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
