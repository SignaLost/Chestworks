using System;
using Chestworks.Common;
using Chestworks.Core;
using HarmonyLib;
using Jotunn.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Chestworks.Patch;

/// <summary>
///     Adds a padlock button to the game's chest panel that locks and unlocks the open chest. It is a copy of the
///     game's own Take All button made square, or of its weight tab, by the <c>Lock Button Style</c> setting, and
///     sits at the player's chosen anchor on the panel, moved by their offset. The padlock shows the chest's state,
///     closed or open, in the color of the game's own text. SeneaL UI has an exclusion button in its chest header
///     already, so the button is left out when it is installed.
/// </summary>
[HarmonyPatch(typeof(InventoryGui), "UpdateContainer")]
internal static class InventoryGui_UpdateContainer_Lock_Patch
{
    private const float Gap = 4f;

    // Auga's buttons trail a pointed flourish, which the next button stays clear of.
    private const float AugaGap = 18f;

    private static float ButtonGap => Compatibility.Auga.Present ? AugaGap : Gap;

    // The icon's share of the button's smaller side, and on the tab, where it and the word under it sit as shares of the tab's height.
    private const float IconShare = 0.5f;
    private const float TabIconShare = 0.34f;
    private const float TabIconAt = 0.13f;
    private const float TabWordAt = -0.19f;
    private const float EdgeClear = 6f;

    // The word's size next to the weight number's; it shrinks further when the showing part of the tab is narrower still.
    private const float TabWordScale = 0.7f;

    // The black edge drawn around the padlock, in pixels, when the text it stands in for has none to copy.
    private const float IconOutline = 1f;

    private static Button _button;
    private static RectTransform _rect;
    private static RectTransform _iconRect;
    private static Image _icon;
    private static TMP_Text _word;

    // Auga's own label on the button, which reads the state in place of the padlock.
    private static TMP_Text _augaLabel;

    // The click sound the game's buttons make, played by hand.
    private static GameObject _clickSound;
    private static TabFeel _feel;
    private static Sprite _locked;
    private static Sprite _unlocked;
    private static Container _shown;
    private static bool _wasLocked;
    private static bool _failed;

    // Rebuilt when a setting changes.
    private static bool _stale;

    // Whether the button was built in the tab style, and the game's weight tab it was copied from.
    private static bool _tab;
    private static RectTransform _weightTab;

    private static void Postfix(InventoryGui __instance)
    {
        Container container = __instance.m_currentContainer;

        if (ChestLock.SeneaLPresent || _failed || !container || !__instance.m_container.gameObject.activeSelf)
        {
            Hide();
            return;
        }

        if (_stale && _button)
        {
            UnityEngine.Object.Destroy(_button.gameObject);
            _button = null;
            _shown = null;
        }

        _stale = false;

        if (!_button && !Create(__instance))
        {
            return;
        }

        bool locked = ChestLock.IsLocked(container);

        if (container != _shown || locked != _wasLocked || !_button.gameObject.activeSelf)
        {
            // The game's buttons can sit elsewhere for another chest size, so the spot is worked out per chest.
            if (container != _shown)
            {
                Place(__instance);
            }

            _shown = container;
            _wasLocked = locked;
            _icon.sprite = locked ? _locked : _unlocked;

            if (_word)
            {
                _word.text = Names.Localize(locked ? Tokens.Locked : Tokens.Unlocked);
            }

            if (_augaLabel)
            {
                _augaLabel.text = Names.Localize(locked ? Tokens.Unlock : Tokens.Lock).ToUpperInvariant();
            }

            _button.gameObject.SetActive(true);
        }
    }

    private static void Hide()
    {
        if (_button && _button.gameObject.activeSelf)
        {
            _button.gameObject.SetActive(false);
        }

        _shown = null;
    }

    /// <summary>Rebuilds the button at its next showing, after a setting changed.</summary>
    internal static void Refresh()
    {
        _stale = true;
    }

