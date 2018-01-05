# TransformJitter
Transformの各要素をパラメータからランダム生成した波形で振幅させるアセットです。

(暫定)[チュートリアル MMD4Mecanim BoneJitter](https://www.youtube.com/watch?v=f6L_DibGg9E)
## PositionJitter
Positionのx,y,zそれぞれを振幅させます。
## RotationJitter
Rotationのx,y,zそれぞれを振幅させます。
## ScaleJitter
Scaleのx,y,zそれぞれを振幅させます。
## EyeJitter
AnimationやIKに対応したサッカード眼球運動です。瞳が揺れます。
# BlendShapeJitter
SkinnedMeshRendererのBlendShapeをプロシージャル生成した波形で振幅させるアセットです。

[チュートリアル BlendShapeJitter](https://www.youtube.com/watch?v=b3AMRbVmCi4)

# Playable
Timeline用のPlayableAsset
## JitterFadeAsset
クリップの始点でFadeinしてLoop再生開始、終点でFadeoutして停止する。
## JitterOnceAsset
クリップの始点でPlayOnceを実行する。
