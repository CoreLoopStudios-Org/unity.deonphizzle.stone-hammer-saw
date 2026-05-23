using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public class WeaponScrollSetupHelper : EditorWindow
{
    [MenuItem("Tools/Stone Hammer Saw/Setup Weapon Scroll View")]
    public static void SetupScrollView()
    {
        // Find Scroll View in the active scene
        ScrollRect scrollRect = FindAnyObjectByType<ScrollRect>();
        if (scrollRect == null)
        {
            Debug.LogError("[ScrollSetup] No ScrollRect found in the active scene!");
            EditorUtility.DisplayDialog("Error", "No ScrollRect found in the active scene! Make sure you are in HomeScene and it contains a Scroll View.", "OK");
            return;
        }

        Undo.RecordObject(scrollRect.gameObject, "Setup Scroll View");

        // 1. Configure ScrollRect
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;
        scrollRect.inertia = true;
        
        RectTransform scrollRectTransform = scrollRect.transform as RectTransform;
        if (scrollRectTransform != null)
        {
            // Set Scroll View width to fit cards (392) + scrollbar buffer
            scrollRectTransform.sizeDelta = new Vector2(420f, scrollRectTransform.sizeDelta.y);
            scrollRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            scrollRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRectTransform.pivot = new Vector2(0.5f, 0.5f);
            scrollRectTransform.anchoredPosition = new Vector2(0f, scrollRectTransform.anchoredPosition.y);
        }

        // 2. Configure Viewport
        RectTransform viewport = scrollRect.viewport;
        if (viewport != null)
        {
            Undo.RecordObject(viewport.gameObject, "Setup Viewport");
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.sizeDelta = Vector2.zero; // Stretch to fill parent
            viewport.pivot = new Vector2(0.5f, 0.5f);

            Mask mask = viewport.GetComponent<Mask>();
            if (mask == null && viewport.GetComponent<RectMask2D>() == null)
            {
                viewport.gameObject.AddComponent<RectMask2D>();
            }
        }

        // 3. Configure Content
        RectTransform content = scrollRect.content;
        if (content != null)
        {
            Undo.RecordObject(content.gameObject, "Setup Content");
            
            // Set anchors to Top-Stretch/Top-Center so it grows downwards
            content.anchorMin = new Vector2(0.5f, 1f);
            content.anchorMax = new Vector2(0.5f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = new Vector2(0f, 0f); // Reset Y to top
            content.sizeDelta = new Vector2(392f, content.sizeDelta.y);

            // Add/Configure VerticalLayoutGroup
            VerticalLayoutGroup layoutGroup = content.GetComponent<VerticalLayoutGroup>();
            if (layoutGroup == null)
            {
                layoutGroup = content.gameObject.AddComponent<VerticalLayoutGroup>();
            }
            layoutGroup.spacing = 20f; // Gap between cards: 260 spacing - 240 card height
            layoutGroup.childAlignment = TextAnchor.UpperCenter;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;

            // Add/Configure ContentSizeFitter to dynamically expand content height
            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            }
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            // 4. Configure Children (Weapon Items)
            int childCount = content.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform child = content.GetChild(i);
                Undo.RecordObject(child.gameObject, "Setup Child Layout Element");

                RectTransform childRect = child as RectTransform;
                if (childRect != null)
                {
                    childRect.sizeDelta = new Vector2(392f, 240f);
                }

                LayoutElement layoutElement = child.GetComponent<LayoutElement>();
                if (layoutElement == null)
                {
                    layoutElement = child.gameObject.AddComponent<LayoutElement>();
                }
                layoutElement.preferredWidth = 392f;
                layoutElement.preferredHeight = 240f;
                layoutElement.minHeight = 100f;
            }

            Debug.Log($"[ScrollSetup] Successfully setup scroll view content size: {childCount} items configured!");
            EditorUtility.DisplayDialog("Success", $"Successfully configured Scroll View Content!\n\n- Content Width: 392\n- Spacing: 20\n- {childCount} items set to 392x240 size.", "OK");
        }
    }
}
