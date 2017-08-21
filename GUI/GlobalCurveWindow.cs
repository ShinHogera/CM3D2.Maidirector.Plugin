using System;
using System.Reflection;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace CM3D2.HandmaidsTale.Plugin
{
    public static class GlobalCurveWindow
    {
        static Texture2D textureBack { get; set; }
        static Texture2D textureBackNarrow { get; set; }

        static GlobalCurveWindow() {
            // instantiate textures first, before using them in window
            {
                textureBack = new Texture2D(128, 128);

                Color[] color = new Color[128 * 128];
                for (int i = 0; i < color.Length; i++)
                {
                    color[i] = Color.clear;
                }
                textureBack.SetPixels(color);
                textureBack.Apply();
            }

            {
                textureBackNarrow = new Texture2D(128, 32);
                Color[] color = new Color[128 * 32];
                for (int i = 0; i < color.Length; i++)
                {
                    color[i] = Color.clear;
                }
                textureBackNarrow.SetPixels(color);
                textureBackNarrow.Apply();
            }

            curve = new CurveWindow(302);
            gsWin = new GUIStyle("box");
            gsWin.fontSize = Util.GetPix(12);
            gsWin.alignment = TextAnchor.UpperRight;
        }

        public static Texture2D CreateCurveTexture(AnimationCurve curve, bool bNarrow)
        {
            Texture2D tex;
            if (bNarrow)
                tex = (Texture2D)UnityEngine.Object.Instantiate(GlobalCurveWindow.textureBackNarrow);
            else
                tex = (Texture2D)UnityEngine.Object.Instantiate(GlobalCurveWindow.textureBack);
            int width = tex.width;
            int height = tex.height;
            Color[] color = tex.GetPixels();
            for (int x = 0; x < width; x++)
            {
                float f = Mathf.Clamp01(curve.Evaluate(x / (float)width));
                color[x + (int)(f * (height - 1)) * width] = Color.green;
            }
            tex.SetPixels(color);
            tex.Apply();
            return tex;
        }

        public static void Update()
        {
            if(curve.show)
            {
                curve.rect = GUI.Window(curve.WINDOW_ID, curve.rect, curve.GuiFunc, string.Empty, gsWin);
            }
        }

        public static bool Visible
        {
            get
            {
                return curve.show;
            }
        }

        public static void Set(Vector2 p, float fWidth, int iFontSize, AnimationCurve _curve, Action<AnimationCurve> f)
        {
            curve.Set(p, fWidth, iFontSize, _curve, f);
        }

        private static GUIStyle gsWin;
        private static CurveWindow curve;

        internal class CurveWindow
        {
            public readonly int WINDOW_ID;

            public Rect rect { get; set; }
            private float fMargin { get; set; }
            private float fRightPos { get; set; }
            private float fUpPos { get; set; }

            public bool show { get; private set; }
            public bool narrowSlider { get; set; }
            private bool changed;

            public Action<AnimationCurve> func { get; private set; }

            private static GUIStyle gsLabel { get; set; }
            private static GUIStyle gsButton { get; set; }
            private static GUIStyle gsText { get; set; }

            private Texture2D texture { get; set; }
            private AnimationCurve curve { get; set; }
            private Keyframe[] keys { get; set; }

            private float[] fCurve { get; set; }

            private string[] sValues { get; set; }

            private bool isGuiTranslation = false;

            private CustomDragPoint cdp;

            public CurveWindow(int iWIndowID)
            {
                WINDOW_ID = iWIndowID;

                texture = (Texture2D)UnityEngine.Object.Instantiate(GlobalCurveWindow.textureBack);

                fCurve = new float[4];
                keys = new Keyframe[2];

                sValues = new string[4];

                gsLabel = new GUIStyle("label");
                gsLabel.alignment = TextAnchor.MiddleLeft;

                gsButton = new GUIStyle("button");
                gsButton.alignment = TextAnchor.MiddleCenter;

                gsText = new GUIStyle("textarea");
                gsText.alignment = TextAnchor.UpperLeft;

                cdp = new CustomDragPoint();
            }

            public void Set(Vector2 p, float fWidth, int iFontSize, AnimationCurve _curve, Action<AnimationCurve> f)
            {
                rect = new Rect(p.x - fWidth, p.y, fWidth, 0f);
                fRightPos = p.x + fWidth;
                fUpPos = p.y;

                gsLabel.fontSize = iFontSize;
                gsButton.fontSize = iFontSize;
                gsText.fontSize = iFontSize;

                fMargin = iFontSize * 0.3f;

                func = f;

                curve = _curve;

                keys = new Keyframe[_curve.keys.Length];
                for(int i = 0; i < keys.Length; i++)
                {
                    keys[i] = _curve.keys[i];
                }
                keys[0] = _curve.keys[0];
                keys[keys.Length - 1] = _curve.keys[keys.Length - 1];

                fCurve = new float[keys.Length * 2];
                for (int i = 0; i < keys.Length; i++)
                {
                    fCurve[i*2] = keys[i].outTangent;
                    fCurve[i*2 + 1] = keys[i].value;
                }
               
                sValues[0] = keys[0].outTangent.ToString();
                sValues[1] = keys[0].value.ToString();
                sValues[2] = keys[keys.Length - 1].inTangent.ToString();
                sValues[3] = keys[keys.Length - 1].value.ToString();

                texture = GlobalCurveWindow.CreateCurveTexture(curve, false);

                show = true;
                changed = true;
            }

            private void CreateCurve()
            {
                curve = new AnimationCurve(keys);
                texture = GlobalCurveWindow.CreateCurveTexture(curve, false);
            }

            private Vector2 dragButton(Vector2 start, Rect rectItem)
            {
                Vector2 screenOffset = new Vector2(this.rect.x, this.rect.y);

                bool clicked = false;
                float maxWidth = 1.0f;
                float maxHeight = 1.0f;
                start.y = maxHeight - start.y;
                Vector2 normalized = new Vector2((start.x / maxWidth) * rectItem.width, (start.y / maxHeight) * rectItem.height);
                
                Rect pointRect = new Rect(normalized.x - 7, normalized.y - 7, 15, 15);

                if (GUI.RepeatButton(pointRect, "o"))
                {
                    clicked = true;
                    Vector2 mousePos = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                    normalized = new Vector2(mousePos.x - screenOffset.x, mousePos.y - screenOffset.y);

                    normalized.x = Mathf.Clamp(normalized.x, 0, rectItem.width);
                    normalized.y = Mathf.Clamp(normalized.y, 0, rectItem.height);

                    start.x = (normalized.x / rectItem.width) * maxWidth;
                    start.y = (normalized.y / rectItem.height) * maxHeight;
                }
                start.y = maxHeight - start.y;

                if(clicked)
                {
                    Rect labelRect = new Rect(normalized.x, normalized.y, 200, 100);
                    GUI.Label(labelRect, start.ToString());
                }
                return start;
            }

            public void GuiFunc(int winId)
            {
                int iFontSize = gsLabel.fontSize;
                Rect rectItem = new Rect(iFontSize * 0.5f, iFontSize * 0.5f, iFontSize, rect.width - iFontSize * 3);

                float fTmp;

                fTmp = GUI.VerticalSlider(rectItem, keys[0].value, 1f, 0f);
                if (fTmp != keys[0].value)
                {
                    keys[0].value = fTmp;
                    sValues[1] = fTmp.ToString();
                }

                rectItem.x = rect.width - rectItem.width - iFontSize * 0.5f;
                fTmp = GUI.VerticalSlider(rectItem, keys[keys.Length - 1].value, 1f, 0f);
                if (fTmp != keys[keys.Length - 1].value)
                {
                    keys[keys.Length - 1].value = fTmp;
                    sValues[3] = fTmp.ToString();
                }

                rectItem.x = rectItem.width + iFontSize * 0.5f;
                rectItem.width = rectItem.height;
                GUI.DrawTexture(rectItem, texture);


                for (int i = 1; i < keys.Length - 1; i++)
                {
                    Vector2 pos = new Vector2(keys[i].time, keys[i].value);
                    pos = this.dragButton(pos, rectItem);

                    if(pos.x != keys[i].time)
                    {
                        changed = true;
                        keys[i].time = pos.x;
                    }
                    if (pos.y != keys[i].value)
                    {
                        changed = true;
                        keys[i].value = pos.y;
                    }
                }


                rectItem.x = iFontSize * 0.5f;
                rectItem.width = (rect.width - iFontSize) / 2f;
                rectItem.y += rectItem.height;
                rectItem.height = iFontSize * 1.5f;
                string sTmp = Util.DrawTextFieldF(rectItem, sValues[1], gsText);
                if (sTmp != sValues[1])
                {
                    if (float.TryParse(sTmp, out fTmp))
                    {
                        keys[0].value = Mathf.Clamp01(fTmp);
                        sTmp = keys[0].value.ToString();
                    }
                    sValues[1] = sTmp;
                }

                rectItem.x += rectItem.width;
                sTmp = Util.DrawTextFieldF(rectItem, sValues[3], gsText);
                if (sTmp != sValues[3])
                {
                    if (float.TryParse(sTmp, out fTmp))
                    {
                        keys[keys.Length - 1].value = Mathf.Clamp01(fTmp);
                        sTmp = keys[keys.Length - 1].value.ToString();
                    }

                    sValues[3] = sTmp;
                }

                //

                rectItem.x = iFontSize * 0.5f;
                rectItem.width = iFontSize * 4;
                rectItem.y += rectItem.height + fMargin;
                GUI.Label(rectItem, "Start", gsLabel);

                rectItem.width = rect.width - rectItem.width - iFontSize;
                rectItem.x = rect.width - rectItem.width - iFontSize * 0.5f;
                sTmp = Util.DrawTextFieldF(rectItem, sValues[0], gsText);

                if (sTmp != sValues[0])
                {
                    if (float.TryParse(sTmp, out fTmp))
                        keys[0].outTangent = fTmp;

                    sValues[0] = sTmp;
                }

                float fMax = narrowSlider ? 1f : 10f;

                rectItem.x = iFontSize * 0.5f;
                rectItem.width = rect.width - iFontSize * 3;
                rectItem.y += rectItem.height;
                fTmp = GUI.HorizontalSlider(rectItem, keys[0].outTangent, -fMax, fMax);
                if (fTmp != keys[0].outTangent)
                {
                    keys[0].outTangent = fTmp;
                    sValues[0] = fTmp.ToString();
                }

                rectItem.x += rectItem.width;
                rectItem.width = iFontSize * 2;
                if (GUI.Button(rectItem, ConstantValues.GUIBUTTON_DEF, gsButton))
                {
                    keys[0].outTangent = 1f;
                    sValues[0] = keys[0].outTangent.ToString();
                }


                rectItem.x = iFontSize * 0.5f;
                rectItem.width = iFontSize * 4;
                rectItem.y += rectItem.height + fMargin;
                GUI.Label(rectItem, "End", gsLabel);

                rectItem.width = rect.width - rectItem.width - iFontSize;
                rectItem.x = rect.width - rectItem.width - iFontSize * 0.5f;
                sTmp = Util.DrawTextFieldF(rectItem, sValues[2], gsText);
                if (sTmp != sValues[2])
                {
                    if (float.TryParse(sTmp, out fTmp))
                        keys[keys.Length - 1].inTangent = fTmp;

                    sValues[2] = sTmp;
                }

                rectItem.x = iFontSize * 0.5f;
                rectItem.width = rect.width - iFontSize * 3;
                rectItem.y += rectItem.height;
                fTmp = GUI.HorizontalSlider(rectItem, keys[keys.Length - 1].inTangent, -fMax, fMax);
                if (fTmp != keys[keys.Length - 1].inTangent)
                {
                    keys[keys.Length - 1].inTangent = fTmp;
                    sValues[2] = fTmp.ToString();
                }

                rectItem.x += rectItem.width;
                rectItem.width = iFontSize * 2;
                if (GUI.Button(rectItem, ConstantValues.GUIBUTTON_DEF, gsButton))
                {
                    keys[keys.Length - 1].inTangent = 1f;
                    sValues[2] = keys[keys.Length - 1].inTangent.ToString();
                }


                rectItem.width = iFontSize * 5f;
                rectItem.x = iFontSize * 0.5f;
                rectItem.y += rectItem.height + fMargin;
                if (GUI.Button(rectItem, isGuiTranslation ? (narrowSlider ? "幅を広く" : "幅を狭く") : (narrowSlider ? "Broad" : "Narrow"), gsButton))
                {
                    narrowSlider = !narrowSlider;
                }


                rectItem.x = rect.width - iFontSize * 5.5f;
                if (GUI.Button(rectItem, isGuiTranslation ? "全値反転" : "Reverse", gsButton))
                {
                    keys[0].value = 1f - keys[0].value;
                    sValues[1] = keys[0].value.ToString();

                    keys[keys.Length - 1].value = 1f - keys[keys.Length - 1].value;
                    sValues[3] = keys[keys.Length - 1].value.ToString();

                    keys[0].outTangent *= -1;
                    sValues[0] = keys[0].outTangent.ToString();

                    keys[keys.Length - 1].inTangent *= -1;
                    sValues[2] = keys[keys.Length - 1].inTangent.ToString();
                }


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

                if (GUI.changed || changed)
                {
                    CreateCurve();
                    func(curve);
                }

                changed = false;

                GUI.DragWindow();

                if (GetAnyMouseButtonDown())
                {
                    Vector2 v2Tmp = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                    if (!rect.Contains(v2Tmp))
                    {
                        func(curve);
                        show = false;
                    }
                }
            }

            private bool GetAnyMouseButtonDown()
            {
                return Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2);
            }

        }
    }
}
