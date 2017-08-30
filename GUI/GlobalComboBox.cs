using System;
using System.Reflection;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace CM3D2.HandmaidsTale.Plugin
{
    /// <summary>
    ///   Combobox window shared across all CustomComboBox objects.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Attempting to create a Window within a ScrollView causes weird things to happen.
    ///     This class lets the CustomComboBox objects access a window that's rendered outside the ScrollView region, which allows it to operate correctly.
    ///     It's a hacky workaround.
    ///   </para>
    /// </remarks>
    public static class GlobalComboBox
    {
        static GlobalComboBox() {
            combo = new ComboBox(300);
            gsWin = new GUIStyle("box");
            gsWin.fontSize = Util.GetPix(12);
            gsWin.alignment = TextAnchor.UpperRight;

        }

        public static void Update()
        {
            if(combo.show)
            {
                combo.rect = GUI.Window(combo.WINDOW_ID, combo.rect, combo.GuiFunc, string.Empty, gsWin);
            }
        }

        public static bool Visible
        {
            get
            {
                return combo.show;
            }
        }

        public static void Set(Rect r, float ih, GUIContent[] s, int i, Action<int> f)
        {
            combo.Set(r, ih, s, i, f);
        }

        private static GUIStyle gsWin;
        private static ComboBox combo;

        internal class ComboBox
        {
            public readonly int WINDOW_ID;

            public Rect rect { get; set; }
            private Rect rectItem { get; set; }
            public bool show { get; private set; }
            private GUIContent[] sItems { get; set; }
            private float itemHeight;

            private GUIStyle gsSelectionGrid { get; set; }
            private GUIStyleState gssBlack { get; set; }
            private GUIStyleState gssWhite { get; set; }

            public Action<int> func { get; private set; }

            private ScrollableComboBox box;
            public Vector2 scrollPos = new Vector2(0.0f, 0.0f);
            private Vector2 scrollPosOld = new Vector2(0.0f, 0.0f);

            public ComboBox(int iWIndowID)
            {
                WINDOW_ID = iWIndowID;
            }

            public void Set(Rect r, float ih, GUIContent[] s, int i, Action<int> f)
            {
                rect = r;
                sItems = s;
                itemHeight = ih;

                gsSelectionGrid = new GUIStyle();
                gsSelectionGrid.fontSize = i;

                gssBlack = new GUIStyleState();
                gssBlack.textColor = Color.white;
                gssBlack.background = Texture2D.blackTexture;

                gssWhite = new GUIStyleState();
                gssWhite.textColor = Color.black;
                gssWhite.background = Texture2D.whiteTexture;

                gsSelectionGrid.normal = gssBlack;
                gsSelectionGrid.hover = gssWhite;

                rectItem = new Rect(0f, 0f, rect.width, rect.height);

                box = new ScrollableComboBox();

                func = f;

                show = true;
            }

            public void GuiFunc(int winId)
            {
                int iTmp = -1;
                if (Input.GetMouseButtonDown(0))
                    this.scrollPosOld = this.scrollPos;

                Rect innerRect = new Rect(0f, 0f, rect.width, this.itemHeight);

                this.scrollPos = GUI.BeginScrollView(rectItem, this.scrollPos, innerRect);
                iTmp = GUI.SelectionGrid(innerRect, -1, sItems, 1, gsSelectionGrid);
                if (iTmp >= 0)
                {
                    func(iTmp);
                    show = false;
                }
                GUI.EndScrollView();

                {
                    Vector2 mousePos = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);

                    bool enableGameGui = true;
                    bool m = Input.GetAxis("Mouse ScrollWheel") != 0;
                    for (int j = 0; j < 3; j++)
                    {
                        m |= Input.GetMouseButtonDown(j);
                    }
                    if (m)
                    {
                        enableGameGui = !rect.Contains(mousePos);
                    }
                    GameMain.Instance.MainCamera.SetControl(enableGameGui);
                    UICamera.InputEnable = enableGameGui;
                }

                if (GetAnyMouseButtonDown())
                {
                    Vector2 v2Tmp = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                    if (!rect.Contains(v2Tmp))
                        show = false;
                }
            }

            private bool GetAnyMouseButtonDown()
            {
                return Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2);
            }
        }
    }
}
