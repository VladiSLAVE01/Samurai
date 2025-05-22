using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MiNET.Effects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using TestsForGame.MonoGameMenu;

namespace MonoGameMenu
{
    public class Opponent
    {
        // Добавляем специальные свойства для боссов
        public bool IsBoss => Type == EnemyType.Boss || Type == EnemyType.Boss2;
        public bool IsDeathAnimationComplete  { get; private set; } = false;
        public bool IsAttacing => _currentState == AIState.Attack;
        public Rectangle AttackHitbox => CalculateAttackHitbox();

        private const float AttackCooldownTime = 1.5f;
        private const float InvincibilityDuration = 0.5f;
        private const float AttackFrameDelay = 0.1f; // Задержка между кадрами атаки

        public int MaxHealth { get; private set; } = 100;
        public int Health { get; private set; } = 100;

        public int BossHealth { get; set; } = 300;
        public int BossDamage { get; set; } = 50;
        public bool IsAlive { get; private set; } = true;


        // Анимации

        private Animation _currentAnimation;
        private Animation _idleAnimation;
        private Animation _runAnimation;
        private Animation _attackAnimation;
        private Animation _deathAnimation;
        private bool _deathAnimationComplete = false;


        private bool _isFacingRight = true;
        private float _attackCooldown = 0f;
        private float _invincibilityTimer = 0f;
        private bool _isHit = false;
        private float _attackFrameTimer = 0f;
        private int _currentAttackFrame = 0;

        // Полоса здоровья
        private Texture2D _healthBarBg, _healthBarFg;
        private float _displayedHealth;
        private const float HealthLerpSpeed = 0.1f;
        private const int HealthBarWidth = 60, HealthBarHeight = 8, HealthBarOffsetY = -40;

        // Параметры существа
        public int Damage { get; private set; }

        public Vector2 Position { get; private set; }
        public bool IsActive { get;private set; } = true;
        public float MoveSpeed { get; set; } = 150f;
        public float DetectionRadius { get; set; } = 600f;
        public float AttackRange { get; set; } = 50f;


        private record EnemyStats(int Health, int Damage, float MoveSpeed,
                                float DetectionRadius, float AttackRange);
        // Характеристики врагов
        private static readonly Dictionary<EnemyType, EnemyStats> _enemyStats = new()
        {
            { EnemyType.Bandit, new EnemyStats(80, 15, 120f, 600f, 50f) },
            { EnemyType.Ninja, new EnemyStats(60, 20, 180f, 700f, 60f) },
            { EnemyType.Boss, new EnemyStats(200, 30, 100f, 800f, 80f) }
        };
        // Состояния ИИ
        public enum EnemyType
        {
            Bandit,    // Для воина
            Ninja,     // Для лучника
            Boss,    // Общий сильный враг
            Boss2
        }

        public EnemyType Type { get; }
        private enum AIState
        {
            Idle,
            Chase,
            Attack,
            Dead
        }
        private AIState _currentState = AIState.Idle;
        private EnemyType enemyType;
        public Dictionary<EnemyType, Animation> _enemyAnimations;

        // Основные параметры
        public Rectangle Bounds => new Rectangle(
            (int)Position.X - _currentAnimation.FrameWidth / 2,
            (int)Position.Y - _currentAnimation.FrameHeight / 2,
            _currentAnimation.FrameWidth,
            _currentAnimation.FrameHeight);


        private Animation CurrentAnimation
        {
            get => _currentAnimation ??= GetDefaultAnimation();
            set => _currentAnimation = value ?? throw new ArgumentNullException(nameof(value));
        }

        private Animation GetDefaultAnimation()
        {
            throw new NotImplementedException();
        }

        // Ссылка на игрока
        private readonly Player _player;
        public event Action<Opponent> OnDeath;

        public Opponent(EnemyType type, Vector2 position, Player player,
                       Dictionary<string, Animation >animations)
        {
            Type = type;
            Position = position;
            _player = player;

            // Установка характеристик
            var stats = _enemyStats[type];
            MaxHealth = Health = stats.Health;
            Damage = stats.Damage;
            MoveSpeed = stats.MoveSpeed;
            DetectionRadius = stats.DetectionRadius;
            AttackRange = stats.AttackRange;

            // Загрузка анимаций
            _idleAnimation = animations["Idle"];
            _runAnimation = animations["Run"];
            _attackAnimation = animations["Attack"];
            _deathAnimation = animations["Death"];
            _currentAnimation = _idleAnimation;
            if (type == EnemyType.Boss || type == EnemyType.Boss2)
            {
                Health = BossHealth;
                Damage = BossDamage;
            }
        }

