// Player.cs
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MiNET.Effects;
using System;
using System.Collections.Generic;
using System.Linq;
using TestsForGame.MonoGameMenu;

namespace MonoGameMenu
{
    public abstract class Player
    {
        // Основные свойства
        public bool IsAlive { get; protected set; } = true;
        public int Health { get; protected set; }
        public int MaxHealth { get; protected set; }
        public int Damage { get; protected set; }
        public float AttackRange { get; protected set; }
        public float AttackCooldownTime { get; protected set; }
        public float MoveSpeed { get; protected set; }
        public Vector2 Position { get; set; }
        public float Speed { get; }
        public Rectangle MapBounds { get; set; } = new Rectangle(0, 0, 1650, 1200);
        public PlayerType CharacterType { get; }

        // Анимации
        protected Dictionary<string, Animation> Animations;
        protected Animation CurrentAnimation;
        protected bool IsFacingRight = true;

        // Состояния
        private float _attackCooldown;
        private float _invincibilityTimer;
        private const float InvincibilityDuration = 1.0f;
        private bool _isHit;

        // Полоса здоровья
        private Texture2D _healthBarBg, _healthBarFg;
        private float _displayedHealth;
        private const float HealthLerpSpeed = 0.1f;
        private const int HealthBarWidth = 60, HealthBarHeight = 8, HealthBarOffsetY = -40;

        // Добавляем параметры атаки
        protected float _attackAnimationTime = 0.3f;
        protected int _attackDamageFrame = 3; // На каком кадре наносится урон
        protected bool _isAttacking = false;

        private bool _attackKeyReleased = true;

        // Обновленный метод получения хитбокса
        protected bool _canDealDamage = false;
        protected virtual Rectangle GetAttackHitbox()
        {
            int width = 60;
            int height = 80;
            int x = IsFacingRight ? (int)Position.X + 40 : (int)Position.X - width - 40;
            int y = (int)Position.Y - height / 2;

            return new Rectangle(x, y, width, height);
        }

        protected Player(PlayerType type, Dictionary<string, Animation> animations, Vector2 position, float speed)
        {
            CharacterType = type;
            Animations = animations;
            Position = position;
            Speed = speed;
            CurrentAnimation = animations["Idle"];
        }
        public void InitializeHealthBar(GraphicsDevice graphicsDevice)
        {
            _healthBarBg = new Texture2D(graphicsDevice, 1, 1);
            _healthBarBg.SetData(new[] { Color.Black });
            _healthBarFg = new Texture2D(graphicsDevice, 1, 1);
            _healthBarFg.SetData(new[] { Color.Red });
        }

        public virtual void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Обновление таймеров
            if (_invincibilityTimer > 0) _invincibilityTimer -= deltaTime;
            if (_attackCooldown > 0) _attackCooldown -= deltaTime;

            // Обработка анимации атаки
            if (_isAttacking)
            {
                CurrentAnimation.Update(gameTime);

                // Проверяем нужный кадр для нанесения урона
                _canDealDamage = CurrentAnimation.CurrentFrameIndex == _attackDamageFrame;

                // Завершение атаки
                if (CurrentAnimation.IsComplete)
                {
                    _isAttacking = false;
                    _canDealDamage = false;
                    CurrentAnimation = Animations["Idle"];
                }
            }
            else
            {
                // Обновление других анимаций
                CurrentAnimation.Update(gameTime);
            }

            HandleInput(deltaTime);
            _displayedHealth = MathHelper.Lerp(_displayedHealth, Health, HealthLerpSpeed);
        }
        protected virtual void HandleInput(float deltaTime)
        {
            var keyboardState = Keyboard.GetState();

            // Обработка атаки
            if (keyboardState.IsKeyDown(Keys.LeftControl))
            {
            if (_attackKeyReleased && !_isAttacking && _attackCooldown <= 0)
                {
                    StartAttack();
                   _attackKeyReleased = false;
                }
            }
            else
            {
                _attackKeyReleased = true;
            }

                // Блокируем движение во время атаки
           if (_isAttacking) return;

            Vector2 direction = Vector2.Zero;

            if (keyboardState.IsKeyDown(Keys.W)) direction.Y -= 5;
            if (keyboardState.IsKeyDown(Keys.S)) direction.Y += 5;
            if (keyboardState.IsKeyDown(Keys.A)) { direction.X -= 5; IsFacingRight = false; }
            if (keyboardState.IsKeyDown(Keys.D)) { direction.X += 5; IsFacingRight = true; }

            if (direction != Vector2.Zero)
            {
                direction.Normalize();
                Position += direction * MoveSpeed * deltaTime;
                ClampPosition();
                CurrentAnimation = Animations["Run"];
            }
            else
            {
                CurrentAnimation = Animations["Idle"];
            }
        }

