using UnityEngine;
namespace AbyssBar.Presentation
{
    // Follows the existing swept projectile. Visual offset never moves its collision origin.
    public sealed class CharacterProjectileView:MonoBehaviour
    {
        private CharacterPresentationDefinition data;private SpriteRenderer source,view;private Vector2 direction;private float age;
        public void Configure(CharacterPresentationDefinition definition,Vector2 aim)
        {
            data=definition;direction=aim;source=GetComponent<SpriteRenderer>();if(source!=null)source.enabled=false;
            var go=new GameObject("Animated projectile visual");go.layer=gameObject.layer;go.transform.SetParent(transform,false);view=go.AddComponent<SpriteRenderer>();
            if(source!=null){view.sortingLayerID=source.sortingLayerID;view.sortingOrder=source.sortingOrder;}
            Draw();
        }
        private void Draw()=>CharacterPresentationView.Effect(view,data,"fx.projectile",age,
            transform.position+Vector3.up*data.aimHeight,new Vector2(.5f,.35f),Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);
        private void LateUpdate(){if(data==null||view==null)return;age+=Time.deltaTime;Draw();if(source!=null)source.enabled=false;}
    }
}
