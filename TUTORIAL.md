# チュートリアル
このチュートリアルで、当プラグインの機能を説明し、動画の編集ワークフローを示すと思います。

まずは撮影モードを開始して、（ディファルトで）`M`キーを押してタイムラインウインドウを開いてください。

## カメラの動作
始めにカメラを動かせましょう。オブジェクトを操縦には、該当のトラックを追加しなければなりません。

トラックを追加するには、`トラック追加`ボタンを押してください。

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/1.png)

トラック追加のウインドウが表示されます。 デフォルトは`カメラ`なので、`OK`ボタンを押すとカメラトラックが追加されます。

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/2.png)

トラック追加の後、新しいクリップがトラックに自動的に導入されます。クリップとは、オブジェクトプロパティを補間する値を持つものです。これらはカーブとキーフレームとして表させられます。

タイムラインの下の色とりどりなラインを示す部分はカーブビューです。トラック対象のオブジェクトのプロパティはひとつずつカーブを持ちます。例えば、カメラの位置には三つのカーブを持ちます（X・Y・Zの座標）。

プロパティのパラメーターを変化させるには、キーフレームの導入が必要です。キーフレームとは、モーション、エフェクト、オーディオ、その他多くのプロパティのパラメーターを設定するためのものです。クリップ制作時、一つのキーフレームはクリップの左に置いています。一番上のキーフレームをドラッグして、パラメーターを変更しましょう。

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/tut1.gif)

これでカメラは回転しています。しかし、今は一つのキーフレームしかないので、カメラの動作はまだできていません。二つのカメラ位置を補間するには、もう一つのキーフレームをクリップの中に導入しましょう。キーフレームを導入するには、まずは編集したいトラックのキーフレームをクリックしてください。カーブを選択した後に、`キーフレーム導入`ボタンを押してください。これは「キーフレーム導入モード」を開始します。次に、カーブビューにキーフレームを入れたい場所をクリックしてください。

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/tut2.gif)

カーブを制作しました。タイムラインの上のシーカーをカーブの左にドラッグして、再生ボタンを押してください（デフォルトで`スペース`も使用できます）。

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/tut3.gif)

これで初めてのアニメーションを作りました。色んなパラメーターをいじって、自分の気に入る動作を楽しんでください。

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/tut4.gif)

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/tut5.gif)

## カメラの細かい動作
カーブの編集が難しい時には、自分でカメラを動かしてキーフレームとして補足できます。

まずは、カメラトラックの左の`E`ボタンを押してトラックを無効させましょう。カメラの動作を行いたくない時に便利な機能です。

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/tut6.gif)

次は、シーカーを補間の終了フレームにドラッグしてください。

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/tut7.gif)

その後、カメラを好きな場所に置きましょう。

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/3.png)

そして、カメラトラックの左の`K`ボタンを押すとキーフレームが導入されます。

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/tut8.gif)

カーブは現在のカメラ位置を補足しました。

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/4.png)

最後に`E`ボタンを押してトラックを有効にして、ストップボタンで再生時間をリセットして、新しいアニメーションを見ましょう。

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/tut11.gif)

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/tut10.gif)

**ヒント**: デフォルトで`削除キー`を押すとUIを消させます。

## オブジェクトの動作
カメラの同じように、シーンのオブジェクトも操縦できます。その前に、以前作ったトラックを消したい場合は、トラックの左の`-`ボタンを押してください。

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/tut12.gif)

`エントランス`の背景を呼び出して、カメラをドアーの前に置いておきましょう。

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/5.png)

次に`トラック追加`を押してください。今回は`種類`を`オブジェクト`に変更してください。

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/6.png)

オブジェクトはそれぞれの種類のカテゴリーに分けています。例えば、ドアーのオブジェクトは`背景`のカテゴリーにあります。この場合、エントランスのドアーは`MainDoorL`と`MainDoorR`です。どちらを`オブジェクト`のボックスで選択してください。

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/7.png)

オブジェクトの位置・回転・スケールを変更したい場合には`位置・回転・スケール`のコンポーネントが必要です。それはデフォルトなので、`OK`で決定してください。

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/8.png)

トラックを制作しましたが、カーブがありません。オブジェクトトラックを操縦するには、どんなプロパティを変更したいかを選ばなければなりません。それをするには、トラックの左にある`+`ボタンを押してください。回転を変更したい場合には、`回転`のプロパティを選択します。

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/tut14.gif)

これでドアーのパラメーターをカメラのように補間できます。

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/tut15.gif)

## メイドモーション
メイドモーションを実行できます。`トラック追加`を押して`メイドモーション`を選択します。

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/9.png)

メイドとモーションを選択して、`OK`で決定します。

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/tut16.gif)

モーションの開始時を変更するには、クリップを新しい場所にドラッグします。

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/tut19.gif)

クリップの長さとモーションの速さを変更するには、`リサイズ`トグルをおします。

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/10.png)

そして、クリップをドラッグしてください。

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/tut17.gif)

ドラッグ操作に戻るには、`ドラッグ`トグルを押してください。

`クリップ複製`で選択中のクリップを複製できます。

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/tut18.gif)

## その他
何かが分からない時は、ぜひ聞かせてください。詳しい説明をいります。

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/tut20.gif)