        public Opponent(Vector2 position, Player player,
                       Animation idle, Animation run,
                       Animation attack, Animation death)
            : this(EnemyType.Bandit, position, player,
                  new Dictionary<string, Animation>
                  {
                      ["Idle"] = idle,
                      ["Run"] = run,
                      ["Attack"] = attack,
                      ["Death"] = death
                  })
        {
        }

        public Opponent(EnemyType type, Vector2 position, Player player, EnemyType enemyType, Dictionary<EnemyType, Animation> enemyAnimations)
        {
            Type = type;
            Position = position;
            _player = player;
            this.enemyType = enemyType;
            _enemyAnimations = enemyAnimations;
            if (IsBoss)
        {
            // Уникальные параметры для боссов
            Health = 500;
            Damage = 40;
            MoveSpeed = 80f;
            DetectionRadius = 1000f;
            AttackRange = 120f;
        }
        }

        public static class EnemyAnimationFactory
        {
            public static Dictionary<string, Animation> CreateBanditAnimations(ContentManager content)
            {
                return new Dictionary<string, Animation>
                {
                    ["Idle"] = new Animation(content.Load<Texture2D>("IDLE_EB"), 5, 0.15f, true),
                    ["Run"] = new Animation(content.Load<Texture2D>("RUN_EB"), 8, 0.1f, true),
                    ["Attack"] = new Animation(content.Load<Texture2D>("ATTACK 2_EB"), 5, 0.12f, false),
                    ["Death"] = new Animation(content.Load<Texture2D>("DEATH_EB"), 9, 0.2f, false)
                };
            }

            public static Dictionary<string, Animation> CreateNinjaAnimations(ContentManager content)
            {
                return new Dictionary<string, Animation>
                {
                    ["Idle"] = new Animation(content.Load<Texture2D>("IDLE_E"), 14, 0.15f, true),
                    ["Run"] = new Animation(content.Load<Texture2D>("RUN_E"), 8, 0.08f, true),
                    ["Attack"] = new Animation(content.Load<Texture2D>("ATTACK 2_E"), 5, 0.12f, false),
                    ["Death"] = new Animation(content.Load<Texture2D>("DEATH_E"), 10, 0.18f, false)
                };
            }

            //public static Dictionary<string, Animation> CreateNinjaWomenAnimations(ContentManager content)
            //{
            //    return new Dictionary<string, Animation>
            //    {
            //        ["Idle"] = new Animation(content.Load<Texture2D>("Textures/Enemies/Ninja/ninja_idle"), 10, 0.15f, true),
            //        ["Run"] = new Animation(content.Load<Texture2D>("Textures/Enemies/Ninja/ninja_run"), 10, 0.08f, true),
            //        ["Attack"] = new Animation(content.Load<Texture2D>("Textures/Enemies/Ninja/ninja_attack"), 8, 0.12f, false),
            //        ["Hurt"] = new Animation(content.Load<Texture2D>("Textures/Enemies/Ninja/ninja_hurt"), 5, 0.18f, false)
            //    };
            //}

            //public static Dictionary<string, Animation> CreateNinjaWomen2Animations(ContentManager content)
            //{
            //    return new Dictionary<string, Animation>
            //    {
            //        ["Idle"] = new Animation(content.Load<Texture2D>("Textures/Enemies/Ninja/ninja_idle"), 10, 0.15f, true),
            //        ["Run"] = new Animation(content.Load<Texture2D>("Textures/Enemies/Ninja/ninja_run"), 10, 0.08f, true),
            //        ["Attack"] = new Animation(content.Load<Texture2D>("Textures/Enemies/Ninja/ninja_attack"), 8, 0.12f, false),
            //        ["Hurt"] = new Animation(content.Load<Texture2D>("Textures/Enemies/Ninja/ninja_hurt"), 5, 0.18f, false)
            //    };
            //}

