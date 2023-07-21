using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FontLoader.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SharpFont;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.Localization;
using Terraria.ModLoader.Config.UI;
using Terraria.ModLoader.UI;
using Terraria.ModLoader.UI.Elements;
using Terraria.UI;
using Velentr.Font;

namespace FontLoader.ConfigElements;

public class FontSelectionElement : ConfigElement<string>
{
    private static Dictionary<string, Font> _loadedFonts = new();

    internal bool UpdateNeeded { get; set; }
    internal bool SelectionExpanded { get; set; }
    internal UIPanel ChooserPanel { get; set; }
    internal UIGrid ChooserGrid { get; set; }
    internal UIFocusInputTextField ChooserFilter { get; set; }
    internal float OptionScale { get; set; } = 0.5f;
    internal List<FontElement> Options { get; set; }
    internal OptionElement OptionChoice { get; set; }

    public override void OnBind() {
        base.OnBind();
        TextDisplayFunction = () => Label + ": " + OptionChoice.Tooltip;
        if (List != null) {
            TextDisplayFunction = () => Index + 1 + ": " + OptionChoice.Tooltip;
        }

        Height.Set(30f, 0f);

        OptionChoice = new OptionElement();
        OptionChoice.Top.Set(2f, 0f);
        OptionChoice.Left.Set(-30, 1f);
        OptionChoice.OnLeftClick += (_, _) => {
            SelectionExpanded = !SelectionExpanded;
            UpdateNeeded = true;
        };
        Append(OptionChoice);

        ChooserPanel = new UIPanel();
        ChooserPanel.Top.Set(30, 0);
        ChooserPanel.Height.Set(200, 0);
        ChooserPanel.Width.Set(0, 1);
        ChooserPanel.BackgroundColor = Color.CornflowerBlue;

        var textBoxBackgroundA = new UIPanel();
        textBoxBackgroundA.Width.Set(160, 0f);
        textBoxBackgroundA.Height.Set(30, 0f);
        textBoxBackgroundA.Top.Set(-6, 0);
        textBoxBackgroundA.PaddingTop = 0;
        textBoxBackgroundA.PaddingBottom = 0;
        ChooserFilter = new UIFocusInputTextField("Filter by Name");
        ChooserFilter.OnTextChange += (a, b) => { UpdateNeeded = true; };
        ChooserFilter.OnRightClick += (a, b) => ChooserFilter.SetText("");
        ChooserFilter.Width = StyleDimension.Fill;
        ChooserFilter.Height.Set(-6, 1f);
        ChooserFilter.Top.Set(6, 0f);
        textBoxBackgroundA.Append(ChooserFilter);
        ChooserPanel.Append(textBoxBackgroundA);

        ChooserGrid = new UIGrid();
        ChooserGrid.Top.Set(30, 0);
        ChooserGrid.Height.Set(-30, 1);
        ChooserGrid.Width.Set(-12, 1);
        ChooserPanel.Append(ChooserGrid);

        UIScrollbar scrollbar = new UIScrollbar();
        scrollbar.SetView(100f, 1000f);
        scrollbar.Height.Set(-30f, 1f);
        scrollbar.Top.Set(30f, 0f);
        scrollbar.Left.Pixels += 8;
        scrollbar.HAlign = 1f;
        ChooserGrid.SetScrollbar(scrollbar);
        ChooserPanel.Append(scrollbar);
        //Append(chooserPanel);
    }

    public override void Update(GameTime gameTime) {
        base.Update(gameTime);

        if (!UpdateNeeded)
            return;

        UpdateNeeded = false;

        if (SelectionExpanded && Options == null) {
            Options = CreateDefinitionOptionElementList()?.ToList() ?? new List<FontElement>();
        }

        if (!SelectionExpanded)
            ChooserPanel.Remove();
        else
            Append(ChooserPanel);

        float newHeight = SelectionExpanded ? 240 : 30;
        Height.Set(newHeight, 0f);

        if (Parent is UISortableElement) {
            Parent.Height.Pixels = newHeight;
        }

        if (SelectionExpanded) {
            var passed = GetPassedOptionElements();
            ChooserGrid.Clear();
            ChooserGrid.AddRange(passed);
        }

        //itemChoice.SetItem(_GetValue()?.GetID() ?? 0);
        OptionChoice.SetItem(Value);
    }

    private IEnumerable<FontElement> GetPassedOptionElements() {
        return Options.Where(option =>
            option.Name.Contains(ChooserFilter.CurrentString, StringComparison.OrdinalIgnoreCase));
    }

    private IEnumerable<FontElement> CreateDefinitionOptionElementList() {
        var fontsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
        string[] fontFiles = Directory.GetFiles(fontsFolderPath, "*.ttf|*.otf");
        foreach (var fontFile in fontFiles) {
            Font font;
            if (!_loadedFonts.TryGetValue(fontFile, out font)) {
                font = FontLoader.Manager.GetFont(fontFile, 20);
                _loadedFonts.Add(fontFile, font);
            }

            var fontElement = new FontElement(font.FontFamily, fontFile);
            yield return fontElement;
        }
    }
}

internal class OptionElement : UIImage
{
    public static Asset<Texture2D> DefaultBackgroundTexture { get; } = TextureAssets.InventoryBack9;

    public Asset<Texture2D> BackgroundTexture { get; set; } = DefaultBackgroundTexture;
    public string Tooltip { get; set; }
    public string FilePath { get; set; }

    internal float Scale { get; set; } = .75f;

    public OptionElement(float scale = .75f) : base(DefaultBackgroundTexture) {
        Scale = scale;
        Width.Set(DefaultBackgroundTexture.Width() * scale, 0f);
        Height.Set(DefaultBackgroundTexture.Height() * scale, 0f);
    }

    public virtual void SetScale(float scale) {
        Scale = scale;
        Width.Set(DefaultBackgroundTexture.Width() * scale, 0f);
        Height.Set(DefaultBackgroundTexture.Height() * scale, 0f);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch) {
        base.DrawSelf(spriteBatch);
        if (IsMouseHovering)
            UICommon.TooltipMouseText(Tooltip);
    }

    public virtual void SetItem(string path) {
        FilePath = path;
        Tooltip = "Load failed!";
        if (File.Exists(path) && FontUtilities.IsTtfOrOtfFile(path)) {
            var fontName = FontLoader.Manager.GetTypeface(path)?.Name ?? "Load failed!";
            Tooltip = fontName;
        }
    }
}