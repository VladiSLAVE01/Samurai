using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;


public class Button
{
    private Texture2D _texture;
    private SpriteFont _font;
    private string _text;
    private Rectangle _bounds;
    private bool _isHovered;

    public Vector2 Position
    {
        get => new Vector2(_bounds.X, _bounds.Y);
        set => _bounds = new Rectangle((int)value.X, (int)value.Y, _bounds.Width, _bounds.Height);
    }

    public Action OnClick { get; set; }
    public Texture2D HighlightTexture { get; internal set; }

    public Button(Texture2D texture, SpriteFont font, string text)
    {
        _texture = texture;
        _font = font;
        _text = text;
        _bounds = new Rectangle(0, 0, 200, 50);
    }

    public void Update(MouseState mouseState)
    {
        _isHovered = _bounds.Contains(mouseState.Position);

        if (_isHovered && mouseState.LeftButton == ButtonState.Pressed)
        {
            OnClick?.Invoke();
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        Color buttonColor = _isHovered ? Color.LightGray : Color.Gray;
        spriteBatch.Draw(HighlightTexture ?? CreateSolidTexture(spriteBatch.GraphicsDevice, buttonColor),
                        _bounds,
                        buttonColor);

        Vector2 textSize = _font.MeasureString(_text);
        Vector2 textPosition = new Vector2(
            _bounds.X + (_bounds.Width - textSize.X) / 2,
            _bounds.Y + (_bounds.Height - textSize.Y) / 2
        );

        spriteBatch.DrawString(_font, _text, textPosition, Color.Black);
    }

    private Texture2D CreateSolidTexture(GraphicsDevice graphicsDevice, Color color)
    {
        Texture2D texture = new Texture2D(graphicsDevice, 1, 1);
        texture.SetData(new[] { color });
        return texture;
    }
}