        protected void StartAttack()
        {
            // Проверяем, что анимация атаки существует
            if (Animations.ContainsKey("Attack"))
            {
                _isAttacking = true;
                CurrentAnimation = Animations["Attack"];
                CurrentAnimation.Reset();
                _attackCooldown = AttackCooldownTime;
                _canDealDamage = false;
            }
        }


        public void TakeDamage(int damage)
        {
            if (_invincibilityTimer > 0 || !IsAlive) return;

            Health = Math.Max(0, Health - damage);
            _invincibilityTimer = InvincibilityDuration;
            _isHit = true;

            if (Health <= 0) Die();
        }

        protected virtual void Die()
        {
            IsAlive = false;
            CurrentAnimation = Animations["Death"];
            CurrentAnimation.Reset();
        }

        public void CheckAttack(List<Opponent> opponents)
        {
            if (!_canDealDamage) return;

            var hitbox = GetAttackHitbox();
            foreach (var enemy in opponents.Where(e => e.IsAlive))
            {
                if (hitbox.Intersects(enemy.Bounds))
                {
                    enemy.TakeDamage(Damage);
                    _canDealDamage = false; // Урон наносится только один раз за атаку
                }
            }

        }

        private void ClampPosition()
        {
            Position = new Vector2(
                MathHelper.Clamp(Position.X, MapBounds.Left, MapBounds.Right),
                MathHelper.Clamp(Position.Y, MapBounds.Top, MapBounds.Bottom)
            );
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            var flip = IsFacingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            var color = _isHit ? Color.Red : Color.White;

            spriteBatch.Draw(
                CurrentAnimation.Texture,
                Position,
                CurrentAnimation.CurrentFrame,
                color,
                0f,
                new Vector2(CurrentAnimation.FrameWidth / 2, CurrentAnimation.FrameHeight / 2),
                1f,
                flip,
                0f);
        }

        public void DrawHealthBar(SpriteBatch spriteBatch)
        {
            if (!IsAlive) return;

            Vector2 pos = new Vector2(Position.X - HealthBarWidth / 2, Position.Y + HealthBarOffsetY);
            spriteBatch.Draw(_healthBarBg, new Rectangle((int)pos.X, (int)pos.Y, HealthBarWidth, HealthBarHeight), Color.Black);

            int healthWidth = (int)(HealthBarWidth * (_displayedHealth / MaxHealth));
            spriteBatch.Draw(_healthBarFg, new Rectangle((int)pos.X, (int)pos.Y, healthWidth, HealthBarHeight - 2), Color.Red);
        }
        public void DrawAttackRange(SpriteBatch spriteBatch)
        {
            if (CurrentAnimation == Animations["Attack"])
            {
                var hitbox = GetAttackHitbox();
                var texture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
                texture.SetData(new[] { Color.Red * 0.3f });

                spriteBatch.Draw(texture, hitbox, Color.Red * 0.3f);
            }
        }
    }

    public class Warrior : Player
    {
        public Warrior(Dictionary<string, Animation> animations, Vector2 position)
            : base(PlayerType.Warrior, animations, position, 200f)
        {
            Health = MaxHealth = 150;
            Damage = 30;
            AttackRange = 80f;
            AttackCooldownTime = 1.2f;
            MoveSpeed = 200f;
            _attackAnimationTime = 0.25f; // Более медленная анимация атаки
            _attackDamageFrame = 4; // Удар на 4 кадре
        }
    }


    public class Archer : Player
    {
        public Archer(Dictionary<string, Animation> animations, Vector2 position)
            : base(PlayerType.Archer, animations, position, 250f)
        {
            Health = MaxHealth = 100;
            Damage = 20;
            AttackRange = 120f; // Большая дальность, но без снарядов
            AttackCooldownTime = 0.8f;
            MoveSpeed = 250f;
            _attackAnimationTime = 0.25f; // Более быстрая анимация атаки
            _attackDamageFrame = 2; // Удар на 2 кадре

        }
    }
}