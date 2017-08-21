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

    internal class TestWindow : ScrollablePane
    {
        #region Methods

        public TestWindow( int fontSize, int id ) : base ( fontSize, id ) {}

        override public void Awake()
        {
            try
            {
                this.button = new Plugin.CustomButton();
                this.button.Text = "button";
                this.button.Click += this.DoIt;
                this.ChildControls.Add(this.button);
                this.showButton = new Plugin.CustomButton();
                this.showButton.Text = "show";
                this.showButton.Click += this.ShowCurve;
                this.ChildControls.Add(this.showButton);
            }
            catch( Exception e )
            {
                Debug.LogError( e.ToString() );
            }
        }
        override public void ShowPane()
        {
            try
            {
                this.showButton.Left = this.Left + ControlBase.FixedMargin;
                this.showButton.Top = this.Top + ControlBase.FixedMargin;
                this.showButton.Width = this.Width / 2 - ControlBase.FixedMargin / 4;
                this.showButton.Height = this.ControlHeight;
                this.showButton.Text = this.Text;
                this.showButton.Visible = true;
                this.showButton.OnGUI();
                GUIUtil.AddGUICheckbox(this, this.button, this.showButton);

                if (this.doingIt)
                {
                    Camera cam = GameObject.Find("Main Camera").GetComponent<Camera>();
                    Vector3 pos = cam.transform.position;
                    pos.x = 2.0f * this.animationCurve.Evaluate(this.currentTime);
                    cam.transform.position = pos;
                    this.currentTime += this.amountPerDelta;
                    if(this.currentTime > 1.0f)
                    {
                        this.currentTime = 1.0f;
                        this.doingIt = false;
                    }
                }

                this.Height = GUIUtil.GetHeightForParent(this);
            }

            catch ( Exception e )
            {
                Debug.LogError( e.ToString() );
            }
        }

        private void ShowCurve( object sender, EventArgs args )
        {
            GlobalCurveWindow.Set(new Vector2(100, 100), this.FontSize * 20, this.FontSize, animationCurve, (x) => { this.animationCurve = x; });
        }

        private void DoIt( object sender, EventArgs args )
        {
            this.currentTime = 0f;
            this.doingIt = true;
        }

        #region Fields
        public AnimationCurve animationCurve = new AnimationCurve(new Keyframe[3]
        {
            new Keyframe(0.0f, 0.0f, 0.0f, 1f),
            new Keyframe(0.5f, 0.5f, 0.5f, 0.5f),
            new Keyframe(1f, 1f, 1f, 0.0f)
        });
        private float currentTime = 0f;
        private float amountPerDelta = 0.001f;
        private bool doingIt = false;

        private CustomButton button = null;
        private CustomButton showButton = null;
        #endregion

        #endregion
    }
    #endregion
}
