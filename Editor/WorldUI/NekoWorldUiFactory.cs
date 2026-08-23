using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace NekoSune.WorldUI.Editor
{
    internal static class NekoWorldUiFactory
    {
        static Font _font;
        static NekoWorldUiBlueprint _theme;

        public static GameObject Build(NekoWorldUiBlueprint blueprint, NekoWorldUiFeedDocument feed, List<string> notes)
        {
            if (blueprint == null) throw new ArgumentNullException("blueprint");
            if (notes == null) notes = new List<string>();
            _theme = blueprint;

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
            background.color = blueprint.backgroundColor;

            GameObject viewport = Child(root, "Viewport");
            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            float pad = Mathf.Max(0f, blueprint.panelPadding);
            Stretch(viewportRect, pad, pad, pad, pad);
            Image viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.001f);
            viewportImage.raycastTarget = false;
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
            int innerPad = Mathf.RoundToInt(Mathf.Max(0f, blueprint.panelPadding));
            layout.padding = new RectOffset(innerPad, innerPad, innerPad, innerPad);
            layout.spacing = Mathf.Max(0f, blueprint.spacing);
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
                CreateElement(content, blueprint.elements[i]);

            if (feed != null && feed.items != null && feed.items.Count > 0)
            {
                CreateDivider(content);
                Text feedHeader = CreateText(content, "JSON / data items", FontSize(32), FontStyle.Bold, TextAnchor.MiddleLeft);
                SetPreferredHeight(feedHeader.gameObject, 52f);
                for (int i = 0; i < feed.items.Count; i++) CreateFeedCard(content, feed.items[i], i);
            }

            NekoWorldUiPlatformBridge.ApplyPlatform(root, blueprint.platform, notes);
            NekoWorldUiPlatformBridge.WireSafeActions(root, notes);

            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);
            return root;
        }

        static void CreateElement(GameObject parent, NekoWorldUiElement e)
        {
            if (e == null) return;
            GameObject go;
            switch (e.type)
            {
                case NekoWorldUiElementType.Heading:
                    go = CreateText(parent, e.label, FontSize(42), FontStyle.Bold, TextAnchor.MiddleLeft).gameObject;
                    SetPreferredHeight(go, Mathf.Max(58f, e.height));
                    break;
                case NekoWorldUiElementType.Text:
                    go = CreateText(parent, e.label, FontSize(26), FontStyle.Normal, TextAnchor.MiddleLeft).gameObject;
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
                    go.AddComponent<RectTransform>();
                    SetPreferredHeight(go, Mathf.Max(8f, e.height));
                    break;
                default:
                    go = CreateText(parent, e.label, FontSize(26), FontStyle.Normal, TextAnchor.MiddleLeft).gameObject;
                    break;
            }

            if (go != null) go.name = MetaName(e) + " " + e.label;

            if (e.action == NekoWorldUiAction.OpenLinkCard && !string.IsNullOrEmpty(e.actionValue))
            {
                Text url = CreateText(parent, e.actionValue, FontSize(19), FontStyle.Italic, TextAnchor.MiddleLeft);
                url.color = Theme.linkColor;
                url.gameObject.name = "Link URL - " + e.actionValue;
                SetPreferredHeight(url.gameObject, 34f);
            }
        }

        static GameObject CreateButton(GameObject parent, NekoWorldUiElement e)
        {
            GameObject go = Child(parent, "Button");
            go.AddComponent<RectTransform>();
            Image image = go.AddComponent<Image>();
            image.color = Theme.primaryColor;
            Button button = go.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Theme.primaryColor;
            colors.highlightedColor = Color.Lerp(Theme.primaryColor, Color.white, 0.16f);
            colors.pressedColor = Color.Lerp(Theme.primaryColor, Color.black, 0.22f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            SetPreferredHeight(go, Mathf.Max(Theme.buttonHeight, e.height));
            Text label = CreateText(go, e.label, FontSize(27), FontStyle.Bold, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform, 8f, 8f, 6f, 6f);
            return go;
        }

        static GameObject CreateToggle(GameObject parent, NekoWorldUiElement e)
        {
            GameObject go = Child(parent, "Toggle");
            go.AddComponent<RectTransform>();
            HorizontalLayoutGroup row = go.AddComponent<HorizontalLayoutGroup>();
            row.spacing = Mathf.Max(8f, Theme.spacing);
            row.padding = new RectOffset(10, 10, 6, 6);
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlHeight = true;
            row.childControlWidth = false;
            SetPreferredHeight(go, Mathf.Max(Theme.buttonHeight, e.height));

            GameObject box = Child(go, "Background");
            box.AddComponent<RectTransform>();
            LayoutElement boxLayout = box.AddComponent<LayoutElement>();
            boxLayout.preferredWidth = 42f;
            boxLayout.preferredHeight = 42f;
            Image boxImage = box.AddComponent<Image>();
            boxImage.color = Theme.controlColor;

            GameObject mark = Child(box, "Checkmark");
            RectTransform markRt = mark.AddComponent<RectTransform>();
            Stretch(markRt, 8f, 8f, 8f, 8f);
            Image markImage = mark.AddComponent<Image>();
            markImage.color = Theme.accentColor;

            Text label = CreateText(go, e.label, FontSize(26), FontStyle.Normal, TextAnchor.MiddleLeft);
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
            column.spacing = Mathf.Max(4f, Theme.spacing * 0.5f);
            column.childControlWidth = true;
            column.childForceExpandHeight = false;
            SetPreferredHeight(container, Mathf.Max(86f, e.height));

            Text label = CreateText(container, e.label, FontSize(24), FontStyle.Normal, TextAnchor.MiddleLeft);
            SetPreferredHeight(label.gameObject, 32f);

            GameObject sliderGo = Child(container, "Slider");
            sliderGo.AddComponent<RectTransform>();
            SetPreferredHeight(sliderGo, 38f);
            Slider slider = sliderGo.AddComponent<Slider>();

            GameObject bg = Child(sliderGo, "Background");
            RectTransform bgRt = bg.AddComponent<RectTransform>();
            Stretch(bgRt, 0f, 0f, 12f, 12f);
            Image bgImage = bg.AddComponent<Image>();
            bgImage.color = Theme.controlColor;

            GameObject fillArea = Child(sliderGo, "Fill Area");
            RectTransform fillAreaRt = fillArea.AddComponent<RectTransform>();
            Stretch(fillAreaRt, 8f, 18f, 12f, 12f);
            GameObject fill = Child(fillArea, "Fill");
            RectTransform fillRt = fill.AddComponent<RectTransform>();
            Stretch(fillRt, 0f, 0f, 0f, 0f);
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = Theme.primaryColor;

            GameObject handleArea = Child(sliderGo, "Handle Slide Area");
            RectTransform handleAreaRt = handleArea.AddComponent<RectTransform>();
            Stretch(handleAreaRt, 10f, 10f, 4f, 4f);
            GameObject handle = Child(handleArea, "Handle");
            RectTransform handleRt = handle.AddComponent<RectTransform>();
            handleRt.sizeDelta = new Vector2(28f, 28f);
            Image handleImage = handle.AddComponent<Image>();
            handleImage.color = Theme.textColor;

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
            GameObject go = Child(parent, "Image Slot");
            go.AddComponent<RectTransform>();
            RawImage image = go.AddComponent<RawImage>();
            image.color = Theme.controlColor;
            image.raycastTarget = false;

            if (!string.IsNullOrEmpty(e.actionValue))
            {
                Texture texture = AssetDatabase.LoadAssetAtPath<Texture>(e.actionValue);
                if (texture != null)
                {
                    image.texture = texture;
                    image.color = Color.white;
                }
            }

            if (!string.IsNullOrEmpty(e.imageUrl))
                go.name = "NUI_REMOTE_IMAGE[" + SanitizeMeta(e.id) + "|" + SanitizeMeta(e.imageUrl) + "]";

            SetPreferredHeight(go, Mathf.Max(120f, e.height));
            Text caption = CreateText(go, image.texture == null ? "IMAGE\nChoose a local Texture or assign this slot to the runtime image loader" : "", FontSize(22), FontStyle.Italic, TextAnchor.MiddleCenter);
            caption.color = Theme.mutedTextColor;
            Stretch(caption.rectTransform, 10f, 10f, 10f, 10f);
            return go;
        }

        static GameObject CreateCard(GameObject parent, string title, string subtitle)
        {
            GameObject card = Child(parent, "Card");
            card.AddComponent<RectTransform>();
            Image bg = card.AddComponent<Image>();
            bg.color = Theme.panelColor;
            VerticalLayoutGroup layout = card.AddComponent<VerticalLayoutGroup>();
            int pad = Mathf.RoundToInt(Mathf.Max(8f, Theme.panelPadding * 0.8f));
            layout.padding = new RectOffset(pad, pad, pad, pad);
            layout.spacing = Mathf.Max(4f, Theme.spacing * 0.45f);
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            Text heading = CreateText(card, title, FontSize(27), FontStyle.Bold, TextAnchor.MiddleLeft);
            SetPreferredHeight(heading.gameObject, 36f);
            if (!string.IsNullOrEmpty(subtitle))
            {
                Text sub = CreateText(card, subtitle, FontSize(20), FontStyle.Normal, TextAnchor.MiddleLeft);
                sub.color = Theme.mutedTextColor;
                SetPreferredHeight(sub.gameObject, 30f);
            }
            return card;
        }

        static void CreateFeedCard(GameObject parent, NekoWorldUiFeedItem item, int index)
        {
            if (item == null) return;
            GameObject card = CreateCard(parent, string.IsNullOrEmpty(item.title) ? "Item " + (index + 1) : item.title, item.subtitle);
            card.name = "JSON Item " + (index + 1) + " - " + item.title;

            if (!string.IsNullOrEmpty(item.imageUrl))
            {
                GameObject imageSlot = Child(card, "NUI_REMOTE_IMAGE[feed-" + index + "|" + SanitizeMeta(item.imageUrl) + "]");
                imageSlot.AddComponent<RectTransform>();
                RawImage image = imageSlot.AddComponent<RawImage>();
                image.color = Theme.controlColor;
                image.raycastTarget = false;
                SetPreferredHeight(imageSlot, 180f);
                Text hint = CreateText(imageSlot, "REMOTE IMAGE SLOT", FontSize(18), FontStyle.Italic, TextAnchor.MiddleCenter);
                hint.color = Theme.mutedTextColor;
                Stretch(hint.rectTransform, 8f, 8f, 8f, 8f);
            }

            if (!string.IsNullOrEmpty(item.description))
            {
                Text desc = CreateText(card, item.description, FontSize(21), FontStyle.Normal, TextAnchor.UpperLeft);
                SetPreferredHeight(desc.gameObject, 54f);
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
                Text url = CreateText(card, item.url, FontSize(17), FontStyle.Italic, TextAnchor.MiddleLeft);
                url.color = Theme.linkColor;
                SetPreferredHeight(url.gameObject, 28f);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(card.GetComponent<RectTransform>());
        }

        static Text CreateText(GameObject parent, string value, int size, FontStyle style, TextAnchor anchor)
        {
            GameObject go = Child(parent, "Text");
            go.AddComponent<RectTransform>();
            Text text = go.AddComponent<Text>();
            text.font = Font;
            text.text = value ?? "";
            text.fontSize = Mathf.Max(10, size);
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = Theme.textColor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        static void CreateDivider(GameObject parent)
        {
            GameObject go = Child(parent, "Divider");
            go.AddComponent<RectTransform>();
            Image image = go.AddComponent<Image>();
            Color c = Theme.textColor;
            c.a = Mathf.Min(0.22f, c.a * 0.22f);
            image.color = c;
            image.raycastTarget = false;
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

        static int FontSize(int nominal)
        {
            float ratio = Mathf.Max(12, Theme.baseFontSize) / 26f;
            return Mathf.RoundToInt(nominal * ratio);
        }

        static string MetaName(NekoWorldUiElement e)
        {
            return "NUI[" + SanitizeMeta(e.id) + "|" + e.action + "|" + SanitizeMeta(e.actionValue) + "]";
        }

        static string SanitizeMeta(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Replace("|", "/").Replace("]", ")");
        }

        static string SafeName(string value)
        {
            if (string.IsNullOrEmpty(value)) return "UI";
            return value.Replace("/", "-").Replace("\\", "-").Replace(":", "-");
        }

        static NekoWorldUiBlueprint Theme
        {
            get
            {
                if (_theme == null)
                {
                    _theme = new NekoWorldUiBlueprint();
                    NekoWorldUiThemePresets.Apply(_theme, NekoWorldUiTheme.NekoDark);
                }
                return _theme;
            }
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
