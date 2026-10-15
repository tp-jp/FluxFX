FluxFX Runtime API Playground

RuntimeApiPlayground.unity を開いて Play してください。
ClientSimでは Tab を押している間、マウスでUIを操作できます。

Main / Emission / Shape / Motion / Render のタブで項目を切り替えます。
Motionはスクロールして各Moduleを操作できます。
Shapeの選択は、あらかじめ生成した6種類のParticle Systemを切り替えます。
標準11 ModuleとCustom Liftが組み込まれています。

通常の項目: Runtime APIを通して即時反映
[initial]: Apply initial values & RestartでGPU実行環境を再初期化して反映
(restart): RestartでEmissionの再生設定を反映
Restart: 既存Particleをクリアして再生
Stop: 新規生成を停止。既存Particleの更新は継続
Play / Pause: 再生・一時停止

MainはConstant、Color/Size/Rotation over LifetimeはLinearの構成です。
Gradient・Curve・乱数モードの切り替えや、Moduleの組み込み構成変更はこのUIの対象外です。
Texture sheetの初期値は同梱の2x2 Atlasに合わせています。
列・行数を変更すると、同じAtlasを指定した分割数で表示します。
操作はローカルのみで同期しません。

Runtime/FluxRuntimeApiSample.cs:
Main / Emission / ShapeとModuleハンドルの呼び出し例
Editor/FluxRuntimeApiSampleBuilder.cs:
UI接続とShader・Materialの事前生成処理
Generated:
形状別のコンパイル済みShader・Render Material・Kernel Material
保存済みSceneが直接参照する配布用Assetのため、.metaと一緒にバージョン管理します。
通常のビルドキャッシュ Assets/FluxFX/Generated とは扱いが異なります。
RuntimeにMaterialを生成しません。
Materials:
床MaterialとParticleの元Material（.metaと一緒にバージョン管理）
Textures:
Particle Atlas画像（.metaと一緒にバージョン管理）

既存のサンプルSceneを編集しても、自動で再生成・上書きはしません。

