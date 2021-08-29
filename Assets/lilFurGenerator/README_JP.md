# lilFurGenerator
Version 1.0.1

# 概要
ファーのメッシュを生成するプラグインです。  
エディタ上でファーを事前生成するためGPUがボトルネックになっている場合やジオメトリシェーダーを利用できない環境などで特に有効です。  
ライティングの仕様はlilToonに合わせており明るさに差が出ないようになっています。

# 対応状況
Unityバージョン
- Unity 2018 - Unity 2021.2

シェーダーモデル
- SM4.0・ES3.0以降

レンダリングパイプライン
- Built-in Render Pipeline (BRP)
- Lightweight Render Pipeline (LWRP)
- Universal Render Pipeline (URP)

# 主な機能
- ファーのメッシュの生成
- テクスチャを用いたファーの長さや向き、硬さの調整
- 生成したメッシュ用に最適化された軽量シェーダー

# ライセンス
MIT Licenseで公開しています。同梱の`LICENSE`をご確認ください。

# 使い方
1. 下記いずれかの方法でUnityにlilFurGeneratorをインポート  
    i. unitypackageをUnityウィンドウにドラッグ＆ドロップでインポート  
    ii. UPMから```https://github.com/lilxyzw/lilFurGenerator.git?path=Assets/lilFurGenerator#master```をインポート  
2. 上部メニューバーから`Window/_lil/Fur Generator`を選択
3. 開いたウィンドウのメッシュの項目でファーを生成したいメッシュを選択
4. 調整してメッシュとマテリアルを保存 (保存していない場合は保存ボタンが赤く表示されます)
5. 元のメッシュを削除するかTagを`EditorOnly`に変更する

マテリアルの細かい調整は直接編集して行って下さい。

# 拡張設定
|名前|説明|
|-|-|
|周期|XYZ軸それぞれの風の周期です。|
|強度|XYZ軸それぞれの風の強さです。|
|ディテール|風向きのランダム度です。|
|接触強度|頂点ライトを用いた接触機能による変形の強度です。|

接触機能に用いるPoint Lightは以下のように設定することで機能します。  
Unityの制限で接触できるライトの数は最大4つまでになります。  
Prefabsフォルダに設定済みのPrefabを同梱しています。
|名前|説明|
|-|-|
|Range|任意 (コライダーの大きさになります)|
|Color|RGBA = (0.0, 0.0, 0.0, 0.055)|
|Mode|Realtime|
|Intensity|1|
|Render Mode|Not Important|

# バグレポート
トラブルが発生し不具合であることが疑われる場合は[Twitter](https://twitter.com/lil_xyzw)、[GitHub](https://github.com/lilxyzw/lilFurGenerator)、[BOOTH](https://lilxyzw.booth.pm/)のいずれかにご連絡いただければ幸いです。  
以下にテンプレートも用意させていただきましたのでバグ報告の際の参考にご活用下さい。
```
バグ: 
再現方法: 

# 可能であれば
Unityバージョン: 
シェーダー設定: 
VRChatのワールド: 
スクリーンショット: 
コンソールログ: 
```

# リファレンス
- [Unity で URP 向けのファーシェーダを書いてみた（フィン法）](https://tips.hecomi.com/entry/2021/07/24/121420)  
- [UnlitWF (whiteflare)](https://github.com/whiteflare/Unlit_WF_ShaderSuite) / [MIT LICENCE](https://github.com/whiteflare/Unlit_WF_ShaderSuite/blob/master/LICENSE)  

# 開発者向け
同様の処理を実装することで外部シェーダーでもプラグインを利用できます。  
（ファイル名ではなく）表示名に`FurGenerator`を含むシェーダーはスクリプトのシェーダーの選択肢に表示されるようになります。  
ファーの実装方法は`lilFurGenerator/Shader/lilFurGeneratorUnlit.shader`を参照してください。

# 変更履歴
## v1.0.1
- HDRカラーピッカーの隣にカラーコードを追加
- 接触機能の追加
- UPMインポートの修正
- Unity 2019のURPでうまく動作しない問題を修正
- 軽量化のためにシェーダー設定機能を追加
- いくつかの処理を頂点シェーダーに移動し最適化
- 行列計算の変更
## v1.0.0
- 公開開始