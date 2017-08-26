using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace CM3D2.HandmaidsTale.Plugin
{
    #region TestWindow

    internal class TimelineWindow : ScrollablePane
    {
        #region Methods

        public TimelineWindow(int fontSize, int id) : base(fontSize, id)
        {
            this.tracks = new List<MovieTrack>();
            this.dragging = new List<bool>();
            this.dragMode = DragMode.Drag;

            //for (int i = 0; i < 15; i++)
            //{
            //    GameObject go = GameObject.Find("Main Camera");
            //    var c = go.GetComponent<Transform>();
            //    MoviePropertyTrack existing = new MoviePropertyTrack(go, c);
            //    existing.AddProp(c.GetType().GetProperty("position"));
            //    var clip = new MovieCurveClip(i * 60, 60);
            //    existing.AddClip(clip);
            //    this.dragging.Add(false);
            //    this.tracks.Add(existing);
            //}
        }

        override public void Awake()
        {
            try
            {
                this.showButton = new Plugin.CustomButton();
                this.ChildControls.Add(this.showButton);

                this.playButton = new Plugin.CustomButton();
                this.playButton.Text = "|>";
                this.playButton.Click += this.Play;
                this.ChildControls.Add(this.playButton);

                this.stopButton = new Plugin.CustomButton();
                this.stopButton.Text = "X";
                this.stopButton.Click += this.Stop;
                this.ChildControls.Add(this.stopButton);

                this.addButton = new Plugin.CustomButton();
                this.addButton.Text = "+";
                this.addButton.Click += this.Add;
                this.ChildControls.Add(this.addButton);

                this.curvePane = new CurvePane();
                this.ChildControls.Add(this.curvePane);
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
            }
        }
        override public void ShowPane()
        {
            try
            {
                Rect seekerRect = new Rect(ControlBase.FixedMargin + (this.seekerPos + 50), 0, 20, 40);

                Rect labelRect = new Rect(seekerRect.x, seekerRect.y, 100, 40);
                GUI.Label(labelRect, this.currentFrame + " frame");

                if (GUI.RepeatButton(seekerRect, "") || (draggingSeeker && Input.GetMouseButton(0)))
                {
                    draggingSeeker = true;
                    this.seekerPos = Input.mousePosition.x - this.ScreenPos.x - 10 - 50;
                    this.updated = true;
                }
                else
                {
                    draggingSeeker = false;
                }

                Rect lineRect = new Rect(ControlBase.FixedMargin + (this.seekerPos + 50), 0, 20, 400);

                GUI.Label(lineRect, "|\n|\n|\n|\n|\n|\n|\n|\n|\n|\n|\n|\n|\n|\n|\n|\n|\n|\n|\n|\n");

                Rect rectScroll = new Rect(0, ControlBase.FixedMargin + 20, this.rectGui.width - ControlBase.FixedMargin, 400 - ControlBase.FixedMargin * 4);
                Rect rectScrollView = new Rect(0, 0, guiScrollWidth, guiScrollHeight);

                this.scrollPosition = GUI.BeginScrollView(rectScroll, this.scrollPosition, rectScrollView);


                for(int i = this.tracks.Count - 1; i >= 0; i--)
                {
                    if(this.tracks[i].wantsDelete)
                    {
                        this.tracks.RemoveAt(i);
                    }
                }
                for (int i = 0; i < this.tracks.Count; i++)
                {
                    this.drawTrack(i);
                }

                guiScrollHeight = (this.tracks.Count) * this.ControlHeight * 2;
                guiScrollWidth = 2000;
                GUI.EndScrollView();

                this.showButton.Left = this.Left + ControlBase.FixedMargin;
                this.showButton.Top = rectScroll.yMax + ControlBase.FixedMargin;
                this.showButton.Width = 100;
                this.showButton.Height = this.ControlHeight;
                this.showButton.Visible = true;
                this.showButton.OnGUI();

                this.playButton.Left = this.showButton.Left + this.showButton.Width;
                this.playButton.Top = this.showButton.Top;
                this.playButton.Width = this.showButton.Width;
                this.playButton.Height = this.ControlHeight;
                this.playButton.Visible = true;
                this.playButton.OnGUI();

                this.stopButton.Left = this.playButton.Left + this.playButton.Width;
                this.stopButton.Top = this.playButton.Top;
                this.stopButton.Width = this.playButton.Width;
                this.stopButton.Height = this.ControlHeight;
                this.stopButton.Visible = true;
                this.stopButton.OnGUI();

                this.addButton.Left = this.stopButton.Left + this.stopButton.Width;
                this.addButton.Top = this.stopButton.Top;
                this.addButton.Width = this.stopButton.Width;
                this.addButton.Height = this.ControlHeight;
                this.addButton.Visible = true;
                this.addButton.OnGUI();

                Rect toggleRect = this.addButton.WindowRect;
                toggleRect.x += toggleRect.width;
                int iTmp;
                if ((iTmp = GUI.Toolbar(toggleRect, (int)this.dragMode, DRAG_MODES)) >= 0)
                {
                    this.dragMode = (DragMode)iTmp;
                }
                //this.Height = GUIUtil.GetHeightForParent(this) + 5 * this.ControlHeight;

                Rect curveRect = new Rect(0, toggleRect.yMax, 1000, 300);
                this.curvePane.SetFromRect(curveRect);

                GUI.BeginGroup(curveRect);
                if (this.tracks.Count > 0 &&
                    this.selectedTrack < this.tracks.Count &&
                    this.selectedClip < this.tracks[this.selectedTrack].clips.Count)
                    this.curvePane.Draw(curveRect, this.tracks[this.selectedTrack].clips, this.selectedClip, this.selectedTrack);
                GUI.EndGroup();
                if (this.curvePane.needsUpdate)
                {
                    this.updated = true;
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        public override void Update()
        {
            if (this.isPlaying)
            {
                foreach (MovieTrack track in this.tracks)
                {
                    track.PreviewTime(this.playTime);
                }
                this.playTime += Time.deltaTime;
            }
            else
            {
                if (this.updated)
                {
                    this.updated = false;
                    foreach (MovieTrack track in this.tracks)
                    {
                        track.PreviewTime(this.currentFrame / framesPerSecond);
                    }
                }
            }
        }

        private void Play(object sender, EventArgs args)
        {
            this.isPlaying = true;
        }

        private void Stop(object sender, EventArgs args)
        {
            if(isPlaying == false)
            {
                this.playTime = 0f;
            }
            this.isPlaying = false;
        }

        private void Add(object sender, EventArgs args)
        {
            GlobalComponentPicker.Set(new Vector2(100, 100), 200, this.FontSize, (existing) =>
                    {
                        existing.InsertClipAtFreePos();
                        this.tracks.Add(existing);
                        this.curvePane.SetUpdate();
                    });
        }

        private void drawTrack(int index)
        {
            Rect rect = new Rect(0, (index) * this.ControlHeight * 2, 50, this.ControlHeight * 2);

            GUILayout.BeginArea(rect);
            this.tracks[index].DrawPanel(this.currentFrame / framesPerSecond);
            GUILayout.EndArea();

            rect.x = 50;
            rect.width = this.Width;
            GUI.Box(rect, "");
            GUILayout.BeginArea(rect);
            for (int i = 0; i < this.tracks[index].clips.Count; i++)
            {
                MovieCurveClip asd = this.tracks[index].clips[i];
                this.drawClip(ref asd, this.tracks[index], index, i);
            }
            GUILayout.EndArea();
        }

        private void drawClip(ref MovieCurveClip clip, MovieTrack track, int trackIdx, int clipIdx)
        {
            Rect rect = new Rect((clip.frame * pixelsPerFrame) + ControlBase.FixedMargin,
                                 0,
                                 (clip.length * pixelsPerFrame),
                                 this.ControlHeight * 2);

            int pixelDiffToFramePos = (int)((Input.mousePosition.x - this.ScreenPos.x) / pixelsPerFrame) - 50;

            clip.Draw(rect, this.ScreenPos);

            if(clip.wasPressed)
            {
                this.selectedTrack = trackIdx;
                this.selectedClip = clipIdx;
            }

            if (clip.isDragging)
            {
                switch(this.dragMode)
                {
                    case DragMode.Drag:
                        int newFrame = pixelDiffToFramePos - (int)((clip.length / 4 * pixelsPerFrame));
                        if (track.CanInsertClip(newFrame, clip.length, clip))
                            clip.frame = newFrame;
                        else if(newFrame <= 0)
                            clip.frame = 0;
                        break;

                    case DragMode.RightResize:
                        int newEnd = pixelDiffToFramePos + 15;
                        if (track.CanInsertClip(clip.frame, newEnd - clip.length))
                            clip.end = newEnd;
                        break;

                    default:
                        break;
                }

                this.updated = true;
            }
            else if (clip.wasClicked)
            {
                this.updated = true;
            }
        }

        private int currentFrame
        {
            get =>(int)(this.seekerPos * posAdjustment);
        }

        public static readonly float FACTOR = 1f / 60f;

        public static float zoom
        {
            get => 50.0f;
        }

        public static float posAdjustment
        {
            get => FACTOR * zoom;
        }

        public static float pixelsPerFrame
        {
            get => 1 / (FACTOR * zoom);
        }

        public static float framesPerSecond
        {
            get => 1 / FACTOR;
        }

        #region Fields
        private static readonly string[] DRAG_MODES = new string[]
            {
                "[",
                "Drag",
                "]"
            };

        private bool isPlaying;

        private float _playTime;
        private float playTime
        {
            get => _playTime;
            set
            {
                _playTime = value;
                this._seekerPos = _playTime * framesPerSecond * pixelsPerFrame;
                this.updated = true;
            }
        }

        private float _seekerPos = 0f;
        private float seekerPos
        {
            get => _seekerPos;
            set
            {
                _seekerPos = value;
                this._playTime = _seekerPos / (framesPerSecond * pixelsPerFrame);
                this.updated = true;
            }
        }

        private CustomButton showButton = null;
        private CustomButton playButton = null;
        private CustomButton stopButton = null;
        private CustomButton addButton = null;
        private CurvePane curvePane;

        private List<MovieTrack> tracks;
        private List<bool> dragging;
        private bool draggingSeeker;
        private bool updated = false;
        private DragMode dragMode;
        private Vector2 scrollPosition;
        private float guiScrollWidth;
        private float guiScrollHeight;
        private int selectedTrack;
        private int selectedClip;
        #endregion

        private enum DragMode
        {
            LeftResize,
            Drag,
            RightResize,
        }

        #endregion
    }
    #endregion
}
