﻿using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.UI;

namespace FontLoader.ConfigElements;

internal class NestedUIList : UIList
{
    public override void MouseOver(UIMouseEvent evt)
    {
        base.MouseOver(evt);

        PlayerInput.LockVanillaMouseScroll("ModLoader/ListElement");
    }

    public override void ScrollWheel(UIScrollWheelEvent evt)
    {
        if (_scrollbar != null) {
            float oldpos = _scrollbar.ViewPosition;

            _scrollbar.ViewPosition -= evt.ScrollWheelValue;

            if (oldpos == _scrollbar.ViewPosition) {
                base.ScrollWheel(evt);
            }
        }
        else {
            base.ScrollWheel(evt);
        }
    }
}
