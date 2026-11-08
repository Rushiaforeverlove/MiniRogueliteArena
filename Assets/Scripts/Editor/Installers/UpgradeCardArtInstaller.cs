#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using AbyssBar.Upgrades;
using UnityEditor;
using UnityEngine;

// Stable card IDs bind art independently of rarity weights, effects, saves or UI layout.
public static class UpgradeCardArtInstaller
{
    public const string SpecPath="Assets/Art/Upgrades/V1/card-art-manifest.json";
    [Serializable]sealed class Manifest{public int version;public Entry[] entries;}
    [Serializable]sealed class Entry{public string id,texturePath,cardPath;}
    [MenuItem("Abyss Bar/Run Upgrades/Art/Install Missing Card Images")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play before importing card artwork.");
        AssetDatabase.Refresh();
        var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(SpecPath));
        if(manifest?.version!=1||manifest.entries==null||manifest.entries.Select(e=>e.id).Distinct(StringComparer.Ordinal).Count()!=manifest.entries.Length)throw new InvalidDataException("Invalid/duplicate card art mapping.");
        var db=Resources.Load<UpgradeDatabase>("RunUpgradeDatabase");if(db==null)throw new InvalidDataException("Upgrade database is required.");
        // Validate every mapping before touching card references.
        foreach(var entry in manifest.entries)
        {
            var card=AssetDatabase.LoadAssetAtPath<UpgradeCardData>(entry.cardPath);
            if(card==null||card.id!=entry.id||!db.cards.Contains(card)||!File.Exists(entry.texturePath)||!entry.texturePath.StartsWith("Assets/Art/Upgrades/V1/",StringComparison.Ordinal))throw new InvalidDataException("Invalid art target: "+entry.id);
        }
        int linked=0,preserved=0;
        foreach(var entry in manifest.entries)
        {
            var importer=AssetImporter.GetAtPath(entry.texturePath) as TextureImporter;
            if(importer==null)throw new InvalidDataException("Texture importer missing: "+entry.texturePath);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=256;
            importer.mipmapEnabled=false;importer.filterMode=FilterMode.Point;importer.wrapMode=TextureWrapMode.Clamp;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.alphaIsTransparency=true;
            importer.isReadable=false;importer.spritePixelsPerUnit=100;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
            settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(entry.texturePath);
            if(sprite==null)throw new InvalidDataException("Sprite import failed: "+entry.id);
            var card=AssetDatabase.LoadAssetAtPath<UpgradeCardData>(entry.cardPath);
            // Preserve later hand edits/custom art. Re-running safely fills only missing images.
            if(card.icon==null){card.icon=sprite;EditorUtility.SetDirty(card);linked++;}
            else if(card.icon!=sprite)preserved++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log("CARD ART READY: "+linked+" linked, "+preserved+" custom images preserved; "+manifest.entries.Length+" mappings.");
        Validate();
    }
    [MenuItem("Abyss Bar/Run Upgrades/Art/Validate All Card Images")]
    public static void Validate()
    {
        var db=Resources.Load<UpgradeDatabase>("RunUpgradeDatabase");if(db==null)throw new InvalidDataException("Upgrade database missing.");
        var missing=db.cards.Where(c=>c==null||c.icon==null).ToArray();
        if(missing.Length>0)throw new InvalidDataException("Missing card images: "+string.Join(", ",missing.Select(c=>c!=null?c.id:"null")));
        Debug.Log("CARD ART VALIDATED: "+db.cards.Length+" cards have assigned sprite images.");
    }
}
#endif
