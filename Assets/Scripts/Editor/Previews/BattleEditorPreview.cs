using AbyssBar.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

internal sealed class BattleEditorPreview
{
    internal const string RootName = "Battle player preview (temporary)";
    private readonly GameObject root;
    private readonly SpriteRenderer original;
    private readonly bool wasHidden;

    private BattleEditorPreview(GameObject root, SpriteRenderer original)
    {
        this.root = root;
        this.original = original;
        wasHidden = original.forceRenderingOff;
        original.forceRenderingOff = true;
    }

    internal static BattleEditorPreview Create(Scene scene)
    {
        var player = ScenePreviewMenu.FindInScene<PlayerCharacter>(scene);
        if (player == null || !player.gameObject.activeInHierarchy)
            return null;

        var role = new SerializedObject(player).FindProperty("characterData").objectReferenceValue as CharacterData;
        var presentation = CharacterPresentationDefinition.Load(role);
        if (presentation?.Find("combat.idle")?.At(0)?.texture == null)
            return null;

        var original = player.GetComponent<SpriteRenderer>();
        var collider = player.GetComponent<Collider2D>();
        Vector3 feet = player.transform.position;
        if (collider != null && collider.enabled)
            feet.y = collider.bounds.min.y + .025f;

        var root = new GameObject(RootName) { hideFlags = HideFlags.HideAndDontSave };
        SceneManager.MoveGameObjectToScene(root, scene);
        try
        {
            var renderer = root.AddComponent<SpriteRenderer>();
            CharacterPresentationView.Draw(renderer, presentation, "combat.idle", 0, feet,
                presentation.battleHeight, original.flipX, original.color, original.sortingOrder, original.sortingLayerID);
            return new BattleEditorPreview(root, original);
        }
        catch
        {
            Object.DestroyImmediate(root);
            throw;
        }
    }

    internal void Clear()
    {
        if (original != null)
            original.forceRenderingOff = wasHidden;
        if (root != null)
            Object.DestroyImmediate(root);
    }
}
