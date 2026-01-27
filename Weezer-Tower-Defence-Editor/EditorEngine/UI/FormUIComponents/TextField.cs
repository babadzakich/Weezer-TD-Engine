using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

class TextField : IShowable
{

    public readonly int top, left;
    public readonly string content;

    public TextField(int top, int left, string content)
    {
        this.top = top;
        this.left = left;
        this.content = content;
    }

    public void Draw(SpriteBatch sb, SpriteFont font, Texture2D pixel)
    {
        sb.DrawString(font, content, new Microsoft.Xna.Framework.Vector2(left, top), Microsoft.Xna.Framework.Color.White);
    }

    public void Update(MouseState mouse, KeyboardState keyboard) {}

    public bool IsAnyFieldActive() => false;
}

