using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using TestsForGame;
using TestsForGame.MonoGameMenu;
using static MonoGameMenu.Opponent;
using Microsoft.Xna.Framework.Media;
using MiNET.Sounds;
using System.Diagnostics;

namespace MonoGameMenu
{
    public enum BossType
    {
        SamuraiBoss,  // Босс для воина
        ArcherBoss    // Босс для лучника
    }
    public enum PlayerType { Warrior, Archer }
    public class Game1 : Game
    {
        private VictoryScreen _victoryScreen;

        private Song song;
        private Song songHistoriA;
        private Song songHistoriT;
        private Song _currentStorySong;
        private bool _isStoryMusicPlaying = false;

        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        // Состояния игры
        public enum GameState { Menu, CharacterSelection, Story, Playing, BossTransition, BossFight, Victory }
        public GameState CurrentState { get; set; } = GameState.Menu;

        // Ресурсы
        private Texture2D _backgroundTexture;
        private Texture2D _gameTexture;
        private Texture2D _buttonTexture;
        private SpriteFont _font;
        private SpriteFont _fontStory;

        // Компоненты
        private CharacterSelectionScreen _characterSelection;

        private StoryScreen _storyScreen;
        private Player _player;

        private int _enemiesKilled = 0;
        private const int MAX_ENEMIES_TO_KILL = 6;
        private List<Opponent> _opponents = new List<Opponent>();
        private int _countDieOpponent;

        // Кнопки меню
        private Button _startButton;
        private Button _exitButton;
        private Texture2D _playerTexture;

        private bool _storyInputHandled = false;
        private bool _storyShown = false;

        private Dictionary<Opponent.EnemyType, Dictionary<string, Animation>> _enemyAnimations;

        private EnemyManager _enemyManager;

        private Rectangle _spawnArea;
        public int _countDieOpponents = 0;
        private float _musicVolume = 0.5f;
        // Выбранный персонаж
        public PlayerType? SelectedCharacter { get; set; } = null;
        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // Настройка размера окна
            _graphics.PreferredBackBufferWidth = 1920;
            _graphics.PreferredBackBufferHeight = 1080;
            _graphics.ApplyChanges();
            //_characterSelection = new CharacterSelectionScreen(this, Content);


            // Определяем область спавна (больше игрового поля)
            _spawnArea = new Rectangle(200, 200,
                _graphics.PreferredBackBufferWidth,
                _graphics.PreferredBackBufferHeight);
            MediaPlayer.Volume = _musicVolume;
            MediaPlayer.IsRepeating = false;
            base.Initialize();
        }
        private void InitializeButtons()
        {
            var buttonTexture = CreateSolidTexture(Color.Gray);
            var highlightTexture = CreateSolidTexture(new Color(255, 255, 255, 128));

            _exitButton = new Button(_buttonTexture, _font, "Выход")
            {
                Position = new Vector2(_graphics.PreferredBackBufferWidth / 2 - 100, 400),
                HighlightTexture = highlightTexture,
                OnClick = () => Exit()
            };

            _startButton = new Button(_buttonTexture, _font, "Начать игру")
            {
                Position = new Vector2(_graphics.PreferredBackBufferWidth / 2 - 100, 300),
                HighlightTexture = highlightTexture,
                OnClick = () => CurrentState = GameState.CharacterSelection
            };
        }

        private Texture2D CreateSolidTexture(Color color)
        {
            var texture = new Texture2D(GraphicsDevice, 1, 1);
            texture.SetData(new[] { color });
            return texture;
        }
        private void InitializeStoryScreen(PlayerType playerType)
        {
            Texture2D menuBackground = Content.Load<Texture2D>("Text Fon");
            string storyText = GetCharacterStory(playerType);

            _storyScreen = new StoryScreen(_fontStory, menuBackground, storyText);
            _storyScreen.OnComplete = () =>
            {
                StopStoryMusic();
                InitializeGameplay(playerType);
                CurrentState = GameState.Playing;
            };

            PlayStoryMusic(playerType);
        }

