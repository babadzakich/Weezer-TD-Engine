using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace SimulationEngine;
public interface Controller
{
    public void Update(GameTime deltaTime);
    public void Draw(SpriteBatch spriteBatch);
}