    /// <summary>Builds the button in the chosen style under the chest panel, with the padlock in place of its text.</summary>
    private static bool Create(InventoryGui gui)
    {
        _locked ??= LoadIcon("Chestworks.Icons.Locked.png", "ChestworksLocked");
        _unlocked ??= LoadIcon("Chestworks.Icons.Unlocked.png", "ChestworksUnlocked");

        if (!_locked || !_unlocked || !gui.m_takeAllButton)
        {
            _failed = true;
            return false;
        }

        _weightTab = ChestworksPlugin.LockButtonStyle.Value == LockButtonStyle.Tab ? WeightTab(gui) : null;
        _tab = _weightTab;
        GameObject model = _tab ? _weightTab.gameObject : gui.m_takeAllButton.gameObject;

        // The tab goes beside the weight tab in the hierarchy, so it is drawn behind the board as that one is.
        GameObject copy = UnityEngine.Object.Instantiate(model, _tab ? model.transform.parent : gui.m_container, false);
        copy.name = "ChestworksLock";
        _rect = (RectTransform)copy.transform;

        // Placed by hand, so a layout group around it must not count it and shuffle its neighbors.
        (copy.GetComponent<LayoutElement>() ?? copy.AddComponent<LayoutElement>()).ignoreLayout = true;
        // The padlock takes the color of the text it stands in for: the weight number on the tab, the Take All label on the button.
        TMP_Text text = _tab && gui.m_containerWeight ? gui.m_containerWeight : gui.m_takeAllButton.GetComponentInChildren<TMP_Text>(true);
        Color color = text ? text.color : Color.white;

        // The text makes way for the padlock, and on the tab so does the weight icon; a button keeps the pictures
        // that draw it, since Auga's buttons draw their shape on children rather than the root.
        // Under Auga the button keeps its label and reads LOCK or UNLOCK instead: its buttons are too slight for a picture.
        _augaLabel = Compatibility.Auga.Present ? copy.GetComponentInChildren<TMP_Text>(true) : null;

        if (_tab)
        {
            foreach (Transform child in copy.transform)
            {
                child.gameObject.SetActive(false);
            }
        }
        else if (!_augaLabel)
        {
            foreach (TMP_Text label in copy.GetComponentsInChildren<TMP_Text>(true))
            {
                label.gameObject.SetActive(false);
            }
        }

        // The background catches the clicks, so it must be hit-testable; the copied tab is not a button on its own.
        // Auga's buttons draw their shape on a child rather than the root.
        Image background = copy.GetComponent<Image>() ?? copy.GetComponentInChildren<Image>(true);

        if (!background)
        {
            UnityEngine.Object.Destroy(copy);
            _failed = true;
            Log.LogWarning("The chest panel's buttons have changed; no lock button is added.");
            return false;
        }

        background.raycastTarget = true;
        _button = copy.GetComponent<Button>() ?? copy.AddComponent<Button>();
        _button.targetGraphic = background;
        _button.interactable = true;
        _button.onClick = new Button.ButtonClickedEvent();
        _button.onClick.AddListener(OnClick);

        // The click sound is played by hand, the same one the game's button makes, so it survives the button being rebuilt.
        ButtonSfx sfx = gui.m_takeAllButton.GetComponent<ButtonSfx>();
        _clickSound = sfx ? sfx.m_sfxPrefab : null;

        foreach (ButtonSfx copied in copy.GetComponentsInChildren<ButtonSfx>(true))
        {
            UnityEngine.Object.Destroy(copied);
        }

        // The button is square; the tab keeps its own shape and, like the weight tab, sits behind the panel's board.
        // Auga's buttons are drawn as hexagons that would not survive squaring, so a copy of one keeps its shape too.
        Rect modelRect = ((RectTransform)model.transform).rect;
        _rect.anchorMin = _rect.anchorMax = _rect.pivot = new Vector2(0.5f, 0.5f);
        _rect.sizeDelta = _tab || Compatibility.Auga.Present ? modelRect.size : new Vector2(modelRect.height, modelRect.height);

        if (_tab)
        {
            _rect.SetSiblingIndex(model.transform.GetSiblingIndex() + 1);
        }

        float height = _rect.sizeDelta.y;
        GameObject iconObject = new("Icon", typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(copy.transform, false);
        RectTransform iconRect = _iconRect = (RectTransform)iconObject.transform;
        iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = Vector2.one * (Mathf.Min(_rect.sizeDelta.x, height) * (_tab ? TabIconShare : IconShare));
        iconRect.anchoredPosition = _tab ? new Vector2(0f, height * TabIconAt) : Vector2.zero;
        _icon = iconObject.GetComponent<Image>();
        _icon.color = color;
        _icon.raycastTarget = false;
        _icon.preserveAspect = true;
        _word = null;

        if (_augaLabel)
        {
            iconObject.SetActive(false);
            return true;
        }

        if (_tab)
        {
            // "Locked" or "Unlocked" under the padlock, in the weight number's own style, as the number sits under the weight.
            if (text)
            {
                GameObject wordObject = UnityEngine.Object.Instantiate(text.gameObject, copy.transform, false);
                wordObject.name = "Word";
                wordObject.SetActive(true);
                _word = wordObject.GetComponent<TMP_Text>();
                RectTransform wordRect = _word.rectTransform;
                wordRect.anchorMin = wordRect.anchorMax = wordRect.pivot = new Vector2(0.5f, 0.5f);
                wordRect.sizeDelta = new Vector2(_rect.sizeDelta.x, _word.fontSize * 1.5f);
                wordRect.anchoredPosition = new Vector2(0f, height * TabWordAt);
                _word.alignment = TextAlignmentOptions.Center;
                _word.enableAutoSizing = true;
                _word.fontSizeMax = _word.fontSize * TabWordScale;
                _word.fontSizeMin = Mathf.Max(6f, _word.fontSize * 0.4f);
                _word.raycastTarget = false;
            }

            // Pressing dims the whole tab, padlock and word included, not the board alone, and sounds like a button.
            _feel = copy.AddComponent<TabFeel>();
            _feel.Lead = background;
        }

        // The edge of the text the padlock stands in for, or a black one when it has none.
        Outline edge = text ? text.GetComponent<Outline>() : null;
        Material font = text ? text.fontSharedMaterial : null;
        bool fontEdge = font && font.HasProperty("_OutlineWidth") && font.GetFloat("_OutlineWidth") > 0f && font.HasProperty("_OutlineColor");

        Outline outline = iconObject.AddComponent<Outline>();
        outline.effectColor = edge ? edge.effectColor : fontEdge ? font.GetColor("_OutlineColor") : Color.black;
        outline.effectDistance = edge ? edge.effectDistance : new Vector2(IconOutline, -IconOutline);
        outline.useGraphicAlpha = true;

        return true;
    }

    // The sprite the game draws its weight tabs with.
    private const string TabSprite = "woodpanel_flik";

    /// <summary>The chest panel's weight tab background, found by the sprite it is drawn with; the closest to the weight text when there are several.</summary>
    private static RectTransform WeightTab(InventoryGui gui)
    {
        Vector3 near = gui.m_containerWeight ? gui.m_containerWeight.transform.position : gui.m_container.position;
        RectTransform found = null;
        float closest = float.MaxValue;

        foreach (Image image in gui.m_container.GetComponentsInChildren<Image>(true))
        {
            if (!image.sprite || image.sprite.name != TabSprite || image.name == "ChestworksLock")
            {
                continue;
            }

            float distance = (image.transform.position - near).sqrMagnitude;

            if (distance < closest)
            {
                closest = distance;
                found = image.rectTransform;
            }
        }

        if (!found)
        {
            Log.LogWarning("The chest panel's weight tab was not found; the lock button is drawn as a button.");
        }

        return found;
    }

    /// <summary>Puts the button or the tab at its anchor, moved by the player's offset for that style.</summary>
    private static void Place(InventoryGui gui)
    {
        RectTransform panel = gui.m_container;
        Vector2 at = _tab ? TabSpot(gui, panel) : ButtonSpot(gui, panel);
        Vector2 offset = _tab ? ChestworksPlugin.LockTabOffset.Value : ChestworksPlugin.LockButtonOffset.Value;

        // The spot is in the panel's space; the button is centered on it wherever it hangs in the hierarchy.
        RectTransform parent = (RectTransform)_rect.parent;
        Vector2 local = parent.InverseTransformPoint(panel.TransformPoint(at + offset));
        _rect.anchoredPosition = local - parent.rect.center;

        if (_tab)
        {
            FitToShowing(at + offset, panel.rect);
            _feel.Rest(_rect.anchoredPosition, Outward(ChestworksPlugin.LockTabAnchor.Value));
        }
    }

    /// <summary>The way out of the panel from the edge the tab hangs on.</summary>
    private static Vector2 Outward(LockTabAnchor anchor)
    {
        return anchor switch
        {
            LockTabAnchor.LeftTop or LockTabAnchor.LeftBottom => Vector2.left,
            LockTabAnchor.TopLeft or LockTabAnchor.TopRight => Vector2.up,
            LockTabAnchor.BottomLeft or LockTabAnchor.BottomRight => Vector2.down,
            _ => Vector2.right
        };
    }

    /// <summary>Keeps the padlock and the word within the part of the tab that shows past the board, since the rest is behind it.</summary>
    private static void FitToShowing(Vector2 center, Rect bounds)
    {
        Vector2 half = _rect.rect.size * 0.5f;
        float left = center.x - half.x;
        float right = center.x + half.x;

        // The showing part is whatever lies outside the board, on either side.
        if (left < bounds.xMin && right > bounds.xMin)
        {
            right = bounds.xMin;
        }
        else if (right > bounds.xMax && left < bounds.xMax)
        {
            left = bounds.xMax;
        }

        // The board's edge is uneven, so a little is kept clear of it.
        if (left == bounds.xMax)
        {
            left += EdgeClear;
        }
        else if (right == bounds.xMin)
        {
            right -= EdgeClear;
        }

        float middle = (left + right) * 0.5f - center.x;
        float width = right - left;
        _iconRect.anchoredPosition = new Vector2(middle, _iconRect.anchoredPosition.y);

        if (_word)
        {
            _word.rectTransform.anchoredPosition = new Vector2(middle, _word.rectTransform.anchoredPosition.y);
            _word.rectTransform.sizeDelta = new Vector2(width, _word.rectTransform.sizeDelta.y);
        }
    }

    /// <summary>
    ///     Hangs the tab off an edge of the panel, sticking out as far as the weight tab sticks out of the right edge,
    ///     with its end flush with the panel's corner.
    /// </summary>
    private static Vector2 TabSpot(InventoryGui gui, RectTransform panel)
    {
        Rect bounds = panel.rect;
        Vector2 half = _rect.rect.size * 0.5f;

        // How far past the right edge the weight tab's center sits; the same reach on every edge.
        Vector2 weight = CenterOf(_weightTab, panel);
        float reach = Mathf.Max(0f, weight.x - bounds.xMax);

        return ChestworksPlugin.LockTabAnchor.Value switch
        {
            LockTabAnchor.RightBottom => new Vector2(bounds.xMax + reach, bounds.yMin + half.y),
            LockTabAnchor.LeftTop => new Vector2(bounds.xMin - reach, bounds.yMax - half.y),
            LockTabAnchor.LeftBottom => new Vector2(bounds.xMin - reach, bounds.yMin + half.y),
            LockTabAnchor.TopLeft => new Vector2(bounds.xMin + half.x, bounds.yMax + reach),
            LockTabAnchor.TopRight => new Vector2(bounds.xMax - half.x, bounds.yMax + reach),
            LockTabAnchor.BottomLeft => new Vector2(bounds.xMin + half.x, bounds.yMin - reach),
            LockTabAnchor.BottomRight => new Vector2(bounds.xMax - half.x, bounds.yMin - reach),
            _ => new Vector2(bounds.xMax + reach, bounds.yMax - half.y)
        };
    }

    /// <summary>Centers the button just past the outer edge of Take All or Place Stacks, or just inside a corner of the panel.</summary>
    private static Vector2 ButtonSpot(InventoryGui gui, RectTransform panel)
    {
        Vector2 half = _rect.rect.size * 0.5f;
        Rect bounds = panel.rect;
        Vector2 at;

        switch (ChestworksPlugin.LockButtonAnchor.Value)
        {
            case LockButtonAnchor.PlaceStacks when gui.m_stackAllButton:
                at = EdgeOf((RectTransform)gui.m_stackAllButton.transform, panel, left: true) - new Vector2(half.x + ButtonGap, 0f);
                break;
            case LockButtonAnchor.TopLeft:
                at = new Vector2(bounds.xMin + half.x, bounds.yMax - half.y);
                break;
            case LockButtonAnchor.TopRight:
                at = new Vector2(bounds.xMax - half.x, bounds.yMax - half.y);
                break;
            case LockButtonAnchor.BottomLeft:
                at = new Vector2(bounds.xMin + half.x, bounds.yMin + half.y);
                break;
            case LockButtonAnchor.BottomRight:
                at = new Vector2(bounds.xMax - half.x, bounds.yMin + half.y);
                break;
            default:
                at = EdgeOf((RectTransform)gui.m_takeAllButton.transform, panel, left: false) + new Vector2(half.x + ButtonGap, 0f);
                break;
        }

        return at;
    }

    /// <summary>The middle of a button's left or right edge, in the panel's space.</summary>
    private static Vector2 EdgeOf(RectTransform button, RectTransform panel, bool left)
    {
        Vector3[] corners = new Vector3[4];
        button.GetWorldCorners(corners);

        // Corners run bottom left, top left, top right, bottom right.
        Vector3 edge = left ? (corners[0] + corners[1]) * 0.5f : (corners[2] + corners[3]) * 0.5f;
        return panel.InverseTransformPoint(edge);
    }

    /// <summary>The middle of a rect, in the panel's space.</summary>
    private static Vector2 CenterOf(RectTransform rect, RectTransform panel)
    {
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        return panel.InverseTransformPoint((corners[0] + corners[2]) * 0.5f);
    }

    private static Sprite LoadIcon(string resource, string name)
    {
        try
        {
            byte[] data = EmbeddedResources.Read(resource);
            Texture2D texture = data != null ? AssetUtils.LoadImage(data) : null;

            if (!texture)
            {
                return null;
            }

            texture.name = name;
            texture.wrapMode = TextureWrapMode.Clamp;
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }
        catch (Exception e)
        {
            Log.LogError($"The lock button's icon could not be loaded: {e.Message}");
            return null;
        }
    }

    private static void OnClick()
    {
        Container container = InventoryGui.instance ? InventoryGui.instance.m_currentContainer : null;

        if (container && Player.m_localPlayer && !Player.m_localPlayer.IsTeleporting())
        {
            ChestLock.Set(container, !ChestLock.IsLocked(container));
            _shown = null;

            if (_clickSound)
            {
                UnityEngine.Object.Instantiate(_clickSound);
            }
        }
    }
}

/// <summary>
///     Gives the tab a button's feel: the tint the button gives the wood is passed on to the padlock and the word, so
///     the whole tab dims together, and the tab slides out a little under the mouse, back in while pressed, and home again.
/// </summary>
internal sealed class TabFeel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    // How far it slides, in pixels, and how quickly it gets there.
    private const float HoverOut = 4f;
    private const float PressIn = 3f;
    private const float Speed = 14f;