        private string GetCharacterStory(PlayerType playerType)
        {
            return playerType == PlayerType.Warrior ?
                "Акечи Мицухидэ:\n\n Когда Акечи был еще юношей, в его родную деревню явился \n" +
                "Тоетоми Хидэеси. По приказу главного генерала, Хидэеси предал деревню огню.\n" +
                "С того дня Акечи лелеял мечту о мести за свой дом, за свою семью.\n" +
                "И вот, кажется, этот день настал... Он сможет исполнить свою мечту..." :

                "Тоетоми Хидэеси:\n\n Первый военачальник Японии.\n" +
                "Судьба загнала Тоетоми в угол. Его семья под контролем  \n" +
                "Императора. Отказ от приказов невозможен. Ценой \n" +
                "не повиновения станет жизнь его близких.\n" +
                "Приказ поступил: уничтожить деревню Анубаги, не оставив камня на камне.\n" +
                "Время не ждет... Нужно двигаться";
        }



        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // Загрузка основных ресурсов
            _font = Content.Load<SpriteFont>("TextStory");
            _fontStory = Content.Load<SpriteFont>("TextStory");
            _backgroundTexture = Content.Load<Texture2D>("горящая деревня");
            _gameTexture = Content.Load<Texture2D>("burning_village_1");
            song = Content.Load<Song>("music");
            songHistoriA = Content.Load<Song>("История Ачеки 2");
            songHistoriT = Content.Load<Song>("История тоётоми 2");
            // начинаем проигрывание мелодии
            if (CurrentState == GameState.Menu)
            {
                if (MediaPlayer.State != MediaState.Playing)
                {
                    MediaPlayer.Play(song); // Запускаем музыку только в меню
                }
            }



            // Инициализация компонентов
            InitializeButtons();
            _characterSelection = new CharacterSelectionScreen(this, Content);

