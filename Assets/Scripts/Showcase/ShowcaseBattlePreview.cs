using System.Collections.Generic;
using AbyssBar.EnemyAnimations;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AbyssBar.Showcase
{
    // Render-only additive scene, no game prefab instantiation or components
    // that register enemies, accept input, spend charges or grant rewards.
    public sealed class ShowcaseBattlePreview : MonoBehaviour
    {
        [SerializeField] private RawImage output;
        [SerializeField] private ShowcasePresentationData presentation;
        [SerializeField] private TMP_FontAsset font;
        private Scene scene;
        private GameObject root;
        private Camera renderCamera;
        private RenderTexture texture;
        private ShowcaseCombatSimulation simulation;
        private ShowcaseAvatarRig rig;
        private CharacterData character;
        private CharacterSkinData skin;
        private SpriteRenderer avatar;
        private AbyssBar.Presentation.CharacterPresentationDefinition characterPresentation;
        private ShowcasePage currentPage;
        private Sprite ringSprite, whiteSprite;
        private Texture2D ringTexture;
        private readonly List<SpriteRenderer> enemies = new List<SpriteRenderer>();
        private readonly List<SpriteRenderer> bars = new List<SpriteRenderer>();
        private readonly List<SpriteRenderer> barBackgrounds = new List<SpriteRenderer>();
        private readonly List<SpriteRenderer> shots = new List<SpriteRenderer>();
        private readonly List<SpriteRenderer> pulses = new List<SpriteRenderer>();
        private readonly List<TextMeshPro> numbers = new List<TextMeshPro>();
        private readonly List<SpriteRenderer> mioFx = new List<SpriteRenderer>();
        private SpriteRenderer characterMuzzle;
        private readonly List<SpriteRenderer> characterHits=new List<SpriteRenderer>();
        private SpriteRenderer chargeAura,chargeBack,chargeFill;
        private TextMeshPro chargeText;
        private int layer = 31;
        public bool IsConfigured => output != null && presentation != null && presentation.enemy != null
            && presentation.enemy.IsValid && presentation.material != null;
        public void Play(CharacterData selected, CharacterSkinData viewedSkin, ShowcasePage page, int previewLayer)
        {
            if (!IsConfigured) return;
            var plan = ShowcaseAttackResolver.Resolve(selected, page);
            if (plan == null) { Stop(); return; }
            layer = Mathf.Clamp(previewLayer, 0, 31); character = selected; skin = viewedSkin;
            EnsureScene(); rig = presentation.Find(character, skin); characterPresentation=AbyssBar.Presentation.CharacterPresentationDefinition.Load(character);currentPage=page;
            simulation = new ShowcaseCombatSimulation(plan); renderCamera.enabled = true; output.texture = texture;
            renderCamera.orthographicSize = plan.pattern == ShowcasePattern.Shockwave && plan.radius > 3.5f ? 3.5f : 2.2f;
            Draw();
        }
        public void Replay() { if (simulation != null) { simulation = new ShowcaseCombatSimulation(simulation.Plan); Draw(); } }
        private void Update()
        {
            if (simulation == null) return;
            simulation.Tick(Mathf.Min(.05f, Time.unscaledDeltaTime));
            if (simulation.Time >= (simulation.Plan.mio != null ? 8.5f : 5)) Replay();
            Draw();
        }
        private void EnsureScene()
        {
            if (root != null) return;
            scene = SceneManager.CreateScene("Character showcase " + System.Guid.NewGuid().ToString("N"));
            root = new GameObject("Isolated showcase objects"); root.layer = layer;
            SceneManager.MoveGameObjectToScene(root, scene); root.transform.position = new Vector3(10000, 10000, 0);
            var cameraObject = Child("Render camera"); renderCamera = cameraObject.AddComponent<Camera>();
            cameraObject.transform.localPosition = new Vector3(-.3f, .55f, -10);
            renderCamera.orthographic = true; renderCamera.clearFlags = CameraClearFlags.SolidColor;
            renderCamera.backgroundColor = new Color(.035f, .05f, .07f); renderCamera.cullingMask = 1 << layer;
            renderCamera.allowHDR = false; renderCamera.allowMSAA = false;
            texture = new RenderTexture(800, 480, 16) { name = "Character demonstration" }; texture.Create();
            renderCamera.targetTexture = texture;
            whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1);
            ringTexture = new Texture2D(128, 128, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var pixels = new Color[128 * 128];
            for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++)
            {
                float dx = x - 63.5f, dy = y - 63.5f, r = dx * dx + dy * dy;
                pixels[y * 128 + x] = r <= 63 * 63 && r >= 58 * 58 ? Color.white : Color.clear;
            }
            ringTexture.SetPixels(pixels); ringTexture.Apply();
            ringSprite = Sprite.Create(ringTexture, new Rect(0, 0, 128, 128), new Vector2(.5f, .5f), 128);
            avatar = Renderer("Character", 100);
            for (int i = 0; i < 3; i++)
            {
                enemies.Add(Renderer("Demonstration enemy", 100)); enemies[i].flipX = true;
                var back = Renderer("Enemy health background", 200); back.sprite = whiteSprite;
                barBackgrounds.Add(back);
                back.color = new Color(.17f, .19f, .22f); back.transform.localScale = new Vector3(.75f, .055f, 1);
                bars.Add(Renderer("Enemy health", 201)); bars[i].sprite = whiteSprite; bars[i].color = new Color(.85f, .3f, .35f);
            }
        }
        private GameObject Child(string name)
        { var go = new GameObject(name); go.layer = layer; go.transform.SetParent(root.transform, false); return go; }
        private SpriteRenderer Renderer(string name, int order)
        {
            var renderer = Child(name).AddComponent<SpriteRenderer>(); renderer.sharedMaterial = presentation.material;
            renderer.sortingOrder = order; renderer.spriteSortPoint = SpriteSortPoint.Pivot; return renderer;
        }
        private static EnemySpriteClip CastClip(ShowcaseAvatarRig rig, ShowcaseAttackPlan plan)
        {
            if (plan.repeatBasic) return rig.attack;
            switch (plan.pattern)
            { case ShowcasePattern.Fan: return rig.fan; case ShowcasePattern.Radial: return rig.radial; case ShowcasePattern.Shockwave: return rig.shock; default: return rig.attack; }
        }
        private void Draw()
        {
            if (simulation == null || avatar == null) return;
            float t = simulation.Time, castAge = t - simulation.LastCast;
            if (simulation.Plan.mio != null)
            {
                var clip=simulation.Plan.mio.Find(simulation.MioClipKey);
                var frame=clip?.At(simulation.MioClipAge,simulation.MioClipDuration);
                avatar.sprite=frame?.sprite;avatar.transform.localScale=Vector3.one*(frame!=null?frame.scale:1);
            }
            else if (rig != null)
            {
                var action = CastClip(rig, simulation.Plan);
                var clip = castAge < action.Duration ? action : rig.idle;
                avatar.sprite = clip.frames[clip.FrameAt(castAge < action.Duration ? castAge : t, clip == rig.idle)];
                avatar.transform.localScale = Vector3.one;
            }
            else
            {
                avatar.sprite = skin != null && skin.GameplaySprite != null ? skin.GameplaySprite : character.GameplaySprite;
                float size = avatar.sprite != null ? Mathf.Max(.01f, avatar.sprite.bounds.size.y) : 1;
                avatar.transform.localScale = Vector3.one * (1.7f / size);
            }
            avatar.enabled = avatar.sprite != null; avatar.color = skin != null ? skin.Tint : Color.white;
            avatar.transform.localPosition = simulation.hero+simulation.MioAvatarOffset;
            if(characterPresentation!=null&&simulation.Plan.mio==null)
            {
                string key=currentPage==ShowcasePage.Attack?"combat.shoot":currentPage==ShowcasePage.E?"skill.e":"skill.q";
                float motionAge=castAge-.2f;var motion=characterPresentation.Find(key);bool casting=motionAge>=0&&motionAge<motion.duration;
                AbyssBar.Presentation.CharacterPresentationView.Draw(avatar,characterPresentation,casting?key:"combat.idle",casting?motionAge:t,
                    root.transform.TransformPoint(simulation.hero-Vector2.up*.5f),characterPresentation.battleHeight,false,skin!=null?skin.Tint:Color.white,100,0);
            }
            if(simulation.Plan.mio!=null)
            {
                avatar.transform.localPosition-=Vector3.up*.5f;
                avatar.transform.localScale*=simulation.Plan.mio.visualScale;
                float size=simulation.MioClipKey.EndsWith(".spin")?simulation.Plan.mio.tuning.eRadius*(simulation.MioReleased?simulation.Plan.mio.tuning.rageSpinMultiplier:1)+1:3.5f;
                renderCamera.orthographicSize=Mathf.Lerp(renderCamera.orthographicSize,size,.2f);
                Vector2 center=simulation.hero+simulation.MioAvatarOffset+Vector2.right*(simulation.Plan.pattern==ShowcasePattern.Melee?2.5f:simulation.MioClipKey.EndsWith(".spin")?0:1);
                renderCamera.transform.localPosition=Vector3.Lerp(renderCamera.transform.localPosition,new Vector3(center.x,.6f,-10),.2f);
            }
            while(enemies.Count<simulation.targets.Count)
            {
                enemies.Add(Renderer("Demonstration enemy",100));enemies[enemies.Count-1].flipX=true;
                var back=Renderer("Enemy health background",200);back.sprite=whiteSprite;back.color=new Color(.17f,.19f,.22f);back.transform.localScale=new Vector3(.75f,.055f,1);barBackgrounds.Add(back);
                var health=Renderer("Enemy health",201);health.sprite=whiteSprite;health.color=new Color(.85f,.3f,.35f);bars.Add(health);
            }
            for (int i = 0; i < simulation.targets.Count; i++)
            {
                var target = simulation.targets[i]; var clips = presentation.enemy;
                var clip = target.health <= 0 ? clips.death : t - target.lastHit < clips.hurt.Duration ? clips.hurt : clips.idle;
                float age = target.health <= 0 ? t - target.deathAt : clip == clips.hurt ? t - target.lastHit : t + i * .1f;
                var enemy = enemies[i]; enemy.enabled = target.health > 0 || age < clip.Duration;
                enemy.sortingOrder = 100 - Mathf.RoundToInt(target.position.y * 10);
                enemy.sprite = clip.frames[clip.FrameAt(age, clip == clips.idle)]; enemy.transform.localPosition = target.position;
                enemy.color = target.health > 0 && t - target.lastHit < .12f ? new Color(1, .4f, .4f) : Color.white;
                var bar = bars[i]; bar.enabled = target.health > 0;
                bar.transform.localPosition = target.position + new Vector2(-.375f + .375f * target.health / 1000, 1.65f);
                bar.transform.localScale = new Vector3(.75f * target.health / 1000, .055f, 1);
                var background = barBackgrounds[i];
                background.enabled = target.health > 0; background.transform.localPosition = target.position + new Vector2(0, 1.65f);
            }
            while (shots.Count < simulation.shots.Count) shots.Add(Renderer("Demo projectile", 150));
            for(int i=simulation.targets.Count;i<enemies.Count;i++){enemies[i].enabled=bars[i].enabled=barBackgrounds[i].enabled=false;}
            for (int i = 0; i < shots.Count; i++)
            {
                var renderer = shots[i]; renderer.enabled = i < simulation.shots.Count; if (!renderer.enabled) continue;
                var shot = simulation.shots[i]; renderer.sprite = simulation.Plan.projectileSprite != null ? simulation.Plan.projectileSprite : ringSprite;
                renderer.sharedMaterial=simulation.Plan.mio!=null&&simulation.Plan.mio.energyMaterial!=null?simulation.Plan.mio.energyMaterial:presentation.material;
                if(simulation.Plan.mio!=null)renderer.sprite=simulation.Plan.mio.Find(shot.mioReleased?"rage.wave":"normal.wave")?.At(shot.age)?.sprite;
                renderer.color = new Color(1, .8f, .3f); renderer.transform.localPosition = shot.position + new Vector2(0, .7f);
                renderer.transform.localScale = Vector3.one * (.16f / Mathf.Max(.001f, renderer.sprite.bounds.size.y));
                if(simulation.Plan.mio!=null&&renderer.sprite!=null){renderer.color=Color.white;float width=simulation.Plan.mio.tuning.waveWidth;renderer.transform.localPosition=shot.position+Vector2.up*simulation.Plan.mio.tuning.visualAimHeight;renderer.transform.localScale=new Vector3(width*.85f/Mathf.Max(.001f,renderer.sprite.bounds.size.x),width*1.3f/Mathf.Max(.001f,renderer.sprite.bounds.size.y),1);renderer.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(shot.direction.y,shot.direction.x)*Mathf.Rad2Deg);}
            }
            if(characterPresentation!=null&&simulation.Plan.mio==null)
                for(int i=0;i<shots.Count&&i<simulation.shots.Count;i++)
                {
                    var shot=simulation.shots[i];
                    AbyssBar.Presentation.CharacterPresentationView.Effect(shots[i],characterPresentation,"fx.projectile",shot.age,
                        root.transform.TransformPoint(shot.position+Vector2.up*characterPresentation.aimHeight),new Vector2(.5f,.35f),Mathf.Atan2(shot.direction.y,shot.direction.x)*Mathf.Rad2Deg);
                }
            while (pulses.Count < simulation.pulses.Count) pulses.Add(Renderer("Demo shock or explosion", 160));
            for (int i = 0; i < pulses.Count; i++)
            {
                var renderer = pulses[i]; renderer.enabled = i < simulation.pulses.Count; if (!renderer.enabled) continue;
                var pulse = simulation.pulses[i]; renderer.sprite = ringSprite;
                renderer.transform.localPosition = pulse.position;
                renderer.transform.localScale = Vector3.one * (pulse.radius * 2 * Mathf.Lerp(.7f, 1, pulse.age / .45f));
                renderer.color = new Color(.25f, .85f, 1, 1 - pulse.age / .45f);
            }
            if(characterPresentation!=null&&simulation.Plan.mio==null)
                for(int i=0;i<pulses.Count&&i<simulation.pulses.Count;i++)
                {
                    var pulse=simulation.pulses[i];string key=characterPresentation.circularSkills?(currentPage==ShowcasePage.E?"fx.e":"fx.q"):"fx.q";
                    AbyssBar.Presentation.CharacterPresentationView.Effect(pulses[i],characterPresentation,key,pulse.age,
                        root.transform.TransformPoint(pulse.position),new Vector2(pulse.radius*2,pulse.radius*1.1f),0,.45f);
                }
            while (numbers.Count < simulation.hits.Count)
            {
                var label = Child("Demo damage number").AddComponent<TextMeshPro>();
                if (font != null) label.font = font; label.fontSize = 3.6f; label.alignment = TextAlignmentOptions.Center;
                label.rectTransform.sizeDelta = new Vector2(3, 1); label.GetComponent<MeshRenderer>().sortingOrder = 300;
                numbers.Add(label);
            }
            for (int i = 0; i < numbers.Count; i++)
            {
                var label = numbers[i]; label.gameObject.SetActive(i < simulation.hits.Count); if (!label.gameObject.activeSelf) continue;
                var hit = simulation.hits[i]; label.text = Mathf.RoundToInt(hit.amount).ToString("N0");
                label.color = new Color(1, .85f, .4f, 1 - hit.age / .75f);
                label.transform.localPosition = hit.position + new Vector2(0, 1.3f + hit.age);
            }
            DrawMioFeedback();DrawCharacterFeedback();
        }
        private void DrawCharacterFeedback()
        {
            if(characterPresentation==null||simulation.Plan.mio!=null)return;
            if(characterMuzzle==null)characterMuzzle=Renderer("Shared muzzle animation",155);
            float age=simulation.Time-simulation.LastCast-.2f;
            characterMuzzle.enabled=age>=0&&age<.2f&&simulation.Plan.pattern!=ShowcasePattern.Shockwave;
            if(characterMuzzle.enabled)AbyssBar.Presentation.CharacterPresentationView.Effect(characterMuzzle,characterPresentation,"fx.muzzle",age,
                root.transform.TransformPoint(simulation.hero+new Vector2(characterPresentation.muzzleReach,characterPresentation.aimHeight)),Vector2.one*.7f,0,.2f);
            while(characterHits.Count<simulation.hits.Count)characterHits.Add(Renderer("Shared hit animation",155));
            for(int i=0;i<characterHits.Count;i++)
            {
                var view=characterHits[i];view.enabled=i<simulation.hits.Count&&simulation.hits[i].age<.35f&&simulation.Plan.pattern!=ShowcasePattern.Shockwave;
                if(!view.enabled)continue;var hit=simulation.hits[i];
                AbyssBar.Presentation.CharacterPresentationView.Effect(view,characterPresentation,"fx.hit",hit.age,
                    root.transform.TransformPoint(hit.position+Vector2.up*characterPresentation.aimHeight),Vector2.one*.7f,0,.35f);
            }
        }
        private void DrawMioFeedback()
        {
            var data=simulation.Plan.mio;
            if(data==null){foreach(var fx in mioFx)fx.enabled=false;if(chargeAura!=null){chargeAura.enabled=chargeBack.enabled=chargeFill.enabled=false;chargeText.gameObject.SetActive(false);}return;}
            while(mioFx.Count<simulation.mioEffects.Count)mioFx.Add(Renderer("Mio demo effect",150));
            for(int i=0;i<mioFx.Count;i++){var renderer=mioFx[i];renderer.enabled=i<simulation.mioEffects.Count;if(!renderer.enabled)continue;var fx=simulation.mioEffects[i];renderer.sortingOrder=AbyssBar.Mio.MioVisuals.Behind(fx.key)?99:150;AbyssBar.Mio.MioVisuals.Draw(renderer,data,fx.key,fx.age,fx.duration,root.transform.TransformPoint(fx.position),fx.direction,fx.radius);}
            if(chargeAura==null){chargeAura=Renderer("Mio demo charge aura",99);chargeBack=Renderer("Mio demo charge background",220);chargeFill=Renderer("Mio demo charge progress",221);chargeBack.sprite=chargeFill.sprite=whiteSprite;chargeText=Child("Mio demo charge label").AddComponent<TextMeshPro>();if(font!=null)chargeText.font=font;chargeText.fontSize=2;chargeText.alignment=TextAlignmentOptions.Center;chargeText.rectTransform.sizeDelta=new Vector2(1,.3f);chargeText.GetComponent<MeshRenderer>().sortingOrder=222;}
            float p=simulation.MioChargeProgress;bool active=p>0,full=p>=.999f;chargeAura.enabled=chargeBack.enabled=chargeFill.enabled=active;chargeText.gameObject.SetActive(active);if(!active)return;
            var color=full?new Color(1,.85f,.4f):simulation.MioReleased?new Color(1,.35f,.5f):new Color(.5f,.85f,1);Vector2 at=simulation.hero+simulation.MioAvatarOffset+Vector2.up*2;
            chargeBack.transform.localPosition=at;chargeBack.transform.localScale=new Vector3(1.02f,.12f,1);chargeBack.color=new Color(.04f,.08f,.14f);chargeFill.transform.localPosition=at+Vector2.left*(1-p)*.49f;chargeFill.transform.localScale=new Vector3(.98f*p,.075f,1);chargeFill.color=color;chargeText.transform.localPosition=at+Vector2.up*.2f;chargeText.text=full?"MAX":Mathf.RoundToInt(p*100)+"%";chargeText.color=color;
            AbyssBar.Mio.MioVisuals.Draw(chargeAura,data,(simulation.MioReleased?"rage.":"normal.")+(full?"charge_full":"charge_gather"),simulation.Time,0,root.transform.TransformPoint(simulation.hero+simulation.MioAvatarOffset-Vector2.up*.5f),Vector2.right,full?1.2f:.85f+p*.25f);
        }
        public void Stop()
        {
            simulation = null; if (output != null) output.texture = null;
            if (renderCamera != null) { renderCamera.enabled = false; renderCamera.targetTexture = null; }
            if (texture != null) { texture.Release(); Destroy(texture); }
            if (root != null) { root.SetActive(false); Destroy(root); }
            if (scene.IsValid() && scene.isLoaded) SceneManager.UnloadSceneAsync(scene);
            if (ringSprite != null) Destroy(ringSprite); if (whiteSprite != null) Destroy(whiteSprite); if (ringTexture != null) Destroy(ringTexture);
            root = null; renderCamera = null; texture = null; rig = null; enemies.Clear(); bars.Clear(); barBackgrounds.Clear(); shots.Clear(); pulses.Clear(); numbers.Clear();mioFx.Clear();chargeAura=chargeBack=chargeFill=null;chargeText=null;
            characterMuzzle=null;characterHits.Clear();characterPresentation=null;
            scene = default;
        }
        private void OnDisable() { Stop(); }
    }
}
