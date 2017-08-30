using System;
using System.Reflection;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace CM3D2.Maidirector.Plugin
{
    public static class GlobalComponentPicker
    {
        static GlobalComponentPicker() {
            color = new ComponentWindow(309);
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

        public static void Set(Vector2 p, float fWidth, int iFontSize, Action<MovieTrack> f)
        {
            color.Set(p, fWidth, iFontSize, f);
        }

        private static GUIStyle gsWin;
        private static ComponentWindow color;

        internal class ComponentWindow
        {
            public readonly int WINDOW_ID;

            public Rect rect { get; set; }
            private float fMargin { get; set; }
            private float fRightPos { get; set; }
            private float fUpPos { get; set; }

            public bool show { get; private set; }

            public Action<MovieTrack> func { get; private set; }

            private GUIStyle gsLabel { get; set; }
            private GUIStyle gsButton { get; set; }

            private Dictionary<ObjectCategory, List<GameObject>> gameObjects;
            private List<Component> components;
            private List<PhotoMotionData> animations;
            private List<int> maidIndices;

            private CustomComboBox trackTypeBox;
            private CustomComboBox objectCategoryBox;
            private CustomComboBox objectBox;
            private CustomComboBox componentBox;
            private CustomComboBox maidBox;
            private CustomComboBox animationNameBox;
            private CustomButton okButton;
            private CustomButton cancelButton;

            private ObjectCategory selectedObjectCategory
            {
                get => (ObjectCategory)this.objectCategoryBox.SelectedIndex;
            }

            private GameObject selectedObject
            {
                get => this.gameObjects[selectedObjectCategory][objectBox.SelectedIndex];
            }

            private Component selectedComponent
            {
                get => this.components[componentBox.SelectedIndex];
            }

            private Maid selectedMaid
            {
                get => GameMain.Instance.CharacterMgr.GetMaid(this.maidIndices[maidBox.SelectedIndex]);
            }

            private PhotoMotionData selectedAnimation
            {
                get => this.animations[animationNameBox.SelectedIndex];
            }

            public ComponentWindow(int iWIndowID)
            {
                WINDOW_ID = iWIndowID;

                this.trackTypeBox = new CustomComboBox(Enum.GetNames(typeof(TrackType)));
                this.trackTypeBox.SelectedIndex = 0;

                // Object property track
                this.objectCategoryBox = new CustomComboBox(Enum.GetNames(typeof(ObjectCategory)));
                this.objectCategoryBox.SelectedIndex = 0;
                this.objectCategoryBox.SelectedIndexChanged += this.SelectObjectCategory;
                this.objectBox = new CustomComboBox();
                this.objectBox.SelectedIndexChanged += this.SelectObject;
                this.componentBox = new CustomComboBox();

                // Maid track
                this.maidBox = new CustomComboBox();

                // Maid animation track
                this.animationNameBox = new CustomComboBox();

                this.okButton = new CustomButton();
                this.okButton.Text = Translation.GetText("UI", "ok");
                this.okButton.Click = this.Ok;
                this.cancelButton = new CustomButton();
                this.cancelButton.Text = Translation.GetText("UI", "cancel");
                this.cancelButton.Click = this.Cancel;
            }

            public void Set(Vector2 p, float fWidth, int iFontSize, Action<MovieTrack> f)
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

                this.LoadObjects();
                this.LoadMaids();
                this.LoadAnimationNames();
            }

            private bool IsPermittedGameObject(GameObject go) => go.activeInHierarchy &&
                !go.GetComponents<Component>().Any(c => c.GetType().Name.StartsWith("UI"));

            private bool IsCamera(GameObject go) => go.name.Contains("Camera") || go.GetComponent<Camera>() != null;

            private void LoadObjects()
            {
                this.gameObjects = new Dictionary<ObjectCategory, List<GameObject>>();

                this.gameObjects[ObjectCategory.All] = UnityEngine.Object.FindObjectsOfType<GameObject>().ToList();

                if(GameMain.Instance.BgMgr.current_bg_object != null)
                {
                this.gameObjects[ObjectCategory.Background] = GameMain.Instance.BgMgr.current_bg_object
                    .GetComponentsInChildren<Transform>()
                    .Select(t => t.gameObject)
                    .ToList();
                }
                else
                {
                    this.gameObjects[ObjectCategory.Background] = new List<GameObject>();
                }
                this.gameObjects[ObjectCategory.Background].Add(GameMain.Instance.BgMgr.current_bg_object);

                this.gameObjects[ObjectCategory.Camera] = UnityEngine.Object.FindObjectsOfType<GameObject>()
                    .Where(IsCamera)
                    .ToList();

                this.objectCategoryBox.SelectedIndex = 0;
            }

            private void LoadMaids()
            {
                List<string> maidNames = new List<string>();
                maidIndices = new List<int>();
                for(int i = 0; i < GameMain.Instance.CharacterMgr.GetMaidCount(); i++)
                {
                    Maid maid = GameMain.Instance.CharacterMgr.GetMaid(i);
                    if(IsValidMaid(maid))
                    {
                        maidIndices.Add(i);
                        maidNames.Add(maid.name);
                    }
                }
                this.maidBox.Items = maidNames.Select(mn => new GUIContent(mn)).ToList();
                this.maidBox.SelectedIndex = 0;
            }

            private void LoadAnimationNames()
            {
                if(PhotoMotionData.data == null)
                    PhotoMotionData.Create();

                this.animations = PhotoMotionData.data.Where(d => !string.IsNullOrEmpty(d.direct_file)).ToList();
                this.animationNameBox.Items = this.animations.Select(d => new GUIContent(d.name)).ToList();
                this.animationNameBox.SelectedIndex = 0;
            }

            private static bool IsValidMaid(Maid maid) => maid != null && maid.body0.trsHead != null && maid.Visible;

            private void SelectObjectCategory(object sender, EventArgs args)
            {
                this.objectBox.Items = this.gameObjects[this.selectedObjectCategory]
                    .Select(ob => new GUIContent(ob.GetType().Name))
                    .ToList();

                this.objectBox.SelectedIndex = 0;
            }

            private void SelectObject(object sender, EventArgs args)
            {
                int index = this.objectBox.SelectedIndex;
                if (!this.gameObjects.Any() || index < 0 || index > this.gameObjects[this.selectedObjectCategory].Count)
                    return;

                this.components = this.selectedObject.GetComponents<Component>().ToList();
                this.componentBox.Items = this.components.Select(co => new GUIContent(co.GetType().Name)).ToList();
                this.componentBox.SelectedIndex = 0;
            }

            private void Ok(object sender, EventArgs args)
            {
                if(this.trackTypeBox.SelectedIndex == (int)TrackType.ObjectProperty)
                {
                    func(new MoviePropertyTrack(this.selectedObject, this.selectedComponent));
                }
                else if(this.trackTypeBox.SelectedIndex == (int)TrackType.CameraTarget)
                {
                    func(new MovieCameraTargetTrack());
                }
                else if(this.trackTypeBox.SelectedIndex == (int)TrackType.MaidAnimation)
                {
                    func(new MovieMaidAnimationTrack(this.selectedMaid, this.selectedAnimation));
                }
                else if(this.trackTypeBox.SelectedIndex == (int)TrackType.MaidFace)
                {
                    func(new MovieMaidFaceTrack(this.selectedMaid));
                }
                // else if(this.trackTypeBox.SelectedIndex == (int)TrackType.MaidIK)
                // {
                //     func(new MovieMaidIKTrack(this.selectedMaid));
                // }
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

                this.trackTypeBox.SetFromRect(rectItem);
                this.trackTypeBox.ScreenPos = new Rect(rect.x, rect.y, 0, 0);
                this.trackTypeBox.OnGUI();

                rectItem.y += rectItem.height;
                if(this.trackTypeBox.SelectedIndex == (int)TrackType.ObjectProperty)
                {
                    this.objectCategoryBox.SetFromRect(rectItem);
                    this.objectCategoryBox.ScreenPos = new Rect(rect.x, rect.y, 0, 0);
                    this.objectCategoryBox.OnGUI();

                    this.objectBox.SetFromRect(rectItem);
                    this.objectBox.ScreenPos = new Rect(rect.x, rect.y, 0, 0);
                    this.objectBox.OnGUI();
                    if(this.objectBox.Items.Count == 0)
                        GUI.enabled = false;

                    rectItem.y += rectItem.height;
                    this.componentBox.SetFromRect(rectItem);
                    this.componentBox.ScreenPos = new Rect(rect.x, rect.y, 0, 0);
                    this.componentBox.OnGUI();
                    if(this.componentBox.Items.Count == 0)
                        GUI.enabled = false;
                }
                else if(this.trackTypeBox.SelectedIndex == (int)TrackType.MaidAnimation ||
                        this.trackTypeBox.SelectedIndex == (int)TrackType.MaidFace)
                    // || this.trackTypeBox.SelectedIndex == (int)TrackType.MaidIK)
                {
                    this.maidBox.SetFromRect(rectItem);
                    this.maidBox.ScreenPos = new Rect(rect.x, rect.y, 0, 0);
                    this.maidBox.OnGUI();
                    if(this.maidBox.Items.Count == 0)
                        GUI.enabled = false;
                }

                if(this.trackTypeBox.SelectedIndex == (int)TrackType.MaidAnimation)
                {
                    rectItem.y += rectItem.height;
                    this.animationNameBox.SetFromRect(rectItem);
                    this.animationNameBox.ScreenPos = new Rect(rect.x, rect.y, 0, 0);
                    this.animationNameBox.OnGUI();
                    if(this.animationNameBox.Items.Count == 0)
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

            private enum ObjectCategory
            {
                Camera,
                Background,
                All
            }

            private enum TrackType
            {
                ObjectProperty,
                CameraTarget,
                MaidAnimation,
                MaidFace,
                // MaidIK
            }
        }
    }
}
