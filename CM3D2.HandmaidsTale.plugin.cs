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
using UnityInjector;
using UnityInjector.Attributes;

namespace CM3D2.HandmaidsTale.Plugin
{
    #region PluginMain
    ///=========================================================================
    /// <summary>モーション変更</summary>
    /// <remarks>
    ///	CM3D2.ChangeMotion.Plugin : モーション等を変更させる UnityInjector/Sybaris 用クラス
    ///
    ///	機能
    ///		F10/F11で設定画面表示/非表示切り替え
    ///
    ///	更新履歴
    ///		1.0.0.0 CM3D2.changeface.Plugin.0.0.2.1を元に作成
    ///		1.1.0.0	実行ボタン廃止、モーション名選択でモーション変更。
    ///				前/次のモーションショートカットキー、実行ショートカットキー廃止。
    ///				光源設定機能追加(光源強度、光源方向)。
    ///				背景設定機能追加。
    ///				表情設定機能追加(changefaceを元に作成。感謝)。
    ///		1.2.0.0	モーションファイル検索方法変更。
    ///				光源リセット機能追加。
    ///				光源関係機能追加。
    ///				メイド設定リセット機能追加。
    ///				アイテム設定機能追加。
    ///				顔の向き設定機能追加。
    ///				背景設定機能追加。
    ///				BGM設定機能追加。
    ///				複数メイド機能追加。
    ///		1.2.0.1	前回の編集対象メイドと違うメイドで再度編集画面に入り、
    ///				前回の編集対象メイドを表示しようとしても表示できない不具合修正
    ///		1.3.0.0	モーション一時停止機能追加
    ///				モーション速度設定機能追加
    ///				モーション再生位置設定機能追加
    ///				夜伽画面で環境設定画面を表示する機能追加
    ///				背景を不足分のみ表示から、全種類表示に変更
    ///				[既知の問題]
    ///					再度編集画面に入り直した際にモーションがモデルベースになる場合がある不具合
    ///					モーション一時停止後、選択対象メイドを切り替えるとモーションの一時停止が解除される不具合
    /// </remarks>
    ///=========================================================================
    [PluginFilter( "CM3D2x64" ), PluginFilter( "CM3D2x86" ), PluginFilter( "CM3D2VRx64" ), PluginName( "CM3D2.HandmaidsTale.Plugin" ), PluginVersion( "0.0.1.0" )]
    public class HandmaidsTale : PluginBase
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

                // モーション情報初期化
                ReadPluginPreferences();
                CurveTexture.Init();
                // ConstantValues.Initialize();
                // Translation.Initialize(configLanguage);
                // Util.LoadShaders();
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

            // メイドエディット画面でない、かつ夜伽画面でない場合に、メイドエディット画面または夜伽画面に遷移した場合
            // if( ( this.sceneNo != ConstantValues.Scene.SceneEdit && this.sceneNo != ConstantValues.Scene.SceneYotogi && this.sceneNo != ConstantValues.Scene.ScenePhoto ) &&
            // 	( sceneLevel == ConstantValues.Scene.SceneEdit || sceneLevel == ConstantValues.Scene.SceneYotogi || sceneLevel == ConstantValues.Scene.ScenePhoto ) )
            // {
            // 初期化
            // モーション情報初期化

            if(this.timelineWindow != null) {
                // // メイドエディット画面または夜伽画面から、メイドエディット画面、夜伽画面以外に遷移した場合
                if(( this.sceneNo == ConstantValues.Scene.ScenePhoto  ) &&
                   ( sceneLevel != ConstantValues.Scene.ScenePhoto ) )
                {
                    initialized = false;
                    // Translation.CurrentTranslation = configLanguage;

                }
                else if (( this.sceneNo != ConstantValues.Scene.ScenePhoto  ) &&
                         ( sceneLevel == ConstantValues.Scene.ScenePhoto ) )
                {
                    initialized = false;
                    // Translation.CurrentTranslation = configLanguage;
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

                    if( Input.GetKeyDown( configEffectKey ) )
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

                    // 表示中
                    if(this.Enable)
                    {
                        float windowWidth = Screen.width / 4 - ControlBase.FixedMargin * 2;

                        // Vector2 point = new Vector2( Input.mousePosition.x, Screen.height - Input.mousePosition.y );
                        Rect pluginPos = new Rect( Screen.width - windowWidth, Screen.height / 15 + ControlBase.FixedMargin, Screen.width / 5 - Screen.width / 65, Screen.height - Screen.height / 5 );

                        if(selectedMode == ConstantValues.EditMode.Movie){
                            this.timelineWindow.Width = 1000;
                            this.timelineWindow.Height = 800;
                            this.timelineWindow.rectGui.width = 1000;
                            this.timelineWindow.rectGui.height = 800;
                            this.timelineWindow.OnGUI();
                        }

                        // update external windows
                        // only one of these are ever needed at a time
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

                float windowWidth = Screen.width / 4 - ControlBase.FixedMargin * 2;

                this.timelineWindow = new TimelineWindow(fontSize, 401);
                timelineWindow.Left = 0;
                timelineWindow.Top = 0;
                timelineWindow.Width = 1000;
                timelineWindow.Height = 800;
                timelineWindow.rectGui.x = 110;
                timelineWindow.rectGui.y = 110;
            }
            catch( Exception e )
            {
                Debug.LogError( e.ToString() );
            }
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
                PluginNameAttribute att = Attribute.GetCustomAttribute( typeof( HandmaidsTale ), typeof( PluginNameAttribute ) ) as PluginNameAttribute;
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
                PluginVersionAttribute att = Attribute.GetCustomAttribute( typeof( HandmaidsTale ), typeof( PluginVersionAttribute ) ) as PluginVersionAttribute;
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
            get
            {
                return this.IsPhotoMode;
            }
        }

        ///-------------------------------------------------------------------------
        /// <summary>エディットモード</summary>
        ///-------------------------------------------------------------------------
        private bool IsEditMode
        {
            get
            {
                return this.sceneNo == ConstantValues.Scene.SceneEdit && CharacterMgr.EditModeLookHaveItem;
            }
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
            get
            {
                return this.sceneNo == ConstantValues.Scene.ScenePhoto;
            }
        }

        #endregion

        #region .ini ファイルの読み込み関係
        /// <summary>.ini ファイルからプラグイン設定を読み込む</summary>
        private void ReadPluginPreferences()
        {
            configLanguage = GetPreferences("Config", "Language", "English");
            configEffectKey = GetPreferences("Config", "EffectWindowKey", "m");
            configEnvironmentKey = GetPreferences("Config", "EnvironmentWindowKey", "x");
            configDataKey = GetPreferences("Config", "DataWindowKey", "c");

            configEffectKey = configEffectKey.ToLower();
            configEnvironmentKey = configEnvironmentKey.ToLower();
            configDataKey = configDataKey.ToLower();
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

        #region Fields
        /// <summary>画面番号</summary>
        private ConstantValues.Scene sceneNo = ConstantValues.Scene.None;

        private bool initialized = false;

        string configLanguage = string.Empty;
        string configEffectKey = string.Empty;
        string configEnvironmentKey = string.Empty;
        string configDataKey = string.Empty;

        /// <summary>夜伽クラス</summary>
        YotogiPlayManager yotogiManager = null;

        private TimelineWindow timelineWindow = null;
        private ConstantValues.EditMode selectedMode = ConstantValues.EditMode.Disable;
        #endregion
    }
    #endregion
}
