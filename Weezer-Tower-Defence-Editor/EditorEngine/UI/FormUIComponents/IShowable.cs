using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

interface IShowable
{
    public void Draw(SpriteBatch sb, SpriteFont font, Texture2D pixel);
    public void Update(MouseState mouse, KeyboardState keyboard);
    public bool IsAnyFieldActive();
}