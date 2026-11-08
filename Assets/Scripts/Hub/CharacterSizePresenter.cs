using System;
using UnityEngine;

namespace AbyssBar.Hub
{
    [DefaultExecutionOrder(600),DisallowMultipleComponent]
    public sealed class CharacterSizePresenter:MonoBehaviour
    {
        private SpriteRenderer source,view;
        private CharacterSizeProfile profile;
        private HubArtCatalog catalog;
        private bool hub,mio,animated;
        private Collider2D ground;
        public void Configure(CharacterData character,bool inHub)
        {
            source=GetComponent<SpriteRenderer>();ground=GetComponent<Collider2D>();catalog=HubArtCatalog.Load();hub=inHub;
            profile=catalog!=null&&character!=null?Array.Find(catalog.characterSizes,p=>p.characterId==character.CharacterId):null;
            if(source==null||profile==null){enabled=false;if(view!=null)view.enabled=false;return;}
            var animation=GetComponent<AbyssBar.Presentation.CharacterAnimationPresenter>();
            var definition=AbyssBar.Presentation.CharacterPresentationDefinition.Load(character);
            if(definition!=null&&animation==null)animation=gameObject.AddComponent<AbyssBar.Presentation.CharacterAnimationPresenter>();
            animation?.Configure(definition,inHub);animated=definition!=null;
            if(animated){if(view!=null)view.enabled=false;enabled=true;return;}
            enabled=true;mio=character.CharacterId==AbyssBar.Mio.MioCharacterDefinition.RoleId;
            if(mio)
            {
                if(view!=null)view.enabled=false;
                float reference=hub?profile.mioHubReferenceHeight:profile.mioBattleReferenceHeight;
                var presenter=GetComponent<AbyssBar.Mio.MioAnimationPresenter>();
                presenter?.SetPresentationScale((hub?profile.hubHeight:profile.battleHeight)/Mathf.Max(.01f,reference),hub?(float?)(HubLayoutData.Load()?.WalkingFloorY??-3.55f):null);
                return;
            }
            if(view==null){var go=new GameObject("Calibrated character visual");go.transform.SetParent(transform,false);view=go.AddComponent<SpriteRenderer>();view.spriteSortPoint=SpriteSortPoint.Pivot;}
            Draw();
        }
        private void LateUpdate(){if(!mio&&!animated)Draw();}
        private void Draw()
        {
            if(source==null||view==null||source.sprite==null||profile==null)return;
            var measure=Array.Find(catalog.bodyMeasures,m=>m.sprite==source.sprite);
            Bounds bounds=source.sprite.bounds;
            Rect body=measure!=null?measure.localVisibleBounds:new Rect(bounds.min.x,bounds.min.y,bounds.size.x,bounds.size.y);
            float scale=(hub?profile.hubHeight:profile.battleHeight)/Mathf.Max(.01f,body.height);
            var parent=transform.lossyScale;view.transform.localScale=new Vector3(scale/Mathf.Max(.001f,Mathf.Abs(parent.x)),scale/Mathf.Max(.001f,Mathf.Abs(parent.y)),1);
            float floor=hub?HubLayoutData.Load()?.WalkingFloorY??-3.55f:ground!=null&&ground.enabled?ground.bounds.min.y+.025f:transform.position.y;
            view.transform.position=new Vector3(transform.position.x,floor-body.yMin*scale,transform.position.z);
            view.sprite=source.sprite;view.sharedMaterial=source.sharedMaterial;view.color=source.color;view.flipX=source.flipX;view.flipY=source.flipY;
            view.sortingLayerID=source.sortingLayerID;view.sortingOrder=source.sortingOrder;view.enabled=true;source.enabled=false;
        }
        private void OnDisable(){if(view!=null)view.enabled=false;}
    }
}
