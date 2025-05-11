using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using TestsForGame;
using TestsForGame.MonoGameMenu;

namespace MonoGameMenu
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private Texture2D _backgroundTexture;

        Random rnd = new Random();
        // Шрифты и текстуры
        private SpriteFont _fontStory;
        private SpriteFont _font;
        private Texture2D _buttonTexture;

        private int CountDieOpponent;

        //состояние игры
        private enum GameState { Menu,Story, Playing }
        private GameState _currentState = GameState.Menu;
        private Texture2D _playerTexture;

        // Ресурсы для игрового экрана

        private Texture2D _gameTexture;

        private Player _player; // ГГ
        private List<Opponent> _opponents = new List<Opponent>(); // Плохие дяди
        public static Random Random { get; } = new Random();

        // Кнопки
        private Button _startButton;
        private Button _settingsButton;
        private Button _exitButton;

        private StoryScreen _storyScreen;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            _opponents = new List<Opponent>();
        }

        protected override void Initialize()
        {
            // Настройка размера окна
            _graphics.PreferredBackBufferWidth = 1650;
            _graphics.PreferredBackBufferHeight = 1200;
            _graphics.ApplyChanges();



            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            try
            {
                // Загрузка текстур с обработкой возможных ошибок
                _playerTexture = Content.Load<Texture2D>("самурай один");
                _gameTexture = Content.Load<Texture2D>("задний фон для битвы 3");
                _backgroundTexture = Content.Load<Texture2D>("горящая деревня");
                _font = Content.Load<SpriteFont>("Arial");
                _fontStory = Content.Load<SpriteFont>("TextStory");

                // Создание текстуры для кнопки
                _buttonTexture = new Texture2D(GraphicsDevice, 1, 1);
                _buttonTexture.SetData(new[] { Color.White });

                // Инициализация кнопок
                InitializeButtons();

                // Загрузка анимаций
                LoadAnimations();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки контента: {ex.Message}");
                throw;
            }
        }
        private void InitializeButtons()
        {
            _exitButton = new Button(_buttonTexture, _font, "Выход")
            {
                Position = new Vector2(720, 400),
                OnClick = () => Exit()
            };

            _startButton = new Button(_buttonTexture, _font, "Начать игру")
            {
                Position = new Vector2(720, 300),
                OnClick = () =>
                {
                    _currentState = GameState.Story;
                    _storyScreen?.Reset();
                }
            };


        }
        private void LoadAnimations()
        {
            // Загрузка текстур для анимаций игрока
            Texture2D idleSpriteSheet = Content.Load<Texture2D>("IDLE new 2");
            Texture2D runSpriteSheet = Content.Load<Texture2D>("RUN new");
            Texture2D attackSpriteSheet = Content.Load<Texture2D>("ATTACK 1 new");
            Texture2D deathSpriteSheet = Content.Load<Texture2D>("HURT new");

            // Создание игрока
            _player = new Player(
                position: new Vector2(200, 750),
                speed: 200f,
                idleAnimation: new Animation(idleSpriteSheet, 10, 0.2f, true),
                runAnimation: new Animation(runSpriteSheet, 16, 0.1f, true),
                attackAnimation: new Animation(attackSpriteSheet, 7, 0.15f, false),
                deathAnimation: new Animation(deathSpriteSheet, 4, 0.2f, false));

            _player.InitializeHealthBar(GraphicsDevice);

            // Загрузка текстур для анимаций противников
            Texture2D enemyIdle = Content.Load<Texture2D>("op idle new");
            Texture2D enemyWalk = Content.Load<Texture2D>("op Run new");
            Texture2D enemyAttack = Content.Load<Texture2D>("op Attack1 new");
            Texture2D enemyDeath = Content.Load<Texture2D>("Death new");

            CreateOpponents(enemyIdle, enemyWalk, enemyAttack, enemyDeath);



            // Инициализация истории
            Texture2D menuBackground = Content.Load<Texture2D>("карта для текста");
            _storyScreen = new StoryScreen(
                font: _fontStory,
                background: menuBackground,
                storyText: "\n\nИнцидент в Хонно-дзи..\n" +
                          "Убийство японского дайме Оды Нобунаги в Хонно-дзи, \n" +
                          "храме в Киото, 21 июня 1582 года,\n" +
                          "Нобунага был на пороге объединения страны, но погиб .\n" +
                          "во время неожиданного восстания своего вассала Акечи Мицухидэ!\n" +
                          "Смерть Нобунаги была отомщена две недели спустя,\n" +
                          "когда его вассал Тоетоми Хидэеси победил Мицухидэ в битве при Ямадзаки \n" +
                          "про него и будет наша история.. \n");
            _storyScreen.OnComplete += () => _currentState = GameState.Playing;
        }

        private void CreateOpponents(Texture2D enemyIdle, Texture2D enemyWalk,
                               Texture2D enemyAttack, Texture2D enemyDeath)
        {
            var idleAnim = new Animation(enemyIdle, 8, 0.2f, true);
            var walkAnim = new Animation(enemyWalk, 8, 0.1f, true);
            var attackAnim = new Animation(enemyAttack, 6, 0.15f, false);
            var deathAnim = new Animation(enemyDeath, 6, 0.2f, false);

            for (int i = 0; i < 3; i++)
            {
                var enemy = new Opponent(
                    position: new Vector2(600 + i * 200, 950),
                    player: _player,
                    idle: idleAnim,
                    run: walkAnim,
                    attack: attackAnim,
                    death: deathAnim)
                {
                    MoveSpeed = 80f + i * 20f,
                    DetectionRadius = 250f,
                    AttackRange = 60f
                };
                enemy.Initialize(GraphicsDevice);
                _opponents.Add(enemy);
            }
        }


        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                 Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                if (_currentState == GameState.Playing || _currentState == GameState.Story)
                    _currentState = GameState.Menu;

            }

            for (int i = _opponents.Count - 1; i >= 0; i--)
            {
                var enemy = _opponents[i];
                enemy.Update(gameTime);

                if (!enemy.IsAlive && enemy.IsDeathAnimationComplete)
                {
                    _opponents.RemoveAt(i);
                    CountDieOpponent++;
                }
            }


            switch (_currentState)
            {
                case GameState.Menu:
                    UpdateMenu(gameTime);
                    break;
                case GameState.Story:
                    _storyScreen.Update(gameTime);
                    if (Keyboard.GetState().IsKeyDown(Keys.Enter))
                    {
                        _currentState = GameState.Playing;
                    }
                    break;
                case GameState.Playing:
                    UpdateGame(gameTime);
                    break;
            }

            base.Update(gameTime);
        }

        // Обновление кнопок
        private void UpdateMenu(GameTime gameTime)
        {
            var mouseState = Mouse.GetState();
            _startButton.Update(mouseState);
            _exitButton.Update(mouseState);
        }

        private void UpdateGame(GameTime gameTime)
        {
            _player.Update(gameTime);
            _player.CheckAttack(_opponents);

        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin();


            switch (_currentState)
            {
                case GameState.Menu:
                    DrawMenu();
                    break;
                case GameState.Story:
                    _storyScreen.Draw(_spriteBatch);

                    break;
                case GameState.Playing:

                    DrawGame();
                    break;
            }

            _spriteBatch.End();

            base.Draw(gameTime);
        }



        private void DrawMenu()
        {
            // Фон меню
            _spriteBatch.Draw(_backgroundTexture, GraphicsDevice.Viewport.Bounds, Color.White);

            // Заголовок
            string title = "Главное меню";
            Vector2 titleSize = _font.MeasureString(title);
            _spriteBatch.DrawString(
                _font,
                title,
                new Vector2(720, 100),
                Color.White
            );

            // Кнопки
            _startButton.Draw(_spriteBatch);
            _exitButton.Draw(_spriteBatch);
        }

        private void DrawGame()
        {
            // Фон игры
            _spriteBatch.Draw(_gameTexture, GraphicsDevice.Viewport.Bounds, Color.White);

            // Игровой объект
            _player.Draw(_spriteBatch);
            _player.DrawHealthBar(_spriteBatch); // Рисуем полоску HP после спрайта игрока
            foreach (var enemy in _opponents)
            {
                enemy.Draw(_spriteBatch);
                //enemy.MapBounds = new Rectangle(0, 0, 1650, 1100);
            }

            // Инструкция для возврата
            string instruction = "ESC - Вернуться в меню";
            Vector2 instructionSize = _font.MeasureString(instruction);
            _spriteBatch.DrawString(
                _font,
                instruction,
                new Vector2(10, 10),
                Color.White
            );
        }
    }
}
