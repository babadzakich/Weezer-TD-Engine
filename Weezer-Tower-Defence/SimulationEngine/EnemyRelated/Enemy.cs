using System;
using System.Runtime.InteropServices.Swift;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SimulationEngine.EnemyRelated;

public class Enemy
{
    public Vector2 Position { get; set; }
    private IEnemyType _type;
    private readonly MapRelated.Path _path;
    public int Health => _type.health;
    public int MaxHealth => _type.MaxHealth;
    public int Damage => _type.Damage;
    public bool isAlive { get; set; } = true;
    public bool isKilled { get; set; } = false;
    
    public string GetDefensePointId() => _path.DefensePointId;
    
    public Enemy(IEnemyType enemyType, Vector2 position, MapRelated.Path path)
    {
        _type = enemyType;
        Position = position;
        _path = path;
    }

    /**
    * Move creature on map
    */
    public void Update(GameTime gameTime)
    {
        _type.Update(this, gameTime, _path);
    }
    public void Draw(SpriteBatch sb)
    {
        _type.Draw(this, sb);
        DrawHealthBar(sb);
    }

    // Draws a red health bar above the enemy
    private void DrawHealthBar(SpriteBatch spriteBatch)
    {
        if (MaxHealth <= 0) return;

        // pixel texture if it doesn't exist 
        Texture2D pixel = CreatePixelTexture(spriteBatch.GraphicsDevice);
        
        // сalculate health percentage
        float healthPercentage = Math.Max(0f, Math.Min(1f, (float)Health / MaxHealth));
        
        int barWidth = 30;
        int barHeight = 4;
        int offsetY = -20; // above the enemy
        
        // cnetred bar above the enemy
        Vector2 barPosition = Position + new Vector2(-barWidth / 2f, offsetY);
        
        Rectangle backgroundRect = new Rectangle(
            (int)barPosition.X,
            (int)barPosition.Y,
            barWidth,
            barHeight
        );
        spriteBatch.Draw(pixel, backgroundRect, Color.Black);
        
        // Draw health bar
        if (healthPercentage > 0)
        {
            Rectangle healthRect = new Rectangle(
                (int)barPosition.X,
                (int)barPosition.Y,
                (int)(barWidth * healthPercentage),
                barHeight
            );
            spriteBatch.Draw(pixel, healthRect, Color.Red);
        }
    }

    private static Texture2D _pixelTexture;
    private static Texture2D CreatePixelTexture(GraphicsDevice graphicsDevice)
    {
        if (_pixelTexture == null)
        {
            _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });
        }
        return _pixelTexture;
    }

    public void TakeDamage(float amount)
    {
        _type.TakeDamage(amount);
        // kill enemy if health is leess or equals than 0
        if (_type.health <= 0)
        {
            isAlive = false;
            isKilled = true;
            return;
        }
    }
}