            // Загрузка анимаций врагов
            _enemyAnimations = new Dictionary<Opponent.EnemyType, Dictionary<string, Animation>>()
            {
                [Opponent.EnemyType.Bandit] = EnemyAnimationFactory.CreateBanditAnimations(Content),
                [Opponent.EnemyType.Ninja] = EnemyAnimationFactory.CreateNinjaAnimations(Content),
                //[Opponent.EnemyType.Boss] = EnemyAnimationFactory.CreateNinjaWomen2Animations(Content)
                //[Opponent.EnemyType.Boss] = EnemyAnimationFactory.CreateNinjaWomenAnimations(Content)
                [Opponent.EnemyType.Boss] = EnemyAnimationFactory.CreateBossAnimations(Content),
                [Opponent.EnemyType.Boss2] = EnemyAnimationFactory.CreateBoss2Animations(Content)
            };
        }

        private void InitializeGameplay(PlayerType playerType)
        {

            Dictionary<string, Animation> animations;
            if (playerType == PlayerType.Warrior)
            // Загрузка анимаций для выбранного персонажа
            {
                animations = new Dictionary<string, Animation>
                {
                    ["Idle"] = LoadAnimation("IDLE new 2", 10, 0.1f, true),
                    ["Run"] = LoadAnimation("RUN new", 16, 0.1f, true),
                    ["Attack"] = LoadAnimation("ATTACK 1 new", 8, 0.15f, false),
                    ["Death"] = LoadAnimation("DEATH", 10, 0.2f, false)
                };
            }
            else // Archer
            {
                animations = new Dictionary<string, Animation>
                {
                    ["Idle"] = LoadAnimation("IDLE_С", 5, 0.1f, true),
                    ["Run"] = LoadAnimation("RUN_С", 7, 0.1f, true),
                    ["Attack"] = LoadAnimation("ATTACK1_С", 5, 0.2f, false),
                    ["Death"] = LoadAnimation("DEATH_С", 10, 0.2f, false)
                };
            }
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            // Создание игрока с характеристиками в зависимости от типа
            _player = playerType == PlayerType.Warrior ?
                new Warrior(animations, new Vector2(200, 750)) :
                new Archer(animations, new Vector2(200, 750));

            // Создаем EnemyManager после создания игрока
            _enemyManager = new EnemyManager(_player, _spawnArea, Content, GraphicsDevice);
            _enemyManager.OnEnemyKilled += (kills) =>
            {
                _enemiesKilled = kills;
                // Можно добавить дополнительные эффекты при убийстве
            };
            _player.InitializeHealthBar(GraphicsDevice);
            //CreateEnemies(playerType);
        }

        private Animation LoadAnimation(string textureName, int frameCount, float frameDuration, bool isLopping)
        {
            try
            {
                var texture = Content.Load<Texture2D>(textureName);
                if (texture == null || texture.IsDisposed)
                {
                    Debug.WriteLine($"Текстура {textureName} не загружена или уничтожена");
                    return null;
                }
                Debug.WriteLine($"Загружена анимация {textureName} ({texture.Width}x{texture.Height})");
                return new Animation(texture, frameCount, frameDuration, isLopping);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки {textureName}: {ex.Message}");
                throw;
            }
            //return new Animation(
            //    Content.Load<Texture2D>(textureName),
            //    frameCount,
            //    frameDuration,
            //    isLopping);
        }


        private readonly Dictionary<PlayerType, Opponent.EnemyType> _playerEnemyMapping = new()
        {
            { PlayerType.Warrior, Opponent.EnemyType.Bandit },
            { PlayerType.Archer, Opponent.EnemyType.Ninja }
        };

        private readonly List<Opponent.EnemyType> _bossTypes = new()
        {
            Opponent.EnemyType.Boss
        };



        private Texture2D CreateHighlightTexture()
        {
            var texture = new Texture2D(GraphicsDevice, 1, 1);
            texture.SetData(new[] { new Color(255, 255, 255, 128) });
            return texture;
        }

        private Texture2D CreateButtonTexture()
        {
            var texture = new Texture2D(GraphicsDevice, 1, 1);
            texture.SetData(new[] { Color.White });
            return texture;
        }

        public void StartGame(PlayerType playerType)
        {
            SelectedCharacter = playerType;
            InitializeStoryScreen(playerType);
            CurrentState = GameState.Story;
        }

        public void StartCharacterStory(PlayerType playerType)
        {
            SelectedCharacter = playerType;
            InitializeStoryScreen(playerType);
            CurrentState = GameState.Story;
            _storyScreen.Reset();
            _storyInputHandled = false; // Сбрасываем флаг обработки ввода
        }

        private void PrepareBossFight()
        {
            // Определяем тип босса в зависимости от выбранного персонажа
            BossType bossType = SelectedCharacter == PlayerType.Warrior ?
                BossType.SamuraiBoss : BossType.ArcherBoss;

            var boss = CreateBoss(bossType);
            _enemyManager.AddBoss(boss);
            PlayBossMusic(bossType);

            // Позиционируем игрока для боя с боссом
            _player.Position = new Vector2(200, 750);

            // Запускаем уникальную музыку для босса
            PlayBossMusic(bossType);
        }

        private Opponent CreateBoss(BossType bossType)
        {
            // Позиция босса (центр верхней части экрана)
            Vector2 bossPosition = new Vector2(
                _graphics.PreferredBackBufferWidth / 2,
                150
            );

            Dictionary<string, Animation> animations;
            EnemyType enemyType;

            switch (bossType)
            {
                case BossType.SamuraiBoss:
                    enemyType = EnemyType.Boss;
                    animations = EnemyAnimationFactory.CreateBossAnimations(Content);
                    break;

                case BossType.ArcherBoss:
                    enemyType = EnemyType.Boss2;
                    animations = EnemyAnimationFactory.CreateBoss2Animations(Content);
                    break;

                default:
                    throw new ArgumentException("Unknown boss type");
            }

            var boss = new Opponent(enemyType, bossPosition, _player, animations)
            {
                BossHealth = 500,
                BossDamage = 40,
                MoveSpeed = 80f,
                DetectionRadius = 1000f,
                AttackRange = 120f
            };

            boss.InitializeHealthBar(GraphicsDevice);
            return boss;
        }

        private void ShowBossWarning(string bossName)
        {
            string message = $"ПРИГОТОВЬТЕСЬ К БИТВЕ С {bossName}!";

        }

        private void PlayBossMusic(BossType bossType)
        {
            switch (bossType)
            {
                case BossType.SamuraiBoss:
                    MediaPlayer.Play(Content.Load<Song>("Audio/Boss/samurai_theme"));
                    break;
                case BossType.ArcherBoss:
                    MediaPlayer.Play(Content.Load<Song>("Audio/Boss/archer_theme"));
                    break;
            }
            MediaPlayer.IsRepeating = true;
        }

        private void ShowVictoryScreen(string bossName)
        {
            // Создаем текст победы
            string victoryText = $"ВЫ ПОБЕДИЛИ {bossName}!";
            string continueText = "Нажмите ENTER чтобы продолжить";

            // Загружаем фоновое изображение
            Texture2D victoryBackground = Content.Load<Texture2D>("Backgrounds/victory");

            // Можно добавить анимацию появления
            _victoryScreen = new VictoryScreen(
                victoryBackground,
                _font,
                victoryText,
                continueText);
        }
        protected override void UnloadContent()
        {
            StopStoryMusic();
            base.UnloadContent();
        }
        private void ReturnToMainMenu()
        {
            CurrentState = GameState.Menu;
            _victoryScreen = null; // Освобождаем ресурсы
        }
        private void RestartStoryMusic()
        {
            if (_isStoryMusicPlaying)
            {
                MediaPlayer.Stop();
                MediaPlayer.Play(_currentStorySong);
                Debug.WriteLine("Музыка истории перезапущена");
            }
        }
        private void PlayStoryMusic(PlayerType playerType)
        {
            StopStoryMusic(); // Останавливаем предыдущую музыку

            _currentStorySong = playerType == PlayerType.Warrior ? songHistoriA : songHistoriT;

            MediaPlayer.Play(_currentStorySong);
            MediaPlayer.IsRepeating = false; // откл зацикливание
            _isStoryMusicPlaying = true;

            Debug.WriteLine($"Запущена музыка истории: {_currentStorySong.Name}");
        }
        private void StopStoryMusic()
        {
            if (_isStoryMusicPlaying)
            {
                MediaPlayer.Stop();
                _isStoryMusicPlaying = false;
                _currentStorySong = null;
                Debug.WriteLine("Музыка истории остановлена");
            }
        }
        protected override void Update(GameTime gameTime)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.Escape) && CurrentState != GameState.Menu)
            {
                CurrentState = GameState.Menu;
                return;
            }

            switch (CurrentState)
            {
                case GameState.Menu:
                    if (Keyboard.GetState().IsKeyDown(Keys.Escape) && CurrentState != GameState.Menu)
                    {
                        // Останавливаем музыку истории при возврате в меню
                        if (CurrentState == GameState.Story)
                        {
                            StopStoryMusic();
                        }
                        CurrentState = GameState.Menu;
                        return;
                    }
                    UpdateMenu(gameTime);
                    break;

                case GameState.CharacterSelection:
                    _characterSelection.Update(gameTime);
                    break;

                case GameState.Story:
                    if (!_storyInputHandled)
                    {
                        _storyInputHandled = true;
                        PlayStoryMusic(SelectedCharacter.Value);
                    }

                    // Просто обновляем экран истории
                    _storyScreen.Update(gameTime);
                    break;

                case GameState.Playing:
                    UpdateGameplay(gameTime);
                    break;
                case GameState.Victory:
                    if (Keyboard.GetState().IsKeyDown(Keys.Enter))
                    {
                        ReturnToMainMenu();
                    }
                    break;

            }

            base.Update(gameTime);
        }

        private void UpdateBossFight(GameTime gameTime)
        {
            _player.Update(gameTime);
            _enemyManager.Update(gameTime);

            if (_enemyManager.Boss != null && !_enemyManager.Boss.IsAlive)
            {
                string bossName = SelectedCharacter == PlayerType.Warrior ?
                    "Генерала Токугаву" : "Мастера Луков";
                string victoryText = $"ВЫ ПОБЕДИЛИ {bossName}!";

                ShowVictoryScreen(victoryText);
            }
        }

        // Обновление кнопок
        private void UpdateMenu(GameTime gameTime)
        {
            var mouseState = Mouse.GetState();
            _startButton.Update(mouseState);
            _exitButton.Update(mouseState);
        }

        private void UpdateGameplay(GameTime gameTime)
        {

            _player.Update(gameTime);
            _enemyManager.Update(gameTime);

            // Проверка перехода к боссу
            if (_enemyManager.ReadyForBossFight &&
                _player.Position.X > _graphics.PreferredBackBufferWidth - 200 &&
                Keyboard.GetState().IsKeyDown(Keys.Enter))
            {
                PrepareBossFight();
            }

            // Проверка победы над боссом
            if (_enemyManager.Boss == null && _enemyManager.EnemiesKilled > MAX_ENEMIES_TO_KILL)
            {
                ShowVictory();
            }
            // Проверяем атаки игрока
            _player.CheckAttack(_opponents.Where(e => e.IsAlive).ToList());

            base.Update(gameTime);
        }
        private void ShowVictory()
        {
            string bossName = SelectedCharacter == PlayerType.Warrior ?
                "Генерала Токугаву" : "Мастера Луков";

            ShowVictoryScreen($"ВЫ ПОБЕДИЛИ {bossName}!");
            CurrentState = GameState.Victory;
        }
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin();

            switch (CurrentState)
            {
                case GameState.Menu:
                    DrawMenu();
                    break;

                case GameState.CharacterSelection:
                    _characterSelection.Draw(_spriteBatch);
                    break;

                case GameState.Story:
                    _storyScreen.Draw(_spriteBatch);
                    break;

                case GameState.Playing:
                    DrawGameplay();
                    _enemyManager.Draw(_spriteBatch);
                    break;
                case GameState.Victory:
                    _victoryScreen.Draw(_spriteBatch);
                    break;
            }

            _spriteBatch.End();
            base.Draw(gameTime);
        }


        private void DrawMenu()
        {
            _spriteBatch.Draw(_backgroundTexture, GraphicsDevice.Viewport.Bounds, Color.White);

            string title = "Главное меню";
            Vector2 titleSize = _font.MeasureString(title);
            _spriteBatch.DrawString(
                _font,
                title,
                new Vector2(_graphics.PreferredBackBufferWidth / 2 - titleSize.X / 2, 100),
                Color.White
            );

            _startButton.Draw(_spriteBatch);
            _exitButton.Draw(_spriteBatch);
        }

        private void DrawGameplay()
        {
            _spriteBatch.Draw(_gameTexture, GraphicsDevice.Viewport.Bounds, Color.White);

            _player.Draw(_spriteBatch);
            _player.DrawHealthBar(_spriteBatch);

            // Отрисовка врагов через EnemyManager
            _enemyManager.Draw(_spriteBatch);

            string instruction = "ESC - Вернуться в меню";
            _spriteBatch.DrawString(_font, instruction, new Vector2(10, 10), Color.White);
            if (_enemyManager.Boss != null && _enemyManager.Boss.IsAlive)
            {
                // Отрисовка здоровья босса
                string bossHealthText = $"БОСС: {_enemyManager.Boss.Health} HP";
                Vector2 bossHealthPos = new Vector2(50, 50);
                _spriteBatch.DrawString(_font, bossHealthText, bossHealthPos, Color.Red);

                // Отображаем счетчик убийств
                string killsText = $"Убито врагов: {_enemyManager.EnemiesKilled}/{MAX_ENEMIES_TO_KILL}";
                Vector2 textSize = _font.MeasureString(killsText);
                _spriteBatch.DrawString(
                    _font,
                    killsText,
                    new Vector2(_graphics.PreferredBackBufferWidth - textSize.X - 20, 20),
                    Color.White);
            }
        }

    }
}

