using System;
using System.Windows;
using System.Windows.Controls.Ribbon;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace GamelistManager.classes.services
{
    public class RibbonAnimationService
    {
        private readonly Ribbon _ribbon;
        private readonly FrameworkElement _hotZone;

        public RibbonAnimationService(Ribbon ribbon, FrameworkElement hotZone)
        {
            _ribbon = ribbon;
            _hotZone = hotZone;
        }

        public void ShowWithAnimation()
        {
            _ribbon.Visibility = Visibility.Visible;

            // Slide down animation
            var slideDown = new DoubleAnimation
            {
                From = -_ribbon.ActualHeight,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            // Fade in animation
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(250)
            };

            var transform = new TranslateTransform();
            _ribbon.RenderTransform = transform;

            transform.BeginAnimation(TranslateTransform.YProperty, slideDown);
            _ribbon.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        }

        public void HideWithAnimation()
        {
            // Slide up animation
            var slideUp = new DoubleAnimation
            {
                From = 0,
                To = -_ribbon.ActualHeight,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            // Fade out animation
            var fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(200)
            };

            slideUp.Completed += (s, e) =>
            {
                _ribbon.Visibility = Visibility.Collapsed;
                _ribbon.RenderTransform = null;
            };

            var transform = _ribbon.RenderTransform as TranslateTransform ?? new TranslateTransform();
            _ribbon.RenderTransform = transform;

            transform.BeginAnimation(TranslateTransform.YProperty, slideUp);
            _ribbon.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }

        public void EnableAutoHide(string xmlFilename)
        {
            _hotZone.MouseEnter += HotZone_MouseEnter;
            _ribbon.MouseLeave += Ribbon_MouseLeave;

            // Auto-hide on startup if file is loaded
            if (!string.IsNullOrEmpty(xmlFilename))
            {
                HideWithAnimation();
            }
        }

        public void DisableAutoHide()
        {
            _hotZone.MouseEnter -= HotZone_MouseEnter;
            _ribbon.MouseLeave -= Ribbon_MouseLeave;
        }

        private void HotZone_MouseEnter(object? sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_ribbon.Visibility == Visibility.Collapsed)
            {
                ShowWithAnimation();
            }
        }

        private void Ribbon_MouseLeave(object? sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_ribbon.IsMouseOver)
            {
                HideWithAnimation();
            }
        }
    }
}