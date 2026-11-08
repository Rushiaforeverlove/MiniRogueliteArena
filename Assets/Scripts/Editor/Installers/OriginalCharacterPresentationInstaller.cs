#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using AbyssBar.Characters;
using AbyssBar.Presentation;
using UnityEditor;
using UnityEngine;

public static class OriginalCharacterPresentationInstaller
{
    const string Art="Assets/Art/Characters/OriginalsV1/",Data="Assets/Resources/CharacterPresentation/";
    [Serializable]sealed class Spec{public int version;public Hero[] heroes;}
    [Serializable]sealed class Hero
    {
        public string key,characterId;public float hubHeight,battleHeight;public bool circularSkills;
        public Motion[] motions;public Motion seatUpper,seatSpecialUpper;public Frame seatLower;public Profile profile;
    }
    [Serializable]sealed class Motion{public string key;public float duration;public bool loop;public Frame[] frames;}
    [Serializable]sealed class Box{public float x,y,width,height;public Rect Rect=>new Rect(x,y,width,height);}
    [Serializable]sealed class Frame{public string texture;public Box rect;public Vector2 pivot;public float pixelsPerUnit;}
    [Serializable]sealed class Profile{public string name,job,role,skin,summary,personality,strength,caution,greeting;public string[] traits;public Skill e,q;}
    [Serializable]sealed class Skill{public string @base,enhanced;}
    [MenuItem("Abyss Bar/Characters/Build Rin and Chitose Presentation")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play before rebuilding presentation assets.");
        var spec=JsonUtility.FromJson<Spec>(File.ReadAllText(Art+"import-spec.json"));
        if(spec?.heroes==null||spec.version!=1)throw new InvalidDataException("Invalid character import specification.");
        Folder(Data.TrimEnd('/'));
        foreach(string path in Directory.GetFiles(Art,"*.png"))
        {
            var ti=(TextureImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));
            bool portrait=path.Contains("portrait");ti.textureType=portrait?TextureImporterType.Sprite:TextureImporterType.Default;
            if(portrait)ti.spriteImportMode=SpriteImportMode.Single;
            ti.npotScale=TextureImporterNPOTScale.None;ti.maxTextureSize=4096;ti.mipmapEnabled=false;
            ti.alphaIsTransparency=true;ti.textureCompression=TextureImporterCompression.Uncompressed;
            ti.filterMode=portrait?FilterMode.Bilinear:FilterMode.Point;ti.isReadable=false;ti.SaveAndReimport();
        }
        var characterMaterial=Material(Data+"Character.mat",.70f);
        var effectMaterial=Material(Data+"Effect.mat",.55f);
        var catalog=Asset<CharacterPresentationCatalog>(Data+"Catalog.asset");
        var definitions=spec.heroes.Select(h=>Build(h,characterMaterial,effectMaterial)).ToArray();
        catalog.characters=definitions;EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
        // Reapply the approved hub-only override when rebuilding the original catalog.
        if(File.Exists(HubContactMotionInstaller.SpecPath))HubContactMotionInstaller.Apply();
        Debug.Log("ORIGINAL CHARACTER PRESENTATION READY: "+definitions.Length+" heroes. Gameplay stats, IDs and saves retained.");
    }
    static CharacterPresentationDefinition Build(Hero hero,Material character,Material effect)
    {
        var data=Asset<CharacterPresentationDefinition>(Data+hero.key+".asset");
        data.characterId=hero.characterId;data.hubHeight=hero.hubHeight;data.battleHeight=hero.battleHeight;data.circularSkills=hero.circularSkills;
        data.characterMaterial=character;data.effectMaterial=effect;data.motions=hero.motions.Select(Convert).ToArray();
        data.seatUpper=Convert(hero.seatUpper);data.seatSpecialUpper=Convert(hero.seatSpecialUpper);data.seatLower=Convert(hero.seatLower);
        if(data.motions.Length<20||data.motions.Any(m=>m.frames.Length!=6||m.frames.Any(f=>f.texture==null)))throw new InvalidDataException("Incomplete animation set: "+hero.key);
        var role=AssetDatabase.FindAssets("t:CharacterData").Select(g=>AssetDatabase.LoadAssetAtPath<CharacterData>(AssetDatabase.GUIDToAssetPath(g))).FirstOrDefault(c=>c.CharacterId==hero.characterId);
        if(role==null||role.IdentityProfile==null)throw new InvalidDataException("Missing existing character/profile: "+hero.characterId);
        var profile=role.IdentityProfile;var p=hero.profile;
        profile.characterName=p.name;profile.roleTitle=p.job;profile.combatRole=p.role;profile.summary=p.summary+"\n\n"+p.personality;
        profile.specialties=p.traits;profile.strength=p.strength;profile.caution=p.caution;
        profile.baseEHint=p.e.@base;profile.enhancedEHint=p.e.enhanced;profile.baseQHint=p.q.@base;profile.enhancedQHint=p.q.enhanced;
        // Technical review notes are replaced by player-facing advice in shipped character text.
        if(hero.circularSkills)profile.caution="移動較慢，需提早避開預警；震爆的範圍很大，但仍要保留閃避與安全的退路。";
        if(hero.circularSkills)profile.enhancedEHint="以自身為中心震爆，半徑提升為 4、基礎傷害提升為 600。";
        EditorUtility.SetDirty(profile);
        var portrait=AssetDatabase.LoadAssetAtPath<Sprite>(Art+hero.key+"-portrait-v2.png");
        var serialized=new SerializedObject(role);serialized.FindProperty("portrait").objectReferenceValue=portrait;serialized.ApplyModifiedPropertiesWithoutUndo();
        var skin=role.Skins.FirstOrDefault(s=>s!=null&&s.SkinId==(hero.circularSkills?"guard_default":"scout_default"));
        if(skin==null)throw new InvalidDataException("Missing default skin: "+hero.characterId);
        serialized=new SerializedObject(skin);serialized.FindProperty("portrait").objectReferenceValue=portrait;
        serialized.FindProperty("tint").colorValue=Color.white;serialized.FindProperty("displayName").stringValue=p.skin;serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(data);return data;
    }
    static CharacterMotion Convert(Motion m)=>new CharacterMotion{key=m.key,duration=m.duration,loop=m.loop,frames=m.frames.Select(Convert).ToArray()};
    static CharacterFrame Convert(Frame f)
    {
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+f.texture+".png");
        var rect=f.rect.Rect;
        if(texture==null||rect.xMin<0||rect.yMin<0||rect.xMax>texture.width+.01f||rect.yMax>texture.height+.01f||rect.width<=0||rect.height<=0)
            throw new InvalidDataException("Atlas rectangle outside source texture: "+f.texture);
        return new CharacterFrame{texture=texture,rect=rect,pivot=f.pivot,pixelsPerUnit=f.pixelsPerUnit};
    }
    static Material Material(string path,float cutoff)
    {
        var shader=Shader.Find("AbyssBar/HubSprite");if(shader==null)throw new InvalidOperationException("HubSprite shader is required.");
        var value=AssetDatabase.LoadAssetAtPath<Material>(path);if(value==null){value=new Material(shader);AssetDatabase.CreateAsset(value,path);}
        value.SetFloat("_Cutoff",cutoff);EditorUtility.SetDirty(value);return value;
    }
    static T Asset<T>(string path)where T:ScriptableObject
    {var value=AssetDatabase.LoadAssetAtPath<T>(path);if(value==null){value=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(value,path);}return value;}
    static void Folder(string path){if(AssetDatabase.IsValidFolder(path))return;int slash=path.LastIndexOf('/');Folder(path.Substring(0,slash));AssetDatabase.CreateFolder(path.Substring(0,slash),path.Substring(slash+1));}
}
#endif
