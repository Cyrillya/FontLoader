using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FontLoader.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.ModLoader.Config.UI;
using Terraria.ModLoader.UI;
using Terraria.UI;
using Color = Microsoft.Xna.Framework.Color;

namespace FontLoader.ConfigElements;

public class FontSelectionElement : ConfigElement<string>
{
    internal bool LoadingFonts { get; set; }
    internal bool ValueNameUpdateNeeded { get; set; }
    internal bool SettingUpOptions { get; set; }
    internal bool UpdateNeeded { get; set; }
    internal bool SelectionExpanded { get; set; }
    internal UIPanel ChooserPanel { get; set; }
    internal UIList ChooserList { get; set; }
    internal UIFocusInputTextField ChooserFilter { get; set; }
    internal List<FontElement> Options { get; set; }

    private const float REGULAR_HEIGHT = 36f;
    private const float EXPANDED_HEIGHT = 260f;

    public override void OnBind() {
        base.OnBind();

        ValueNameUpdateNeeded = true;

        DrawLabel = false;
        Height.Set(REGULAR_HEIGHT, 0f);

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
                fontNameText.SetText("NONE");
            }

            foreach (var fonts in FontStatics.Manager.MinimalTypefaces.Values.Select(t => t.Fonts.Values)) {
                foreach (var font in fonts) {
                    if (font.TypefaceName != Value) continue;

                    var fontName = font.FullName;
                    if (OperatingSystem.IsWindows()) {
                        var pfc = new PrivateFontCollection();
                        pfc.AddFontFile(font.TypefaceName);
                        fontName = pfc.Families[0].Name;
                    }

                    fontNameText.SetText(fontName);
                    return;
                }
            }
        };
        Append(fontNameText);

        var invisibleClickBox = new UIPanel {
            Width = {Precent = 1f},
            Height = {Pixels = REGULAR_HEIGHT},
            BackgroundColor = Color.Transparent,
            BorderColor = Color.Transparent
        };
        invisibleClickBox.OnLeftClick += (_, _) => {
            // 防止还在加载的时候就打开选择器
            if (Loader.InstalledFontLoading) return;
            SelectionExpanded = !SelectionExpanded;
            UpdateNeeded = true;
        };
        Append(invisibleClickBox);

        ChooserPanel = new UIPanel();
        ChooserPanel.Top.Set(REGULAR_HEIGHT, 0);
        ChooserPanel.Left.Set(10, 0);
        ChooserPanel.Height.Set(200, 0);
        ChooserPanel.Width.Set(-20, 1);
        ChooserPanel.SetPadding(2f);
        ChooserPanel.BorderColor = Color.Transparent;
        ChooserPanel.BackgroundColor = Color.Transparent;

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
        ChooserFilter = new UIFocusInputTextField("Filter by Name");
        ChooserFilter.OnTextChange += (_, _) => { UpdateNeeded = true; };
        ChooserFilter.OnRightClick += (_, _) => ChooserFilter.SetText("");
        ChooserFilter.Width = StyleDimension.Fill;
        ChooserFilter.Height.Set(-6, 1f);
        ChooserFilter.Top.Set(6, 0f);
        textBoxBackground.Append(ChooserFilter);
        ChooserPanel.Append(textBoxBackground);

        ChooserList = new UIList();
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

        float newHeight = SelectionExpanded ? EXPANDED_HEIGHT : REGULAR_HEIGHT;
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
        foreach (var fonts in FontStatics.Manager.MinimalTypefaces.Values.Select(t => t.Fonts.Values)) {
            foreach (var font in fonts) {
                var fontName = font.FullName;
                if (OperatingSystem.IsWindows()) {
                    var pfc = new PrivateFontCollection();
                    pfc.AddFontFile(font.TypefaceName);
                    fontName = pfc.Families[0].Name;
                }

                var fontElement = new FontElement(font, fontName);
                fontElement.OnLeftClick += (_, _) => {
                    Value = fontElement.TypefaceName;
                    UpdateNeeded = true;
                    SelectionExpanded = false;
                    ValueNameUpdateNeeded = true;
                };
                yield return fontElement;
            }
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
        SetScale(0f);
    }

    public virtual void SetScale(float scale, params float[] aaa) {
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
        if (File.Exists(path) && Utilities.ModUtilities.IsTtfOrOtfFile(path)) {
            var fontName = FontStatics.Manager.GetTypeface(path)?.Name ?? "Load failed!";
            Tooltip = fontName;
        }
    }
}