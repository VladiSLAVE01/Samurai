using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace MonoGameMenu
{
    public class StoryScreen
    {
        private readonly SpriteFont _fontStory;
        private readonly Texture2D _background;
        private string _storyText;
        private int _visibleChars;
        private float _textTimer;
        private const float TextSpeed = 0.05f;

        public bool IsComplete { get; private set; }
        public event Action OnComplete;

        public StoryScreen(SpriteFont font, Texture2D background, string storyText)
        {
            _fontStory = font ?? throw new ArgumentNullException(nameof(font));
            _background = background ?? throw new ArgumentNullException(nameof(background));
            _storyText = storyText ?? throw new ArgumentNullException(nameof(storyText));
            Reset();
        }
        private void Complete()
        {
            IsComplete = true;
            OnComplete?.Invoke(); // Вызов события при завершении
        }
        public void Update(GameTime gameTime)
        {
            if (IsComplete) return;

            var keyboardState = Keyboard.GetState();
            if (Keyboard.GetState().IsKeyDown(Keys.Enter))
            {
                Complete(); // Вызывается при нажатии Enter
            }

            if (_visibleChars < _storyText.Length)
            {
                _textTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (_textTimer >= TextSpeed)
                {
                    _textTimer = 0;
                    _visibleChars++;
                }
            }
            else
            {
                IsComplete = true;
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(_background, spriteBatch.GraphicsDevice.Viewport.Bounds, Color.White * 0.7f);

            string visibleText = _storyText.Substring(0, _visibleChars);
            string[] lines = visibleText.Split('\n');
            float y = 150;

            foreach (string line in lines)
            {
                if (!string.IsNullOrEmpty(line))
                {
                    Vector2 textSize = _fontStory.MeasureString(line);
                    spriteBatch.DrawString(
                        _fontStory,
                        line,
                        new Vector2((1650 - textSize.X) / 2, y),
                        Color.White);
                    y += textSize.Y + 5;
                }
            }

            if (_visibleChars >= _storyText.Length)
            {
                string skipText = "Нажмите Enter чтобы продолжить";
                Vector2 skipSize = _fontStory.MeasureString(skipText);
                spriteBatch.DrawString(
                    _fontStory,
                    skipText,
                    new Vector2(720, 500),
                    Color.Yellow);
            }
        }

        public void Reset()
        {
            _visibleChars = 0;
            _textTimer = 0;
            IsComplete = false;
        }

        public void SetText(string text)
        {
            _storyText = text ?? throw new ArgumentNullException(nameof(text));
            Reset();
        }
    }
}