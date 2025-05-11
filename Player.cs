using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestsForGame
{
    using global::MonoGameMenu;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Microsoft.Xna.Framework.Input;

    namespace MonoGameMenu
    {
        public enum PlayerState
        {
            Idle,       // Стояние
            Running,    // Бег
            Attacking,  // Атака
            Dead        // Смерть
        }

        public class Player
        {
            public bool IsAlive { get; private set; } = true;
            public int Health { get; private set; } = 100;
            public int MaxHealth { get; private set; } = 100;


            public bool IsAttacking => _currentAnimation == _attackAnimation && !_currentAnimation.IsComplete;

            private float _attackCooldown;
            public Rectangle AttackHitbox
            {
                get
                {
                    int x = _isFacingRight ?
                        (int)Position.X + 30 :
                        (int)Position.X - 70;
                    return new Rectangle(x, (int)Position.Y - 30, 40, 60);
                }
            }
            public Rectangle MapBounds { get; set; } = new Rectangle(0, 0, 1650, 1200); // Края для ограничения персонажа
            public Vector2 Position { get; set; }
            public float Speed { get; set; }
            public PlayerState State { get; private set; }


            public Rectangle Bounds { get; set; }


            // Текстуры для полосы здоровья
            private Texture2D _healthBarBg;
            private Texture2D _healthBarFg;

            private float _displayedHealth;
            private const float HealthLerpSpeed = 0.1f;

            // Размеры полосы здоровья
            private const int HealthBarWidth = 60;
            private const int HealthBarHeight = 8;
            private const int HealthBarOffsetY = -40; // Смещение вверх от позиции игрока

            private Animation _idleAnimation;
            private Animation _runAnimation;
            private Animation _attackAnimation;
            private Animation _deathAnimation;
            private Animation _currentAnimation;

            private bool _isFacingRight = true;

            private float _invincibilityTimer = 0f;
            private const float INVINCIBILITY_DURATION = 1.0f; // 1 сек неуязвимости после удара
            private float _hitEffectTimer = 0f;
            private bool _isHit = false;


            public Player(Vector2 position, float speed,
                         Animation idleAnimation, Animation runAnimation,
                         Animation attackAnimation, Animation deathAnimation)
            {
                Position = position;
                Speed = speed;
                _idleAnimation = idleAnimation;
                _runAnimation = runAnimation;
                _attackAnimation = attackAnimation;
                _deathAnimation = deathAnimation;

                _currentAnimation = _idleAnimation;
                State = PlayerState.Idle;

            }

            public void TakeDamage(int damage)
            {
                if (_invincibilityTimer > 0 || !IsAlive)
                    return;

                Health = Math.Max(0, Health - damage);
                _invincibilityTimer = INVINCIBILITY_DURATION;
                _isHit = true;
                _hitEffectTimer = 0.2f; // Длительность эффекта мигания

                if (Health <= 0)
                {
                    Die();
                }
            }


            // Загрузка движения персонажа
            public void Update(GameTime gameTime)
            {
                float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

                // Обновляем таймер неуязвимости
                if (_invincibilityTimer > 0)
                {
                    _invincibilityTimer -= deltaTime;
                }

                // Обновляем таймер эффекта удара
                if (_hitEffectTimer > 0)
                {
                    _hitEffectTimer -= deltaTime;
                }
                else
                {
                    _isHit = false;
                }

                // Остальная логика обновления
                switch (State)
                {
                    case PlayerState.Idle:
                        HandleIdleState(deltaTime);
                        break;
                    case PlayerState.Running:
                        HandleRunningState(deltaTime);
                        break;
                    case PlayerState.Attacking:
                        HandleAttackingState(deltaTime);
                        break;
                    case PlayerState.Dead:
                        break;
                }

                _displayedHealth = MathHelper.Lerp(_displayedHealth, Health, HealthLerpSpeed);

                _currentAnimation.Update(gameTime);

                if (Keyboard.GetState().IsKeyDown(Keys.Space) && !IsAttacking && _attackCooldown <= 0)
                {
                    _currentAnimation = _attackAnimation;
                    _currentAnimation.Reset();
                    _attackCooldown = 0.1f;
                }

                if (_attackCooldown > 0)
                    _attackCooldown -= deltaTime;
            }

            private void HandleIdleState(float deltaTime)
            {
                var keyboardState = Keyboard.GetState();

                if (keyboardState.IsKeyDown(Keys.A) || keyboardState.IsKeyDown(Keys.D) ||
                    keyboardState.IsKeyDown(Keys.W) || keyboardState.IsKeyDown(Keys.S))
                {
                    ChangeState(PlayerState.Running);
                    return;
                }

                if (keyboardState.IsKeyDown(Keys.Space) && _attackCooldown <= 0)
                {
                    ChangeState(PlayerState.Attacking);
                    return;
                }


            }

            private void HandleRunningState(float deltaTime)
            {
                var keyboardState = Keyboard.GetState();
                bool isMoving = false;
                Vector2 direction = Vector2.Zero;

                if (keyboardState.IsKeyDown(Keys.W)) { direction.Y -= 1; isMoving = true; }
                if (keyboardState.IsKeyDown(Keys.S)) { direction.Y += 1; isMoving = true; }
                if (keyboardState.IsKeyDown(Keys.A)) { direction.X -= 1; _isFacingRight = false; isMoving = true; }
                if (keyboardState.IsKeyDown(Keys.D)) { direction.X += 1; _isFacingRight = true; isMoving = true; }

                if (direction != Vector2.Zero)
                    direction.Normalize();

                Position += direction * Speed * deltaTime;

                if (keyboardState.IsKeyDown(Keys.Space) && _attackCooldown <= 0)
                {
                    ChangeState(PlayerState.Attacking);
                    return;
                }

                if (!isMoving)
                {
                    ChangeState(PlayerState.Idle);
                }

                // Применяем движение с учетом границ
                Vector2 newPosition = Position + direction * Speed * deltaTime;

                // Ограничиваем позицию по X
                newPosition.X = Math.Clamp(
                    newPosition.X,
                    MapBounds.X + _currentAnimation.FrameWidth / 2,
                    MapBounds.X + MapBounds.Width - _currentAnimation.FrameWidth / 2);

                // Ограничиваем позицию по Y
                newPosition.Y = Math.Clamp(
                    newPosition.Y,
                    MapBounds.Y + _currentAnimation.FrameHeight / 2,
                    MapBounds.Y + MapBounds.Height - _currentAnimation.FrameHeight / 2);

                Position = newPosition;
            }

            private void HandleAttackingState(float deltaTime)
            {
                if (_currentAnimation.IsComplete)
                {
                    ChangeState(PlayerState.Idle);
                    _attackCooldown = 0.1f;
                }
            }

            private void Die()
            {
                if (State != PlayerState.Dead)
                {
                    ChangeState(PlayerState.Dead);
                    IsAlive = false;
                }
            }

            private void ChangeState(PlayerState newState)
            {
                if (State == newState) return;

                State = newState;

                switch (newState)
                {
                    case PlayerState.Idle:
                        _currentAnimation = _idleAnimation;
                        break;
                    case PlayerState.Running:
                        _currentAnimation = _runAnimation;
                        break;
                    case PlayerState.Attacking:
                        _currentAnimation = _attackAnimation;
                        break;
                    case PlayerState.Dead:
                        _currentAnimation = _deathAnimation;
                        break;
                }

                _currentAnimation.Reset();
            }

            public void CheckAttack(List<Opponent> opponents)
            {
                if (!IsAttacking) return;

                // Наносим урон на определенном кадре анимации
                if (_currentAnimation.CurrentFrameIndex == 3)
                {
                    foreach (var enemy in opponents.Where(e => e.IsAlive))
                    {
                        if (AttackHitbox.Intersects(enemy.Bounds))
                        {
                            enemy.TakeDamage(20);
                        }
                    }
                }
            }

            public void InitializeHealthBar(GraphicsDevice graphicsDevice)
            {
                // Создаем текстуры для полосы здоровья
                _healthBarBg = new Texture2D(graphicsDevice, 1, 1);
                _healthBarBg.SetData(new[] { Color.Black });

                _healthBarFg = new Texture2D(graphicsDevice, 1, 1);
                _healthBarFg.SetData(new[] { Color.Red });
            }
            public void DrawHealthBar(SpriteBatch spriteBatch)
            {
                if (!IsAlive) return;

                // Позиция полосы здоровья (над головой игрока)
                Vector2 healthBarPos = new Vector2(
                    Position.X - HealthBarWidth / 2,
                    Position.Y + HealthBarOffsetY
                );

                // Фон полосы здоровья (полная длина)
                spriteBatch.Draw(
                    _healthBarBg,
                    new Rectangle((int)healthBarPos.X, (int)healthBarPos.Y,
                                 HealthBarWidth, HealthBarHeight),
                    Color.Black);

                // Текущее здоровье (рассчитываем ширину)
                int currentHealthWidth = (int)(HealthBarWidth * ((float)_displayedHealth / MaxHealth));
                spriteBatch.Draw(
                    _healthBarFg,
                    new Rectangle((int)healthBarPos.X, (int)healthBarPos.Y,
                                 currentHealthWidth, HealthBarHeight - 2),
                    Color.Red);
            }

            public void Draw(SpriteBatch spriteBatch)
            {
                SpriteEffects flip = _isFacingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                Color drawColor = _isHit ? Color.Red : Color.White; // Мигание красным при ударе

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
            }
        }
    }
}