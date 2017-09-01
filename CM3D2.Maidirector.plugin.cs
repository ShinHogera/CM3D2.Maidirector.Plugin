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
using UnityInjector;
using UnityInjector.Attributes;

namespace CM3D2.Maidirector.Plugin
{
    #region PluginMain
    [PluginFilter( "CM3D2x64" ), PluginFilter( "CM3D2x86" ), PluginFilter( "CM3D2VRx64" ), PluginName( "Maidirector" ), PluginVersion( "0.0.2.0" )]
    public class Maidirector : PluginBase
    {
        #region Methods
        ///-------------------------------------------------------------------------
        /// <summary>起動処理</summary>
        ///-------------------------------------------------------------------------
        public void Awake()
        {
            try
            {
                GameObject.DontDestroyOnLoad( this );

                ReadPluginPreferences();
                CurveTexture.Init();
                Translation.Initialize(configLanguage);
            }
            catch( Exception e )
            {
                Debug.LogError( e.ToString() );
            }
        }

        ///-------------------------------------------------------------------------
        /// <summary>シーン変更</summary>
        ///-------------------------------------------------------------------------
        public void OnLevelWasLoaded( int level )
        {
            ConstantValues.Scene sceneLevel = ( ConstantValues.Scene )level;

            if(this.timelineWindow != null) {
                // // メイドエディット画面または夜伽画面から、メイドエディット画面、夜伽画面以外に遷移した場合
                if(( this.sceneNo == ConstantValues.Scene.ScenePhoto  ) &&
                   ( sceneLevel != ConstantValues.Scene.ScenePhoto ) )
                {
                    initialized = false;
                    Translation.CurrentTranslation = configLanguage;

                }
                else if (( this.sceneNo != ConstantValues.Scene.ScenePhoto  ) &&
                         ( sceneLevel == ConstantValues.Scene.ScenePhoto ) )
                {
                    initialized = false;
                    Translation.CurrentTranslation = configLanguage;
                }
            }

            this.sceneNo = sceneLevel;
        }

        ///-------------------------------------------------------------------------
        /// <summary>更新</summary>
        ///-------------------------------------------------------------------------
        public void Update()
        {
            try
            {
                // if(this.Enable)
                // {
                if (!initialized) {
                    {
                        this.Initialize();
                        this.initialized = true;
                    }
                }

                if(this.timelineWindow != null && this.timelineWindow.wantsLanguageChange)
                {
                    string lang = this.timelineWindow.LanguageValue;
                    if(Translation.HasTranslation(lang))
                    {
                        Preferences["Config"]["Language"].Value = timelineWindow.LanguageValue;
                        configLanguage = timelineWindow.LanguageValue;
                        SaveConfig();

                        this.timelineWindow.wantsLanguageChange = false;
                    }
                }

                if( Input.GetKeyDown( configWindowKey ) )
                {
                    if( this.selectedMode == ConstantValues.EditMode.Movie )
                    {
                        this.selectedMode = ConstantValues.EditMode.Disable;
                    }
                    else
                    {
                        this.selectedMode = ConstantValues.EditMode.Movie;
                    }
                }
                else if( Input.GetKeyDown( configPlayKey ) )
                {
                    this.timelineWindow.Play(this, new EventArgs());
                }
                else if( Input.GetKeyDown( configStopKey ) )
                {
                    this.timelineWindow.Stop(this, new EventArgs());
                }
                else if( Input.GetKeyDown( configHideUIKey ) )
                {
                    this.enableUI = !this.enableUI;

                    if(this.windowMgr == null)
                    {
                        GameObject placementWindow = GameObject.Find("PlacementWindow");
                        if(placementWindow != null)
                        {
                            PlacementWindow placementWindowCompo = placementWindow.GetComponent<PlacementWindow>();
                            this.windowMgr = placementWindowCompo.mgr;
                        }
                    }

                    if(this.windowMgr != null)
                    {
                        this.SetUIEnabled(this.enableUI);
                    }
                }

                // if( this.selectedMode == ConstantValues.EditMode.Movie )
                // {
                this.timelineWindow.Update();
                // }
                // }
            }
            catch( Exception e )
            {
                Debug.LogError( e.ToString() );
            }
        }

