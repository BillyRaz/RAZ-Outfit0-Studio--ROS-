using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(OutfitData))]
public class OutfitDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPropertiesExcluding(serializedObject, "m_Script");

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Preview Helper", EditorStyles.boldLabel);

        OutfitData outfitData = (OutfitData)target;
        Texture2D previewTexture = outfitData.icon;
        Texture2D selectedTexture = (Texture2D)EditorGUILayout.ObjectField(
            "Preview Texture",
            previewTexture,
            typeof(Texture2D),
            false);

        if (selectedTexture != previewTexture)
        {
            AssignPreviewTexture(outfitData, selectedTexture);
        }

        if (GUILayout.Button("Use Icon As Preview Sprite") && outfitData.icon != null)
        {
            AssignPreviewTexture(outfitData, outfitData.icon);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void AssignPreviewTexture(OutfitData outfitData, Texture2D texture)
    {
        SerializedObject outfitObject = new SerializedObject(outfitData);
        SerializedProperty iconProperty = outfitObject.FindProperty("icon");
        SerializedProperty previewSpriteProperty = outfitObject.FindProperty("previewSprite");
        SerializedProperty customPreviewProperty = outfitObject.FindProperty("hasCustomPreview");

        iconProperty.objectReferenceValue = texture;

        if (texture == null)
        {
            previewSpriteProperty.objectReferenceValue = null;
            customPreviewProperty.boolValue = false;
            outfitObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(outfitData);
            return;
        }

        string assetPath = AssetDatabase.GetAssetPath(texture);
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sprite == null)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (Object asset in assets)
            {
                if (asset is Sprite loadedSprite)
                {
                    sprite = loadedSprite;
                    break;
                }
            }
        }

        if (sprite != null)
        {
            previewSpriteProperty.objectReferenceValue = sprite;
            customPreviewProperty.boolValue = true;
            outfitObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(outfitData);
            AssetDatabase.SaveAssets();
        }
        else
        {
            outfitObject.ApplyModifiedProperties();
            EditorUtility.DisplayDialog(
                "Preview Conversion Failed",
                "Unity could not load a sprite from that texture. Make sure the texture imports as Sprite (2D and UI).",
                "OK");
        }
    }
}