    internal Graphic Lead;
    private Graphic[] _followers;
    private RectTransform _rect;
    private Vector2 _home;
    private Vector2 _outward = Vector2.right;
    private bool _hovered;
    private bool _pressed;

    /// <summary>Where the tab sits when left alone, and which way is out of the panel.</summary>
    internal void Rest(Vector2 home, Vector2 outward)
    {
        _home = home;
        _outward = outward;
        _rect ??= (RectTransform)transform;
        _rect.anchoredPosition = home;
    }

    public void OnPointerEnter(PointerEventData eventData) => _hovered = true;

    public void OnPointerExit(PointerEventData eventData)
    {
        _hovered = false;
        _pressed = false;
    }

    public void OnPointerDown(PointerEventData eventData) => _pressed = true;
    public void OnPointerUp(PointerEventData eventData) => _pressed = false;

    private void OnDisable()
    {
        _hovered = false;
        _pressed = false;

        if (_rect)
        {
            _rect.anchoredPosition = _home;
        }
    }

    private void LateUpdate()
    {
        _rect ??= (RectTransform)transform;
        Vector2 target = _home + _outward * (_pressed ? -PressIn : _hovered ? HoverOut : 0f);
        _rect.anchoredPosition = Vector2.Lerp(_rect.anchoredPosition, target, 1f - Mathf.Exp(-Speed * Time.unscaledDeltaTime));

        if (!Lead)
        {
            return;
        }

        _followers ??= GetComponentsInChildren<Graphic>(true);
        Color tint = Lead.canvasRenderer.GetColor();

        foreach (Graphic graphic in _followers)
        {
            if (graphic && graphic != Lead)
            {
                graphic.canvasRenderer.SetColor(tint);
            }
        }
    }
}

/// <summary>Adds "(Locked)" in gray after a locked chest's name on its hover text.</summary>
[HarmonyPatch(typeof(Container), nameof(Container.GetHoverText))]
internal static class Container_GetHoverText_Lock_Patch
{
    private static void Postfix(Container __instance, ref string __result)
    {
        if (string.IsNullOrEmpty(__result) || !ChestLock.IsLocked(__instance))
        {
            return;
        }

        int lineEnd = __result.IndexOf('\n');
        string mark = " " + ChestLock.LockedMark();
        __result = lineEnd < 0 ? __result + mark : __result.Insert(lineEnd, mark);
    }
}
