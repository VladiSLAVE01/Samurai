using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace MonoGameMenu
{

    public class CharacterSelectionScreen
    {
        private readonly Game1 _game;
        private readonly ContentManager _content;

        // Текстуры
        private Texture2D _background;
        private Texture2D _warriorTexture;
        private Texture2D _archerTexture;
        private Texture2D _highlightTexture;
        private Texture2D _buttonTexture;
        private SpriteFont _font;
        private Texture2D _selectionFrame;

        //Список персонажей
        private List<CharacterOption> _characters;

        //Кнопка подтвержения  
        private Rectangle _confirmRect;

        // Анимационные параметры
        private float _hoverPulseValue = 0f;
        private const float PulseSpeed = 3f;

        // Состояния наведения

        private bool _isConfirmHovered = false;

        // Выбранный персонаж
        public PlayerType? SelectedCharacter { get; private set; } = null;

        public CharacterSelectionScreen(Game1 game, ContentManager content)
        {
            _game = game;
            _content = content;
            _warriorTexture = content.Load<Texture2D>("самурай один");
            _archerTexture = content.Load<Texture2D>("Сёгун один");
            LoadContent();
            InitializeCharacters();
            InitializeRectangles();
        }


        private void LoadContent()
        {
            _background = _content.Load<Texture2D>("горящая деревня");
            _font = _content.Load<SpriteFont>("TextStory");

            // Создаем простые текстуры программно
            _highlightTexture = CreateTexture(1, 1, new Color(255, 255, 255, 128));
            _buttonTexture = CreateTexture(1, 1, Color.Gray);
            _selectionFrame = CreateTexture(210, 310, Color.Gold, 5); // Рамка выделения

            // Загрузка текстур персонажей
            _warriorTexture = _content.Load<Texture2D>("самурай один");
            _archerTexture = _content.Load<Texture2D>("Сёгун один");

            // Описание персонажей
            string warriorDesc = "Беглец ставший самураем";
            string archerDesc = "Японский полководец";

            //_highlightTexture = CreateHighlightTexture();
            //_buttonTexture = CreateButtonTexture();

            _characters = new List<CharacterOption>
            {
                new CharacterOption(_warriorTexture, "Акечи Мицухидэ", warriorDesc, PlayerType.Warrior),
                new CharacterOption(_archerTexture, "Тоетоми Хидэеси", archerDesc, PlayerType.Archer)
            };
        }

        private Texture2D CreateTexture(int width, int height, Color color, int borderWidth = 0)
        {
            var texture = new Texture2D(_game.GraphicsDevice, width, height);
            var data = new Color[width * height];

            for (int i = 0; i < data.Length; i++)
            {
                bool isBorder = (i % width < borderWidth) ||
                               (i % width >= width - borderWidth) ||
                               (i < width * borderWidth) ||
                               (i >= width * (height - borderWidth));

                data[i] = isBorder ? color : Color.Transparent;
            }

            texture.SetData(data);
            return texture;
        }

        private void InitializeCharacters()
        {
            int centerX = _game.GraphicsDevice.Viewport.Width / 2;
            int centerY = _game.GraphicsDevice.Viewport.Height / 2;
            int characterWidth = 300;
            int characterHeight = 300;
            int spacing = 100;

            for (int i = 0; i < _characters.Count; i++)
            {
                _characters[i].Bounds = new Rectangle(
                    centerX - (_characters.Count * (characterWidth + spacing)) / 2 +
                    i * (characterWidth + spacing),
                    centerY - characterHeight / 2,
                    characterWidth,
                    characterHeight);
            }
        }

        private void InitializeRectangles()
        {
            int centerX = _game.GraphicsDevice.Viewport.Width / 2;
            _confirmRect = new Rectangle(
                centerX - 100,
                _game.GraphicsDevice.Viewport.Height - 100,
                200,
                50);
        }

        public void Update(GameTime gameTime)
        {
            var mouseState = Mouse.GetState();
            var mousePoint = new Point(mouseState.X, mouseState.Y);

            foreach (var character in _characters)
            {
                character.IsHovered = character.Bounds.Contains(mousePoint);

                if (character.IsHovered && mouseState.LeftButton == ButtonState.Pressed)
                {
                    _game.SelectedCharacter = character.Type;
                }
            }

            _isConfirmHovered = _confirmRect.Contains(mousePoint) && _game.SelectedCharacter.HasValue;

            if (_isConfirmHovered && mouseState.LeftButton == ButtonState.Pressed)
            {
                // Всегда запускаем историю при выборе любого персонажа
                _game.StartCharacterStory(_game.SelectedCharacter.Value);
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            // Фон
            spriteBatch.Draw(_background, _game.GraphicsDevice.Viewport.Bounds, Color.White);

            // Заголовок
            string title = "Выберите своего самурая";
            Vector2 titleSize = _font.MeasureString(title);
            spriteBatch.DrawString(
                _font,
                title,
                new Vector2(_game.GraphicsDevice.Viewport.Width / 2 - titleSize.X / 2, 50),
                Color.Gold);

            // Отрисовка персонажей
            foreach (var character in _characters)
            {
                DrawCharacter(spriteBatch, character);
            }

            // Отрисовка кнопки подтверждения
            DrawButton(spriteBatch);
        }

        private void DrawWrappedText(SpriteBatch spriteBatch, string text, Vector2 position, float maxWidth)
        {
            string[] words = text.Split(' ');
            string currentLine = "";
            float y = position.Y;

            foreach (string word in words)
            {
                string testLine = currentLine.Length > 0 ? currentLine + " " + word : word;
                Vector2 size = _font.MeasureString(testLine);

                if (size.X < maxWidth)
                {
                    currentLine = testLine;
                }
                else
                {
                    spriteBatch.DrawString(_font, currentLine,
                        new Vector2(position.X - _font.MeasureString(currentLine).X / 2, y),
                        Color.White);
                    y += _font.LineSpacing;
                    currentLine = word;
                }
            }

            if (!string.IsNullOrEmpty(currentLine))
            {
                spriteBatch.DrawString(_font, currentLine,
                    new Vector2(position.X - _font.MeasureString(currentLine).X / 2, y),
                    Color.White);
            }
        }
        private void DrawCharacter(SpriteBatch spriteBatch, CharacterOption character)
        {
            // Подсветка при наведении с эффектом пульсации
            if (character.IsHovered)
            {
                float pulseAlpha = 0.5f + 0.2f * (float)Math.Sin(_hoverPulseValue);
                spriteBatch.Draw(_highlightTexture, character.Bounds,
                    new Color(1f, 1f, 1f, pulseAlpha));
            }

            // Рамка если персонаж выбран
            if (_game.SelectedCharacter == character.Type)
            {
                spriteBatch.Draw(_selectionFrame,
                    new Rectangle(
                        character.Bounds.X - 5,
                        character.Bounds.Y - 5,
                        character.Bounds.Width + 10,
                        character.Bounds.Height + 10),
                    Color.White);
            }

            // Персонаж
            spriteBatch.Draw(character.Texture, character.Bounds, Color.White);

            // Имя персонажа
            Vector2 nameSize = _font.MeasureString(character.Name);
            spriteBatch.DrawString(
                _font,
                character.Name,
                new Vector2(character.Bounds.Center.X - nameSize.X / 2,
                          character.Bounds.Bottom + 15),
                _game.SelectedCharacter == character.Type ? Color.Gold : Color.White);

            // Описание персонажа (при наведении)
            if (character.IsHovered)
            {
                DrawWrappedText(spriteBatch, character.Description,
                    new Vector2(character.Bounds.Center.X, character.Bounds.Bottom + 50),
                    400);
            }
        }

        private void DrawButton(SpriteBatch spriteBatch)
        {
            Color buttonColor = _isConfirmHovered ? Color.LightGray : Color.Gray;
            if (!_game.SelectedCharacter.HasValue) buttonColor = Color.DarkGray;

            spriteBatch.Draw(_buttonTexture, _confirmRect, buttonColor);

            string buttonText = "Начать игру";
            Vector2 textSize = _font.MeasureString(buttonText);
            spriteBatch.DrawString(
                _font,
                buttonText,
                new Vector2(
                    _confirmRect.Center.X - textSize.X / 2,
                    _confirmRect.Center.Y - textSize.Y / 2),
                _isConfirmHovered ? Color.Black : Color.DarkSlateGray);
        }

        public void Reset()
        {
            _game.SelectedCharacter = null;
        }
    }

    public class CharacterOption
    {
        public Texture2D Texture { get; }
        public string Name { get; }
        public string Description { get; }
        public PlayerType Type { get; }
        public Rectangle Bounds { get; set; }
        public bool IsHovered { get; set; }

        public CharacterOption(Texture2D texture, string name, string description, PlayerType type)
        {
            Texture = texture;
            Name = name;
            Description = description;
            Type = type;
        }
    }
}
