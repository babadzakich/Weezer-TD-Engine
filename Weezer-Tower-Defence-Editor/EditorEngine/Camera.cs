using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

namespace EditorEngine;

/// <summary>
/// Камера редактора с поддержкой перемещения и масштабирования
/// </summary>
public class Camera
{
    public Vector2 Position { get; set; }
    public float Zoom { get; private set; }
    
    private const float MinZoom = 0.25f;
    private const float MaxZoom = 4.0f;
    private const float ZoomSpeed = 0.1f;
    private const float MoveSpeed = 300f;

    private readonly Vector2 _mapSize;
    private readonly int _screenWidth;
    private readonly int _screenHeight;
    
    private int previousScrollValue;

    public Camera(Vector2 mapSize, int screenWidth, int screenHeight)
    {
        Position = Vector2.Zero;
        Zoom = 1.0f;
        previousScrollValue = 0;
        
        _mapSize = mapSize;
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
    }

    /// <summary>
    /// Обновление камеры - обработка WASD и колёсика мыши
    /// </summary>
    public void Update(GameTime gameTime, KeyboardState keyboardState, MouseState mouseState)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        
        // Масштабирование колёсиком мыши (делаем первым, чтобы обновить zoom)
        int scrollDelta = mouseState.ScrollWheelValue - previousScrollValue;
        if (scrollDelta != 0)
        {
            float zoomChange = scrollDelta > 0 ? ZoomSpeed : -ZoomSpeed;
            Zoom = MathHelper.Clamp(Zoom + zoomChange, MinZoom, MaxZoom);
        }
        previousScrollValue = mouseState.ScrollWheelValue;
        
        // Перемещение камеры WASD с ограничениями
        float actualSpeed = MoveSpeed / Zoom;
        Vector2 newPosition = Position;
        
        if (keyboardState.IsKeyDown(Keys.W)) 
            newPosition.Y -= actualSpeed * deltaTime;
        if (keyboardState.IsKeyDown(Keys.S)) 
            newPosition.Y += actualSpeed * deltaTime;
        if (keyboardState.IsKeyDown(Keys.A)) 
            newPosition.X -= actualSpeed * deltaTime;
        if (keyboardState.IsKeyDown(Keys.D)) 
            newPosition.X += actualSpeed * deltaTime;
        
        // Вычисляем максимальные границы с учётом текущего zoom
        float visibleWidth = _screenWidth / Zoom;
        float visibleHeight = _screenHeight / Zoom;
        
        float maxX = Math.Max(0, _mapSize.X - visibleWidth);
        float maxY = Math.Max(0, _mapSize.Y - visibleHeight);
        
        // Ограничиваем позицию
        Position = new Vector2(
            MathHelper.Clamp(newPosition.X, 0, maxX),
            MathHelper.Clamp(newPosition.Y, 0, maxY)
        );
    }

    /// <summary>
    /// Преобразование из мировых координат в экранные
    /// </summary>
    public Vector2 WorldToScreen(Vector2 worldPos)
    {
        return (worldPos - Position) * Zoom;
    }

    /// <summary>
    /// Преобразование из экранных координат в мировые
    /// </summary>
    public Vector2 ScreenToWorld(Vector2 screenPos)
    {
        return screenPos / Zoom + Position;
    }

    /// <summary>
    /// Получить матрицу трансформации для SpriteBatch
    /// </summary>
    public Matrix GetTransformMatrix()
    {
        return Matrix.CreateTranslation(-Position.X, -Position.Y, 0) *
               Matrix.CreateScale(Zoom, Zoom, 1);
    }

    /// <summary>
    /// Сброс камеры в начальное положение
    /// </summary>
    public void Reset()
    {
        Position = Vector2.Zero;
        Zoom = 1.0f;
    }
}
