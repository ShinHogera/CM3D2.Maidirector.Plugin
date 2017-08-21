using System;
using System.Reflection;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace CM3D2.HandmaidsTale.Plugin
{
    public static class GlobalMovieCurveWindow
    {
        static Texture2D textureBack { get; set; }
        static Texture2D textureBackNarrow { get; set; }

        static readonly int TEXTURE_WIDTH_EXPONENT = 8;
        private static int TextureWidth
        {
            get
            {
                return (int)Math.Pow(2, TEXTURE_WIDTH_EXPONENT);
            }
        }

        static Color GetColor(int i)
        {
            if ((i % 7) == 0)
                return Color.red;
            else if ((i % 7) == 1)
                return Color.green;
            else if ((i % 7) == 2)
                return Color.blue;
            else if ((i % 7) == 3)
                return Color.yellow;
            else if ((i % 7) == 4)
                return Color.magenta;
            else if ((i % 7) == 5)
                return Color.cyan;
            else
                return Color.white;
        }

        static GlobalMovieCurveWindow()
        {
            // instantiate textures first, before using them in window
            {
                textureBack = new Texture2D(TextureWidth, TextureWidth);

                Color[] color = new Color[TextureWidth * TextureWidth];
                for (int i = 0; i < color.Length; i++)
                {
                    color[i] = Color.black;
                }
                textureBack.SetPixels(color);
                textureBack.Apply();
            }

            {
                textureBackNarrow = new Texture2D(TextureWidth, (TextureWidth / 4));
                Color[] color = new Color[TextureWidth * (TextureWidth / 4)];
                for (int i = 0; i < color.Length; i++)
                {
                    color[i] = Color.black;
                }
                textureBackNarrow.SetPixels(color);
                textureBackNarrow.Apply();
            }

            curve = new CurveWindow(303);
            gsWin = new GUIStyle("box");
            gsWin.fontSize = Util.GetPix(12);
            gsWin.alignment = TextAnchor.UpperRight;
        }

        public static Texture2D CreateCurveTexture(List<MovieCurve> curves, float rangeMin, float rangeMax, bool bNarrow)
        {
            Texture2D tex;
            if (bNarrow)
                tex = (Texture2D)UnityEngine.Object.Instantiate(GlobalMovieCurveWindow.textureBackNarrow);
            else
                tex = (Texture2D)UnityEngine.Object.Instantiate(GlobalMovieCurveWindow.textureBack);

            int width = tex.width;
            int height = tex.height;
            Color[] color = tex.GetPixels();

            int i = 0;
            foreach (MovieCurve curve in curves)
            {
                AddCurveTexture(curve, ref color, width, height, rangeMin, rangeMax, i++);
            }

            tex.SetPixels(color);
            tex.Apply();
            return tex;
        }

        public static Texture2D CreateClipCurveTexture(List<MovieCurve> curves, float rangeMin, float rangeMax)
        {
            Texture2D tex = new Texture2D(128, 16);
            Color[] color = new Color[128 * 16];
            for (int j = 0; j < color.Length; j++)
            {
                color[j] = Color.black;
            }

            tex.SetPixels(color);
            tex.Apply();

            int width = tex.width;
            int height = tex.height;

            color = tex.GetPixels();

            int i = 0;
            foreach (MovieCurve curve in curves)
            {
                AddCurveTexture(curve, ref color, width, height, rangeMin, rangeMax, i++);
            }

            tex.SetPixels(color);
            tex.Apply();
            return tex;
        }

        private static void AddCurveTexture(MovieCurve curve, ref Color[] color, int width, int height, float rangeMin, float rangeMax, int i)
        {
            for (int x = 0; x < width; x++)
            {
                float f = Mathf.Clamp(curve.Evaluate(x / (float)width), rangeMin, rangeMax);
                float mapped = Util.MapRange(f, rangeMin, rangeMax, 0, 1);
                int cols = (int)(mapped * (height - 1));
                color[x + (cols * width)] = GetColor(i);
            }
        }

        public static void Update()
        {
            if (curve.show)
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
            set
            {
                curve.show = value;
            }
        }

        public static void Set(Vector2 p, float fWidth, int iFontSize, List<MovieCurve> _clip, Action<List<MovieCurve>> f)
        {
            curve.Set(p, fWidth, iFontSize, _clip, f);
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

            private float rangeMin { get; set; }
            private float rangeMax { get; set; }

            public bool show { get; set; }
            public bool narrowSlider { get; set; }
            private bool changed;

            public Action<List<MovieCurve>> func { get; private set; }

            private static GUIStyle gsLabel { get; set; }
            private static GUIStyle gsButton { get; set; }
            private static GUIStyle gsText { get; set; }
            private static GUIStyle gsComboBox { get; set; }

            private Texture2D texture { get; set; }
            private List<MovieCurve> curves { get; set; }
            private MovieKeyframe[][] keys { get; set; }

            private float[] fCurve { get; set; }

            private string[] sValues { get; set; }
            private bool[][] dragged { get; set; }

            private bool isGuiTranslation = false;
            private bool deleteCurrent;

            private int selectedIndex;
            private int selectedKeyframeIndex;
            private int selectedKeyframeCurve;

            private CustomDragPoint cdp;
            private CustomComboBox selectedBox;
            private CustomComboBox easeTypeBox;

            private MovieKeyframe selectedKeyframe
            {
                get
                {
                    return this.keys[this.selectedKeyframeCurve][this.selectedKeyframeIndex];
                }
            }

            public CurveWindow(int iWIndowID)
            {
                WINDOW_ID = iWIndowID;

                texture = (Texture2D)UnityEngine.Object.Instantiate(GlobalMovieCurveWindow.textureBack);

                fCurve = new float[4];
                keys = new MovieKeyframe[2][];

                sValues = new string[4];

                gsLabel = new GUIStyle("label");
                gsLabel.alignment = TextAnchor.MiddleLeft;

                gsButton = new GUIStyle("button");
                gsButton.alignment = TextAnchor.MiddleCenter;

                gsText = new GUIStyle("textarea");
                gsText.alignment = TextAnchor.UpperLeft;

                gsComboBox = new GUIStyle();
                gsComboBox.normal.textColor = Color.white;
                gsComboBox.onHover.background = gsComboBox.hover.background = new Texture2D(2, 2);
                gsComboBox.padding.left = gsComboBox.padding.right = gsComboBox.padding.top = gsComboBox.padding.bottom = 0;
                gsComboBox.fontSize = 12;

                cdp = new CustomDragPoint();
            }

            public void Set(Vector2 p, float fWidth, int iFontSize, List<MovieCurve> _curves, Action<List<MovieCurve>> f)
            {
                if (!_curves.Any())
                    return;

                rect = new Rect(p.x - fWidth, p.y, fWidth, 0f);
                fRightPos = p.x + fWidth;
                fUpPos = p.y;

                gsLabel.fontSize = iFontSize;
                gsButton.fontSize = iFontSize;
                gsText.fontSize = iFontSize;

                rangeMin = _curves.OrderBy(curve => curve.minValue).First().minValue * 1.25f;
                rangeMax = _curves.OrderByDescending(curve => curve.maxValue).First().maxValue * 1.25f;
                selectedIndex = 0;

                fMargin = iFontSize * 0.3f;

                func = f;

                curves = _curves;

                keys = new MovieKeyframe[curves.Count][];
                for (int i = 0; i < curves.Count; i++)
                {
                    MovieKeyframe[] ckeys = _curves[i].keyframes.ToArray();
                    keys[i] = new MovieKeyframe[ckeys.Count()];
                    for (int j = 0; j < ckeys.Length; j++)
                    {
                        keys[i][j] = ckeys[j];
                    }
                }

                sValues[0] = "";
                sValues[1] = rangeMin.ToString();
                sValues[2] = "";
                sValues[3] = rangeMax.ToString();

                texture = GlobalMovieCurveWindow.CreateCurveTexture(curves, rangeMin, rangeMax, false);

                String[] content = new String[this.curves.Count];
                for (int i = 0; i < content.Length; i++)
                {
                    content[i] = this.curves[i].name;
                }

                this.dragged = new bool[curves.Count][];
                for (int i = 0; i < curves.Count; i++)
                {
                    this.dragged[i] = new bool[curves[i].keyframes.Count];
                }

                selectedBox = new CustomComboBox(content);
                selectedBox.SelectedIndex = 0;
                selectedBox.SelectedIndexChanged += this.RebuildDragged;

                easeTypeBox = new CustomComboBox(Enum.GetNames(typeof(AMTween.EaseType)));
                easeTypeBox.SelectedIndex = 0;
                easeTypeBox.SelectedIndexChanged += this.ChangeEaseType;

                show = true;
                changed = true;
            }

            private void RebuildDragged(object sender, EventArgs args)
            {
                this.selectedIndex = selectedBox.SelectedIndex;
                this.selectedKeyframeIndex = 1;
                this.selectedKeyframeCurve = this.selectedIndex;
                Debug.Log(this.selectedKeyframe + " " + this.selectedIndex);
                this.easeTypeBox.SelectedIndex = (int)this.selectedKeyframe.easeType;
            }

            private void ChangeEaseType(object sender, EventArgs args)
            {
                this.selectedKeyframe.SetEasingFunction((AMTween.EaseType)this.easeTypeBox.SelectedIndex);
                this.changed = true;
            }

            private void CreateCurve()
            {
                for (int i = 0; i < keys.Count(); i++)
                {
                    curves[i].keyframes = keys[i].ToList();
                    curves[i].keyframes.Sort((a, b) => a.time.CompareTo(b.time));

                    if(this.deleteCurrent &&
                       i == this.selectedKeyframeCurve &&
                       this.selectedKeyframeIndex != 0 &&
                       this.selectedKeyframeIndex != this.keys[this.selectedKeyframeCurve].Length - 1)
                    {
                        curves[i].keyframes.Remove(this.selectedKeyframe);
                    }
                }
                Texture2D.Destroy(texture);
                texture = GlobalMovieCurveWindow.CreateCurveTexture(curves, rangeMin, rangeMax, false);
            }

            private Vector2 dragButton(Vector2 start, Rect rectItem, ref bool dragged, int curveIndex, int keyframeIndex)
            {
                Vector2 screenOffset = new Vector2(this.rect.x, this.rect.y);

                bool clicked = false;
                //start.y = rangeMax - start.y;
                Vector2 normalized = new Vector2(Util.MapRange(start.x, 0, 1, 0, rectItem.width),
                    Util.MapRange(start.y, rangeMin, rangeMax, 0, rectItem.height));
                normalized.y = rectItem.height - normalized.y;

                Rect pointRect = new Rect(normalized.x - 7, normalized.y - 7, 15, 15);

                GUIColor color = null;
                if(curveIndex == this.selectedKeyframeCurve && keyframeIndex == this.selectedKeyframeIndex)
                {
                    color = new GUIColor(Color.green, GUI.contentColor);
                }

                if (GUI.RepeatButton(pointRect, "o") || (dragged && Input.GetMouseButton(0)))
                {
                    dragged = true;
                    if (!changed)
                    {
                        this.selectedKeyframeIndex = keyframeIndex;
                        this.selectedKeyframeCurve = curveIndex;
                        clicked = true;
                        Vector2 mousePos = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                        normalized = new Vector2(mousePos.x - screenOffset.x, mousePos.y - screenOffset.y);

                        normalized.x = Mathf.Clamp(normalized.x, 0, rectItem.width);
                        normalized.y = Mathf.Clamp(normalized.y, 0, rectItem.height);

                        normalized.y = rectItem.height - normalized.y;

                        start.x = Util.MapRange(normalized.x, 0, rectItem.width, 0, 1);
                        start.y = Util.MapRange(normalized.y, 0, rectItem.height, rangeMin, rangeMax);
                    }
                }
                else
                {
                    dragged = false;
                }
                if(color != null && curveIndex == this.selectedKeyframeCurve && keyframeIndex == this.selectedKeyframeIndex)
                {
                    color.Dispose();
                }
                //start.y = rangeMax - start.y;

                if (clicked)
                {
                    Rect labelRect = new Rect(normalized.x, rectItem.height - normalized.y, 200, 100);
                    GUI.Label(labelRect, start.ToString() + " " + normalized.ToString());
                }
                return start;
            }

            private void keyframeDragPoint(Rect rectItem, ref MovieKeyframe key, ref bool dragged, int curveIndex, int keyframeIndex)
            {
                Vector2 pos = new Vector2(key.time, key.value);
                pos = this.dragButton(pos, rectItem, ref dragged, curveIndex, keyframeIndex);

                if (pos.x != key.time)
                {
                    changed = true;
                    key.time = pos.x;
                }
                if (pos.y != key.value)
                {
                    changed = true;
                    key.value = pos.y;
                }
            }

            private void startEndSlider(Rect rectItem, ref MovieKeyframe key, int keyframeIndex)
            {
                float fTmp;

                fTmp = GUI.VerticalSlider(rectItem, key.value, rangeMax, rangeMin);
                if (fTmp != key.value)
                {
                    key.value = fTmp;

                    this.selectedKeyframeIndex = keyframeIndex;
                    this.selectedKeyframeCurve = this.selectedIndex;
                    this.easeTypeBox.SelectedIndex = (int)this.selectedKeyframe.easeType;
                }
            }

            /*
            private void tangentSliders(ref Rect rectItem)
            {
                int iFontSize = gsLabel.fontSize;
                int selectedKeyframe = 1;
                MovieKeyframe key = this.keys[selectedIndex][selectedKeyframe];

                float fMax = (float)Math.PI;
                float fTmp;

                rectItem.x = iFontSize * 0.5f;
                rectItem.width = rect.width - iFontSize * 3;
                rectItem.y += rectItem.height;
                fTmp = GUI.HorizontalSlider(rectItem, key.inTangent, -fMax, fMax);
                if (fTmp != key.inTangent)
                {
                    key.inTangent = fTmp;
                }

                rectItem.x = iFontSize * 0.5f;
                rectItem.width = rect.width - iFontSize * 3;
                rectItem.y += rectItem.height;
                fTmp = GUI.HorizontalSlider(rectItem, key.outTangent, -fMax, fMax);
                if (fTmp != key.outTangent)
                {
                    key.outTangent = fTmp;
                }

                this.keys[selectedIndex][selectedKeyframe] = key;
            }=
        */

            private void graph(ref Rect rectItem)
            {
                int iFontSize = gsLabel.fontSize;
                rectItem.x = rectItem.width + iFontSize * 0.5f;
                rectItem.width = rectItem.height;
                GUI.DrawTexture(rectItem, texture);
            }

            private void propSelectBox(ref Rect rectItem)
            {
                int iFontSize = gsLabel.fontSize;

                rectItem.y += rectItem.height;
                rectItem.height = iFontSize * 1.5f;
                selectedBox.SetFromRect(rectItem);
                selectedBox.ScreenPos = new Rect(rect.x, rect.y, 0, 0);
                selectedBox.OnGUI();

                rectItem.y += rectItem.height;
                rectItem.height = iFontSize * 1.5f;
                easeTypeBox.SetFromRect(rectItem);
                easeTypeBox.ScreenPos = new Rect(rect.x, rect.y, 0, 0);
                easeTypeBox.OnGUI();

                rectItem.y += rectItem.height;
                rectItem.height = iFontSize * 1.5f;
                this.deleteCurrent = GUI.Button(rectItem, "Delete");

                rectItem.y += rectItem.height;
                rectItem.height = iFontSize * 1.5f;

                if(GUI.Button(rectItem, "Reset"))
                {
                    if(this.selectedKeyframeIndex == 0)
                    {
                        this.selectedKeyframe.value = this.keys[this.selectedKeyframeCurve][this.keys[this.selectedKeyframeCurve].Length - 1].value;
                    }
                    else
                    {
                        this.selectedKeyframe.value = this.keys[this.selectedKeyframeCurve][0].value;
                    }
                }
            }

            public void GuiFunc(int winId)
            {
                this.deleteCurrent = false;

                int iFontSize = gsLabel.fontSize;
                Rect rectItem = new Rect(iFontSize * 0.5f, iFontSize * 0.5f, iFontSize, rect.width - iFontSize * 3);

                float fTmp;

                if (this.keys.Count() == 0)
                {
                    this.show = false;
                    return;
                }

                this.startEndSlider(rectItem, ref keys[selectedIndex][0], 0);

                rectItem.x = rect.width - rectItem.width - iFontSize * 0.5f;

                this.startEndSlider(rectItem, ref keys[selectedIndex][keys[selectedIndex].Length - 1], keys[selectedIndex].Length - 1);

                this.graph(ref rectItem);

                for (int i = 0; i < keys.Length; i++)
                {
                    for (int j = 1; j < keys[i].Length - 1; j++)
                    {
                        keyframeDragPoint(rectItem, ref keys[i][j], ref dragged[i][j], i, j);
                        if (dragged[i][j])
                            changed = true;
                    }
                }

                this.propSelectBox(ref rectItem);


                rectItem.x = iFontSize * 0.5f;
                rectItem.width = (rect.width - iFontSize) / 2f;
                rectItem.y += rectItem.height;
                rectItem.height = iFontSize * 1.5f;
                string sTmp = Util.DrawTextFieldF(rectItem, sValues[1], gsText);
                if (sTmp != sValues[1])
                {
                    if (float.TryParse(sTmp, out fTmp))
                    {
                        rangeMin = Mathf.Min(fTmp, rangeMax);
                        //sTmp = rangeMin.ToString();
                    }
                    sValues[1] = sTmp;
                }

                rectItem.x += rectItem.width;
                sTmp = Util.DrawTextFieldF(rectItem, sValues[3], gsText);
                if (sTmp != sValues[3])
                {
                    if (float.TryParse(sTmp, out fTmp))
                    {
                        rangeMax = Mathf.Max(rangeMin, fTmp);
                        //sTmp = rangeMax.ToString();
                    }

                    sValues[3] = sTmp;
                }


                //this.tangentSliders(ref rectItem);

                //
                /*
                rectItem.x = iFontSize * 0.5f;
                rectItem.width = iFontSize * 4;
                rectItem.y += rectItem.height + fMargin;
                GUI.Label(rectItem, "Start", gsLabel);

                rectItem.width = rect.width - rectItem.width - iFontSize;
                rectItem.x = rect.width - rectItem.width - iFontSize * 0.5f;
                string sTmp = Util.DrawTextFieldF(rectItem, sValues[0], gsText);

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
                */

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
                    func(curves);
                }

                changed = false;

                GUI.DragWindow();

                if (GetAnyMouseButtonDown())
                {
                    Vector2 v2Tmp = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                    if (!rect.Contains(v2Tmp))
                    {
                        //func(curves);
                        //show = false;
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
