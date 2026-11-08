using System;
using System.Collections.Generic;
using AbyssBar.Upgrades;
using UnityEngine;

namespace AbyssBar.Mio
{
    public sealed class MioWave : MonoBehaviour
    {
        private MioCharacterDefinition definition;
        private RunUpgradeRuntime owner;
        private RunAction action;
        private Vector2 start, position, direction;
        private float damage, remaining, age;
        private int targets;
        private bool generated, returning, backward, released, trailEmitted;
        private Action<Vector2,Vector2> trail;
        private float push,visualHeight=.65f,collisionWidth;
        private readonly HashSet<int> hits = new HashSet<int>();
        private SpriteRenderer sprite;
        public static void Spawn(MioCharacterDefinition data, RunUpgradeRuntime owner, Vector2 from, Vector2 direction,
            float damage, int targets, float range, RunAction action = null, bool generated = true, bool released = false,
            bool returning = false, Action<Vector2,Vector2> trail = null, float push = 0, float visualHeight = 0)
        {
            if (data == null || owner == null || !owner.Active || damage <= 0 || range <= 0) return;
            var go = new GameObject("Mio sword wave"); go.transform.SetParent(owner.transform, false);
            go.AddComponent<RunOwnedObject>();
            var shot = go.AddComponent<MioWave>(); shot.definition = data; shot.owner = owner;
            shot.start = shot.position = from; shot.direction = direction.sqrMagnitude > .0001f ? direction.normalized : Vector2.right;
            shot.damage = damage; shot.targets = Mathf.Clamp(targets,1,64); shot.remaining = Mathf.Min(80,range);
            shot.action = action; shot.generated = generated; shot.released = released; shot.returning = returning; shot.trail = trail;
            shot.push=push;shot.visualHeight=visualHeight>0?visualHeight:data.tuning.waveWidth*1.3f;shot.collisionWidth=visualHeight>0?visualHeight/1.3f:data.tuning.waveWidth;go.layer=owner.gameObject.layer;
            shot.sprite = go.AddComponent<SpriteRenderer>();
            var original = owner.GetComponent<SpriteRenderer>();
            if (original != null) { shot.sprite.sharedMaterial = original.sharedMaterial; shot.sprite.sortingLayerID = original.sortingLayerID; shot.sprite.sortingOrder = original.sortingOrder + 4; }
            shot.Draw();
        }
        private void FixedUpdate()
        {
            if (owner == null || !owner.Active) { Destroy(gameObject); return; }
            if (Time.timeScale <= 0) return;
            float distance = Mathf.Min(remaining, definition.tuning.waveSpeed * Time.fixedDeltaTime);
            Vector2 next = position + direction * distance;
            float wall = distance + 1;
            foreach (var hit in Physics2D.CircleCastAll(position,collisionWidth*.5f,direction,distance)) if (RunProjectile.IsWall(hit.collider)) wall = Mathf.Min(wall,hit.distance);
            Vector2 stopped = position + direction * Mathf.Min(distance, Mathf.Max(0,wall-.015f));
            var candidates=owner.Targets(position,distance+3);
            candidates.Sort((a,b)=>Vector2.Dot((Vector2)a.transform.position-position,direction).CompareTo(Vector2.Dot((Vector2)b.transform.position-position,direction)));
            foreach (var enemy in candidates)
            {
                if (hits.Count >= targets || enemy == null || !enemy.IsAlive || hits.Contains(enemy.RuntimeId)) continue;
                Vector2 closest = RunProjectile.Closest(position,stopped,enemy.transform.position);
                float edge = Vector2.Distance(closest,enemy.transform.position);
                foreach (var collider in enemy.GetComponentsInChildren<Collider2D>()) if (collider.enabled) edge = Mathf.Min(edge,Vector2.Distance(closest,collider.ClosestPoint(closest)));
                if (edge > collisionWidth*.5f || RunProjectile.Blocked(position,closest)) continue;
                hits.Add(enemy.RuntimeId); owner.Hit(enemy,damage,action,generated,false,start,hits.Count,melee:false);
                if(push>0&&enemy!=null&&enemy.IsAlive)enemy.Status.Push(start,push);
                if (!returning && hits.Count >= targets) { position=closest; Finish(); return; }
            }
            position=stopped; remaining=Mathf.Max(0,remaining-distance);
            if (wall<=distance || remaining<=.001f)
            {
                EmitTrail();
                if (returning&&!backward&&Vector2.Distance(position,start)>.05f)
                { backward=true; damage*=owner.Param("mio.moon_return","coefficient",.5f); direction=(start-position).normalized; remaining=Vector2.Distance(position,start); hits.Clear(); }
                else { Destroy(gameObject); return; }
            }
            Draw();
        }
        private void EmitTrail() { if(trailEmitted)return;trailEmitted=true;trail?.Invoke(start,position); }
        private void Finish(){EmitTrail();Destroy(gameObject);}
        private void Update(){if(Time.timeScale>0)age+=Time.deltaTime;Draw();}
        private void Draw()
        {
            if(definition==null||sprite==null)return;
            var clip=definition.Find(released?"rage.wave":"normal.wave");var frame=clip?.At(age);
            sprite.sprite=frame?.sprite;
            if(definition.energyMaterial!=null)sprite.sharedMaterial=definition.energyMaterial;
            transform.position=new Vector3(position.x,position.y+definition.tuning.visualAimHeight,owner!=null?owner.transform.position.z:0);
            transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);
            var parent=owner!=null?owner.transform.lossyScale:Vector3.one;
            float size=sprite.sprite!=null?visualHeight/Mathf.Max(.01f,sprite.sprite.bounds.size.y):1;
            transform.localScale=new Vector3(collisionWidth*.85f/Mathf.Max(.001f,sprite.sprite!=null?sprite.sprite.bounds.size.x*Mathf.Abs(parent.x):1),size/Mathf.Max(.001f,Mathf.Abs(parent.y)),1);
        }
    }
}
