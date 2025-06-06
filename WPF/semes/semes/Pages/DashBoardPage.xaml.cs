﻿using semes.Services;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace semes
{
    public partial class DashboardPage : Page
    {
        private readonly DashboardService _dashboardService = new();

        public DashboardPage()
        {
            InitializeComponent();
            Loaded += (s, e) => AnimatePageFadeIn();
            LoadDashboardData(DateTime.Today);
        }

        private void AnimatePageFadeIn()
        {
            var fade = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.5))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            this.BeginAnimation(Page.OpacityProperty, fade);
        }

        private async void DefectCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DefectCalendar.SelectedDate is DateTime selectedDate)
            {
                LoadDashboardData(selectedDate);
            }
        }

        private async void LoadDashboardData(DateTime date)
        {
            try
            {
                var (total, good, defect) = await _dashboardService.GetDashboardStatsByDateAsync(date);
                double todayRate = total > 0 ? (double)defect / total * 100 : 0;

                double overallRate = await _dashboardService.GetOverallDefectRateAsync();
                double yesterdayRate = await _dashboardService.GetDefectRateByDateAsync(date.AddDays(-1));
                double average7dayRate = await _dashboardService.Get7DayAverageDefectRateAsync();

                검사수Text.Text = $"{total} 건";
                양품수Text.Text = $"{good} 건";
                불량수Text.Text = $"{defect} 건";

                AnimateDefectDetectionChart(todayRate);
                AnimateDefectRateChart(overallRate);

                지난7일평균불량률Text.Text = $"지난 7일 평균 불량률: {average7dayRate:F1}%";
                전일대비Text.Text = $"전일 대비: {(todayRate - yesterdayRate):F1}%";

                var records = await _dashboardService.GetInspectionRecordsByDateAsync(date);
                InspectionTable.ItemsSource = records;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"대시보드 데이터 로드 중 오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AnimateDefectDetectionChart(double percentage)
        {
            AnimateArc(DefectDetectionPath, DefectDetectionText, percentage);
        }

        private void AnimateDefectRateChart(double percentage)
        {
            AnimateArc(DefectRatePath, DefectRateText, percentage);
        }

        private void AnimateArc(Path targetPath, TextBlock targetText, double percentage)
        {
            percentage = Math.Min(percentage, 99.999); // prevent full circle overlap

            var anim = new DoubleAnimation(0, percentage, TimeSpan.FromMilliseconds(1000))
            {
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseOut }
            };

            var clock = anim.CreateClock();
            double current = 0;

            EventHandler handler = null;
            handler = (s, e) =>
            {
                if (clock.CurrentProgress.HasValue)
                {
                    current = percentage * clock.CurrentProgress.Value;

                    var arcPath = CreateArcPathData(current);
                    targetPath.Data = arcPath;
                    targetText.Text = $"{current:F0}%";
                }

                if (clock.CurrentState == ClockState.Stopped)
                    CompositionTarget.Rendering -= handler;
            };

            CompositionTarget.Rendering += handler;
        }

        private static PathGeometry CreateArcPathData(double percentage)
        {
            if (percentage <= 0)
                return Geometry.Empty as PathGeometry;

            double angleInDegrees = 360 * (percentage / 100);
            double angleInRadians = angleInDegrees * (Math.PI / 180);

            double radius = 80;
            Point center = new Point(100, 100);
            Point startPoint = new Point(center.X, center.Y - radius);

            double endX = center.X + radius * Math.Sin(angleInRadians);
            double endY = center.Y - radius * Math.Cos(angleInRadians);
            Point endPoint = new Point(endX, endY);

            PathFigure figure = new PathFigure { StartPoint = startPoint };
            ArcSegment arc = new ArcSegment
            {
                Point = endPoint,
                Size = new Size(radius, radius),
                SweepDirection = SweepDirection.Clockwise,
                IsLargeArc = angleInDegrees >= 180
            };

            figure.Segments.Add(arc);
            PathGeometry geometry = new PathGeometry();
            geometry.Figures.Add(figure);
            return geometry;
        }
    }
}
