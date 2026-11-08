#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using AbyssBar.Mio;
using AbyssBar.Characters;
using UnityEditor;
using UnityEngine;

public static class MioV4AnimationInstaller
{
    const string Definition="Assets/Resources/Mio/MioCharacterDefinition.asset",Art="Assets/Art/Characters/Mio/V4/";
    [MenuItem("Abyss Bar/Mio/4 - Apply Approved Combat V4")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("請先停止 Play Mode。");
        var data=AssetDatabase.LoadAssetAtPath<MioCharacterDefinition>(Definition);
        if(data==null||data.character==null)throw new InvalidOperationException("需要先安裝既有澪角色資料，不能用此更新包建立全新角色。");
        var textures=new Dictionary<string,Texture2D>();
        foreach(string file in MioV4AtlasCatalog.Entries.Select(e=>e.file).Distinct())
        {
            var importer=AssetImporter.GetAtPath(Art+file) as TextureImporter;if(importer==null)throw new InvalidOperationException("缺少 v4 圖集："+file);
            importer.textureType=TextureImporterType.Default;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=4096;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();textures[file]=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+file);
        }
        string project=Directory.GetParent(Application.dataPath).FullName,backup=Path.Combine(project,"Backups/MioV4",DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff"));Directory.CreateDirectory(backup);
        var assets=new List<UnityEngine.Object>{data,data.character,data.character.StartingWeapon};
        string profilePath="Assets/Data/Characters/Mio/MioProfile.asset";var profile=AssetDatabase.LoadAssetAtPath<CharacterProfileData>(profilePath);if(profile!=null)assets.Add(profile);
        var characterSO=new SerializedObject(data.character);var skins=characterSO.FindProperty("skins");for(int i=0;i<skins.arraySize;i++)if(skins.GetArrayElementAtIndex(i).objectReferenceValue!=null)assets.Add(skins.GetArrayElementAtIndex(i).objectReferenceValue);
        foreach(var asset in assets.Where(a=>a!=null)){string p=AssetDatabase.GetAssetPath(asset),name=p.Replace('/','_');File.Copy(Path.Combine(project,p),Path.Combine(backup,name),true);if(File.Exists(Path.Combine(project,p)+".meta"))File.Copy(Path.Combine(project,p)+".meta",Path.Combine(backup,name)+".meta",true);}
        var oldIdle=data.Find("normal.idle")?.frames[0].sprite;var all=new List<MioSpriteClip>(data.clips??Array.Empty<MioSpriteClip>());
        var existing=AssetDatabase.LoadAllAssetsAtPath(Definition).OfType<Sprite>().ToDictionary(s=>s.name,s=>s);
        foreach(var entry in MioV4AtlasCatalog.Entries)
        {
            var tex=textures[entry.file];var frames=new MioSpriteFrame[6];
            for(int i=0;i<6;i++)
            {
                string name="mio_v4_"+entry.key+"_"+i;Sprite sprite;
                if(!existing.TryGetValue(name,out sprite))
                {
                    int x0=Mathf.RoundToInt((float)i*tex.width/entry.columns),x1=Mathf.RoundToInt((float)(i+1)*tex.width/entry.columns);
                    int y0=Mathf.RoundToInt((float)(entry.rows-entry.row-1)*tex.height/entry.rows),y1=Mathf.RoundToInt((float)(entry.rows-entry.row)*tex.height/entry.rows);
                    float w=x1-x0,h=y1-y0;var crop=entry.crop;
                    var rect=new Rect(x0+crop.x*w,y0+(1-crop.y-crop.height)*h,crop.width*w,crop.height*h);
                    var pivot=entry.fx?new Vector2(.5f,.5f):new Vector2((entry.pivotX-crop.x)/crop.width,(1-entry.grounds[i]-(1-crop.y-crop.height))/crop.height);
                    sprite=Sprite.Create(tex,rect,pivot,entry.fx?100:h/(entry.size/100),0,SpriteMeshType.FullRect);sprite.name=name;AssetDatabase.AddObjectToAsset(sprite,data);existing.Add(name,sprite);
                }
                frames[i]=new MioSpriteFrame{sprite=sprite,scale=1};
            }
            all.RemoveAll(c=>c!=null&&c.key==entry.key);all.Add(new MioSpriteClip{key=entry.key,fps=entry.fps,loop=entry.loop,frames=frames,frameEndFractions=entry.ends});
        }
        data.clips=all.ToArray();
        const string materialPath="Assets/Data/Characters/Mio/MioSeatBreathing.mat";
        data.seatedMaterial=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(data.seatedMaterial==null){data.seatedMaterial=new Material(Shader.Find("Sprites/Default"));AssetDatabase.CreateAsset(data.seatedMaterial,materialPath);}
        const string energyPath="Assets/Data/Characters/Mio/MioEnergyV4.mat";
        data.energyMaterial=AssetDatabase.LoadAssetAtPath<Material>(energyPath);
        if(data.energyMaterial==null){var shader=AssetDatabase.LoadAssetAtPath<Shader>(Art+"MioEnergy.shader");if(shader==null)throw new InvalidOperationException("Missing Mio energy shader");data.energyMaterial=new Material(shader);AssetDatabase.CreateAsset(data.energyMaterial,energyPath);}
        if(data.animationRevision<4)
        {
            data.tuning.slashRadius=3.2f;data.tuning.eRadius=5;data.tuning.ePush=2.75f;data.tuning.waveWidth=1.5f;data.tuning.waveTargets=4;data.tuning.rageSpinMultiplier=1.2f;
            if(data.character.StartingWeapon!=null){var weapon=new SerializedObject(data.character.StartingWeapon);weapon.FindProperty("attackInterval").floatValue=data.tuning.tapSeconds;weapon.FindProperty("attackRange").floatValue=data.tuning.slashRadius;weapon.ApplyModifiedPropertiesWithoutUndo();}
        }
        var idle=data.Find("normal.idle").frames[0].sprite;
        if(characterSO.FindProperty("gameplaySprite").objectReferenceValue==oldIdle){characterSO.FindProperty("gameplaySprite").objectReferenceValue=idle;characterSO.ApplyModifiedPropertiesWithoutUndo();}
        foreach(var skin in assets.OfType<CharacterSkinData>()){var so=new SerializedObject(skin);if(so.FindProperty("gameplaySprite").objectReferenceValue==oldIdle){so.FindProperty("gameplaySprite").objectReferenceValue=idle;so.ApplyModifiedPropertiesWithoutUndo();}}
        if(profile!=null){profile.baseEHint="雙充能、逐次回復。朝滑鼠方向位移後大範圍旋斬並大幅擊退，位移與旋斬全程無敵，牆壁仍會阻擋。";profile.enhancedEHint=profile.baseEHint+"並在經過路徑留下延遲斬擊。";EditorUtility.SetDirty(profile);}
        data.animationRevision=4;EditorUtility.SetDirty(data);AssetDatabase.SaveAssets();Validate();Selection.activeObject=data;
        Debug.Log("已套用澪 v4。實戰與選角普攻／E／Q 展示共用動畫、範圍與劍氣設定。資料備份："+backup+"。請測試 BarHub 與 Main。");
    }
    [MenuItem("Abyss Bar/Mio/5 - Validate Combat V4")]
    public static void Validate()
    {
        var data=AssetDatabase.LoadAssetAtPath<MioCharacterDefinition>(Definition);if(data==null||data.animationRevision<4)throw new InvalidOperationException("請先執行 Apply Approved Combat V4。");
        foreach(var e in MioV4AtlasCatalog.Entries){var c=data.Find(e.key);if(c==null||c.frames.Length!=6||c.frames.Any(f=>f?.sprite==null||!f.sprite.name.StartsWith("mio_v4_")))throw new InvalidOperationException("v4 動畫不完整："+e.key);}
        if(data.tuning.eRadius<5||data.tuning.waveTargets<4||data.tuning.waveWidth<1.5f)throw new InvalidOperationException("E／劍氣設定未完成遷移。");
        Debug.Log("澪 v4 資料檢查通過，實際動作與碰撞仍需 Play Mode 驗收。");
    }
}
#endif
