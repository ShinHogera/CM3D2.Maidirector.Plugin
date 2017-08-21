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
                this.showButton.Left = this.Left + ControlBase.FixedMargin;
                this.showButton.Top = this.Top + ControlBase.FixedMargin;
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

                Rect toggleRect = this.showButton.WindowRect;
                toggleRect.y -= this.ControlHeight;
                int iTmp;
                if ((iTmp = GUI.Toolbar(toggleRect, (int)this.dragMode, DRAG_MODES)) >= 0)
                {
                    this.dragMode = (DragMode)iTmp;
                }

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

                for (int i = 0; i < this.tracks.Count; i++)
                {
                    this.drawTrack(i);
                }

                this.Height = GUIUtil.GetHeightForParent(this) + 5 * this.ControlHeight;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        public override void Update()
        {
            if(!this.tracks.Any())
            {
                MovieCameraTargetRotationTrack cam = new MovieCameraTargetRotationTrack();

                cam.AddClip(new MovieCurveClip(0, 60));
                this.tracks.Add(cam);
            }
            if (this.isPlaying)
            {
                foreach (MovieTrack track in this.tracks)
                {
                    track.PreviewTime(this.playTime);
                }
                this.seekerPos = this.playTime * framesPerSecond * pixelsPerFrame;
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
            if(this.isPlaying == false)
            {
                this.playTime = 0f;
                this.Update();
            }

            this.isPlaying = false;
        }

        private void Add(object sender, EventArgs args)
        {
            GlobalComponentPicker.Set(new Vector2(100, 100), 200, this.FontSize, (g, c) =>
                    {
                        MoviePropertyTrack existing = new MoviePropertyTrack(g, c);

                        existing.AddClip(new MovieCurveClip(60, 60));
                        this.tracks.Add(existing);
                    });
        }

        private void drawTrack(int index)
        {
            Rect rect = new Rect(0, (index + 1) * (this.ControlHeight * 2), 50, this.ControlHeight * 2);

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
                this.drawClip(ref asd, this.tracks[index]);
            }
            GUILayout.EndArea();
        }

        private void drawClip(ref MovieCurveClip clip, MovieTrack track)
        {
            Rect rect = new Rect((clip.frame * pixelsPerFrame) + ControlBase.FixedMargin,
                                 0,
                                 (clip.length * pixelsPerFrame),
                                 this.ControlHeight * 2);

            int pixelDiffToFramePos = (int)((Input.mousePosition.x - this.ScreenPos.x) / pixelsPerFrame) - 50;

            clip.Draw(rect, this.ScreenPos);

            if (clip.isDragging)
            {
                if (this.dragMode == DragMode.Drag)
                {
                    int newFrame = pixelDiffToFramePos - (int)((clip.length / 4 * pixelsPerFrame));
                    if (track.CanInsertClip(newFrame, clip.length))
                        clip.frame = newFrame;
                }
                else if (this.dragMode == DragMode.LeftResize)
                {

                }
                else if (this.dragMode == DragMode.RightResize)
                {
                    int newEnd = pixelDiffToFramePos + (int)((clip.length / 2 * pixelsPerFrame));
                    if (track.CanInsertClip(clip.frame, newEnd - clip.length))
                        clip.end = newEnd;
                }
                this.updated = true;
            }
            else if (clip.wasClicked)
            {
                clip.Edit(() => { this.updated = true; });
            }
        }

        private int currentFrame
        {
            get
            {
                return (int)(this.seekerPos * posAdjustment);
            }
        }

        public static readonly float FACTOR = 1f / 60f;

        private float zoom
        {
            get
            {
                return 50.0f;
            }
        }

        private float posAdjustment
        {
            get
            {
                return FACTOR * zoom;
            }
        }

        private float pixelsPerFrame
        {
            get
            {
                return 1 / (FACTOR * zoom);
            }
        }

        private float framesPerSecond
        {
            get
            {
                return 1 / FACTOR;
            }
        }

        #region Fields
        private static readonly string[] DRAG_MODES = new string[]
            {
                "[",
                "Drag",
                "]"
            };

        private bool isPlaying;
        private float playTime;

        private CustomButton showButton = null;
        private CustomButton playButton = null;
        private CustomButton stopButton = null;
        private CustomButton addButton = null;
        private List<MovieTrack> tracks;
        private List<bool> dragging;
        private bool draggingSeeker;
        private float seekerPos = 0f;
        private bool updated = false;
        private DragMode dragMode;
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

    internal class TestTrack
    {
        public TestTrack()
        {
            this.clips = new List<MovieCurveClip>();
        }

        public List<MovieCurveClip> clips;
    }

    internal class TestClip
    {
        public int frame;
        public int length;
        public bool dragging;
    }
}