        ///-------------------------------------------------------------------------
        /// <summary>GUI処理</summary>
        ///-------------------------------------------------------------------------
        public void OnGUI()
        {
            try
            {
                // 機能有効の場合
                // if( this.Enable )

                if( GizmoRender.UIVisible )
                {
                    // 補助キーの押下有無確認
                    bool isCtrl = Input.GetKey( KeyCode.LeftControl ) || Input.GetKey( KeyCode.RightControl );
                    bool isShift = Input.GetKey( KeyCode.LeftShift ) || Input.GetKey( KeyCode.RightShift );
                    bool isAlt = Input.GetKey( KeyCode.LeftAlt ) || Input.GetKey( KeyCode.RightAlt );

                    if(this.Enable)
                    {
                        float windowWidth = Screen.width / 4 - ControlBase.FixedMargin * 2;

                        Rect pluginPos = new Rect( Screen.width - windowWidth, Screen.height / 15 + ControlBase.FixedMargin, Screen.width / 5 - Screen.width / 65, Screen.height - Screen.height / 5 );

                        if(selectedMode == ConstantValues.EditMode.Movie){
                            this.timelineWindow.Width = 1000;
                            this.timelineWindow.Height = 800;
                            this.timelineWindow.rectGui.width = 1000;
                            this.timelineWindow.rectGui.height = 800;
                            this.timelineWindow.OnGUI();
                        }

                        GlobalCurveWindow.Update();
                        GlobalComponentPicker.Update();
                        GlobalPropertyPicker.Update();
                        GlobalComboBox.Update();
                    }
                    else
                    {
                        // 補助キーを押下していない場合
                        bool isEnableControl = ( isCtrl == false && isShift == false && isAlt == false );
                        GameMain.Instance.MainCamera.SetControl( isEnableControl );
                        UICamera.InputEnable = isEnableControl;
                    }
                }
                // else
                // {
                //     GameMain.Instance.MainCamera.SetControl( true );
                //     UICamera.InputEnable = true;
                // }
            }
            catch( Exception e )
            {
                Debug.LogError( e.ToString() );
            }
        }

        ///-------------------------------------------------------------------------
        /// <summary>初期化</summary>
        ///-------------------------------------------------------------------------
        private void Initialize()
        {
            try
            {
                int fontSize;
                if(Screen.width < 1366)
                    fontSize = 10;
                else
                    fontSize = 11;

                this.timelineWindow = new TimelineWindow(fontSize, 401);
                timelineWindow.Left = 0;
                timelineWindow.Top = 0;
                timelineWindow.Width = 1000;
                timelineWindow.Height = 800;
                timelineWindow.rectGui.x = 200;
                timelineWindow.rectGui.y = 200;
            }
            catch( Exception e )
            {
                Debug.LogError( e.ToString() );
            }
        }

        private void SetUIEnabled(bool enable)
        {
            UTY.GetChildObject(this.windowMgr.gameObject, "WindowVisibleBtnsParent", false).SetActive(enable);
            Base.SetActive(enable);
            Gear.SetActive(enable);
        }

        ///-------------------------------------------------------------------------
        /// <summary>プラグイン名取得</summary>
        /// <returns>プラグイン名</returns>
        ///-------------------------------------------------------------------------
        public static String GetPluginName()
        {
            String name = String.Empty;
            try
            {
                // 属性クラスからプラグイン名取得
                PluginNameAttribute att = Attribute.GetCustomAttribute( typeof( Maidirector ), typeof( PluginNameAttribute ) ) as PluginNameAttribute;
                if( att != null )
                {
                    name = att.Name;
                }
            }
            catch( Exception e )
            {
                Debug.LogError( e.ToString() );
            }

            return name;
        }

        ///-------------------------------------------------------------------------
        /// <summary>プラグインバージョン取得</summary>
        /// <returns>プラグインバージョン</returns>
        ///-------------------------------------------------------------------------
        public static String GetPluginVersion()
        {
            String version = String.Empty;
            try
            {
                // 属性クラスからバージョン番号取得
                PluginVersionAttribute att = Attribute.GetCustomAttribute( typeof( Maidirector ), typeof( PluginVersionAttribute ) ) as PluginVersionAttribute;
                if( att != null )
                {
                    version = att.Version;
                }
            }
            catch( Exception e )
            {
                Debug.LogError( e.ToString() );
            }

            return version;
        }
        #endregion

        #region Properties
        ///-------------------------------------------------------------------------
        /// <summary>プラグイン機能有効</summary>
        ///-------------------------------------------------------------------------
        private bool Enable
        {
            get => true || this.IsPhotoMode;
        }

        ///-------------------------------------------------------------------------
        /// <summary>エディットモード</summary>
        ///-------------------------------------------------------------------------
        private bool IsEditMode
        {
            get => this.sceneNo == ConstantValues.Scene.SceneEdit &&
                CharacterMgr.EditModeLookHaveItem;
        }

