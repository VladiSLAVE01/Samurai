using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestsForGame
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    namespace MonoGameMenu
    {
        public class Animation
        {
            public Texture2D Texture { get; }
            public Rectangle[] Frames { get; }
            public float FrameTime { get; }
            public bool IsLooping { get; }
            public bool IsComplete { get; private set; }

            public int CurrentFrameIndex { get; private set; }
            public Rectangle CurrentFrame => Frames[CurrentFrameIndex];
            public int FrameWidth { get; }  //свойство с явным заданием ширины
            public int FrameHeight { get; } // высота кадра
            public int FrameCount { get; internal set; }

            private float _timer;

            // Новый конструктор с расширенными параметрами
            public Animation(Texture2D texture,
                           int frameWidth, int frameHeight,
                           int frameCount,
                           float frameTime,
                           bool isLooping = false,
                           int startX = 0,
                           int startY = 0,
                           int paddingX = 0,
                           int paddingY = 0)
            {
                Texture = texture;
                FrameTime = frameTime;
                IsLooping = isLooping;
                FrameWidth = frameWidth;
                FrameHeight = frameHeight;

                // Создание массива кадров с учетом новых параметров
                Frames = new Rectangle[frameCount];

                for (int i = 0; i < frameCount; i++)
                {
                    // Расчет позиции кадра с учетом отступов
                    int x = startX + i * (frameWidth + paddingX);
                    int y = startY;

                    // Проверка, чтобы не выйти за границы текстуры
                    if (x + frameWidth > texture.Width)
                    {
                        throw new ArgumentException($"Кадр {i} выходит за границы текстуры по ширине");
                    }
                    
                    //if (y + frameHeight > texture.Height)
                    //{
                    //    throw new ArgumentException($"Кадр {i} выходит за границы текстуры по высоте");
                    //}

                    Frames[i] = new Rectangle(x, y, frameWidth, frameHeight);
                }
            }

            
            public Animation(Texture2D texture, int frameCount, float frameTime, bool isLooping)
                : this(texture,
                      texture.Width / frameCount, // Автоматический расчет ширины
                      texture.Height,             // Полная высота текстуры
                      frameCount,
                      frameTime,
                      isLooping)
            {
            }

            public void Update(GameTime gameTime)
            {
                if (IsComplete) return;

                _timer += (float)gameTime.ElapsedGameTime.TotalSeconds;

                if (_timer >= FrameTime)
                {
                    _timer = 0f;
                    CurrentFrameIndex++;

                    if (CurrentFrameIndex >= Frames.Length)
                    {
                        if (IsLooping)
                        {
                            CurrentFrameIndex = 0;
                        }
                        else
                        {
                            CurrentFrameIndex = Frames.Length - 1;
                            IsComplete = true;
                        }
                    }
                }
            }

            public void Reset()
            {
                CurrentFrameIndex = 0;
                _timer = 0f;
                IsComplete = false;
            }
        }

    }
}
//    public class Animation
//    {
//        public Texture2D Texture { get; }
//        public Rectangle[] Frames { get; }
//        public float FrameTime { get; }
//        public bool IsLooping { get; }
//        public bool IsComplete { get; private set; }

//        public int CurrentFrameIndex { get; private set; }
//        public Rectangle CurrentFrame => Frames[CurrentFrameIndex];
//        public int FrameWidth => Frames[0].Width;
//        public int FrameHeight => Frames[0].Height;

//        private float _timer;


//        public Animation(Texture2D texture, int frameCount, float frameTime, bool isLooping)
//        {
//            Texture = texture;
//            FrameTime = frameTime;
//            IsLooping = isLooping;

//            // Разделение спрайтшита на кадры
//            int frameWidth = texture.Width / frameCount;
//            Frames = new Rectangle[frameCount];

//            for (int i = 0; i < frameCount; i++)
//            {
//                Frames[i] = new Rectangle(i * frameWidth, 0, frameWidth, texture.Height);
//            }
//        }

//        public void Update(GameTime gameTime)
//        {
//            if (IsComplete) return;

//            _timer += (float)gameTime.ElapsedGameTime.TotalSeconds;

//            if (_timer >= FrameTime)
//            {
//                _timer = 0f;
//                CurrentFrameIndex++;

//                if (CurrentFrameIndex >= Frames.Length)
//                {
//                    if (IsLooping)
//                    {
//                        CurrentFrameIndex = 0;
//                    }
//                    else
//                    {
//                        CurrentFrameIndex = Frames.Length - 1;
//                        IsComplete = true;
//                    }
//                }
//            }
//        }

//        public void Reset()
//        {
//            CurrentFrameIndex = 0;
//            _timer = 0f;
//            IsComplete = false;
//        }
//    }
//}
//}
