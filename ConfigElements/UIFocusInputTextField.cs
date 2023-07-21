using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.GameInput;
using Terraria.UI;

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

    public UIFocusInputTextField(string hintText) => this._hintText = hintText;

    public void SetText(string text) {
        if (text == null)
            text = "";
        if (!(this.CurrentString != text))
            return;
        this.CurrentString = text;
        EventHandler onTextChange = this.OnTextChange;
        if (onTextChange == null)
            return;
        onTextChange((object) this, new EventArgs());
    }

    public override void LeftClick(UIMouseEvent evt) {
        Main.clrInput();
        this.Focused = true;
    }

    public override void Update(GameTime gameTime) {
        if (!this.ContainsPoint(new Vector2((float) Main.mouseX, (float) Main.mouseY)) && Main.mouseLeft) {
            this.Focused = false;
            EventHandler onUnfocus = this.OnUnfocus;
            if (onUnfocus != null)
                onUnfocus((object) this, new EventArgs());
        }

        base.Update(gameTime);
    }

    private static bool JustPressed(Keys key) => Main.inputText.IsKeyDown(key) && !Main.oldInputText.IsKeyDown(key);

    protected override void DrawSelf(SpriteBatch spriteBatch) {
        if (this.Focused) {
            PlayerInput.WritingText = true;
            Main.instance.HandleIME();
            string inputText = Main.GetInputText(this.CurrentString);
            if (!inputText.Equals(this.CurrentString)) {
                this.CurrentString = inputText;
                EventHandler onTextChange = this.OnTextChange;
                if (onTextChange != null)
                    onTextChange((object) this, new EventArgs());
            }
            else
                this.CurrentString = inputText;

            if (JustPressed(Keys.Tab)) {
                if (this.UnfocusOnTab) {
                    this.Focused = false;
                    EventHandler onUnfocus = this.OnUnfocus;
                    if (onUnfocus != null)
                        onUnfocus((object) this, new EventArgs());
                }

                EventHandler onTab = this.OnTab;
                if (onTab != null)
                    onTab((object) this, new EventArgs());
            }

            if (++this._textBlinkerCount >= 20) {
                this._textBlinkerState = (this._textBlinkerState + 1) % 2;
                this._textBlinkerCount = 0;
            }
        }

        string currentString = this.CurrentString;
        if (this._textBlinkerState == 1 && this.Focused)
            currentString += "|";
        CalculatedStyle dimensions = this.GetDimensions();
        if (this.CurrentString.Length == 0 && !this.Focused)
            Utils.DrawBorderString(spriteBatch, this._hintText, new Vector2(dimensions.X, dimensions.Y), Color.Gray);
        else
            Utils.DrawBorderString(spriteBatch, currentString, new Vector2(dimensions.X, dimensions.Y), Color.White);
    }

    public delegate void EventHandler(object sender, EventArgs e);
}