            public static Dictionary<string, Animation> CreateBossAnimations(ContentManager content)
            {
                return new Dictionary<string, Animation>
                {
                    ["Idle"] = new Animation(content.Load<Texture2D>("IDLE new 2"), 10, 0.1f, true),
                    ["Run"] = new Animation(content.Load<Texture2D>("RUN new"), 16, 0.05f, true),
                    ["Attack"] = new Animation(content.Load<Texture2D>("ATTACK 1 new"), 7, 0.15f, false),
                    ["Hurt"] = new Animation(content.Load<Texture2D>("DEATH"), 10, 0.2f, false)
                };
            }
            public static Dictionary<string, Animation> CreateBoss2Animations(ContentManager content)
            {
                return new Dictionary<string, Animation>
                {
                    ["Idle"] = new Animation(content.Load<Texture2D>("IDLE_С"), 5, 0.1f, true),
                    ["Run"] = new Animation(content.Load<Texture2D>("RUN_С"), 7, 0.05f, true),
                    ["Attack"] = new Animation(content.Load<Texture2D>("ATTACK1_С"), 5, 0.2f, false),
                    ["Hurt"] = new Animation(content.Load<Texture2D>("DEATH_С"), 10, 0.2f, false)
                };
            }

        }

        public void InitializeHealthBar(GraphicsDevice graphicsDevice)
        {
            _healthBarBg = new Texture2D(graphicsDevice, 1, 1);
            _healthBarBg.SetData(new[] { Color.Black });
            _healthBarFg = new Texture2D(graphicsDevice, 1, 1);
            _healthBarFg.SetData(new[] { Color.Red });
        }



        public void TakeDamage(int damage)
        {
            if (!IsAlive || _invincibilityTimer > 0) return;

            Health = Math.Max(0, Health - damage);
            _invincibilityTimer = InvincibilityDuration;
            _isHit = true;

            if (Health <= 0) { Die(); }

        }
        public void Die()
        {
            _currentState = AIState.Dead;
            SetAnimation(_deathAnimation);
            OnDeath?.Invoke(this); // Вызываем событие при смерти
        }

        public void DrawHealthBar(SpriteBatch spriteBatch)
        {
            if (!IsAlive) return;

            Vector2 pos = new Vector2(Position.X - HealthBarWidth / 2, Position.Y + HealthBarOffsetY);
            spriteBatch.Draw(_healthBarBg, new Rectangle((int)pos.X, (int)pos.Y, HealthBarWidth, HealthBarHeight), Color.Black);

            int healthWidth = (int)(HealthBarWidth * (_displayedHealth / MaxHealth));
            spriteBatch.Draw(_healthBarFg, new Rectangle((int)pos.X, (int)pos.Y, healthWidth, HealthBarHeight - 2), Color.Red);
        }


        public void Update(GameTime gameTime)
        {
            if (IsDeathAnimationComplete) return;

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Обновление таймеров
            UpdateTimers(deltaTime);

            if (!IsAlive)
            {
                _currentAnimation.Update(gameTime);
                if (_currentAnimation == _deathAnimation && _currentAnimation.IsComplete)
                {
                    IsDeathAnimationComplete = true;
                }
                return;
            }
            _displayedHealth = MathHelper.Lerp(_displayedHealth, Health, HealthLerpSpeed);

            // Обновление логики ИИ
            UpdateAI(deltaTime);

            // Обновление текущей анимации
            _currentAnimation.Update(gameTime);
        }

        private void UpdateTimers(float deltaTime)
        {
            if (_invincibilityTimer > 0)
                _invincibilityTimer -= deltaTime;
            else
                _isHit = false;

            if (_attackCooldown > 0)
                _attackCooldown -= deltaTime;

            if (_currentState == AIState.Attack)
                _attackFrameTimer -= deltaTime;
        }
        private void UpdateAI(float deltaTime)
        {
            if (!IsAlive) return;
            float distanceToPlayer = Vector2.Distance(Position, _player.Position);

            switch (_currentState)
            {
                case AIState.Idle:
                    UpdateIdleState(distanceToPlayer);
                    break;
                case AIState.Chase:
                    UpdateChaseState(distanceToPlayer, deltaTime);
                    break;
                case AIState.Attack:
                    UpdateAttackState(distanceToPlayer, deltaTime);
                    break;
            }
        }
        //private void UpdateAI(float deltaTime)
        //{
        //    if (!IsAlive) return;
        //    float distanceToPlayer = Vector2.Distance(Position, _player.Position);

