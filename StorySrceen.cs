using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SixLabors.Fonts;
using System;

namespace MonoGameMenu
{
    public class StoryScreen
    {
        private float _minDisplayTime = 3.0f; // Минимальное время показа истории (3 секунды)
        private float _displayTimer = 0f;
        private bool _canSkip = false;
        private int _graphicsW = 1920;
        private int _graphicsH = 1080;
        private readonly SpriteFont _font;
        private readonly Texture2D _background;
        private string[] _storyTexts;
        private int _currentPage = 0;
        private const float TextSpeed = 0.05f;
        private readonly string _fullText;
        private string _visibleText = "";
        private float _typingTimer = 0f;
        private float _typingSpeed = 0.05f; // Скорость печати (секунд на символ)
        private bool _isTextComplete = false;
        private bool _inputReleased = false; // Флаг отпускания кнопки
        //public bool IsComplete { get; private set; }
        public Action OnComplete { get; set; }

        public StoryScreen(SpriteFont font, Texture2D background, string fullText, float typingSpeed = 0.05f, float minDisplayTime = 3.0f)
        {
            _font = font ?? throw new ArgumentNullException(nameof(font));
            _background = background ?? throw new ArgumentNullException(nameof(background));
            _fullText = fullText ?? throw new ArgumentNullException(nameof(fullText));
            _typingSpeed = typingSpeed;
            _minDisplayTime = minDisplayTime;
        }

        public void Update(GameTime gameTime)
        {
            var keyboardState = Keyboard.GetState();
            var mouseState = Mouse.GetState();

            // Проверяем, отпустили ли кнопки/клавиши
            if (!keyboardState.IsKeyDown(Keys.Enter) &&
                mouseState.LeftButton != ButtonState.Pressed)
            {
                _inputReleased = true;
            }

            // Обновляем анимацию текста
            if (!_isTextComplete)
            {
                _typingTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

                if (_typingTimer >= _typingSpeed)
                {
                    _typingTimer = 0f;
                    if (_visibleText.Length < _fullText.Length)
                    {
                        _visibleText = _fullText.Substring(0, _visibleText.Length + 1);
                    }
                    else
                    {
                        _isTextComplete = true;
                    }
                }
            }

            // Пропуск только если кнопка была отпущена и снова нажата
            if (_inputReleased && _isTextComplete &&
                (keyboardState.IsKeyDown(Keys.Enter) ||
                 mouseState.LeftButton == ButtonState.Pressed))
            {
                OnComplete?.Invoke();
            }
        
        }
        //    if (IsComplete) return;

        //    var keyboardState = Keyboard.GetState();
        //    if (Keyboard.GetState().IsKeyDown(Keys.Enter))
        //    {
        //        Complete(); // Вызывается при нажатии Enter
        //    }

        //    if (_visibleChars < _storyText.Length)
        //    {
        //        _textTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
        //        if (_textTimer >= TextSpeed)
        //        {
        //            _textTimer = 0;
        //            _visibleChars++;
        //        }
        //    }
        //    else
        //    {
        //        IsComplete = true;
        //    }
        //}
        public void Draw(SpriteBatch spriteBatch)
        {
            // Разбивка текста на строки для правильного отображения
            string[] lines = _visibleText.Split('\n');

            float y = 150; // Начальная позиция Y (центр экрана)

            // Получаем текущие размеры окна
            var viewport = spriteBatch.GraphicsDevice.Viewport;

            // Отрисовка фона с масштабированием
            spriteBatch.Draw(
                _background,
                new Rectangle(0, 0, viewport.Width, viewport.Height),
                Color.White);

            foreach (string line in lines)
            {
                Vector2 textSize = _font.MeasureString(line);
                // Отрисовываем строку с сохранением оригинальных отступов
                spriteBatch.DrawString(
                    _font,
                    line,
                    new Vector2((1920 - textSize.X )/2, y + 200), // X остается постоянным для всех строк
                    Color.White);

                    y += textSize.Y + 5; // Переход на следующую строку
            }

            // Подсказка для продолжения (только когда весь текст показан)
            if (_isTextComplete)
            {
                string hint = "Нажмите Enter для продолжения...";
                Vector2 hintSize = _font.MeasureString(hint);
                spriteBatch.DrawString(
                    _font,
                    hint,
                    new Vector2(
                        _background.Width - hintSize.X - 50,
                        _background.Height - hintSize.Y - 50),
                    Color.Gray);
            }
        }
        //public void Draw(SpriteBatch spriteBatch)
        //{
        //    spriteBatch.Draw(_background, spriteBatch.GraphicsDevice.Viewport.Bounds, Color.White * 0.7f);

        //    string visibleText = _storyText.Substring(0, _visibleChars);
        //    string[] lines = visibleText.Split('\n');
        //    float y = 150;

        //    foreach (string line in lines)
        //    {
        //        if (!string.IsNullOrEmpty(line))
        //        {
        //            Vector2 textSize = _fontStory.MeasureString(line);
        //            spriteBatch.DrawString(
        //                _fontStory,
        //                line,
        //                new Vector2((1650 - textSize.X) / 2, y),
        //                Color.White);
        //            y += textSize.Y + 5;
        //        }
        //    }

        //    if (_visibleChars >= _storyText.Length)
        //    {
        //        string skipText = "Нажмите Enter чтобы продолжить";
        //        Vector2 skipSize = _fontStory.MeasureString(skipText);
        //        spriteBatch.DrawString(
        //            _fontStory,
        //            skipText,
        //            new Vector2(720, 500),
        //            Color.Yellow);
        //    }
        //}

        public void Reset()
        {
            _visibleText = "";
            _typingTimer = 0f;
            _isTextComplete = false;
            _displayTimer = 0f;
            _canSkip = false;
            _inputReleased = false; // Сбрасываем флаг при рестарте
        }
    }
}