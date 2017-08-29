using UnityEngine;
using System.Collections;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace CM3D2.HandmaidsTale.Plugin
{
    public class MovieCurveClip {
        private Texture2D curveTexture;

        public List<MovieCurve> curves;
        private int _frame;
        private int _length;

        public int frame
        {
            get => this._frame;
            set => this._frame = Mathf.Clamp(value, 0, value);
        }
        public int length
        {
            get => this._length;
            set => this._length = Mathf.Clamp(value, 10, value);
        }

        public float startSeconds
        {
            get => CM3D2.HandmaidsTale.Plugin.TimelineWindow.FACTOR * this.frame;
        }

        public float lengthSeconds
        {
            get => CM3D2.HandmaidsTale.Plugin.TimelineWindow.FACTOR * this.length;
        }

        public float endSeconds
        {
            get => this.startSeconds + this.lengthSeconds;
        }

        public int end
        {
            get => this.frame + this.length;
            set => this.length = (value - this.frame);
        }

        public float maxValue
        {
            get => this.curves.OrderByDescending(curve => curve.maxValue).First().maxValue;
        }

        public float minValue
        {
            get => this.curves.OrderBy(curve => curve.minValue).First().minValue;
        }

        public MovieCurveClip(int frame, int length)
        {
            this.curves = new List<MovieCurve>();

            {
                curveTexture = new Texture2D(128, (128 / 4));
                Color[] color = new Color[128 * (128 / 4)];
                for (int i = 0; i < color.Length; i++)
                {
                    color[i] = Color.black;
                }
                curveTexture.SetPixels(color);
                curveTexture.Apply();
            }

            this.frame = frame;
            this.length = length;
            this.heldFrames = 0;
        }

        public MovieCurveClip(MovieCurveClip other)
        {
            this.curves = new List<MovieCurve>();
            foreach(MovieCurve curve in other.curves)
            {
                this.curves.Add(new MovieCurve(curve));
            }

            this.frame = other.frame;
            this.length = other.length;

            this.RemakeTexture();
        }

        public int AddCurve(MovieCurve curve)
        {
            this.curves.Add(curve);
            this.RemakeTexture();
            return this.curves.Count - 1;
        }

        public void RemoveCurve(int index)
        {
            this.curves.RemoveAt(index);
            this.RemakeTexture();
        }

        public void RemakeTexture()
        {
            Texture2D.Destroy(this.curveTexture);
            this.curveTexture = CurveTexture.CreateClipCurveTexture(this.curves, this.minValue * 1.25f, this.maxValue * 1.25f);
        }

        public void Draw(Rect rectItem, Rect screenPos)
        {
            this.wasClicked = false;
            if(GUI.RepeatButton(rectItem, ""))
            {
                this.wasPressed = true;
                notHeldFrames = 0;
                Vector2 mousePos = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                if (this.heldFrames == 0)
                {
                    this.startClickPos = mousePos;
                }

                this.heldFrames += 1;

                if(Vector2.Distance(mousePos, this.startClickPos) > 1)
                {
                    isDragging = true;
                }
                //Debug.Log(isResizing + " " + isDragging + " " + heldFrames + " " + Vector2.Distance(mousePos, this.startClickPos));
            }
            else
            {
                this.wasPressed = false;
                this.isDragging = false;
                this.isResizingLeft = false;
                this.isResizingRight = false;

                if (notHeldFrames > 2)
                {
                    if (this.heldFrames > 1 && this.heldFrames < 30 && !isDragging && !isResizingLeft && !isResizingRight)
                        this.wasClicked = true;
                    this.heldFrames = 0;
                }
                notHeldFrames++;
            }
            GUI.DrawTexture(rectItem, this.curveTexture);
        }

        private int heldFrames;
        private int notHeldFrames;
        public bool wasPressed { get; private set; }
        public bool wasClicked { get; private set; }
        public bool isDragging { get; private set; }
        public bool isResizingLeft { get; private set; }
        public bool isResizingRight { get; private set; }
        private Vector2 startClickPos;

        public bool HasTime(float time)
        {
            if (time < this.startSeconds || time > this.endSeconds)
            {
                return false;
            }
            return true;
        }

        private void OnCurveWindow(List<MovieCurve> curves)
        {
            this.curves = curves;
            this.RemakeTexture();
        }

        public void InsertKeyframeAtTime(float time)
        {
            foreach(MovieCurve curve in this.curves)
            {
                curve.InsertKeyframeAtTime(time);
            }
        }
    }
}
