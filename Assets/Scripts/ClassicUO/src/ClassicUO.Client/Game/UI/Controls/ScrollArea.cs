// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal enum ScrollbarBehaviour
    {
        ShowWhenDataExceedFromView,
        ShowAlways
    }

    internal class ScrollArea : Control
    {
        private bool _isNormalScroll;
        private readonly ScrollBarBase _scrollBar;
        private bool _isTouchDragging;
        private Point _touchStartPos;
        private int _lastTouchY;
        private bool _hasTouchMoved;

        public ScrollArea
        (
            int x,
            int y,
            int w,
            int h,
            bool normalScrollbar,
            int scroll_max_height = -1
        )
        {
            X = x;
            Y = y;
            Width = w;
            Height = h;
            _isNormalScroll = normalScrollbar;

            if (normalScrollbar)
            {
                _scrollBar = new ScrollBar(Width - 14, 0, Height);
            }
            else
            {
                _scrollBar = new ScrollFlag
                {
                    X = Width - 19, Height = h
                };

                Width += 15;
            }

            ScrollMaxHeight = scroll_max_height;

            _scrollBar.MinValue = 0;
            _scrollBar.MaxValue = scroll_max_height >= 0 ? scroll_max_height : Height;
            _scrollBar.Parent = this;

            AcceptMouseInput = true;
            WantUpdateSize = false;
            CanMove = false;
            ScrollbarBehaviour = ScrollbarBehaviour.ShowWhenDataExceedFromView;
        }


        public int ScrollMaxHeight { get; set; } = -1;
        public ScrollbarBehaviour ScrollbarBehaviour { get; set; }
        public int ScrollValue => _scrollBar.Value;
        public int ScrollMinValue => _scrollBar.MinValue;
        public int ScrollMaxValue => _scrollBar.MaxValue;


        public Rectangle ScissorRectangle;


        public override void Update()
        {
            base.Update();

            CalculateScrollBarMaxValue();

            if (ScrollbarBehaviour == ScrollbarBehaviour.ShowAlways)
            {
                _scrollBar.IsVisible = true;
            }
            else if (ScrollbarBehaviour == ScrollbarBehaviour.ShowWhenDataExceedFromView)
            {
                _scrollBar.IsVisible = _scrollBar.MaxValue > _scrollBar.MinValue;
            }

            // Тач-скролл пальцем (finger drag scrolling) для мобильных устройств
            if (_scrollBar.MaxValue > _scrollBar.MinValue)
            {
                Point mousePos = Mouse.Position;
                int screenX = ScreenCoordinateX;
                int screenY = ScreenCoordinateY;
                bool isInsideArea = mousePos.X >= screenX && mousePos.X < screenX + Width &&
                                    mousePos.Y >= screenY && mousePos.Y < screenY + Height;

                if (Mouse.LButtonPressed)
                {
                    if (!_isTouchDragging && isInsideArea)
                    {
                        // Не перехватываем, если нажали точно на ползунок или стрелки самого скроллбара
                        int relX = mousePos.X - screenX;
                        if (!_scrollBar.IsVisible || relX < Width - 20)
                        {
                            _isTouchDragging = true;
                            _touchStartPos = mousePos;
                            _lastTouchY = mousePos.Y;
                            _hasTouchMoved = false;
                        }
                    }
                    else if (_isTouchDragging)
                    {
                        if (!_hasTouchMoved && (Math.Abs(mousePos.Y - _touchStartPos.Y) > 5 || Math.Abs(mousePos.X - _touchStartPos.X) > 5))
                        {
                            _hasTouchMoved = true;
                        }

                        if (_hasTouchMoved)
                        {
                            int deltaY = mousePos.Y - _lastTouchY;
                            if (deltaY != 0)
                            {
                                _scrollBar.Value -= deltaY;
                                _lastTouchY = mousePos.Y;
                            }
                        }
                    }
                }
                else
                {
                    _isTouchDragging = false;
                    _hasTouchMoved = false;
                }
            }
            else
            {
                _isTouchDragging = false;
                _hasTouchMoved = false;
            }
        }

        public void Scroll(bool isup)
        {
            if (isup)
            {
                _scrollBar.Value -= _scrollBar.ScrollStep;
            }
            else
            {
                _scrollBar.Value += _scrollBar.ScrollStep;
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            ScrollBarBase scrollbar = (ScrollBarBase) Children[0];
            scrollbar.Draw(batcher, x + scrollbar.X, y + scrollbar.Y);

            if (batcher.ClipBegin(x + ScissorRectangle.X, y + ScissorRectangle.Y, Width - 14 + ScissorRectangle.Width, Height + ScissorRectangle.Height))
            {
                for (int i = 1; i < Children.Count; i++)
                {
                    Control child = Children[i];

                    if (!child.IsVisible)
                    {
                        continue;
                    }

                    int finalY = y + child.Y - scrollbar.Value + ScissorRectangle.Y;

                    child.Draw(batcher, x + child.X, finalY);
                }

                batcher.ClipEnd();
            }

            return true;
        }


        protected override void OnMouseWheel(MouseEventType delta)
        {
            switch (delta)
            {
                case MouseEventType.WheelScrollUp:
                    _scrollBar.Value -= _scrollBar.ScrollStep;

                    break;

                case MouseEventType.WheelScrollDown:
                    _scrollBar.Value += _scrollBar.ScrollStep;

                    break;
            }
        }

        public override void Clear()
        {
            for (int i = 1; i < Children.Count; i++)
            {
                Children[i].Dispose();
            }
        }

        private void CalculateScrollBarMaxValue()
        {
            _scrollBar.Height = ScrollMaxHeight >= 0 ? ScrollMaxHeight : Height;
            bool maxValue = _scrollBar.Value == _scrollBar.MaxValue && _scrollBar.MaxValue != 0;

            int startX = 0, startY = 0, endX = 0, endY = 0;

            for (int i = 1; i < Children.Count; i++)
            {
                Control c = Children[i];

                if (c.IsVisible && !c.IsDisposed)
                {
                    if (c.X < startX)
                    {
                        startX = c.X;
                    }

                    if (c.Y < startY)
                    {
                        startY = c.Y;
                    }

                    if (c.Bounds.Right > endX)
                    {
                        endX = c.Bounds.Right;
                    }

                    if (c.Bounds.Bottom > endY)
                    {
                        endY = c.Bounds.Bottom;
                    }
                }
            }

            int width = Math.Abs(startX) + Math.Abs(endX);
            int height = Math.Abs(startY) + Math.Abs(endY) - _scrollBar.Height;
            height = Math.Max(0, height - (-ScissorRectangle.Y + ScissorRectangle.Height));

            if (height > 0)
            {
                _scrollBar.MaxValue = height;

                if (maxValue)
                {
                    _scrollBar.Value = _scrollBar.MaxValue;
                }
            }
            else
            {
                _scrollBar.Value = _scrollBar.MaxValue = 0;
            }

            _scrollBar.UpdateOffset(0, Offset.Y);

            for (int i = 1; i < Children.Count; i++)
            {
                Children[i].UpdateOffset(0, -_scrollBar.Value + ScissorRectangle.Y);
            }
        }
    }
}