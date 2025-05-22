using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using MonoGameMenu;
using static MonoGameMenu.Opponent;
using System.Collections.Generic;
using System;
using System.Timers;
using System.Linq;
using Microsoft.Xna.Framework.Content;
using TestsForGame.MonoGameMenu;
using System.Diagnostics;

public class EnemyManager
{
    private Opponent _boss; // Приватное поле

    // Публичное свойство для доступа к боссу
    public Opponent Boss => _boss;
    public int EnemiesKilled => _enemiesKilled;
    public int CurrentEnemiesCount => _enemies.Count;
    public event Action<int> OnEnemyKilled; // Событие при убийстве врага
    private GraphicsDevice _graphicsDevice;
    private List<Opponent> _enemies;
    private Player _player;
    private Random _random;
    private Rectangle _spawnArea;
    private int _maxEnemies;
    private int _enemiesKilled = 0;
    private const int MAX_ENEMIES_TO_KILL = 6;
    private bool _spawningEnabled = true;
    private ContentManager _content;


    public EnemyManager(Player player, Rectangle spawnArea, ContentManager content, GraphicsDevice graphicsDevice, int maxEnemies = 10)
    {
        _content = content;
        _player = player;
        _spawnArea = spawnArea;
        _maxEnemies = maxEnemies;
        _content = content;
        _random = new Random();
        _enemies = new List<Opponent>();
        _graphicsDevice = graphicsDevice;

        SpawnInitialEnemies();
    }
    public List<Opponent> GetEnemies()
    {
        return _enemies;
    }
    //private Dictionary<string, Animation> LoadEnemyAnimations(EnemyType type)
    //{
    //    return type switch
    //    {
    //        EnemyType.Bandit => EnemyAnimationFactory.CreateBanditAnimations(_content),
    //        EnemyType.Ninja => EnemyAnimationFactory.CreateNinjaAnimations(_content),
    //        // EnemyType.Boss => EnemyAnimationFactory.CreateBossAnimations(_content),
    //        _ => EnemyAnimationFactory.CreateBanditAnimations(_content)
    //    };
    //}

    private void SpawnInitialEnemies()
    {
        for (int i = 0; i < 3; i++) // Начальное количество врагов
        {
            SpawnEnemy();
        }
    }
    // Добавляем метод для проверки готовности к бою с боссом
    public bool ReadyForBossFight =>
        _enemiesKilled >= MAX_ENEMIES_TO_KILL &&
        _enemies.Count == 0 &&
        _boss == null;
    public void AddBoss(Opponent boss)
    {
        if (_boss != null) return;

        _boss = boss;
        _boss.OnDeath += (opponent) =>
        {
            Debug.WriteLine("Босс побежден!");
            OnEnemyKilled?.Invoke(MAX_ENEMIES_TO_KILL + 1); // +1 для триггера победы
        };

        _boss.InitializeHealthBar(_graphicsDevice);
    }
    private void SpawnEnemy()
    {
        Vector2 position = new Vector2(
            _random.Next(_spawnArea.Left, _spawnArea.Right),
            _random.Next(_spawnArea.Top, _spawnArea.Bottom)
        );

        // Создаем случайного врага
        EnemyType type = (EnemyType)_random.Next(0, 3); // 0-Bandit, 1-Ninja, 2-Boss

        Dictionary<string, Animation> animations;
        switch (type)
        {
            case EnemyType.Bandit:
                animations = EnemyAnimationFactory.CreateBanditAnimations(_content);
                break;
            case EnemyType.Ninja:
                animations = EnemyAnimationFactory.CreateNinjaAnimations(_content);
                break;
            case EnemyType.Boss:
            default:
                animations = EnemyAnimationFactory.CreateBanditAnimations(_content); // Заглушка
                break;
        }

        var enemy = new Opponent(type, position, _player, animations);
        enemy.InitializeHealthBar(_graphicsDevice);

        enemy.OnDeath += HandleEnemyDeath; // Подписываемся на событие смерти
        _enemies.Add(enemy);
    }

    private void HandleEnemyDeath(Opponent enemy)
    {
        _enemies.Remove(enemy);
        _enemiesKilled++;
        OnEnemyKilled?.Invoke(_enemiesKilled);
        if (_enemiesKilled >= MAX_ENEMIES_TO_KILL)
        {
            _spawningEnabled = false;
            Debug.WriteLine("Достигнут лимит убийств - спавн остановлен");
            return;
        }

        if (_spawningEnabled && _enemies.Count < 3) // Поддерживаем 3 врага на карте
        {
            Timer timer = new Timer(2000);
            timer.Elapsed += (s, e) =>
            {
                if (_spawningEnabled)
                {
                    SpawnEnemy();
                }
                timer.Dispose();
            };
            timer.Start();
        }
    }

    public void Update(GameTime gameTime)
    {
        foreach (var enemy in _enemies.ToList())
        {
            enemy.Update(gameTime);
        }
        // Обновляем босса если есть
        if (_boss != null)
        {
            _boss.Update(gameTime);

            // Проверяем смерть босса
            if (!_boss.IsAlive && !_boss.IsDeathAnimationComplete)
            {
                _boss = null; // Удаляем босса после анимации смерти
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        foreach (var enemy in _enemies)
        {
            enemy.Draw(spriteBatch);
        }
        if (_boss != null && _boss.IsAlive)
        {
            _boss.Draw(spriteBatch);
        }
    }
}