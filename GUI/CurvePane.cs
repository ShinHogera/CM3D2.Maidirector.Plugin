﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace CM3D2.HandmaidsTale.Plugin
{
    internal class CurvePane : ControlBase
    {
        private Texture2D curveTexture;
        public bool needsUpdate { get; private set; }
        private float rangeMin = 0;
        private float rangeMax = 1;
        private bool[][] dragging;
        private bool keyframeUpdated;
        private bool wrapModeChanged;
        private bool keyframeChanged;
        private string[] keyframeValueStrings;

        private bool isInserting;

        private int selectedKeyframeIndex = -1;
        private int selectedKeyframeCurveIndex = -1;

        private int selectedClipIndex = -1;
        private int selectedTrackIndex = -1;

        private static readonly string[] CURVE_WRAP_TYPES = Enum.GetNames(typeof(WrapMode));
        private CustomComboBox wrapBeforeBox;
        private CustomComboBox wrapAfterBox;
        private GUIStyle gsText;

        private Vector2 keyframeScrollPosition;
        private float keyframeScrollWidth;
        private float keyframeScrollHeight;

        public CurvePane()
        {
            this.curveTexture = (Texture2D)UnityEngine.Object.Instantiate(CurveTexture.textureBack);
            this.needsUpdate = true;

            this.wrapBeforeBox = new CustomComboBox(CURVE_WRAP_TYPES);
            this.wrapBeforeBox.SelectedIndexChanged += (o, e) =>
                {
                    this.needsUpdate = true;
                    this.wrapModeChanged = true;
                };
            this.wrapAfterBox = new CustomComboBox(CURVE_WRAP_TYPES);
            this.wrapAfterBox.SelectedIndexChanged += (o, e) =>
                {
                    this.needsUpdate = true;
                    this.wrapModeChanged = true;
                };

            gsText = new GUIStyle("textarea");
            gsText.alignment = TextAnchor.UpperLeft;

            this.keyframeValueStrings = new string[2];
            this.keyframeValueStrings[0] = "";
            this.keyframeValueStrings[1] = "";
        }

        private Vector2 timeSpaceToScreenSpace(Vector2 vec, Rect rectItem)
        {
            Vector2 result =  new Vector2(Util.MapRange(vec.x, 0, 1, 0, rectItem.width),
                                          Util.MapRange(vec.y, rangeMin, rangeMax, 0, rectItem.height));
            result.y = rectItem.height - result.y;
            return result;
        }

        private Vector2 screenSpaceToTimeSpace(Vector2 vec, Rect rectItem)
        {
            vec.y = rectItem.height - vec.y;

            Vector2 result = new Vector2(Util.MapRange(vec.x, 0, rectItem.width, 0, 1),
                                         Util.MapRange(vec.y, 0, rectItem.height, rangeMin, rangeMax));
            return result;
        }

        private Vector2 dragButton(Vector2 start, Rect rectItem, float min, float max, ref bool dragged, int curveIndex, int keyframeIndex)
        {
            bool clicked = false;
            //start.y = rangeMax - start.y;

            Vector2 normalized = timeSpaceToScreenSpace(start, rectItem);

            Rect pointRect = new Rect(normalized.x - 7, normalized.y - 7, 15, 15);

            GUIColor color = null;
            if (curveIndex == this.selectedKeyframeCurveIndex && keyframeIndex == this.selectedKeyframeIndex)
            {
                color = new GUIColor(Color.green, GUI.contentColor);
            }

            if (GUI.RepeatButton(pointRect, "o") || (dragged && Input.GetMouseButton(0)))
            {
                dragged = true;
                if (!needsUpdate)
                {
                    this.SelectKeyframe(curveIndex, keyframeIndex);

                    clicked = true;
                    normalized = getMousePos();

                    normalized.x = Mathf.Clamp(normalized.x, 0, rectItem.width);
                    normalized.y = Mathf.Clamp(normalized.y, 0, rectItem.height);

                    start = screenSpaceToTimeSpace(normalized, rectItem);
                }
            }
            else
            {
                dragged = false;
            }
            if (color != null && curveIndex == this.selectedKeyframeCurveIndex && keyframeIndex == this.selectedKeyframeIndex)
            {
                color.Dispose();
            }

            //start.y = rangeMax - start.y;

            if (clicked)
            {
                Rect labelRect = new Rect(normalized.x, normalized.y, 200, 100);
                GUI.Label(labelRect, start.ToString() + " " + normalized.ToString());
            }
            return start;
        }

        private void SelectKeyframe(int curveIndex, int keyframeIndex)
        {
            this.selectedKeyframeIndex = keyframeIndex;
            this.selectedKeyframeCurveIndex = curveIndex;
            this.keyframeChanged = true;
        }

        public void SetUpdate() => this.needsUpdate = true;

        public void UpdateFromClip(MovieCurveClip clip)
        {
            this.selectedKeyframeCurveIndex = 0;
            this.selectedKeyframeIndex = 0;

            this.rangeMin = clip.minValue;
            this.rangeMax = clip.maxValue;

            this.ZoomOut();
            Texture2D.Destroy(this.curveTexture);
            Debug.Log(this.rangeMin + " " + this.rangeMax);
            this.curveTexture = CurveTexture.CreateCurveTexture(clip.curves, this.rangeMin, this.rangeMax, false);

            this.dragging = new bool[clip.curves.Count][];
            for (int i = 0; i < clip.curves.Count; i++)
            {
                dragging[i] = new bool[clip.curves[i].keyframes.Length];
            }
        }

        private Keyframe keyframeDragPoint(Rect rectItem, Keyframe key, ref bool dragged, int curveIndex, int keyframeIndex)
        {
            Vector2 pos = new Vector2(key.time, key.value);
            pos = this.dragButton(pos, rectItem, rangeMin, rangeMax, ref dragged, curveIndex, keyframeIndex);

            if (pos.x != key.time)
            {
                keyframeUpdated = true;
                key.time = pos.x;
            }
            if (pos.y != key.value)
            {
                keyframeUpdated = true;
                key.value = pos.y;
            }
            return key;
        }

        private Vector2 getMousePos()
        {
            Vector2 screenOffset = new Vector2(this.WindowRect.x + this.ScreenPos.x, this.WindowRect.y + this.ScreenPos.y);
            Vector2 mousePos = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            return new Vector2(mousePos.x - screenOffset.x, mousePos.y - screenOffset.y - 25);
        }

        private Vector2 tangentDragPoint(Rect rectItem, Keyframe key1, Keyframe key2)
        {
            Vector2 p1 = new Vector2(key1.time, key1.value);
            Vector2 p2 = new Vector2(key2.time, key2.value);

            float tangLengthX = Mathf.Abs(p1.x - p2.x) * 0.333333f;
            float tangLengthY = tangLengthX;
            Vector2 c1 = p1;
            Vector2 c2 = p2;
            c1.x += tangLengthX;
            c1.y += tangLengthY * key1.outTangent;
            c2.x -= tangLengthX;
            c2.y -= tangLengthY * key2.inTangent;

            c1 = timeSpaceToScreenSpace(c1, rectItem);
            c2 = timeSpaceToScreenSpace(c2, rectItem);

            float res1 = key1.outTangent;
            float res2 = key2.inTangent;

            if (GUI.RepeatButton(new Rect(c1, new Vector2(20, 20)), "A"))
            {
                c1 = getMousePos();
                c1 = screenSpaceToTimeSpace(c1, rectItem);
                res1 = (c1.y - p1.y) / tangLengthY;
                keyframeUpdated = true;
            }
            if (GUI.RepeatButton(new Rect(c2, new Vector2(20, 20)), "B"))
            {
                c2 = getMousePos();
                c2 = screenSpaceToTimeSpace(c2, rectItem);
                res2 = -(c2.y - p2.y) / tangLengthY;
                keyframeUpdated = true;
            }

            return new Vector2(res1, res2);
        }

        private float getAngle(Vector2 target, Vector2 b)
        {
            float angle = Mathf.Atan2(target.y - b.y, target.x - b.x);

            if (angle < 0)
            {
                angle += (float)Math.PI;
            }

            return angle;
        }

        private void ZoomOut()
        {
            this.rangeMin -= this.rangeMin * 0.25f + 1;
            this.rangeMax += this.rangeMax * 0.25f + 1;
            this.needsUpdate = true;
        }

        private void ZoomIn()
        {
            if (this.rangeMax - this.rangeMin < 0.1f)
                return;

            this.rangeMin += this.rangeMin * 0.25f + 1;
            this.rangeMax -= this.rangeMax * 0.25f + 1;
            this.needsUpdate = true;
        }

        private void PanUp()
        {
            float diff = (this.rangeMax - this.rangeMin) / 4;
            this.rangeMax += diff;
            this.rangeMin += diff;
            this.needsUpdate = true;
        }

        private void PanDown()
        {
            float diff = (this.rangeMax - this.rangeMin) / 4;
            this.rangeMax -= diff;
            this.rangeMin -= diff;
            this.needsUpdate = true;
        }

        private void DrawCurveView(Rect curveRect, ref MovieCurveClip clip)
        {
            Rect curveAreaRect = new Rect(0, 0, curveRect.width / 4 * 3, curveRect.height);

            GUI.DrawTexture(curveAreaRect, this.curveTexture);

            for (int i = 0; i < clip.curves.Count; i++)
            {
                for (int j = 0; j < clip.curves[i].keyframes.Length; j++)
                {
                    this.keyframeUpdated = false;
                    Keyframe key = this.keyframeDragPoint(curveAreaRect, clip.curves[i].keyframes[j], ref dragging[i][j], i, j);
                    if (this.keyframeUpdated)
                    {
                        clip.curves[i].TryMoveKey(j, key);
                        this.needsUpdate = true;
                    }

                    this.keyframeUpdated = false;
                    if (j != clip.curves[i].keyframes.Length - 1)
                    {
                        Keyframe key1 = clip.curves[i].keyframes[j];
                        Keyframe key2 = clip.curves[i].keyframes[j + 1];
                        Vector2 result = this.tangentDragPoint(curveAreaRect, key1, key2);

                        if(this.keyframeUpdated)
                        {
                            key1.outTangent = result.x;
                            key2.inTangent = result.y;

                            clip.curves[i].TryMoveKey(j, key1);
                            clip.curves[i].TryMoveKey(j + 1, key2);
                            this.needsUpdate = true;
                        }
                    }
                }
            }

            if(this.isInserting && Input.GetMouseButtonDown(0))
            {
                Vector2 pos = this.getMousePos();
                pos = screenSpaceToTimeSpace(pos, curveAreaRect);

                if(pos.x >= 0f && pos.x <= 1f)
                {
                    clip.curves[this.selectedKeyframeCurveIndex].AddKeyframe(new Keyframe(pos.x, pos.y));
                    this.isInserting = false;
                    this.UpdateFromClip(clip);
                }
            }

            Rect panelRect = new Rect(curveAreaRect.width, 0, curveRect.width / 4, curveRect.height);
            GUILayout.BeginArea(panelRect);
            Rect rectItem = new Rect(0, 0, panelRect.width / 4, 20);
            if (GUI.Button(rectItem, "-"))
            {
                this.ZoomOut();
            }
            rectItem.x += rectItem.width;
            if (GUI.Button(rectItem, "+"))
            {
                this.ZoomIn();
            }

            rectItem.x += rectItem.width;
            if (GUI.Button(rectItem, "^"))
            {
                this.PanUp();
            }

            rectItem.x += rectItem.width;
            if (GUI.Button(rectItem, "v"))
            {
                this.PanDown();
            }

            rectItem.x = 0;
            rectItem.y += rectItem.height;
            if (GUI.Button(rectItem, "Del"))
            {
                if(clip.curves[selectedKeyframeCurveIndex].curve.length > 1)
                {
                    clip.curves[selectedKeyframeCurveIndex].curve.RemoveKey(selectedKeyframeIndex);
                    this.UpdateFromClip(clip);
                }
            }

            rectItem.width = panelRect.width / 2;
            rectItem.y += rectItem.height;
            rectItem.x = 0;
            wrapBeforeBox.SetFromRect(rectItem);
            wrapBeforeBox.ScreenPos = this.ScreenPos;
            wrapBeforeBox.OnGUI();

            rectItem.x += rectItem.width;
            wrapAfterBox.SetFromRect(rectItem);
            wrapAfterBox.OnGUI();
            wrapAfterBox.ScreenPos = this.ScreenPos;

            rectItem.x = 0;
            rectItem.y += rectItem.height;

            bool bTmp = GUI.Toggle(rectItem, this.isInserting, "Insert");
            if(bTmp != isInserting)
            {
                this.isInserting = bTmp;
            }

            GUILayout.EndArea();
        }

        private void UpdateSelectedKeyframe(MovieCurveClip clip, Keyframe updated)
        {
            clip.curves[selectedKeyframeCurveIndex].TryMoveKey(selectedKeyframeIndex, updated);
            this.needsUpdate = true;
        }

        private void keyframeScrollPane(Rect keyframeViewRect, ref MovieCurveClip clip)
        {
            for(int i = 0; i < clip.curves.Count; i++)
            {
                MovieCurve curve = clip.curves[i];
                float yPos = i * 20;

                GUI.Label(new Rect(keyframeViewRect.x, yPos, 100, 20), curve.name);
            }

            keyframeViewRect.x += 100;
            keyframeViewRect.width -= 100;

            Rect keyframeScrollRect = new Rect(0, 0, keyframeScrollWidth, keyframeScrollHeight);
            keyframeScrollWidth = 0;
            keyframeScrollHeight = 0;

            this.keyframeScrollPosition = GUI.BeginScrollView(keyframeViewRect, this.keyframeScrollPosition, keyframeScrollRect);
            for(int i = 0; i < clip.curves.Count; i++)
            {
                MovieCurve curve = clip.curves[i];
                float yPos = i * 20;

                for(int j = 0; j < curve.keyframes.Length; j++)
                {
                    Keyframe key = curve.keyframes[j];
                    float xPos = key.time * clip.length * TimelineWindow.pixelsPerFrame * 20;

                    keyframeScrollWidth = Mathf.Max(keyframeScrollWidth, xPos + 20);

                    Rect keyframeButton = new Rect(xPos, yPos, 20, 20);
                    if(GUI.Button(keyframeButton, "x"))
                    {
                        this.SelectKeyframe(i, j);
                    }
                }
                keyframeScrollHeight = Mathf.Max(keyframeScrollHeight, yPos + 20);
            }
            GUI.EndScrollView();
        }

        private void keyframeEditPane(Rect curveRect, Rect keyframeViewRect, ref MovieCurveClip clip)
        {
            MovieCurve selectedKeyframeCurve = clip.curves[selectedKeyframeCurveIndex];
            if(selectedKeyframeCurve != null)
            {
                Keyframe selectedKeyframe = selectedKeyframeCurve.keyframes[selectedKeyframeIndex];

                Rect panelRect = new Rect(keyframeViewRect.width, 0, curveRect.width / 4, curveRect.height);

                GUILayout.BeginArea(panelRect);

                Rect rectItem = new Rect(0, 0, panelRect.width, 20);
                GUI.Label(rectItem, selectedKeyframeCurve.name);

                rectItem.y += rectItem.height;
                rectItem.width = panelRect.width / 2;

                this.keyframeUpdated = false;
                string sTmp = GUI.TextField(rectItem, this.keyframeValueStrings[0], gsText);
                if (sTmp != this.keyframeValueStrings[0])
                {
                    if (float.TryParse(sTmp, out float fTmp))
                    {
                        selectedKeyframe.time = Mathf.Clamp01(fTmp);
                        sTmp = selectedKeyframe.time.ToString();
                        this.keyframeUpdated = true;
                    }
                    this.keyframeValueStrings[0] = sTmp;
                }

                rectItem.x += rectItem.width;
                sTmp = GUI.TextField(rectItem, this.keyframeValueStrings[1], gsText);
                if (sTmp != this.keyframeValueStrings[1])
                {
                    if (float.TryParse(sTmp, out float fTmp))
                    {
                        selectedKeyframe.value = fTmp;
                        sTmp = selectedKeyframe.value.ToString();
                        this.keyframeUpdated = true;
                    }
                    this.keyframeValueStrings[1] = sTmp;
                }

                if (this.keyframeUpdated)
                {
                    this.UpdateSelectedKeyframe(clip, selectedKeyframe);
                }

                GUILayout.EndArea();
            }
        }

        private void DrawKeyframeView(Rect curveRect, ref MovieCurveClip clip)
        {
            Rect keyframeViewRect = new Rect(0, 0, curveRect.width / 4 * 3, curveRect.height);
            this.keyframeScrollPane(keyframeViewRect, ref clip);
            this.keyframeEditPane(curveRect, keyframeViewRect, ref clip);
        }

        public void Draw(Rect curveRect, List<MovieCurveClip> clips, int clipIndex, int trackIndex)
        {
            this.needsUpdate = false;
            MovieCurveClip clip = clips[clipIndex];

            if (clip.curves.Count == 0)
                return;

            if (this.selectedClipIndex != clipIndex || this.selectedTrackIndex != trackIndex || this.dragging.Length != clip.curves.Count)
            {
                this.UpdateFromClip(clip);
                this.selectedClipIndex = clipIndex;
                this.selectedTrackIndex = trackIndex;
            }

            if (this.keyframeChanged)
            {
                wrapBeforeBox.SelectedItem = clip.curves[selectedKeyframeCurveIndex].curve.preWrapMode.ToString();
                wrapAfterBox.SelectedItem = clip.curves[selectedKeyframeCurveIndex].curve.postWrapMode.ToString();
                this.keyframeValueStrings[0] = clip.curves[selectedKeyframeCurveIndex].keyframes[selectedKeyframeIndex].time.ToString();
                this.keyframeValueStrings[1] = clip.curves[selectedKeyframeCurveIndex].keyframes[selectedKeyframeIndex].value.ToString();
                this.keyframeChanged = false;
            }

            if (this.wrapModeChanged)
            {
                clip.curves[selectedKeyframeCurveIndex].curve.preWrapMode = (WrapMode)Enum.Parse(typeof(WrapMode), wrapBeforeBox.SelectedItem);
                clip.curves[selectedKeyframeCurveIndex].curve.postWrapMode = (WrapMode)Enum.Parse(typeof(WrapMode), wrapAfterBox.SelectedItem);
                wrapModeChanged = false;
            }

            this.DrawKeyframeView(curveRect, ref clip);

            if (needsUpdate)
            {
                Texture2D.Destroy(this.curveTexture);
                this.curveTexture = CurveTexture.CreateCurveTexture(clip.curves, this.rangeMin, this.rangeMax, false);
                clip.RemakeTexture();
            }
        }
    }

    public static class CurveTexture
    {
        public static Texture2D textureBack { get; set; }
        public static Texture2D textureBackNarrow { get; set; }

        static readonly int TEXTURE_WIDTH_EXPONENT = 8;
        private static int TextureWidth
        {
            get => (int)Math.Pow(2, TEXTURE_WIDTH_EXPONENT);
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

        public static void Init()
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
        }

        public static Texture2D CreateCurveTexture(List<MovieCurve> curves, float rangeMin, float rangeMax, bool bNarrow)
        {
            Texture2D tex;
            if (bNarrow)
                tex = (Texture2D)UnityEngine.Object.Instantiate(CurveTexture.textureBackNarrow);
            else
                tex = (Texture2D)UnityEngine.Object.Instantiate(CurveTexture.textureBack);

            int width = tex.width;
            int height = tex.height;
            Color[] color = tex.GetPixels();

            int i = 0;
            foreach (MovieCurve curve in curves)
            {
                AddCurveTexture(curve, ref color, width, height, rangeMin, rangeMax, i++);
            }

            tex.hideFlags = HideFlags.HideAndDontSave;
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

                if(x > 0)
                {
                    float f2 = Mathf.Clamp(curve.Evaluate((x-1) / (float)width), rangeMin, rangeMax);
                    mapped = Util.MapRange(f2, rangeMin, rangeMax, 0, 1);
                    int colsb = (int)(mapped * (height - 1));

                    int start = cols;
                    int end = colsb;
                    if (colsb < cols) {
                        int t = start;
                        start = end;
                        end = t;
                    }
                    for (int y = start; y < end; y++)
                    {
                        color[x + (y * width)] = GetColor(i);
                    }
                }
            }
        }
    }
}
