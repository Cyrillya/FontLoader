using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.UI;
using Terraria.UI.Chat;

namespace FontLoader.ConfigElements;

internal class UIFocusInputTextField : UIElement
{
    internal bool Focused;
    internal string CurrentString = "";
    private readonly string _hintText;
    private int _textBlinkerCount;
    private int _textBlinkerState;

    public bool UnfocusOnTab { get; internal set; }

    public event EventHandler OnTextChange;

    public event EventHandler OnUnfocus;

    public event EventHandler OnTab;

    public UIFocusInputTextField(string hintText) => _hintText = hintText;

    public void SetText(string text) {
        if (text == null)
            text = "";
        if (!(CurrentString != text))
            return;
        CurrentString = text;
        EventHandler onTextChange = OnTextChange;
        if (onTextChange == null)
            return;
        onTextChange((object) this, new EventArgs());
    }

    public override void LeftClick(UIMouseEvent evt) {
        Main.clrInput();
        Focused = true;
    }

    public override void Update(GameTime gameTime) {
        if (!ContainsPoint(new Vector2((float) Main.mouseX, (float) Main.mouseY)) && Main.mouseLeft) {
            Focused = false;
            EventHandler onUnfocus = OnUnfocus;
            if (onUnfocus != null)
                onUnfocus((object) this, new EventArgs());
        }

        base.Update(gameTime);
    }

    private static bool JustPressed(Keys key) => Main.inputText.IsKeyDown(key) && !Main.oldInputText.IsKeyDown(key);

    protected override void DrawSelf(SpriteBatch spriteBatch) {
        if (Focused) {
            PlayerInput.WritingText = true;
            Main.instance.HandleIME();
            string inputText = Main.GetInputText(CurrentString);
            if (!inputText.Equals(CurrentString)) {
                CurrentString = inputText;
                EventHandler onTextChange = OnTextChange;
                if (onTextChange != null)
                    onTextChange((object) this, new EventArgs());
            }
            else
                CurrentString = inputText;

            if (JustPressed(Keys.Tab)) {
                if (UnfocusOnTab) {
                    Focused = false;
                    EventHandler onUnfocus = OnUnfocus;
                    if (onUnfocus != null)
                        onUnfocus((object) this, new EventArgs());
                }

                EventHandler onTab = OnTab;
                if (onTab != null)
                    onTab((object) this, new EventArgs());
            }

            if (++_textBlinkerCount >= 20) {
                _textBlinkerState = (_textBlinkerState + 1) % 2;
                _textBlinkerCount = 0;
            }
        }

        string currentString = CurrentString;
        if (_textBlinkerState == 1 && Focused)
            currentString += "|";
        var dimensions = GetDimensions();
        var font = FontAssets.MouseText.Value;
        var position = new Vector2(dimensions.X, dimensions.Y);
        position.X += 6f;
        if (CurrentString.Length == 0 && !Focused)
            ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, _hintText, position, Color.Gray, 0.0f, Vector2.Zero, Vector2.One, spread: 1.5f);
        else
            ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, currentString, position, Color.White, 0.0f, Vector2.Zero, Vector2.One, spread: 1.5f);
    }

    public delegate void EventHandler(object sender, EventArgs e);
}