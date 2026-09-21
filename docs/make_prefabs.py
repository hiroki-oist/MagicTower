#!/usr/bin/env python3
"""不足しているプレハブを .prefab YAML として直接生成する。Unity不要。
スプライトは Assets/Sprites/Tiles/ の動画抽出タイルを参照する。"""
import os,hashlib
ROOT=os.path.expanduser("~/Code/MagicTower")
def sguid(key): return hashlib.md5(f"MagicTower/tile/{key}".encode()).hexdigest()
LAYER={"Item":15,"Enemy":11,"People":7,"Wall":9,"Lock":17}
# (出力先, プレハブ名, タグ, レイヤー, スプライトkey, 幅, 高さ, トリガーか)
SPEC=[
 ("Item","Sword_1","Sword","Item",51,1,1,True),  ("Item","Sword_2","Sword","Item",52,1,1,True),
 ("Item","Sword_3","Sword","Item",53,1,1,True),  ("Item","Sword_4","Sword","Item",54,1,1,True),
 ("Item","Sword_5","Sword","Item",55,1,1,True),
 ("Item","Shield_1","Shield","Item",56,1,1,True),("Item","Shield_2","Shield","Item",57,1,1,True),
 ("Item","Shield_3","Shield","Item",58,1,1,True),("Item","Shield_4","Shield","Item",59,1,1,True),
 ("Item","Shield_5","Shield","Item",60,1,1,True),
 ("Item","Orb_Flight","Item_Orb","Item",38,1,1,True), ("Item","Orb_Wisdom","Item_Orb","Item",37,1,1,True),
 ("Item","Ball_Destruction","Item_Orb","Item",42,1,1,True),
 ("Item","Wing_Up","Item_Matock","Item",44,1,1,True),
 # 降臨の翼(ID45) は INITIAL.DAT には無く、41階の解錠で現れる。
 ("Item","Wing_Down","Item_Matock","Item",45,1,1,True),
 ("Item","Key_Magic","Item_SpecialKey","Item",48,1,1,True),
 ("Item","Coin_Lucky","Item_SpecialKey","Item",50,1,1,True),
 ("Item","Marker_ID7","road","Item",7,1,1,True),
 # 十字架(ID39) は INITIAL.DAT には無く、19階の隠れマスを壊すと現れる。
 ("Item","Cross","Item_Orb","Item",39,1,1,True),
 # ID10 は隠し壁(ID9)と見た目が同じだが「何かが隠れている」マス。全50階で4つだけ。
 # 見た目も通行可否も壁と同じなので、壁として置いて中身は階ごとのできごとで出す。
 ("Wall","Marker_ID10","Wall_Secret","Wall","Wall_Field",1,1,False),
 # F50 の星空。通行不可なので壁と同じ扱い（レイヤー Wall・トリガーでない）。
 # 見た目は動画から抽出した ID14 のタイル。
 ("Wall","Tile_Void","Wall_Field","Wall",14,1,1,False),
 ("Enemy","Enemy_ID93","Enemy_ID93","Enemy",93,1,1,True),
 ("Enemy","Enemy_ID147","Enemy_ID147","Enemy",147,1,1,True),
 ("Enemy","Enemy_ID149","Enemy_ID149","Enemy",149,1,1,True),
 ("Enemy","Enemy_ID151","Enemy_ID151","Enemy",151,1,1,True),
 ("Enemy","Enemy_ID153","Enemy_ID153","Enemy",153,1,1,True),
 ("Enemy","Enemy_ID155","Enemy_ID155","Enemy",155,1,1,True),
 ("Enemy","Enemy_ID157","Enemy_ID157","Enemy",157,1,1,True),
 ("Enemy","Boss_Dragon","Enemy_Dragon","Enemy","900_Boss_Dragon",3,3,True),
 ("Enemy","Boss_GiantOctopus","Enemy_GiantOctopus","Enemy","901_Boss_GiantOctopus",3,3,True),
 ("FieldObstacle","Shop_Altar","Shop_Altar","People","902_Shop_Altar",3,1,False),
]
TPL="""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1 &{go}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {tr}}}
  - component: {{fileID: {sr}}}
  - component: {{fileID: {col}}}
  m_Layer: {layer}
  m_Name: {name}
  m_TagString: {tag}
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &{tr}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  serializedVersion: 2
  m_LocalRotation: {{x: -0, y: -0, z: -0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: 0}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!212 &{sr}
SpriteRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  m_Enabled: 1
  m_CastShadows: 0
  m_ReceiveShadows: 0
  m_DynamicOccludee: 1
  m_StaticShadowCaster: 0
  m_MotionVectors: 1
  m_LightProbeUsage: 1
  m_ReflectionProbeUsage: 1
  m_RayTracingMode: 0
  m_RayTraceProcedural: 0
  m_RenderingLayerMask: 1
  m_RendererPriority: 0
  m_Materials:
  - {{fileID: 10754, guid: 0000000000000000f000000000000000, type: 0}}
  m_StaticBatchInfo:
    firstSubMesh: 0
    subMeshCount: 0
  m_StaticBatchRoot: {{fileID: 0}}
  m_ProbeAnchor: {{fileID: 0}}
  m_LightProbeVolumeOverride: {{fileID: 0}}
  m_ScaleInLightmap: 1
  m_ReceiveGI: 1
  m_PreserveUVs: 0
  m_IgnoreNormalsForChartDetection: 0
  m_ImportantGI: 0
  m_StitchLightmapSeams: 1
  m_SelectedEditorRenderState: 0
  m_MinimumChartSize: 4
  m_AutoUVMaxDistance: 0.5
  m_AutoUVMaxAngle: 89
  m_LightmapParameters: {{fileID: 0}}
  m_SortingLayerID: 0
  m_SortingLayer: 0
  m_SortingOrder: {order}
  m_Sprite: {{fileID: 21300000, guid: {sprite}, type: 3}}
  m_Color: {{r: 1, g: 1, b: 1, a: 1}}
  m_FlipX: 0
  m_FlipY: 0
  m_DrawMode: 0
  m_Size: {{x: {w}, y: {h}}}
  m_AdaptiveModeThreshold: 0.5
  m_SpriteTileMode: 0
  m_WasSpriteAssigned: 1
  m_MaskInteraction: 0
  m_SpriteSortPoint: 0
--- !u!61 &{col}
BoxCollider2D:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  m_Enabled: 1
  m_Density: 1
  m_Material: {{fileID: 0}}
  m_IncludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_ExcludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_LayerOverridePriority: 0
  m_ForceSendLayers:
    serializedVersion: 2
    m_Bits: 4294967295
  m_ForceReceiveLayers:
    serializedVersion: 2
    m_Bits: 4294967295
  m_ContactCaptureLayers:
    serializedVersion: 2
    m_Bits: 4294967295
  m_CallbackLayers:
    serializedVersion: 2
    m_Bits: 4294967295
  m_IsTrigger: {trig}
  m_UsedByEffector: 0
  m_UsedByComposite: 0
  m_Offset: {{x: 0, y: 0}}
  m_SpriteTilingProperty:
    border: {{x: 0, y: 0, z: 0, w: 0}}
    pivot: {{x: 0.5, y: 0.5}}
    oldSize: {{x: {w}, y: {h}}}
    newSize: {{x: {w}, y: {h}}}
    adaptiveTilingThreshold: 0.5
    drawMode: 0
    adaptiveTiling: 0
  m_AutoTiling: 0
  serializedVersion: 2
  m_Size: {{x: {w}, y: {h}}}
  m_EdgeRadius: 0
"""
n=0
for folder,name,tag,layer,skey,w,h,trig in SPEC:
    d=os.path.join(ROOT,"Assets/Prefab",folder); os.makedirs(d,exist_ok=True)
    p=os.path.join(d,name+".prefab")
    ids=[int(hashlib.md5(f"{name}/{k}".encode()).hexdigest()[:15],16) for k in ("go","tr","sr","col")]
    open(p,"w").write(TPL.format(go=ids[0],tr=ids[1],sr=ids[2],col=ids[3],
        layer=LAYER[layer],name=name,tag=tag,sprite=sguid(skey),
        w=w,h=h,order=2 if layer!="Wall" else 1,trig=1 if trig else 0))
    open(p+".meta","w").write(f"""fileFormatVersion: 2
guid: {hashlib.md5(f"MagicTower/prefab/{name}".encode()).hexdigest()}
PrefabImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
""")
    n+=1
print(f"{n} 個のプレハブを生成")
