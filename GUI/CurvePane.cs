using System;
using System.Collections.Generic;
using UnityEngine;

namespace CM3D2.HandmaidsTale.Plugin
{
    internal class CurvePane : ControlBase
    {
        private Texture2D curveTexture;
        public bool needsUpdate { get; private set; }
        private bool[][] dragging;
        private bool[] curvesVisible;
        private bool keyframeUpdated;
        private bool wrapModeChanged;
        private bool keyframeChanged;
        private bool tangentModeChanged;
        private string[] keyframeValueStrings;

        private bool isInserting;

        private int selectedKeyframeIndex = -1;
        private int selectedKeyframeCurveIndex = -1;

        private int selectedClipIndex = -1;
        private int selectedTrackIndex = -1;

        private static readonly string[] CURVE_WRAP_TYPES = Enum.GetNames(typeof(WrapMode));
        private static readonly string[] TANGENT_MODES = Enum.GetNames(typeof(TangentUtility.TangentMode));
        private CustomComboBox wrapBeforeBox;
        private CustomComboBox wrapAfterBox;
        private GUIStyle gsText;

        private Vector2 keyframeScrollPosition;
        private float keyframeScrollWidth;
        private float keyframeScrollHeight;

        public CurvePaneMode mode { get; set; }

        public enum CurvePaneMode
        {
            Curve,
            Keyframe
        }

        private CurveDetailPanelMode curveDetailPanelMode { get; set; }

        private enum CurveDetailPanelMode
        {
            Detail,
            ToggleVisible
        }

        private float scale;
        private float pos;

        private float rangeMin
        {
            get => pos - (scale / 2);
        }

        private float rangeMax
        {
            get => pos + (scale / 2);
        }

        private static readonly string[] CURVE_DETAIL_PANEL_MODES = Enum.GetNames(typeof(CurveDetailPanelMode));