        //    switch (_currentState)
        //    {
        //        case AIState.Idle:
        //            UpdateIdleState(distanceToPlayer);
        //            break;
        //        case AIState.Chase:
        //            UpdateChaseState(distanceToPlayer, deltaTime);
        //            break;
        //        case AIState.Attack:
        //            UpdateAttackState(distanceToPlayer, deltaTime);
        //            break;
        //    }
        //}
        private void UpdateIdleState(float distanceToPlayer)
        {
            SetAnimation(_idleAnimation);

            if (distanceToPlayer < DetectionRadius)
                _currentState = AIState.Chase;
        }
        private void UpdateChaseState(float distanceToPlayer, float deltaTime)
        {
            SetAnimation(_runAnimation);

            // Плавное движение к игроку с учетом времени кадра
            Vector2 direction = Vector2.Normalize(_player.Position - Position);
            Position += direction * MoveSpeed * deltaTime; // Используем gameTime

            // Поворот персонажа в сторону движения
            if (direction.X != 0)
            {
                _isFacingRight = direction.X > 0;
            }

            if (distanceToPlayer < AttackRange) // Используем поле AttackRange
            {
                _currentState = AIState.Attack;
                _currentAttackFrame = 0;
                _attackFrameTimer = AttackFrameDelay;
                SetAnimation(_attackAnimation);
            }
            else if (distanceToPlayer > DetectionRadius * 1.5f) // Используем поле DetectionRadius
            {
                _currentState = AIState.Idle;
            }
        }
        private void UpdateAttackState(float distanceToPlayer, float deltaTime)
        {

            if (_attackCooldown <= 0 && _player.IsAlive)
            {
                if (Vector2.Distance(Position, _player.Position) < AttackRange * 1.5f)
                {
                    _player.TakeDamage(Damage); // Используем Damage из характеристик

                    // Особые эффекты для разных типов
                    switch (Type)
                    {
                        case EnemyType.Ninja:
                            // Ниндзя наносит дополнительный урон при низком здоровье
                            if (Health < MaxHealth * 0.3f)
                                _player.TakeDamage(5);
                            break;
                        case EnemyType.Bandit:
                            // Ниндзя наносит дополнительный урон при низком здоровье
                            if (Health < MaxHealth * 0.3f)
                                _player.TakeDamage(5);
                            break;
                            //case EnemyType.Boss:
                            //    // Босс отбрасывает игрока
                            //    var knockback = Vector2.Normalize(_player.Position - Position) * 150f;
                            //    _player.ApplyKnockback(knockback);
                            //    break;
                    }

                    _attackCooldown = AttackCooldownTime;

                }
                if (_currentAnimation.IsComplete)
                {
                    _currentState = AIState.Chase;

                }
            }
        }

        private void SetAnimation(Animation animation)
        {
            if (_currentAnimation != animation)
            {
                _currentAnimation = animation;
                _currentAnimation.Reset();
            }
        }

        private void Attack()
        {
            if (_attackCooldown <= 0 && _player.IsAlive)
            {
                if (Vector2.Distance(Position, _player.Position) < AttackRange * 1.5f)
                {
                    _player.TakeDamage(10);
                    _attackCooldown = AttackCooldownTime;
                }
            }
        }

        private Rectangle CalculateAttackHitbox()
        {
            int x = _isFacingRight
                ? (int)Position.X + _currentAnimation.FrameWidth / 2
                : (int)Position.X - _currentAnimation.FrameWidth / 2;
            return new Rectangle(x, (int)Position.Y - _currentAnimation.FrameHeight / 2,
                               _currentAnimation.FrameWidth, _currentAnimation.FrameHeight);
        }

        //public void CheckPlayerHit(Player player)
        //{
        //    if (IsAttacking && _currentAnimation.CurrentFrameIndex == 3) // На нужном кадре анимации
        //    {
        //        if (AttackHitbox.Intersects(player.Bounds))
        //        {
        //            player.TakeDamage(10); // Урон от противника
        //        }
        //    }
        //}

        // Визуализация в зависимости от типа
        public void Draw(SpriteBatch spriteBatch)
        {
            SpriteEffects flip = _isFacingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            // Разные цвета для разных типов врагов
            Color baseColor = Type switch
            {
                EnemyType.Bandit => Color.White,
                EnemyType.Ninja => Color.White,
                //EnemyType.Boss => Color.Red,
                _ => Color.White
            };

            Color drawColor = _isHit ? Color.White :
                (!IsAlive ? new Color(150, 150, 150, 200) : baseColor);

            spriteBatch.Draw(
                _currentAnimation.Texture,
                Position,
                _currentAnimation.CurrentFrame,
                drawColor,
                0f,
                new Vector2(_currentAnimation.FrameWidth / 2, _currentAnimation.FrameHeight / 2),
                1f,
                flip,
                0f);

            DrawHealthBar(spriteBatch);
        }

    }
}