        ///-------------------------------------------------------------------------
        /// <summary>夜伽モード</summary>
        ///-------------------------------------------------------------------------
        private bool IsYotogiMode
        {
            get
            {
                if( this.yotogiManager == null )
                {
                    this.yotogiManager = FindObjectOfType<YotogiPlayManager>();
                }

                if( this.yotogiManager != null )
                {
                    return this.sceneNo == ConstantValues.Scene.SceneYotogi && yotogiManager.fade_status == WfScreenChildren.FadeStatus.Wait;
                }
                else
                {
                    return false;
                }
            }
        }

        ///-------------------------------------------------------------------------
        /// <summary>photoモード</summary>
        ///-------------------------------------------------------------------------
        private bool IsPhotoMode
        {
            get => this.sceneNo == ConstantValues.Scene.ScenePhoto;
        }

        #endregion

        #region .ini ファイルの読み込み関係
        /// <summary>.ini ファイルからプラグイン設定を読み込む</summary>
        private void ReadPluginPreferences()
        {
            configLanguage = GetPreferences("Config", "Language", "English");
            configWindowKey = GetPreferences("Config", "WindowKey", "m");
            configPlayKey = GetPreferences("Config", "PlayKey", "space");
            configStopKey = GetPreferences("Config", "StopKey", "s");
            configHideUIKey = GetPreferences("Config", "HideUIKey", "delete");

            configWindowKey = configWindowKey.ToLower();
            configPlayKey = configPlayKey.ToLower();
            configStopKey = configStopKey.ToLower();
            configHideUIKey = configHideUIKey.ToLower();
        }

        /// <summary>設定ファイルから string データを読む</summary>
        private string GetPreferences( string section, string key, string defaultValue )
        {
            if (!Preferences.HasSection(section) || !Preferences[section].HasKey(key) || string.IsNullOrEmpty(Preferences[section][key].Value))
            {
                Preferences[section][key].Value = defaultValue;
                SaveConfig();
            }
            return Preferences[section][key].Value;
        }

        /// <summary>設定ファイルから bool データを読む</summary>
        private bool GetPreferences( string section, string key, bool defaultValue )
        {
            if( !Preferences.HasSection( section ) || !Preferences[section].HasKey( key ) || string.IsNullOrEmpty( Preferences[section][key].Value ))
            {
                Preferences[section][key].Value = defaultValue.ToString();
                SaveConfig();
            }
            bool b = defaultValue;
            bool.TryParse( Preferences[section][key].Value, out b );
            return b;
        }

        /// <summary>設定ファイルから int データを読む</summary>
        private int GetPreferences( string section, string key, int defaultValue )
        {
            if( !Preferences.HasSection( section ) || !Preferences[section].HasKey( key ) || string.IsNullOrEmpty( Preferences[section][key].Value ))
            {
                Preferences[section][key].Value = defaultValue.ToString();
                SaveConfig();
            }
            int i = defaultValue;
            int.TryParse( Preferences[section][key].Value, out i );
            return i;
        }

        /// <summary>設定ファイルから float データを読む</summary>
        private float GetPreferences( string section, string key, float defaultValue )
        {
            if( !Preferences.HasSection( section ) || !Preferences[section].HasKey( key ) || string.IsNullOrEmpty( Preferences[section][key].Value ))
            {
                Preferences[section][key].Value = defaultValue.ToString();
                SaveConfig();
            }
            float f = defaultValue;
            float.TryParse( Preferences[section][key].Value, out f );
            return f;
        }
        #endregion

        public static SystemShortcut SysShortcut { get => GameMain.Instance.SysShortcut; }
        public static GameObject Base { get => SysShortcut.gameObject.transform.Find("Base").gameObject; }
        public static GameObject Grid { get => Base.gameObject.transform.Find("Grid").gameObject; }
        public static GameObject Gear { get => SysShortcut.gameObject.transform.Find("Gear").gameObject; }
        public static UIGrid GridUI { get => Grid.GetComponent<UIGrid>(); }

        #region Fields
        /// <summary>画面番号</summary>
        private ConstantValues.Scene sceneNo = ConstantValues.Scene.None;

        private bool initialized = false;
        private bool enableUI = true;

        string configLanguage = string.Empty;
        string configWindowKey = string.Empty;
        string configPlayKey = string.Empty;
        string configStopKey = string.Empty;
        string configHideUIKey = string.Empty;

        /// <summary>夜伽クラス</summary>
        YotogiPlayManager yotogiManager = null;
        private PhotoWindowManager windowMgr = null;

        private TimelineWindow timelineWindow = null;
        private ConstantValues.EditMode selectedMode = ConstantValues.EditMode.Disable;
        #endregion
    }
    #endregion
}
