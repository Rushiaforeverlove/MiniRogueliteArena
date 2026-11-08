#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using AbyssBar.Mio;
using AbyssBar.Upgrades;
using AbyssBar.Characters;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Only the new role's assets are seeded. Running again preserves Inspector edits.
public static class MioCharacterInstaller
{
    const string Art="Assets/Art/Characters/Mio/", Data="Assets/Data/Characters/Mio/";
    const string Definition="Assets/Resources/Mio/MioCharacterDefinition.asset";
    [Serializable] sealed class Catalog { public CardSeed[] cards; }
    [Serializable] sealed class CardSeed
    {
        public string id,title,ownerRoleId,effectKey,description;
        public UpgradeQuality quality; public UpgradeCategory category;
        public int maxRank,iconIndex; public float drawWeight;
        public bool requiresQ,requiresNodeFive; public string[] requiredCardIds;
        public UpgradeNumber[] numbers;
    }
    static T Asset<T>(string path,out bool fresh) where T:ScriptableObject
    {
        var value=AssetDatabase.LoadAssetAtPath<T>(path);fresh=value==null;
        if(fresh){Folder(Path.GetDirectoryName(path).Replace('\\','/'));value=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(value,path);}
        return value;
    }
    static void Set(SerializedObject so,string key,UnityEngine.Object value)=>so.FindProperty(key).objectReferenceValue=value;
    static void EditMode(){if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("請先停止 Play Mode。");}
    static void Folder(string path)
    {if(AssetDatabase.IsValidFolder(path))return;int cut=path.LastIndexOf('/');Folder(path.Substring(0,cut));AssetDatabase.CreateFolder(path.Substring(0,cut),path.Substring(cut+1));}
    static Texture2D Texture(string file,bool portrait=false)
    {
        var importer=AssetImporter.GetAtPath(Art+file) as TextureImporter;
        if(importer==null)throw new InvalidOperationException("缺少素材："+Art+file);
        importer.textureType=portrait?TextureImporterType.Sprite:TextureImporterType.Default;
        if(portrait){importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=100;}
        importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=4096;
        importer.mipmapEnabled=false;importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=true;
        importer.filterMode=portrait?FilterMode.Bilinear:FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(Art+file);
    }
    static Sprite Slice(MioCharacterDefinition owner,Texture2D tex,string name,int cols,int rows,int index,Vector2 pivot,float height=2.2f)
    {
        var old=AssetDatabase.LoadAllAssetsAtPath(Definition).OfType<Sprite>().FirstOrDefault(s=>s.name==name);
        if(old!=null)return old;
        int col=index%cols,row=index/cols;
        int x0=Mathf.RoundToInt((float)col*tex.width/cols),x1=Mathf.RoundToInt((float)(col+1)*tex.width/cols);
        int y0=Mathf.RoundToInt((float)(rows-row-1)*tex.height/rows),y1=Mathf.RoundToInt((float)(rows-row)*tex.height/rows);
        var sprite=Sprite.Create(tex,new Rect(x0,y0,x1-x0,y1-y0),pivot,(float)tex.height/rows/height,0,SpriteMeshType.FullRect);
        sprite.name=name;AssetDatabase.AddObjectToAsset(sprite,owner);return sprite;
    }
    static void Clip(MioCharacterDefinition owner,List<MioSpriteClip> clips,string file,string key,int cols,int rows,int row,int count,
        bool loop=false,float fps=8,float scale=1,float px=.5f,float topY=.98f,float[] x=null,float[] y=null,bool centered=false)
    {
        if(clips.Any(c=>c!=null&&c.key==key&&c.frames!=null&&c.frames.Length==count&&c.frames.All(f=>f?.sprite!=null)))return;
        var tex=Texture(file);var frames=new MioSpriteFrame[count];
        for(int i=0;i<count;i++)frames[i]=new MioSpriteFrame{scale=scale,sprite=Slice(owner,tex,key+"_"+i,cols,rows,row*cols+i,
            centered?new Vector2(.5f,.5f):new Vector2(x!=null?x[i]:px,1-(y!=null?y[i]:topY)))};
        clips.RemoveAll(c=>c!=null&&c.key==key);clips.Add(new MioSpriteClip{key=key,loop=loop,fps=fps,frames=frames});
    }
    static void Animations(MioCharacterDefinition owner)
    {
        if(owner.animationRevision>=4)return; // Do not downgrade an approved newer set.
        var clips=new List<MioSpriteClip>(owner.clips??Array.Empty<MioSpriteClip>());
        foreach(string form in new[]{"normal","rage"})
        {
            string[] movement={"idle","move","dodge"},attacks={"tap","charge","release"},skills={"dash","spin","hurt"};
            for(int row=0;row<3;row++)
            {
                Clip(owner,clips,form+"-movement.png",form+"."+movement[row],4,3,row,4,row<2,row==0?5:10);
                float[] x=null,y=null;
                if(row==1)
                {
                    x=form=="normal"?new[]{.45165746f,.50414365f,.48895028f,.49723757f}:new[]{.45856354f,.50276243f,.5179558f,.52900552f};
                    y=form=="normal"?new[]{.92541436f,.91712707f,.91712707f,.91160221f}:new[]{.95027624f,.95027624f,.95027624f,.94475138f};
                }
                Clip(owner,clips,form+"-attacks.png",form+"."+attacks[row],4,3,row,4,false,10,1,.5f,.98f,x,y);
                Clip(owner,clips,form+"-skills.png",form+"."+skills[row],4,3,row,4,false,10);
                string fx=row==0?"slash":row==1?"wave":"spin_fx";
                Clip(owner,clips,"fx-"+form+".png",form+"."+fx,4,3,row,4,true,12,centered:true);
            }
            int holdRow=form=="normal"?0:1;
            float holdScale=form=="normal"?(.91160221f-.38522489f)/(.95828636f-.31384370f):(.94475138f-.31881067f)/(.96279594f-.38200120f);
            float[] hx=form=="normal"?new[]{.50845547f,.51071026f,.50958286f,.51071026f}:new[]{.51634724f,.51747463f,.51747463f,.51860203f};
            Clip(owner,clips,"charge-holds.png",form+".hold",4,2,holdRow,4,true,7,holdScale,.5f,form=="normal"?.95828636f:.96279594f,hx);
            Clip(owner,clips,"aftermath.png",form+".defeat",4,4,form=="normal"?0:1,4,false,7);
            Clip(owner,clips,"aftermath.png",form+".victory",4,4,form=="normal"?2:3,4,false,6);
        }
        Clip(owner,clips,"q-transitions.png","q.enter",4,2,0,4,false,10);
        Clip(owner,clips,"q-transitions.png","q.exit",4,2,1,4,false,8);
        Clip(owner,clips,"fx-q-transition.png","fx.pulse",4,2,0,4,false,12,centered:true);
        Clip(owner,clips,"fx-q-transition.png","fx.fade",4,2,1,4,false,10,centered:true);
        Clip(owner,clips,"hub-idle.png","hub.idle",2,2,0,4,true,5,320f/335,.43779904f,.89633174f,y:new[]{.89633174f,.89633174f,.89792663f,.89792663f});
        Clip(owner,clips,"hub-sleep.png","hub.sleep",3,2,0,6,true,5,304.76625757f/335,.46777344f,.935546875f);
        Clip(owner,clips,"hub-walk.png","hub.walk",3,2,0,6,true,8,320f/335,.73f,.98f);
        Clip(owner,clips,"seat-sleep.png","seat.sleep",3,2,0,6,true,4,1,.5f,.6f);
        owner.clips=clips.ToArray();EditorUtility.SetDirty(owner);
    }
    static ActiveSkillData Skill(string file,string name,ActiveSkillKind kind,float cooldown,int charges)
    {
        var skill=Asset<ActiveSkillData>(Data+file+".asset",out bool fresh);
        if(fresh){var so=new SerializedObject(skill);so.FindProperty("displayName").stringValue=name;so.FindProperty("kind").enumValueIndex=(int)kind;
            so.FindProperty("cooldown").floatValue=cooldown;so.FindProperty("baseCharges").intValue=charges;so.ApplyModifiedPropertiesWithoutUndo();}
        return skill;
    }
    static CharacterSkinData Skin(string file,string id,string title,MioCharacterDefinition def,Sprite portrait,bool unlocked,Color tint)
    {
        var skin=Asset<CharacterSkinData>(Data+file+".asset",out bool fresh);
        if(fresh){var so=new SerializedObject(skin);so.FindProperty("skinId").stringValue=id;so.FindProperty("displayName").stringValue=title;
            Set(so,"gameplaySprite",def.Find("normal.idle").frames[0].sprite);Set(so,"seatedSprite",def.Find("seat.sleep").frames[0].sprite);
            Set(so,"portrait",portrait);so.FindProperty("tint").colorValue=tint;so.FindProperty("unlockedByDefault").boolValue=unlocked;so.ApplyModifiedPropertiesWithoutUndo();}
        return skin;
    }
    [MenuItem("Abyss Bar/Mio/1 - Prepare Character And Animations")]
    public static void Prepare()
    {
        EditMode();Folder(Data.TrimEnd('/'));Folder("Assets/Resources/Mio");
        var def=Asset<MioCharacterDefinition>(Definition,out _);Animations(def);
        Texture("mio-portrait.png",true);var portrait=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"mio-portrait.png");
        var profile=Asset<CharacterProfileData>(Data+"MioProfile.asset",out bool profileNew);
        if(profileNew)
        {
            profile.characterName="如月 澪";profile.roleTitle="破陣隊員";profile.combatRole="近身破陣／蓄力劍氣／解放爆發";
            profile.summary="只要吧檯還有空位，澪就能睡著。那把比她還顯眼的大劍，只有在同伴需要一條退路時才會出鞘。";
            profile.specialties=new[]{"點按近斬","長按蓄力","雙充能位移","紅瞳解放"};
            profile.strength="用 E 穿過包圍後旋斬，再以蓄力劍氣整理敵陣；Q 的六秒爆發能同時照顧近身與周圍敵人。";
            profile.caution="點按在放開滑鼠時出刀。蓄力未滿會近斬；E 與 Shift 可以穿過敵人，但牆壁仍會阻擋。";
            profile.baseEHint="雙充能、逐次回復。朝移動方向位移後旋斬；沒有移動輸入時朝滑鼠方向。";
            profile.enhancedEHint="保留雙充能位移與旋斬，並在經過的路徑留下延遲斬擊。";
            profile.baseQHint="解放紅瞳六秒。變身時只擊退、不造成傷害；點按追加劍氣，蓄力釋放六向劍氣，E 位移與旋斬範圍提升並擊退。";
            profile.enhancedQHint="保留解放效果；期間成功命中的攻擊最多記錄六次，解放結束後依序放出殘響劍氣。";EditorUtility.SetDirty(profile);
        }
        var style=Asset<MioPrimaryAttackStyle>(Data+"GreatswordStyle.asset",out _);style.definition=def;EditorUtility.SetDirty(style);
        var projectile=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Mio/SwordWave.asset.prefab");
        if(projectile==null)
        {
            Folder("Assets/Prefabs/Mio");var go=new GameObject("Mio Sword Wave Settings");go.SetActive(false);
            try
            {
                var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=def.Find("normal.wave").frames[0].sprite;
                renderer.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Data/Enemies/EnemyUnlit.mat");
                var body=go.AddComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Kinematic;body.gravityScale=0;
                go.AddComponent<CircleCollider2D>().isTrigger=true;var component=go.AddComponent<Projectile>();var so=new SerializedObject(component);
                so.FindProperty("speed").floatValue=def.tuning.waveSpeed;so.FindProperty("lifetime").floatValue=3;so.ApplyModifiedPropertiesWithoutUndo();
                projectile=PrefabUtility.SaveAsPrefabAsset(go,"Assets/Prefabs/Mio/SwordWave.asset.prefab");
            }finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        var weapon=Asset<WeaponData>(Data+"Greatsword.asset",out bool weaponNew);
        if(weaponNew){var so=new SerializedObject(weapon);so.FindProperty("displayName").stringValue="眠月・巨劍";
            Set(so,"projectilePrefab",projectile);Set(so,"primaryAttackStyle",style);so.FindProperty("attackInterval").floatValue=.7f;
            so.FindProperty("attackRange").floatValue=def.tuning.slashRadius;so.ApplyModifiedPropertiesWithoutUndo();}
        var e=Skill("E","借路一圈",ActiveSkillKind.MioDash,7,2);var enhancedE=Skill("EEnhanced","月痕迴旋",ActiveSkillKind.MioDash,7,2);
        var q=Skill("Q","別吵醒我",ActiveSkillKind.MioRelease,24,1);var enhancedQ=Skill("QEnhanced","醒月・殘響",ActiveSkillKind.MioRelease,24,1);
        var basic=Skin("BaseSkin","mio_base","半醒的月色",def,portrait,true,Color.white);
        // This is explicitly a tint reward; a distinct costume can replace its artwork later without changing its ID.
        var mastery=Skin("MasterySkin","mio_mastery","閉店後的月色・紀念染色",def,portrait,false,new Color(.84f,.9f,1));
        var character=Asset<CharacterData>(Data+"Mio.asset",out bool charNew);
        if(charNew)
        {
            var so=new SerializedObject(character);so.FindProperty("characterId").stringValue=MioCharacterDefinition.RoleId;
            so.FindProperty("displayName").stringValue="如月 澪";so.FindProperty("unlockedByDefault").boolValue=false;so.FindProperty("unlockCost").intValue=500;
            Set(so,"identityProfile",profile);Set(so,"portrait",portrait);Set(so,"gameplaySprite",def.Find("normal.idle").frames[0].sprite);
            var skins=so.FindProperty("skins");skins.arraySize=2;skins.GetArrayElementAtIndex(0).objectReferenceValue=basic;skins.GetArrayElementAtIndex(1).objectReferenceValue=mastery;
            so.FindProperty("baseMaxHealth").intValue=8;so.FindProperty("baseMoveSpeed").floatValue=4.2f;Set(so,"startingWeapon",weapon);
            Set(so,"startingESkill",e);Set(so,"ultimateQSkill",q);Set(so,"enhancedESkill",enhancedE);Set(so,"enhancedQSkill",enhancedQ);
            Set(so,"masteryRewardSkin",mastery);
            var steps=so.FindProperty("statTrainingSteps");steps.arraySize=3;
            int[] kinds={0,4,5};float[] amounts={2,.1f,.15f};int[] costs={60,90,120};
            for(int i=0;i<3;i++){var step=steps.GetArrayElementAtIndex(i);step.FindPropertyRelative("kind").enumValueIndex=kinds[i];step.FindPropertyRelative("amount").floatValue=amounts[i];step.FindPropertyRelative("cost").intValue=costs[i];}
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        def.character=character;EditorUtility.SetDirty(def);Cards(def);AssetDatabase.SaveAssets();ValidateData();
        Selection.activeObject=character;Debug.Log("澪資料已建立。既有角色、存檔與抽選權重保留；接著開啟 BarHub，執行 Mio / 2。");
    }
    static void Cards(MioCharacterDefinition def)
    {
        var text=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Resources/Mio/CardDefinitions.json");
        if(text==null)throw new InvalidOperationException("Missing Mio CardDefinitions.json");
        var catalog=JsonUtility.FromJson<Catalog>(text.text);
        if(catalog?.cards==null||catalog.cards.Length!=25||catalog.cards.Select(c=>c.id).Distinct().Count()!=25)throw new InvalidOperationException("Expected 25 unique Mio cards.");
        var tex=Texture("upgrade-icons.png");var cards=new List<UpgradeCardData>();
        foreach(var row in catalog.cards)
        {
            var icon=Slice(def,tex,"icon_"+row.iconIndex,5,5,row.iconIndex,new Vector2(.5f,.5f),1);
            var card=Asset<UpgradeCardData>(Data+"Upgrades/"+row.id+".asset",out bool fresh);
            if(fresh)
            {card.id=row.id;card.title=row.title;card.description=row.description;card.ownerRoleId=row.ownerRoleId;card.effectKey=row.effectKey;
                card.quality=row.quality;card.category=row.category;card.maxRank=row.maxRank;card.drawWeight=row.drawWeight;
                card.requiresQ=row.requiresQ;card.requiresNodeFive=row.requiresNodeFive;card.requiredCardIds=row.requiredCardIds;
                card.numbers=row.numbers;card.icon=icon;card.artBrief="眠月巨劍／月色／拖劍與解放；圖集格 "+row.iconIndex;EditorUtility.SetDirty(card);}
            cards.Add(card);
        }
        def.cardIcon=cards[0].icon;EditorUtility.SetDirty(def);
        var db=AssetDatabase.LoadAssetAtPath<UpgradeDatabase>("Assets/Resources/RunUpgradeDatabase.asset");
        if(db==null)throw new InvalidOperationException("請先安裝現有四選一升級資料庫。");
        bool change=cards.Any(c=>!(db.cards??Array.Empty<UpgradeCardData>()).Any(old=>old!=null&&old.id==c.id))
            ||!(db.roster??Array.Empty<UpgradeRosterEntry>()).Any(r=>r?.character!=null&&r.character.CharacterId==MioCharacterDefinition.RoleId);
        if(change)BackupAsset(db);
        var all=new List<UpgradeCardData>(db.cards??Array.Empty<UpgradeCardData>());
        foreach(var card in cards)if(!all.Any(c=>c!=null&&c.id==card.id))all.Add(card);
        db.cards=all.ToArray();var roster=new List<UpgradeRosterEntry>(db.roster??Array.Empty<UpgradeRosterEntry>());
        if(!roster.Any(r=>r?.character!=null&&r.character.CharacterId==MioCharacterDefinition.RoleId))roster.Add(new UpgradeRosterEntry{character=def.character,unlockedByDefault=false});
        db.roster=roster.ToArray();EditorUtility.SetDirty(db);
    }
    [MenuItem("Abyss Bar/Mio/2 - Install In BarHub")]
    public static void Install()
    {
        EditMode();var scene=SceneManager.GetActiveScene();if(scene.name!="BarHub")throw new InvalidOperationException("請先開啟 BarHub 場景。");
        var hub=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<HubCharacterSelection>(true)).Single();
        Prepare();var def=MioCharacterDefinition.Load();
        var existing=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<HubCharacterSeat>(true)).Where(s=>def.Matches(s.Character)).ToArray();
        if(existing.Length>1)throw new InvalidOperationException("重複的澪座位，請保留一個。");
        Folder("Assets/Scenes/Backups");string backup=AssetDatabase.GenerateUniqueAssetPath("Assets/Scenes/Backups/BarHub_BeforeMio.unity");
        if(!EditorSceneManager.SaveScene(scene,backup,true))throw new InvalidOperationException("場景備份失敗，未安裝座位。");
        Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Install Mio seat");
        var so=new SerializedObject(hub);HubCharacterSeat seat;
        if(existing.Length==1)seat=existing[0];
        else
        {
            var go=new GameObject("SeatedMio");SceneManager.MoveGameObjectToScene(go,scene);Undo.RegisterCreatedObjectUndo(go,"Create Mio seat");
            seat=Undo.AddComponent<HubCharacterSeat>(go);var renderer=go.GetComponent<SpriteRenderer>();renderer.sprite=def.Find("seat.sleep").frames[0].sprite;
            var template=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<HubCharacterSeat>(true)).FirstOrDefault(s=>s!=seat);
            if(template!=null){var source=template.GetComponent<SpriteRenderer>();renderer.sharedMaterial=source.sharedMaterial;renderer.sortingLayerID=source.sortingLayerID;renderer.sortingOrder=source.sortingOrder;}
            else{renderer.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Data/Enemies/EnemyUnlit.mat");renderer.sortingOrder=10;}
            renderer.spriteSortPoint=SpriteSortPoint.Pivot;
            var transforms=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
            var stool=transforms.FirstOrDefault(t=>t.name=="Stool_LockedA");var counter=transforms.FirstOrDefault(t=>t.name=="BarCounter")?.GetComponent<SpriteRenderer>();
            float x=stool!=null?stool.position.x:1.5f;
            float y=counter!=null?counter.bounds.max.y-.35f*2.2f:template!=null?template.transform.position.y:-.4f;
            go.transform.position=new Vector3(x,y,template!=null?template.transform.position.z:0);
            var box=go.GetComponent<BoxCollider2D>();box.isTrigger=true;box.size=new Vector2(1,1.9f);box.offset=new Vector2(0,-.1f);
            var seatSO=new SerializedObject(seat);Set(seatSO,"character",def.character);Set(seatSO,"selectionController",hub);
            seatSO.FindProperty("greeting").stringValue="……再五分鐘。要出戰的話，叫我一聲就好。";
            seatSO.FindProperty("dailyDialogue").stringValue="吧檯這個位置最安靜。你也累了？那把大劍……不要拿來當靠枕。";seatSO.ApplyModifiedProperties();
        }
        var animator=seat.GetComponent<MioSeatAnimator>()??Undo.AddComponent<MioSeatAnimator>(seat.gameObject);
        var animSO=new SerializedObject(animator);Set(animSO,"definition",def);animSO.ApplyModifiedProperties();
        var seats=so.FindProperty("seats");bool bound=false;for(int i=0;i<seats.arraySize;i++)if(seats.GetArrayElementAtIndex(i).objectReferenceValue==seat)bound=true;
        if(!bound){seats.arraySize++;seats.GetArrayElementAtIndex(seats.arraySize-1).objectReferenceValue=seat;so.ApplyModifiedProperties();}
        EditorUtility.SetDirty(hub);Undo.CollapseUndoOperations(group);EditorSceneManager.MarkSceneDirty(scene);Selection.activeGameObject=seat.gameObject;
        Debug.Log("澪座位已加入。備份："+backup+"。Ctrl+S 儲存，再測試選角、走路與出戰。可移動 SeatedMio 調整趴桌接觸位置。");
    }
    [MenuItem("Abyss Bar/Mio/3 - Validate Data And Current Scene")]
    public static void Validate()
    {
        EditMode();ValidateData();var scene=SceneManager.GetActiveScene();
        if(scene.name=="BarHub")
        {
            var def=MioCharacterDefinition.Load();var seats=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<HubCharacterSeat>(true)).Where(s=>def.Matches(s.Character)).ToArray();
            if(seats.Length!=1||seats[0].GetComponent<MioSeatAnimator>()==null)throw new InvalidOperationException("BarHub 需要一個澪座位與 MioSeatAnimator，請執行步驟 2。");
            var hub=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<HubCharacterSelection>(true)).Single();var list=new SerializedObject(hub).FindProperty("seats");
            if(!Enumerable.Range(0,list.arraySize).Any(i=>list.GetArrayElementAtIndex(i).objectReferenceValue==seats[0]))throw new InvalidOperationException("座位未加入 HubCharacterSelection.Seats。");
        }
        if(scene.name=="Main")
        {
            var actor=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PlayerCharacter>(true)).Single();
            foreach(var type in new[]{typeof(PlayerController),typeof(PlayerDodge),typeof(PlayerAutoAttack),typeof(PlayerActiveSkill),typeof(PlayerHealth),typeof(RunUpgradeRuntime),typeof(SpriteRenderer)})
                if(actor.GetComponent(type)==null)throw new InvalidOperationException("戰鬥玩家缺少 "+type.Name);
        }
        Debug.Log("澪資料與目前場景引用檢查通過。碰撞、排序、桌面對齊、動畫節奏和 Windows 版仍需 Play Mode／實機驗收。");
    }
    static void ValidateData()
    {
        var def=MioCharacterDefinition.Load();if(def==null||!def.Matches(def.character))throw new InvalidOperationException("Missing Mio definition/character.");
        if(def.tuning==null||float.IsNaN(def.visualScale)||float.IsInfinity(def.visualScale)||def.visualScale<=0)throw new InvalidOperationException("Invalid Mio scale/tuning.");
        foreach(var field in typeof(MioTuning).GetFields())if(field.FieldType==typeof(float))
        {float value=(float)field.GetValue(def.tuning);if(float.IsNaN(value)||float.IsInfinity(value)||value<=0)throw new InvalidOperationException("Invalid Mio tuning: "+field.Name);}
        var c=def.character;if(!c.HasConfiguredStatTraining||!c.HasConfiguredEnhancedESkill||!c.HasConfiguredEnhancedQSkill||c.StartingESkill==null||!c.StartingESkill.IsConfigured||c.StartingESkill.BaseCharges!=2
            ||c.UltimateQSkill==null||!c.UltimateQSkill.IsConfigured||c.MasteryRewardSkin==null)throw new InvalidOperationException("Mio skill/training bindings invalid.");
        if(def.clips==null||def.clips.Length<38||def.clips.Any(clip=>clip==null)||def.clips.Select(clip=>clip.key).Distinct().Count()!=def.clips.Length||def.clips.Any(clip=>clip.frames==null||clip.frames.Length==0||clip.frames.Any(f=>f?.sprite==null)))
            throw new InvalidOperationException("Expected at least 38 unique, populated Mio clips.");
        var db=AssetDatabase.LoadAssetAtPath<UpgradeDatabase>("Assets/Resources/RunUpgradeDatabase.asset");
        if(db==null||db.cards==null)throw new InvalidOperationException("Missing run upgrade database/cards.");
        var errors=db.Validate();var own=db.cards.Where(card=>card!=null&&card.ownerRoleId==MioCharacterDefinition.RoleId).ToArray();
        if(own.Length!=25||own.Any(card=>card.icon==null)||db.roster==null||!db.roster.Any(r=>r?.character==c))errors.Add("Expected 25 illustrated cards and a Mio roster entry.");
        foreach(var card in own)foreach(string required in card.requiredCardIds??Array.Empty<string>())if(!db.cards.Any(v=>v!=null&&v.id==required))errors.Add(card.id+": missing prerequisite "+required);
        if(errors.Count>0)throw new InvalidOperationException(string.Join("\n",errors));
    }
    static void BackupAsset(UnityEngine.Object asset)
    {
        string project=Directory.GetParent(Application.dataPath).FullName;
        string destination=Path.Combine(project,"Backups/Mio",DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff"));Directory.CreateDirectory(destination);
        string path=AssetDatabase.GetAssetPath(asset),full=Path.Combine(project,path);File.Copy(full,Path.Combine(destination,Path.GetFileName(path)));
        if(File.Exists(full+".meta"))File.Copy(full+".meta",Path.Combine(destination,Path.GetFileName(path)+".meta"));
    }
}
#endif