        public CurvePane()
        {
            this.curveTexture = (Texture2D)UnityEngine.Object.Instantiate(CurveTexture.textureBack);
            this.needsUpdate = true;

            this.wrapBeforeBox = new CustomComboBox(CURVE_WRAP_TYPES);
            this.wrapBeforeBox.Text = "WrapBefore";
            this.wrapBeforeBox.SelectedIndexChanged += (o, e) =>
                {
                    this.needsUpdate = true;
                    this.wrapModeChanged = true;
                };
            this.wrapAfterBox = new CustomComboBox(CURVE_WRAP_TYPES);
            this.wrapAfterBox.Text = "WrapAfter";
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
            Vector2 result = new Vector2(Util.MapRange(vec.x, 0, 1, 0, rectItem.width),
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
                GUI.Label(labelRect, $"{start.ToString()}");
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

            this.FitAllCurves(clip);

            this.ZoomOut();
            this.RemakeTexture(ref clip);

            this.dragging = new bool[clip.curves.Count][];
            for (int i = 0; i < clip.curves.Count; i++)
            {
                dragging[i] = new bool[clip.curves[i].keyframes.Length];
            }

            this.curvesVisible = new bool[clip.curves.Count];
            for (int i = 0; i < clip.curves.Count; i++)
            {
                curvesVisible[i] = true;
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

        private Vector2 tangentDragPoint(Rect rectItem, Keyframe key1, Keyframe key2, bool show1, bool show2)
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

            if (show1 && GUI.RepeatButton(new Rect(c1, new Vector2(20, 20)), "A"))
            {
                c1 = getMousePos();
                c1 = screenSpaceToTimeSpace(c1, rectItem);
                res1 = (c1.y - p1.y) / tangLengthY;
                keyframeUpdated = true;
            }
            if (show2 && GUI.RepeatButton(new Rect(c2, new Vector2(20, 20)), "B"))
            {
                c2 = getMousePos();
                c2 = screenSpaceToTimeSpace(c2, rectItem);
                res2 = -(c2.y - p2.y) / tangLengthY;
                keyframeUpdated = true;
            }

            return new Vector2(res1, res2);
        }

        private void ZoomOut()
        {
            this.scale *= 2;
            this.needsUpdate = true;
        }

        private void ZoomIn()
        {
            if (this.scale <= 1)
                return;

            this.scale /= 2;
            this.needsUpdate = true;
        }

        private void PanUp()
        {
            this.pos += this.scale / 4;
            this.needsUpdate = true;
        }

        private void PanDown()
        {
            this.pos -= this.scale / 4;
            this.needsUpdate = true;
        }

        private void CenterCurve(MovieCurve curve) => CenterBetween(curve.minValue, curve.maxValue);
        private void CenterSelectedCurve(MovieCurveClip clip) => CenterCurve(clip.curves[this.selectedKeyframeCurveIndex]);

        private void FitAllCurves(MovieCurveClip clip) => CenterBetween(clip.minValue, clip.maxValue);

        private void CenterBetween(float minVal, float maxVal)
        {
            float diff = Mathf.Max(1, (maxVal - minVal));
            this.scale = Mathf.Max(1, ((maxVal + .25f * diff) - (minVal - .25f * diff)));
            this.pos = (minVal - .25f * diff) + (this.scale / 2);

            this.needsUpdate = true;
        }

        private void UpdateSelectedKeyframe(MovieCurveClip clip, Keyframe updated)
        {
            clip.curves[selectedKeyframeCurveIndex].TryMoveKey(selectedKeyframeIndex, updated);
            this.needsUpdate = true;
        }

        private void curveDetailPanel(Rect panelRect, ref MovieCurveClip clip)
        {
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
            if (GUI.Button(rectItem, "￪"))
            {
                this.PanUp();
            }

            rectItem.x += rectItem.width;
            if (GUI.Button(rectItem, "￬"))
            {
                this.PanDown();
            }

            rectItem.width = panelRect.width;
            rectItem.x = 0;
            rectItem.y += rectItem.height;
            wrapBeforeBox.SetFromRect(rectItem);
            wrapBeforeBox.ScreenPos = this.ScreenPos;
            wrapBeforeBox.OnGUI();

            rectItem.y += rectItem.height;
            wrapAfterBox.SetFromRect(rectItem);
            wrapAfterBox.OnGUI();
            wrapAfterBox.ScreenPos = this.ScreenPos;

            rectItem.y += rectItem.height;

            using( GUIColor color = new GUIColor( GUI.backgroundColor, CurveTexture.GetCurveColor(selectedKeyframeCurveIndex) ) )
            {
                GUI.Label(rectItem, $"Curve: {clip.curves[selectedKeyframeCurveIndex].name}");
            }

            rectItem.y += rectItem.height;

            int iTmp;
            MovieCurve keyframeCurve = clip.curves[selectedKeyframeCurveIndex];

            GUI.enabled = selectedKeyframeIndex > 0;

            iTmp = GUI.Toolbar(rectItem, (int)TangentUtility.GetKeyLeftTangentMode(keyframeCurve, selectedKeyframeIndex), TANGENT_MODES);
            if(iTmp != (int)TangentUtility.GetKeyLeftTangentMode(keyframeCurve, selectedKeyframeIndex))
            {
                TangentUtility.SetKeyLeftTangentMode(keyframeCurve, selectedKeyframeIndex, (TangentUtility.TangentMode)iTmp);
                this.needsUpdate = true;
            }

            GUI.enabled = selectedKeyframeIndex < clip.curves[selectedKeyframeCurveIndex].keyframes.Length - 1;

            rectItem.y += rectItem.height;
            iTmp = GUI.Toolbar(rectItem, (int)TangentUtility.GetKeyRightTangentMode(keyframeCurve, selectedKeyframeIndex), TANGENT_MODES);
            if(iTmp != (int)TangentUtility.GetKeyRightTangentMode(keyframeCurve, selectedKeyframeIndex))
            {
                TangentUtility.SetKeyRightTangentMode(keyframeCurve, selectedKeyframeIndex, (TangentUtility.TangentMode)iTmp);
                this.needsUpdate = true;
            }

            GUI.enabled = true;


            bool bTmp;
            bool val = TangentUtility.GetKeyBroken(clip.curves[selectedKeyframeCurveIndex], selectedKeyframeIndex);
            rectItem.y += rectItem.height;
            bTmp = GUI.Toggle(rectItem, val, "Broken");
            if(bTmp != val)
            {
                TangentUtility.SetKeyBroken(clip.curves[selectedKeyframeCurveIndex], selectedKeyframeIndex, bTmp);
                this.needsUpdate = true;
            }

            rectItem.y += rectItem.height;
            rectItem.width = panelRect.width / 2;


            GUIStyle style = new GUIStyle("button");
            using( GUIColor color = new GUIColor( this.isInserting ? Color.green : GUI.backgroundColor, GUI.contentColor ) )
            {
                bTmp = GUI.Toggle(rectItem, this.isInserting, "Insert", style);
                if(bTmp != isInserting)
                {
                    this.isInserting = bTmp;
                }
            }

            rectItem.x += rectItem.width;
            if (GUI.Button(rectItem, "Delete"))
            {
                if(clip.curves[selectedKeyframeCurveIndex].curve.length > 1)
                {
                    clip.curves[selectedKeyframeCurveIndex].RemoveKeyframe(selectedKeyframeIndex);
                    this.UpdateFromClip(clip);
                }
            }

            rectItem.width = panelRect.width / 2;
            rectItem.y += rectItem.height;
            rectItem.x = 0;

            if(GUI.Button(rectItem, "Center"))
            {
                this.CenterSelectedCurve(clip);
            }

            rectItem.x += rectItem.width;

            if(GUI.Button(rectItem, "Fit All"))
            {
                this.FitAllCurves(clip);
            }

            GUILayout.EndArea();
        }

        private void curveToggleVisiblePanel(Rect panelRect, ref MovieCurveClip clip)
        {
            GUILayout.BeginArea(panelRect);
            Rect rectItem = new Rect(0, 0, panelRect.width/2, 20);

            if(GUI.Button(rectItem, "All On"))
            {
                for(int i = 0; i < clip.curves.Count; i++)
                {
                    this.curvesVisible[i] = true;
                }
            }

            rectItem.x += rectItem.width;

            if(GUI.Button(rectItem, "All Off"))
            {
                for(int i = 0; i < clip.curves.Count; i++)
                {
                    this.curvesVisible[i] = false;
                }
            }

            rectItem.x = 0;
            rectItem.width = panelRect.width;
            rectItem.y += rectItem.height;

            GUIStyle style = new GUIStyle("toggle");
            for(int i = 0; i < clip.curves.Count; i++)
            {
                MovieCurve curve = clip.curves[i];
                using( GUIColor color = new GUIColor( CurveTexture.GetCurveColor(i), CurveTexture.GetCurveColor(i) ))
                {
                    bool bTmp = GUI.Toggle(rectItem, this.curvesVisible[i], curve.name, style);
                    if(bTmp != this.curvesVisible[i])
                    {
                        this.curvesVisible[i] = bTmp;
                    }
                }
                rectItem.y += rectItem.height;
            }
            GUILayout.EndArea();
        }

        private void DrawCurveView(Rect curveRect, ref MovieCurveClip clip)
        {
            Rect curveAreaRect = new Rect(0, 0, curveRect.width / 4 * 3, curveRect.height);

            GUI.DrawTexture(curveAreaRect, this.curveTexture);

            for (int i = 0; i < clip.curves.Count; i++)
            {
                if(!this.curvesVisible[i])
                    continue;

                for (int j = 0; j < clip.curves[i].keyframes.Length; j++)
                {
                    this.keyframeUpdated = false;
                    Keyframe key = this.keyframeDragPoint(curveAreaRect, clip.curves[i].keyframes[j], ref dragging[i][j], i, j);
                    if (this.keyframeUpdated)
                    {
                        clip.curves[i].TryMoveKey(j, key);
                        TangentUtility.UpdateTangentsFromModeSurrounding(clip.curves[i], j);
                        this.needsUpdate = true;
                    }

                    this.keyframeUpdated = false;
                    if (i == selectedKeyframeCurveIndex &&
                        j == selectedKeyframeIndex &&
                        j != clip.curves[i].keyframes.Length - 1)
                    {
                        Keyframe key1 = clip.curves[i].keyframes[j];
                        Keyframe key2 = clip.curves[i].keyframes[j + 1];
                        bool show1 = TangentUtility.GetKeyRightTangentMode(clip.curves[i], j) == TangentUtility.TangentMode.Free;
                        bool show2 = TangentUtility.GetKeyLeftTangentMode(clip.curves[i], j + 1) == TangentUtility.TangentMode.Free;
                        Vector2 result = this.tangentDragPoint(curveAreaRect, key1, key2, show1, show2);

                        if(this.keyframeUpdated)
                        {
                            if(key1.outTangent != result.x)
                            {
                                key1.outTangent = result.x;
                                TangentUtility.SetKeyRightTangentMode(clip.curves[i], j, TangentUtility.TangentMode.Free);
                                if (!TangentUtility.GetKeyBroken(clip.curves[i], j))
                                {
                                    key1.inTangent = key1.outTangent;
                                    TangentUtility.SetKeyLeftTangentMode(clip.curves[i], j, TangentUtility.TangentMode.Free);
                                }
                                clip.curves[i].TryMoveKey(j, key1);
                            }
                            if(key2.inTangent != result.y)
                            {
                                key2.inTangent = result.y;
                                TangentUtility.SetKeyLeftTangentMode(clip.curves[i], j+1, TangentUtility.TangentMode.Free);
                                if (!TangentUtility.GetKeyBroken(clip.curves[i], j+1))
                                {
                                    key2.outTangent = key2.inTangent;
                                    TangentUtility.SetKeyRightTangentMode(clip.curves[i], j+1, TangentUtility.TangentMode.Free);
                                }
                                clip.curves[i].TryMoveKey(j + 1, key2);
                            }

                            this.needsUpdate = true;
                        }
                    }
                }
            }

            if(this.isInserting && Input.GetMouseButtonDown(0))
            {
                this.TryInsertCurveKeyframe(curveAreaRect, ref clip);
            }

            Rect toggleRect = new Rect(curveAreaRect.width, 0, curveRect.width / 4, 20);

            int iTmp;
            if ((iTmp = GUI.Toolbar(toggleRect, (int)this.curveDetailPanelMode, CURVE_DETAIL_PANEL_MODES)) >= 0)
            {
                this.curveDetailPanelMode = (CurveDetailPanelMode)iTmp;
            }

            Rect panelRect = new Rect(curveAreaRect.width, 20, curveRect.width / 4, curveRect.height - 20);

            switch(this.curveDetailPanelMode)
            {
                case CurveDetailPanelMode.Detail:
                    this.curveDetailPanel(panelRect, ref clip);
                    break;
                case CurveDetailPanelMode.ToggleVisible:
                    this.curveToggleVisiblePanel(panelRect, ref clip);
                    break;
            }
        }

        private void TryInsertCurveKeyframe(Rect curveAreaRect, ref MovieCurveClip clip)
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

        private void keyframeScrollPane(Rect keyframeViewRect, ref MovieCurveClip clip)
        {
            for(int i = 0; i < clip.curves.Count; i++)
            {
                MovieCurve curve = clip.curves[i];
                float yPos = i * 20;

                GUI.Label(new Rect(keyframeViewRect.x, yPos - this.keyframeScrollPosition.y, 100, 20), curve.name);
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

        private void keyframeDetailPane(Rect curveRect, Rect keyframeViewRect, ref MovieCurveClip clip)
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
                    if (int.TryParse(sTmp, out int iTmp))
                    {
                        float percent = (float)iTmp / (float)clip.length;
                        selectedKeyframe.time = Mathf.Clamp01(percent);
                        sTmp = iTmp.ToString();
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
            this.keyframeDetailPane(curveRect, keyframeViewRect, ref clip);
        }

        private void RefreshSelectedClip(ref MovieCurveClip clip, int clipIndex, int trackIndex)
        {
            this.UpdateFromClip(clip);
            this.selectedClipIndex = clipIndex;
            this.selectedTrackIndex = trackIndex;
        }

        private void RefreshSelectedKeyframe(ref MovieCurveClip clip)
        {
            MovieCurve keyframeCurve = clip.curves[selectedKeyframeCurveIndex];
            Keyframe keyframe = keyframeCurve.keyframes[selectedKeyframeIndex];

            wrapBeforeBox.SelectedItem = keyframeCurve.curve.preWrapMode.ToString();
            wrapAfterBox.SelectedItem = keyframeCurve.curve.postWrapMode.ToString();

            int frame = (int)(keyframe.time * clip.length);
            this.keyframeValueStrings[0] = frame.ToString();
            this.keyframeValueStrings[1] = keyframe.value.ToString();
            this.keyframeChanged = false;
        }

        private void RefreshSelectedWrapMode(ref MovieCurveClip clip)
        {
            MovieCurve curve = clip.curves[selectedKeyframeCurveIndex];
            curve.curve.preWrapMode = (WrapMode)Enum.Parse(typeof(WrapMode), wrapBeforeBox.SelectedItem);
            curve.curve.postWrapMode = (WrapMode)Enum.Parse(typeof(WrapMode), wrapAfterBox.SelectedItem);
            this.wrapModeChanged = false;

            // refresh the texture
            this.needsUpdate = true;
        }

        private void RefreshSelectedTangentMode(ref MovieCurveClip clip)
        {
            this.tangentModeChanged = false;

            // refresh the texture
            this.needsUpdate = true;
        }

        private bool SelectedClipChanged(ref MovieCurveClip clip, int clipIndex, int trackIndex) => this.selectedClipIndex != clipIndex || this.selectedTrackIndex != trackIndex || this.dragging.Length != clip.curves.Count;

        private void RemakeTexture(ref MovieCurveClip clip)
        {
            Texture2D.Destroy(this.curveTexture);
            this.curveTexture = CurveTexture.CreateCurveTexture(clip.curves, this.rangeMin, this.rangeMax, false);
            clip.RemakeTexture();
        }

        private void DrawView(Rect curveRect, ref MovieCurveClip clip)
        {
            switch(this.mode)
            {
                case CurvePaneMode.Curve:
                    this.DrawCurveView(curveRect, ref clip);
                    break;
                case CurvePaneMode.Keyframe:
                    this.DrawKeyframeView(curveRect, ref clip);
                    break;
            }
        }

        public void Draw(Rect curveRect, List<MovieCurveClip> clips, int clipIndex, int trackIndex)
        {
            this.needsUpdate = false;
            MovieCurveClip clip = clips[clipIndex];

            if (clip.curves.Count == 0)
                return;

            if (this.SelectedClipChanged(ref clip, clipIndex, trackIndex))
            {
                this.RefreshSelectedClip(ref clip, clipIndex, trackIndex);
            }

            if (this.keyframeChanged)
            {
                this.RefreshSelectedKeyframe(ref clip);
            }

            if (this.wrapModeChanged)
            {
                this.RefreshSelectedWrapMode(ref clip);
            }

            if (this.tangentModeChanged)
            {
                this.RefreshSelectedTangentMode(ref clip);
            }

            this.DrawView(curveRect, ref clip);

            if (needsUpdate)
            {
                this.RemakeTexture(ref clip);
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

        public static Color GetCurveColor(int i)
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
                color[x + (cols * width)] = GetCurveColor(i);

                // Draw vertical line
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
                        color[x + (y * width)] = GetCurveColor(i);
                    }
                }
            }
        }
    }
}
