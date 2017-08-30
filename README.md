# CM3D2.Maidirector.Plugin
CM3D2のアニメーションツールです。メイドにモーションを実行し、カメラやメイドの顔や他のオブジェクトのパラメータを操作できます。

![スクリーンショット](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/screenshot.png)

## 導入方法
* `CM3D2.Maidirector.Plugin.dll`を`UnityInjector`フォルダに置いてください。
* `Config`フォルダの中身を`UnityInjector/Config`フォルダにコピーしてください。

## ビルド方法
`Assembly-CSharp.dll`, `UnityEngine.dll`, `ExIni.dll`, `UnityInjector.dll`を親ディレクトリにある`References`フォルダに入れて、`msbuild`を実行してください。

## 使用方法
撮影モードは推薦します。

### タイムラインビュー
（デフォルトで）`M`キーを押してタイムラインウインドウを開いて、**トラック追加**でトラックを追加できます. トラックの種類は以下です。
- **カメラ**: カメラを操縦する。
- **オブジェクト**: シーンオブジェクトのプロパティを操縦する。
- **メイドモーション**: メイドモーションを実行する. モーションの速度はクリップのリサイズで変化できます。
- **メイド顔**: メイド顔のパラメーターを操縦する。

トラックの左にあるボタンは以下の機能を持っています。
- **[E]（有効・無効）**: トラック操縦の有効・無効
- **[K]（キーフレーム）**: 現在のトラックの操縦対象の値を利用して、キーフレームを導入する。例として、カメラの位置を固定した後にキーフレームを入れて、カメラを移動して、そしてもう一度キーフレームを入れたら、元のカメラ位置と補間できます。
- **[C]（クリップ）**: クリップを導入する。
- **[-]**: トラックを削除する。
- **[+]**: オブジェクトトラックのみ。オブジェクトのプロパティを追加する。

クリップをクリックしたら選択対象になれ、カーブビューに表示されます。

リサイズするには、**リサイズ**のトグルを押して、リサイズしたいクリップをドラッグしてください。ドラッグ操作に戻るには、**ドラッグ**のトグルを押してください。

### カーブビュー
ここでは、キーフレームをドラッグすればトラックのパラメータを変化できます。ここのコントロール説明：

- **-**/**+** - ズームイン・ズームアウトする。
- **▲**/**▼** - パンアップ・パンダウンする。
- **◀**/**▶** - 前・次のカーブを選択する。
- **カーブを合わせる** - 選択中のカーブをカーブウインドウに合わせる。
- **全部合わせる** - 全部のカーブをカーブウインドウに合わせる。
- **キーフレーム導入** - 選択中のカーブにキーフレーム導入操作を開始する。導入するには、カーブウインドウに入れたい場所をクリックしてください。 
- **キーフレーム削除** - 選択中のキーフレーム（最近選択したキーフレーム）を削除する。
- **接線モード** - 選択中のキーフレームの接線を制約する。
- **キーフレーム分割** - キーフレームの接線を合わせる・分割する。
- **折り返し** - カーブの末端を超えた後の制約を選択する。

**表示・非表示**ボタンで、カーブのキーフレームを表示・非表示にするコントロールを持つパネルは表示されます。特定のカーブを調整する場合に便利な機能です。

### キーフレームビュー
**キーフレーム**ボタンを押すとMMDもどきのキーフレームビューは表示されます。ここでキーフレームに特定な数値を与えます。

### キー割り当て
キーを変更したい場合は、`UnityInjector/Config/Maidirector.ini`を編集してください。

| キー     | 操作                        |
|----------|-----------------------------|
| M        | タイムラインウインドウを開く|
| スペース | プレイ・一時停止            |
| S        | 停止                        |

## 寄与・貢献
バグとか不良点があれば、ぜひ報告してください。よろしくお願いします。