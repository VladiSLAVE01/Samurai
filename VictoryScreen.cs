using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class VictoryScreen
{
    private readonly Texture2D _background;
    private readonly SpriteFont _font;
    private readonly string _victoryText;
    private readonly string _continueText;

    public VictoryScreen(Texture2D background, SpriteFont font,
                       string victoryText, string continueText)
    {
        _background = background;
        _font = font;
        _victoryText = victoryText;
        _continueText = continueText;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(_background, Vector2.Zero, Color.White);

        // Отрисовка текста победы
        Vector2 textSize = _font.MeasureString(_victoryText);
        Vector2 position = new Vector2(
            960 - textSize.X / 2, // Центр экрана (1920/2)
            200);

        spriteBatch.DrawString(_font, _victoryText, position, Color.Gold);

        // Отрисовка текста продолжения
        position.Y += 100;
        spriteBatch.DrawString(_font, _continueText, position, Color.White);
    }
}