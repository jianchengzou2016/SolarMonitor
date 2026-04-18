using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace SolarMonitor.App;

public partial class MainWindow : Window
{
    private readonly ViewModels.MainViewModel _viewModel = new();
    private readonly DispatcherTimer _refreshTimer = new();
    private bool _isRefreshingRealtime;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
        _refreshTimer.Tick += RefreshTimer_Tick;
        _refreshTimer.Interval = _viewModel.RefreshInterval;
    }

    private async void LoadDevices_Click(object sender, RoutedEventArgs e)
    {
        await RunAsync(_viewModel.LoadDevicesAsync);
    }

    private async void LoadDetail_Click(object sender, RoutedEventArgs e)
    {
        await RunAsync(_viewModel.LoadDetailAsync);
    }

    private async void LoadRealtime_Click(object sender, RoutedEventArgs e)
    {
        await RunAsync(_viewModel.LoadRealtimeAsync);
        UpdateRefreshTimer();
    }

    private void RefreshInterval_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateRefreshTimer();
    }

    private void RefreshToggle_Changed(object sender, RoutedEventArgs e)
    {
        UpdateRefreshTimer();
    }

    private async Task RunAsync(Func<Task> action)
    {
        try
        {
            Mouse.OverrideCursor = Cursors.Wait;
            await action();
        }
        catch (FoxEss.FoxEssApiException ex)
        {
            MessageBox.Show(this, ex.Message, "FoxESS API error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "SolarMonitor", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    private async void RefreshTimer_Tick(object? sender, EventArgs e)
    {
        if (_isRefreshingRealtime || !_viewModel.IsAutoRefreshEnabled)
        {
            return;
        }

        _isRefreshingRealtime = true;
        try
        {
            await RunAsync(_viewModel.LoadRealtimeAsync);
        }
        finally
        {
            _isRefreshingRealtime = false;
        }
    }

    private void UpdateRefreshTimer()
    {
        _refreshTimer.Interval = _viewModel.RefreshInterval;

        if (_viewModel.IsAutoRefreshEnabled)
        {
            _refreshTimer.Start();
        }
        else
        {
            _refreshTimer.Stop();
        }
    }

    private void TrendChartOverlay_MouseMove(object sender, MouseEventArgs e)
    {
        if (_viewModel.TrendSamples.Count == 0 || sender is not FrameworkElement overlay)
        {
            HideTrendHover();
            return;
        }

        var mousePosition = e.GetPosition(overlay);
        var nearestIndex = GetNearestTrendIndex(mousePosition.X);
        if (nearestIndex < 0 || nearestIndex >= _viewModel.TrendSamples.Count)
        {
            HideTrendHover();
            return;
        }

        var sample = _viewModel.TrendSamples[nearestIndex];
        var pointX = GetTrendPointX(nearestIndex);

        TrendHoverGuide.Visibility = Visibility.Visible;
        TrendHoverGuide.X1 = pointX;
        TrendHoverGuide.X2 = pointX;

        TrendHoverText.Text =
            $"{sample.Timestamp:HH:mm}\n" +
            $"Home usage: {sample.HomeUsagePower:0.###} kW\n" +
            $"Derived PV output: {sample.DerivedPvOutputPower:0.###} kW\n" +
            $"Grid Import: {sample.GridImportPower:0.###} kW\n" +
            $"Grid Export: {sample.GridExportPower:0.###} kW";

        TrendHoverCard.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var desiredSize = TrendHoverCard.DesiredSize;
        var cardLeft = pointX + 12;
        var maxLeft = Math.Max(0, _viewModel.ChartCanvasWidth - desiredSize.Width - 8);
        if (cardLeft > maxLeft)
        {
            cardLeft = Math.Max(0, pointX - desiredSize.Width - 12);
        }

        var cardTop = 10d;
        var maxTop = Math.Max(0, _viewModel.ChartCanvasHeight - desiredSize.Height - 8);
        if (cardTop > maxTop)
        {
            cardTop = maxTop;
        }

        Canvas.SetLeft(TrendHoverCard, cardLeft);
        Canvas.SetTop(TrendHoverCard, cardTop);
        TrendHoverCard.Visibility = Visibility.Visible;
    }

    private void TrendChartOverlay_MouseLeave(object sender, MouseEventArgs e)
    {
        HideTrendHover();
    }

    private int GetNearestTrendIndex(double x)
    {
        var sampleCount = _viewModel.TrendSamples.Count;
        if (sampleCount == 0)
        {
            return -1;
        }

        if (sampleCount == 1)
        {
            return 0;
        }

        var normalized = Math.Clamp(x / Math.Max(1, _viewModel.ChartCanvasWidth), 0, 1);
        var index = (int)Math.Round(normalized * (sampleCount - 1), MidpointRounding.AwayFromZero);
        return Math.Clamp(index, 0, sampleCount - 1);
    }

    private double GetTrendPointX(int index)
    {
        var sampleCount = _viewModel.TrendSamples.Count;
        if (sampleCount <= 1)
        {
            return 0;
        }

        return index * (_viewModel.ChartCanvasWidth / (sampleCount - 1));
    }

    private void HideTrendHover()
    {
        TrendHoverGuide.Visibility = Visibility.Collapsed;
        TrendHoverCard.Visibility = Visibility.Collapsed;
    }
}
