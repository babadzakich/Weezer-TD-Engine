using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

class Tower
{
    public Vector2 Position { get; set; }
    private TowerType _type;
    // private List<Enemy> enemiesInRange;
    private float _fireCooldown;

    private Texture2D _sprite;

    public Tower(TowerType type, Vector2 position)
    {
        _type = type;
        Position = position;
        // enemiesInRange = new List<Enemy>();
        _fireCooldown = 0f;
    }

    public void Update(GameTime gameTime)
    {
     _fireCooldown -= (float)gameTime.ElapsedGameTime.TotalSeconds;
     if (_fireCooldown <= 0f)
        {
            
        }   
    }
    public void Draw(SpriteBatch sb)
    {
        if (_sprite != null)
            sb.Draw(_sprite, Position - new Vector2(_sprite.Width/2, _sprite.Height/2), Color.White);
        else 
        {
            // Draw a placeholder rectangle if the sprite is not loaded
            Texture2D placeholder = new Texture2D(sb.GraphicsDevice, 40, 40);
            Color[] data = new Color[40 * 40];
            for (int i = 0; i < data.Length; ++i) data[i] = Color.Gray;
            placeholder.SetData(data);
            sb.Draw(placeholder, Position - new Vector2(20, 20), Color.White);
        }
    }
}