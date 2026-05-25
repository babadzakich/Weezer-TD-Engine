using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SimulationEngine.Infrastructure;

/// <summary>
/// Класс для отображения коротких анимаций (взрывы, искры и т.д.)
/// </summary>
public class VisualEffect
{
    public Vector2 Position { get; set; }
    public Texture2D Texture { get; set; }
    public bool IsActive { get; private set; } = true;

    private int _frameCount;
    private int _currentFrame;
    private float _frameTime; // Время на один кадр
    private float _timer;
    private float _scale;

    public VisualEffect(Texture2D texture, Vector2 position, int frameCount, float duration, float scale = 1.0f)
    {
        Texture = texture;
        Position = position;
        _frameCount = frameCount;
        _frameTime = duration / frameCount;
        _scale = scale;
        _currentFrame = 0;
        _timer = 0;
    }

    public void Update(GameTime gameTime)
    {
        if (!IsActive) return;

        _timer += (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_timer >= _frameTime)
        {
            _timer = 0;
            _currentFrame++;

            if (_currentFrame >= _frameCount)
            {
                IsActive = false; // Анимация закончилась
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (!IsActive || Texture == null) return;

        int frameWidth = Texture.Width / _frameCount;
        Rectangle sourceRect = new Rectangle(_currentFrame * frameWidth, 0, frameWidth, Texture.Height);
        
        // Рисуем по центру
        Vector2 origin = new Vector2(frameWidth / 2f, Texture.Height / 2f);
        
        spriteBatch.Draw(Texture, Position, sourceRect, Color.White, 0f, origin, _scale, SpriteEffects.None, 0f);
    }
}
