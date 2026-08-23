using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace NekoSune.WorldUI.Editor
{
    internal static class NekoWorldUiFactory
    {
        static Font _font;

        public static GameObject Build(NekoWorldUiBlueprint blueprint, NekoWorldUiFeedDocument feed, List<string> notes)
        {
            if (blueprint == null) throw new ArgumentNullException("blueprint");
            if (notes == null) notes = new List<string>();

            GameObject root = new GameObject("NekoWorldUI - " + SafeName(blueprint.name));
            Undo.RegisterCreatedObjectUndo(root, "Create NekoSune World UI");

            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 10;

            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;

            root.AddComponent<GraphicRaycaster>();

            RectTransform canvasRect = root.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(Mathf.Max(320f, blueprint.width), Mathf.Max(240f, blueprint.height));
            root.transform.localScale = Vector3.one * Mathf.Clamp(blueprint.worldScale, 0.0001f, 0.02f);

            Image background = root.AddComponent<Image>();
            background.color = new Color(0.055f, 0.065f, 0.085f, 0.97f);

            GameObject viewport = Child(root, "Viewport");
            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            Stretch(viewportRect, 26f, 26f, 26f, 26f);
            Image viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.001f);
            Mask mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            GameObject content = Child(viewport, "Content");
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 16, 16);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            ScrollRect scroll = root.AddComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 32f;

            for (int i = 0; i < blueprint.elements.Count; i++)
                CreateElement(content, blueprint.elements[i], root, notes);

            if (feed != null && feed.items != null && feed.items.Count > 0)
            {
                CreateDivider(content);
                Text feedHeader = CreateText(content, "JSON / data items", 32, FontStyle.Bold, TextAnchor.MiddleLeft);
                SetPreferredHeight(feedHeader.gameObject, 52f);
                for (int i = 0; i < feed.items.Count; i++) CreateFeedCard(content, feed.items[i], i);
            }

            NekoWorldUiPlatform.ApplyPlatform(root, blueprint.platform, notes);
            NekoWorldUiPlatform.WireSafeActions(root, notes);

            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);
            return root;
        }

        static void CreateElement(GameObject parent, NekoWorldUiElement e, GameObject root, List<string> notes)
        {
            if (e == null) return;
            GameObject go;
            switch (e.type)
            {
                case NekoWorldUiElementType.Heading:
                    go = CreateText(parent, e.label, 42, FontStyle.Bold, TextAnchor.MiddleLeft).gameObject;
                    SetPreferredHeight(go, Mathf.Max(58f, e.height));
                    break;
                case NekoWorldUiElementType.Text:
                    go = CreateText(parent, e.label, 26, FontStyle.Normal, TextAnchor.MiddleLeft).gameObject;
                    SetPreferredHeight(go, Mathf.Max(44f, e.height));
                    break;
                case NekoWorldUiElementType.Image:
                    go = CreateImage(parent, e);
                    break;
                case NekoWorldUiElementType.Button:
                    go = CreateButton(parent, e);
                    break;
                case NekoWorldUiElementType.Toggle:
                    go = CreateToggle(parent, e);
                    break;
                case NekoWorldUiElementType.Slider:
                    go = CreateSlider(parent, e);
                    break;
                case NekoWorldUiElementType.GridItem:
                    go = CreateCard(parent, e.label, e.secondary);
                    SetPreferredHeight(go, Mathf.Max(88f, e.height));
                    break;
                case NekoWorldUiElementType.Divider:
                    CreateDivider(parent);
                    return;
                case NekoWorldUiElementType.Spacer:
                    go = Child(parent, "Spacer");
                    SetPreferredHeight(go, Mathf.Max(8f, e.height));
                    break;
                default:
                    go = CreateText(parent, e.label, 26, FontStyle.Normal, TextAnchor.MiddleLeft).gameObject;
                    break;
            }

            if (go != null)
                go.name = MetaName(e) + " " + e.label;

            if (e.action == NekoWorldUiAction.OpenLinkCard && !string.IsNullOrEmpty(e.actionValue))
            {
                Text url = CreateText(parent, e.actionValue, 19, FontStyle.Italic, TextAnchor.MiddleLeft);
                url.color = new Color(0.65f, 0.82f, 1f, 1f);
                url.gameObject.name = "Link URL - " + e.actionValue;
                SetPreferredHeight(url.gameObject, 34f);
            }
        }

        static GameObject CreateButton(GameObject parent, NekoWorldUiElement e)
        {
            GameObject go = Child(parent, "Button");
            RectTransform rt = go.AddComponent<RectTransform>();
            Image image = go.AddComponent<Image>();
            image.color = new Color(0.18f, 0.42f, 0.72f, 1f);
            Button button = go.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.26f, 0.52f, 0.86f, 1f);
            colors.pressedColor = new Color(0.12f, 0.31f, 0.58f, 1f);
            button.colors = colors;
            SetPreferredHeight(go, Mathf.Max(56f, e.height));
            Text label = CreateText(go, e.label, 27, FontStyle.Bold, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform, 8f, 8f, 6f, 6f);
            return go;
        }

        static GameObject CreateToggle(GameObject parent, NekoWorldUiElement e)
        {
            GameObject go = Child(parent, "Toggle");
            go.AddComponent<RectTransform>();
            HorizontalLayoutGroup row = go.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 12f;
            row.padding = new RectOffset(10, 10, 6, 6);
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlHeight = true;
            row.childControlWidth = false;
            SetPreferredHeight(go, Mathf.Max(58f, e.height));

            GameObject box = Child(go, "Background");
            RectTransform boxRt = box.AddComponent<RectTransform>();
            LayoutElement boxLayout = box.AddComponent<LayoutElement>();
            boxLayout.preferredWidth = 42f;
            boxLayout.preferredHeight = 42f;
            Image boxImage = box.AddComponent<Image>();
            boxImage.color = new Color(0.14f, 0.16f, 0.22f, 1f);

            GameObject mark = Child(box, "Checkmark");
            RectTransform markRt = mark.AddComponent<RectTransform>();
            Stretch(markRt, 8f, 8f, 8f, 8f);
            Image markImage = mark.AddComponent<Image>();
            markImage.color = new Color(0.32f, 0.78f, 0.58f, 1f);

            Text label = CreateText(go, e.label, 26, FontStyle.Normal, TextAnchor.MiddleLeft);
            LayoutElement labelLayout = label.gameObject.AddComponent<LayoutElement>();
            labelLayout.flexibleWidth = 1f;

            Toggle toggle = go.AddComponent<Toggle>();
            toggle.targetGraphic = boxImage;
            toggle.graphic = markImage;
            toggle.isOn = false;
            return go;
        }

        static GameObject CreateSlider(GameObject parent, NekoWorldUiElement e)
        {
            GameObject container = Child(parent, "Slider Row");
            container.AddComponent<RectTransform>();
            VerticalLayoutGroup column = container.AddComponent<VerticalLayoutGroup>();
            column.spacing = 6f;
            column.childControlWidth = true;
            column.childForceExpandHeight = false;
            SetPreferredHeight(container, Mathf.Max(86f, e.height));

            Text label = CreateText(container, e.label, 24, FontStyle.Normal, TextAnchor.MiddleLeft);
            SetPreferredHeight(label.gameObject, 32f);

            GameObject sliderGo = Child(container, "Slider");
            RectTransform sliderRt = sliderGo.AddComponent<RectTransform>();
            SetPreferredHeight(sliderGo, 38f);
            Slider slider = sliderGo.AddComponent<Slider>();

            GameObject bg = Child(sliderGo, "Background");
            RectTransform bgRt = bg.AddComponent<RectTransform>();
            Stretch(bgRt, 0f, 0f, 12f, 12f);
            Image bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0.12f, 0.13f, 0.18f, 1f);

            GameObject fillArea = Child(sliderGo, "Fill Area");
            RectTransform fillAreaRt = fillArea.AddComponent<RectTransform>();
            Stretch(fillAreaRt, 8f, 18f, 12f, 12f);
            GameObject fill = Child(fillArea, "Fill");
            RectTransform fillRt = fill.AddComponent<RectTransform>();
            Stretch(fillRt, 0f, 0f, 0f, 0f);
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.22f, 0.58f, 0.88f, 1f);

            GameObject handleArea = Child(sliderGo, "Handle Slide Area");
            RectTransform handleAreaRt = handleArea.AddComponent<RectTransform>();
            Stretch(handleAreaRt, 10f, 10f, 4f, 4f);
            GameObject handle = Child(handleArea, "Handle");
            RectTransform handleRt = handle.AddComponent<RectTransform>();
            handleRt.sizeDelta = new Vector2(28f, 28f);
            Image handleImage = handle.AddComponent<Image>();
            handleImage.color = Color.white;

            slider.fillRect = fillRt;
            slider.handleRect = handleRt;
            slider.targetGraphic = handleImage;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.5f;
            return container;
        }

        static GameObject CreateImage(GameObject parent, NekoWorldUiElement e)
        {
            GameObject go = Child(parent, "Image");
            go.AddComponent<RectTransform>();
            Image image = go.AddComponent<Image>();
            image.color = new Color(0.14f, 0.16f, 0.22f, 1f);
            image.preserveAspect = true;
            if (!string.IsNullOrEmpty(e.actionValue))
            {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(e.actionValue);
                if (sprite != null)
                {
                    image.sprite = sprite;
                    image.color = Color.white;
                }
            }
            SetPreferredHeight(go, Mathf.Max(120f, e.height));
            Text caption = CreateText(go, image.sprite == null ? "IMAGE\nDrag a Sprite here or choose one in the Builder" : "", 22, FontStyle.Italic, TextAnchor.MiddleCenter);
            Stretch(caption.rectTransform, 10f, 10f, 10f, 10f);
            return go;
        }

        static GameObject CreateCard(GameObject parent, string title, string subtitle)
        {
            GameObject card = Child(parent, "Card");
            card.AddComponent<RectTransform>();
            Image bg = card.AddComponent<Image>();
            bg.color = new Color(0.09f, 0.105f, 0.14f, 0.98f);
            VerticalLayoutGroup layout = card.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 10, 10);
            layout.spacing = 4f;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            Text heading = CreateText(card, title, 27, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetPreferredHeight(heading.gameObject, 36f);
            if (!string.IsNullOrEmpty(subtitle))
            {
                Text sub = CreateText(card, subtitle, 20, FontStyle.Normal, TextAnchor.MiddleLeft);
                sub.color = new Color(0.72f, 0.76f, 0.84f, 1f);
                SetPreferredHeight(sub.gameObject, 30f);
            }
            return card;
        }

        static void CreateFeedCard(GameObject parent, NekoWorldUiFeedItem item, int index)
        {
            if (item == null) return;
            GameObject card = CreateCard(parent, string.IsNullOrEmpty(item.title) ? "Item " + (index + 1) : item.title, item.subtitle);
            card.name = "JSON Item " + (index + 1) + " - " + item.title;
            if (!string.IsNullOrEmpty(item.description))
            {
                Text desc = CreateText(card, item.description, 21, FontStyle.Normal, TextAnchor.UpperLeft);
                SetPreferredHeight(desc.gameObject, 54f);
            }
            if (!string.IsNullOrEmpty(item.imageUrl))
            {
                Text img = CreateText(card, "Image URL: " + item.imageUrl, 17, FontStyle.Italic, TextAnchor.MiddleLeft);
                img.color = new Color(0.62f, 0.75f, 0.9f, 1f);
                SetPreferredHeight(img.gameObject, 30f);
            }
            if (!string.IsNullOrEmpty(item.url))
            {
                NekoWorldUiElement link = new NekoWorldUiElement();
                link.id = "feed-link-" + index;
                link.type = NekoWorldUiElementType.Button;
                link.label = "View Link";
                link.action = NekoWorldUiAction.OpenLinkCard;
                link.actionValue = item.url;
                GameObject button = CreateButton(card, link);
                button.name = MetaName(link) + " View Link";
                Text url = CreateText(card, item.url, 17, FontStyle.Italic, TextAnchor.MiddleLeft);
                url.color = new Color(0.65f, 0.82f, 1f, 1f);
                SetPreferredHeight(url.gameObject, 28f);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(card.GetComponent<RectTransform>());
        }

        static Text CreateText(GameObject parent, string value, int size, FontStyle style, TextAnchor anchor)
        {
            GameObject go = Child(parent, "Text");
            RectTransform rt = go.AddComponent<RectTransform>();
            Text text = go.AddComponent<Text>();
            text.font = Font;
            text.text = value ?? "";
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        static void CreateDivider(GameObject parent)
        {
            GameObject go = Child(parent, "Divider");
            go.AddComponent<RectTransform>();
            Image image = go.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.13f);
            SetPreferredHeight(go, 2f);
        }

        static GameObject Child(GameObject parent, string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        static void Stretch(RectTransform rt, float left, float right, float top, float bottom)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        static void SetPreferredHeight(GameObject go, float height)
        {
            LayoutElement l = go.GetComponent<LayoutElement>();
            if (l == null) l = go.AddComponent<LayoutElement>();
            l.preferredHeight = height;
            l.minHeight = Mathf.Min(height, 30f);
        }

        static string MetaName(NekoWorldUiElement e)
        {
            string value = (e.actionValue ?? "").Replace("|", "/").Replace("]", ")");
            return "NUI[" + e.id + "|" + e.action + "|" + value + "]";
        }

        static string SafeName(string value)
        {
            if (string.IsNullOrEmpty(value)) return "UI";
            return value.Replace("/", "-").Replace("\\", "-").Replace(":", "-");
        }

        static Font Font
        {
            get
            {
                if (_font != null) return _font;
                try { _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
                if (_font == null) try { _font = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { }
                return _font;
            }
        }
    }
}
