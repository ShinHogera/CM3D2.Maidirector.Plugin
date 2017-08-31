using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Linq;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace CM3D2.Maidirector.Plugin
{
    #region TimelineWindow

    internal class TimelineWindow : ScrollablePane
    {
        #region Methods

        public TimelineWindow(int fontSize, int id) : base(fontSize, id)
        {
            this.tracks = new List<MovieTrack>();
            this.dragging = new List<bool>();
            this.dragMode = DragMode.Drag;

            this.lineTexture = new Texture2D(1, 1);
            this.lineTexture.SetPixel(0, 0, Color.red);
            this.lineTexture.Apply();

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
                this.languageBox = new CustomComboBox( Translation.GetTranslations() );
                this.languageBox.Text = Translation.GetText("UI", "language");
                this.languageBox.SelectedItem = Translation.CurrentTranslation;
                this.languageBox.SelectedIndexChanged += this.ChangeLanguage;
                this.ChildControls.Add( this.languageBox );

                this.playButton = new Plugin.CustomButton();
                this.playButton.Text = "▶";
                this.playButton.Click += this.Play;
                this.ChildControls.Add(this.playButton);

                this.stopButton = new Plugin.CustomButton();
                this.stopButton.Text = "■";
                this.stopButton.Click += this.Stop;
                this.ChildControls.Add(this.stopButton);

                this.addButton = new Plugin.CustomButton();
                this.addButton.Text = Translation.GetText("Timeline", "addTrack");
                this.addButton.Click += this.Add;
                this.ChildControls.Add(this.addButton);

                this.copyClipButton = new Plugin.CustomButton();
                this.copyClipButton.Text = Translation.GetText("Timeline", "copyClip");
                this.copyClipButton.Click += this.CopyClip;
                this.ChildControls.Add(this.copyClipButton);

                this.deleteClipButton = new Plugin.CustomButton();
                this.deleteClipButton.Text = Translation.GetText("Timeline", "deleteClip");
                this.deleteClipButton.Click += this.DeleteClip;
                this.ChildControls.Add(this.deleteClipButton);

                this.curvePane = new CurvePane();
                this.ChildControls.Add(this.curvePane);
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
            }
        }

        private const float PANEL_WIDTH = 200;

        override public void ShowPane()
        {
            try
            {
                float seek = this.seekerPos + PANEL_WIDTH - this.scrollPosition.x;
                Rect seekerRect = new Rect(ControlBase.FixedMargin + seek, 0, 20, 40);

                Rect labelRect = new Rect(seekerRect.x + seekerRect.width + 5, seekerRect.y, 100, 40);
                GUI.Label(labelRect, Translation.GetText("Timeline", "frame") + " " + this.currentFrame);

                if (GUI.RepeatButton(seekerRect, "") || (draggingSeeker && Input.GetMouseButton(0)))
                {
                    draggingSeeker = true;
                    this.seekerPos = Input.mousePosition.x - this.ScreenPos.x + this.scrollPosition.x - 10 - PANEL_WIDTH;
                    this.updated = true;
                }
                else
                {
                    draggingSeeker = false;
                }

                Rect trackPanelRect = new Rect(0, ControlBase.FixedMargin + 20, PANEL_WIDTH, 400 - ControlBase.FixedMargin * 4);

                GUILayout.BeginArea(trackPanelRect);
                for(int i = 0; i < this.tracks.Count; i++)
                {
                    MovieTrack track = this.tracks[i];
                    Rect panel = new Rect(0,
                                          this.scrollPosition.y + (this.ControlHeight * 2 * i),
                                          PANEL_WIDTH - 150,
                                          this.ControlHeight * 2);

                    GUILayout.BeginArea(panel);
                    track.DrawPanel(this.currentFrame / framesPerSecond);

                    if(track.inserted)
                        this.updated = true;


                    GUILayout.EndArea();

                    GUIStyle style = new GUIStyle( "label" );
                    style.alignment = TextAnchor.MiddleLeft;
                    panel.x = PANEL_WIDTH - 150 + ControlBase.FixedMargin;
                    panel.width = 150 - ControlBase.FixedMargin * 2;
                    GUI.Label(panel, this.tracks[i].GetName(), style);
                }
                GUILayout.EndArea();

                Rect rectScroll = new Rect(PANEL_WIDTH, ControlBase.FixedMargin + 20, this.rectGui.width - ControlBase.FixedMargin - PANEL_WIDTH, 400 - ControlBase.FixedMargin * 4);
                Rect rectScrollView = new Rect(0, 0, guiScrollWidth, guiScrollHeight);

                this.scrollPosition = GUI.BeginScrollView(rectScroll, this.scrollPosition, rectScrollView);


                for(int i = this.tracks.Count - 1; i >= 0; i--)
                {
                    if(this.tracks[i].wantsDelete)
                    {
                        // reset to the value at the beginning of the track, instead of in the middle
                        this.tracks[i].PreviewTime(0f);
                        this.tracks.RemoveAt(i);
                    }
                }
                for (int i = 0; i < this.tracks.Count; i++)
                {
                    this.drawTrack(i);
                }

                guiScrollHeight = (this.tracks.Count) * this.ControlHeight * 2;
                GUI.EndScrollView();

                Rect lineRect = new Rect(ControlBase.FixedMargin + seek, 0, 1, 400);
                GUI.DrawTexture(lineRect, this.lineTexture);

                this.playButton.Left = this.Left + ControlBase.FixedMargin;
                this.playButton.Top = rectScroll.yMax + ControlBase.FixedMargin;
                this.playButton.Width = 50;
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
                this.addButton.Width = 100;
                this.addButton.Height = this.ControlHeight;
                this.addButton.Visible = true;
                this.addButton.OnGUI();

                this.copyClipButton.Left = this.addButton.Left + this.addButton.Width;
                this.copyClipButton.Top = this.addButton.Top;
                this.copyClipButton.Width = this.addButton.Width;
                this.copyClipButton.Height = this.ControlHeight;
                this.copyClipButton.Visible = true;
                this.copyClipButton.OnGUI();

                this.deleteClipButton.Left = this.copyClipButton.Left + this.copyClipButton.Width;
                this.deleteClipButton.Top = this.copyClipButton.Top;
                this.deleteClipButton.Width = this.copyClipButton.Width;
                this.deleteClipButton.Height = this.ControlHeight;
                this.deleteClipButton.Visible = true;
                this.deleteClipButton.OnGUI();

                Rect toggleRect = this.deleteClipButton.WindowRect;
                toggleRect.x += toggleRect.width + ControlBase.FixedMargin * 2;
                toggleRect.width *= 1.5f;
                int iTmp;
                if ((iTmp = GUI.Toolbar(toggleRect, (int)this.dragMode, DRAG_MODES)) >= 0)
                {
                    this.dragMode = (DragMode)iTmp;
                }
                //this.Height = GUIUtil.GetHeightForParent(this) + 5 * this.ControlHeight;

                toggleRect.x += toggleRect.width + ControlBase.FixedMargin * 2;
                toggleRect.width *= 1.25f;
                if ((iTmp = GUI.Toolbar(toggleRect, (int)this.curvePane.mode, CURVE_PANE_MODES)) >= 0)
                {
                    this.curvePane.mode = (CurvePane.CurvePaneMode)iTmp;
                }

                toggleRect.x += toggleRect.width + ControlBase.FixedMargin * 2;
                this.languageBox.SetFromRect(toggleRect);
                this.languageBox.OnGUI();

                // toggleRect.x += toggleRect.width + ControlBase.FixedMargin * 4;
                // string sTmp =
                // toggleRect.x += toggleRect.width + ControlBase.FixedMargin * 4;

                Rect curveRect = new Rect(0, toggleRect.yMax, 1000, 300);
                this.curvePane.SetFromRect(curveRect);

                GUI.BeginGroup(curveRect);
                if (this.ClipIsSelected())
                    this.curvePane.Draw(curveRect, this.rectGui, this.tracks[this.selectedTrackIndex].clips, this.selectedClipIndex, this.selectedTrackIndex);
                GUI.EndGroup();
                if (this.curvePane.needsUpdate)
                {
                    this.updated = true;
                }
                if (this.curvePane.wantsSave)
                {
                    Directory.CreateDirectory(SAVE_DIR);
                    XDocument doc = Serialize.SerializeTake(this);
                    string savePath = GetSavePath("test");
                    doc.Save(savePath);
                    this.curvePane.wantsSave = false;
                }

                {
                    Vector2 mousePos = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);

                    bool enableGameGui = true;
                    bool m = Input.GetAxis("Mouse ScrollWheel") != 0;
                    for (int i = 0; i < 3; i++)
                    {
                        m |= Input.GetMouseButtonDown(i);
                    }
                    if (m)
                    {
                        enableGameGui = !rectGui.Contains(mousePos);
                    }
                    GameMain.Instance.MainCamera.SetControl(enableGameGui);
                    UICamera.InputEnable = enableGameGui;
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        private readonly static string SAVE_DIR = ConstantValues.ConfigDir + @"\Saves";
        public string GetSavePath(string name)
        {
            return SAVE_DIR + @"\" + name + ".xml";
        }

        private bool ClipIsSelected()
        {
            return this.tracks.Count > 0 &&
                this.selectedTrackIndex < this.tracks.Count &&
                this.selectedClipIndex < this.tracks[this.selectedTrackIndex].clips.Count;
        }

        private void FollowCursor()
        {
            float range = this.rectGui.width / 2;
            float seek = (this.seekerPos) + PANEL_WIDTH;
            if(seek > range && seek < this.guiScrollWidth - range + PANEL_WIDTH + 10)
            {
                this.scrollPosition.x = seek - range;
            }
            else if(seek <= range)
            {
                this.scrollPosition.x = 0;
            }
            else if(guiScrollWidth > this.rectGui.width && seek >= this.guiScrollWidth - range + PANEL_WIDTH + 10)
            {
                this.scrollPosition.x = this.guiScrollWidth - this.rectGui.width + PANEL_WIDTH + 10;
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

                this.FollowCursor();
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

                    if(this.tracks.Count == 0)
                        this.guiScrollWidth = 300;
                    else
                        this.guiScrollWidth = TakeEndFrame();
                }
            }
        }

        private int TakeEndFrame() => (int)(this.tracks.OrderByDescending(track => track.endTime).First().endTime + 300);

        public void Play(object sender, EventArgs args)
        {
            this.isPlaying = !this.isPlaying;
        }

        public void Stop(object sender, EventArgs args)
        {
            if(isPlaying == false)
            {
                this.playTime = 0f;
                this.scrollPosition.x = 0f;
            }
            this.isPlaying = false;
        }

        private void Add(object sender, EventArgs args)
        {
            GlobalComponentPicker.Set(new Vector2(this.rectGui.x + 250, this.rectGui.y - 20), 250, this.FontSize, (existing) =>
                    {
                        existing.InsertNewClip();
                        this.tracks.Add(existing);
                        this.curvePane.SetUpdate();
                    });
        }

        private void CopyClip(object sender, EventArgs args)
        {
            if(!this.ClipIsSelected())
                return;

            this.tracks[this.selectedTrackIndex].CopyClip(this.selectedClipIndex);
            this.updated = true;
        }

        private void DeleteClip(object sender, EventArgs args)
        {
            if(!this.ClipIsSelected())
                return;

            this.tracks[this.selectedTrackIndex].DeleteClip(this.selectedClipIndex);
            this.updated = true;
        }

        private void drawTrack(int index)
        {
            Rect rect = new Rect(0, (index) * this.ControlHeight * 2, this.guiScrollWidth, this.ControlHeight * 2);

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

            int pixelDiffToFramePos = (int)((Input.mousePosition.x - this.ScreenPos.x + this.scrollPosition.x - PANEL_WIDTH) / pixelsPerFrame);

            // GUI.Box(new Rect(pixelDiffToFramePos * pixelsPerFrame, 20, 40, 40), "");
            bool draggingBefore = clip.isDragging;
            clip.Draw(rect, this.ScreenPos);

            if(clip.wasPressed)
            {
                this.selectedTrackIndex = trackIdx;
                this.selectedClipIndex = clipIdx;
            }

            if (clip.isDragging && !this.updated)
            {
                switch(this.dragMode)
                {
                    case DragMode.Drag:
                        int newFrame = pixelDiffToFramePos - (clip.length / 2);
                        if (track.CanInsertClip(newFrame, clip.length, clip))
                            clip.frame = newFrame;
                        else if(newFrame <= 0)
                            clip.frame = 0;
                        break;

                    case DragMode.RightResize:
                        int newEnd = pixelDiffToFramePos + (clip.length / 10);
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
            else if(draggingBefore == true && clip.isDragging == false)
            {
                track.ResolveCollision(clipIdx);
            }
        }

        private void ChangeLanguage( object sender, EventArgs args )
        {
            this.wantsLanguageChange = true;
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

        public string LanguageValue
        {
            get => this.languageBox.SelectedItem;
        }

        #region Fields
        private static readonly string[] DRAG_MODES = new string[]
            {
                Translation.GetText("Timeline", "drag"),
                Translation.GetText("Timeline", "resize"),
            };

        private static readonly string[] CURVE_PANE_MODES = new string[]
            {
                Translation.GetText("Timeline", "curve"),
                Translation.GetText("Timeline", "keyframe"),
                "Data"// Translation.GetText("Timeline", "data")
            };

        private bool isPlaying;

        private float _playTime;
        private float playTime
        {
            get => _playTime;
            set
            {
                _playTime = Mathf.Max(0, value);
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
                _seekerPos = Mathf.Max(0, value);
                this._playTime = _seekerPos / (framesPerSecond * pixelsPerFrame);
                this.updated = true;
            }
        }

        public bool wantsLanguageChange { get; set; }

        private CustomButton playButton = null;
        private CustomButton stopButton = null;
        private CustomButton addButton = null;
        private CustomButton copyClipButton = null;
        private CustomButton deleteClipButton = null;
        private CustomComboBox languageBox = null;
        public CurvePane curvePane;

        private Texture2D lineTexture = null;

        public List<MovieTrack> tracks;
        private List<bool> dragging;
        private bool draggingSeeker;
        private bool updated = false;
        private DragMode dragMode;
        private Vector2 scrollPosition;
        private float guiScrollWidth;
        private float guiScrollHeight;
        private int selectedTrackIndex;
        private int selectedClipIndex;
        #endregion

        private enum DragMode
        {
            // LeftResize,
            Drag,
            RightResize,
        }

        #endregion
    }
    #endregion
}
