using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MiNET.Effects;
using System;
using TestsForGame.MonoGameMenu;

namespace MonoGameMenu
{
    public class Opponent
    {
        public bool IsDeathAnimationComplete  { get; private set; } = false;
        public bool IsAttacing => _currentState == AIState.Attack;
        public Rectangle AttackHitbox => CalculateAttackHitbox();

        private const float AttackCooldownTime = 1.5f;
        private const float InvincibilityDuration = 0.5f;
        private const float AttackFrameDelay = 0.1f; // Задержка между кадрами атаки

        public int MaxHealth { get; private set; } = 100;
        public int Health { get; private set; } = 100;
        public bool IsAlive => Health > 0;

        // Индикатор здоровья
        private Texture2D _healthBarBg;
        private Texture2D _healthBarFg;

        private float _displayedHealth;
        private const float HealthLerpSpeed = 0.1f;

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

        // Параметры существа
        public Vector2 Position { get; private set; }
        public bool IsActive { get; private set; } = true;
        public float MoveSpeed { get; set; } = 150f;
        public float DetectionRadius { get; set; } = 600f;
        public float AttackRange { get; set; } = 50f;

        // Состояния ИИ
        private enum AIState
        {
            Idle,
            Chase,
            Attack,
            Dead
        }
        private AIState _currentState = AIState.Idle;

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

        public Opponent(Vector2 position, Player player,
                       Animation idle, Animation run,
                       Animation attack, Animation death)
        {
            Position = position;
            _player = player ?? throw new ArgumentNullException(nameof(player));

            _idleAnimation = idle ?? throw new ArgumentNullException(nameof(idle));
            _runAnimation = run ?? throw new ArgumentNullException(nameof(run));
            _attackAnimation = attack ?? throw new ArgumentNullException(nameof(attack));
            _deathAnimation = death ?? throw new ArgumentNullException(nameof(death));

            _currentAnimation = _idleAnimation;

        }

        
        public void Initialize(GraphicsDevice graphicsDevice)
        {
            Health = MaxHealth;

            // Создаем текстуры для полосы здоровья
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
        private void Die()
        {
            _currentState = AIState.Dead;
            SetAnimation(_deathAnimation);
        }

        private void DrawHealthBar(SpriteBatch spriteBatch)
        {
            if (!IsAlive) return;

            int healthWidth = (int)(60 * ((float)_displayedHealth / MaxHealth));
            Vector2 healthBarPos = new Vector2(Position.X - 30, Position.Y - 40);

            spriteBatch.Draw(_healthBarBg,
                new Rectangle((int)healthBarPos.X, (int)healthBarPos.Y, 60, 8),
                Color.Black);

            spriteBatch.Draw(_healthBarFg,
                new Rectangle((int)healthBarPos.X, (int)healthBarPos.Y, healthWidth, 6),
                Color.Red);
        }

        //public void DrawHealthBar(SpriteBatch spriteBatch)
        //{
        //    if (!IsAlive) return;

        //    // Позиция полосы здоровья над головой
        //    Vector2 healthBarPos = new Vector2(
        //        Position.X - 30,
        //        Position.Y - 40);

        //    // Фон полосы здоровья
        //    spriteBatch.Draw(_healthBarBg,
        //        new Rectangle((int)healthBarPos.X, (int)healthBarPos.Y, 60, 8),
        //        Color.Black);

        //    // Текущее здоровье
        //    int healthWidth = (int)(60 * ((float)Health / MaxHealth));
        //    spriteBatch.Draw(_healthBarFg,
        //        new Rectangle((int)healthBarPos.X, (int)healthBarPos.Y, healthWidth, 6),
        //        Color.Red);
        //}

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
            if (_attackFrameTimer <= 0)
            {
                _currentAttackFrame++;
                _attackFrameTimer = AttackFrameDelay;


                if (_currentAttackFrame >= _attackAnimation.FrameCount / 5)
                {
                    Attack();
                }

                if (_currentAttackFrame >= _attackAnimation.FrameCount)
                {
                    if (distanceToPlayer > AttackRange * 1.2f)
                        _currentState = AIState.Chase;
                    else
                        _currentAttackFrame = 0;
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

        public void Draw(SpriteBatch spriteBatch)
        {
            SpriteEffects flip = _isFacingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            float deathAlpha = IsDeathAnimationComplete ? 0.7f : 1f;
            Color drawColor = _isHit ? Color.Red :
                            (!IsAlive ? new Color(150, 150, 150, (int)(255 * deathAlpha)) : Color.White);

            spriteBatch.Draw(
                _currentAnimation.Texture,
                Position,
                _currentAnimation.CurrentFrame,
                Color.White,
                0f,
                new Vector2(_currentAnimation.FrameWidth / 2, _currentAnimation.FrameHeight / 2),
                1f,
                flip,
                0f);

            if (!_deathAnimationComplete)
            {
                DrawHealthBar(spriteBatch);
            }
        }

    }
}