using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FontLoader.Core;
using FontLoader.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.Localization;
using Terraria.ModLoader.Config.UI;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace FontLoader.ConfigElements;

public class FontSelectionElement : ConfigElement<string>
{
    internal bool ValueNameUpdateNeeded { get; set; }
    internal bool SettingUpOptions { get; set; }
    internal bool UpdateNeeded { get; set; }
    internal bool SelectionExpanded { get; set; }
    internal UIPanel ChooserPanel { get; set; }
    internal NestedUIList ChooserList { get; set; }
    internal UIFocusInputTextField ChooserFilter { get; set; }
    internal List<FontElement> Options { get; set; }

    private const float RegularHeight = 36f;
    private const float ExpandedHeight = 260f;

    public override void OnBind() {
        base.OnBind();

        ValueNameUpdateNeeded = true;

        DrawLabel = false;
        Height.Set(RegularHeight, 0f);

        var labelText = new UIText(Label, textScale: 0.9f) {
            Top = {Pixels = 12},
            Left = {Pixels = 10}
        };
        Append(labelText);

        var fontNameText = new UIText(Label, textScale: 0.9f) {
            Top = {Pixels = 12},
            Left = {Pixels = -10},
            Width = {Precent = 1f},
            TextOriginX = 1f
        };
        fontNameText.OnUpdate += _ => {
            if (!ValueNameUpdateNeeded) return;

            ValueNameUpdateNeeded = false;

            if (string.IsNullOrWhiteSpace(Value) || !File.Exists(Value)) {
                fontNameText.SetText(Language.GetTextValue("Mods.FontLoader.None"));
            }

            foreach (var (_, fontPath, name) in FontPreviewHolder.Targets) {
                if (fontPath != Value) continue;

                fontNameText.SetText(name);
                return;
            }
        };
        Append(fontNameText);

        var invisibleClickBox = new UIPanel {
            Width = {Precent = 1f},
            Height = {Pixels = RegularHeight},
            BackgroundColor = Color.Transparent,
            BorderColor = Color.Transparent
        };
        invisibleClickBox.OnLeftClick += (_, _) => {
            SelectionExpanded = !SelectionExpanded;
            UpdateNeeded = true;
        };
        Append(invisibleClickBox);

        ChooserPanel = new UIPanel();
        ChooserPanel.Top.Set(RegularHeight, 0);
        ChooserPanel.Left.Set(10, 0);
        ChooserPanel.Height.Set(200, 0);
        ChooserPanel.Width.Set(-20, 1);
        ChooserPanel.SetPadding(2f);
        ChooserPanel.BorderColor = Color.Transparent;
        ChooserPanel.BackgroundColor = Color.Transparent;
        
        string countText = Language.GetTextValue("Mods.FontLoader.XInTotal", FontPreviewHolder.Targets.Count);
        var fontCountText = new UIText(countText) {
            TextOriginX = 1f,
            TextOriginY = 0.5f
        };
        fontCountText.Width.Set(0f, 1f);
        fontCountText.Height.Set(30, 0f);
        fontCountText.Top.Set(-6, 0);
        ChooserPanel.Append(fontCountText);

        var textBoxBackground = new UIPanel {
            PaddingTop = 0,
            PaddingBottom = 0,
            PaddingLeft = 0,
            PaddingRight = 0,
            BackgroundColor = Color.Transparent,
            BorderColor = Color.Transparent
        };
        textBoxBackground.Width.Set(240, 0f);
        textBoxBackground.Height.Set(30, 0f);
        textBoxBackground.Top.Set(-6, 0);
        textBoxBackground.Append(new UIHorizontalSeparator {
            Top = new StyleDimension(-8f, 1f),
            Width = StyleDimension.FromPercent(1f),
            Color = Color.Lerp(Color.White, new Color(63, 65, 151, 255), 0.85f) * 0.9f
        });
        ChooserFilter = new UIFocusInputTextField(Language.GetTextValue("Mods.FontLoader.FilterByName"));
        ChooserFilter.OnTextChange += (_, _) => { UpdateNeeded = true; };
        ChooserFilter.OnRightClick += (_, _) => ChooserFilter.SetText("");
        ChooserFilter.Width = StyleDimension.Fill;
        ChooserFilter.Height.Set(-6, 1f);
        ChooserFilter.Top.Set(6, 0f);
        textBoxBackground.Append(ChooserFilter);
        ChooserPanel.Append(textBoxBackground);

        ChooserList = new NestedUIList();
        ChooserList.Top.Set(30, 0);
        ChooserList.Height.Set(-30, 1);
        ChooserList.Width.Set(-12, 1);
        ChooserList.ManualSortMethod = _ => { }; // 不让他使用ManualSortMethod，我们在外面排过序了
        ChooserPanel.Append(ChooserList);

        var scrollbar = new UIScrollbar();
        scrollbar.SetView(100f, 1000f);
        scrollbar.Height.Set(-30f, 1f);
        scrollbar.Top.Set(30f, 0f);
        scrollbar.Left.Pixels += 8;
        scrollbar.HAlign = 1f;
        ChooserList.SetScrollbar(scrollbar);
        ChooserPanel.Append(scrollbar);
        //Append(chooserPanel);
    }

    public override void Update(GameTime gameTime) {
        base.Update(gameTime);

        if (!UpdateNeeded)
            return;

        UpdateNeeded = false;

        if (SelectionExpanded && Options is null) {
            Task.Run(() => {
                SettingUpOptions = true;
                Options = CreateDefinitionOptionElementList()?.ToList() ?? new List<FontElement>();
                SettingUpOptions = false;
                UpdateNeeded = true;
            });
        }

        if (SettingUpOptions) return;

        if (!SelectionExpanded) {
            ChooserPanel.Remove();
        }
        else {
            Append(ChooserPanel);
        }

        float newHeight = SelectionExpanded ? ExpandedHeight : RegularHeight;
        Height.Set(newHeight, 0f);

        if (Parent is UISortableElement) {
            Parent.Height.Pixels = newHeight;
        }

        if (SelectionExpanded && Options is not null) {
            var passed = GetPassedOptionElements();
            ChooserList.Clear();
            ChooserList.AddRange(passed);
        }
    }

    private IEnumerable<FontElement> GetPassedOptionElements() {
        return Options
            .Where(option => option.Name.Contains(ChooserFilter.CurrentString, StringComparison.OrdinalIgnoreCase))
            .OrderBy(option => option.Name);
    }

    private IEnumerable<FontElement> CreateDefinitionOptionElementList() {
        foreach (var info in FontPreviewHolder.Targets) {
            var fontElement = new FontElement(info.Target, info.FontName);
            fontElement.OnLeftClick += (_, _) => {
                Value = info.FontPath;
                UpdateNeeded = true;
                SelectionExpanded = false;
                ValueNameUpdateNeeded = true;
                MouseOut(new UIMouseEvent(this, Main.MouseScreen));
            };
            yield return fontElement;
        }
    }
}