using System.Windows;

namespace Traincrew_MultiATS_Server.LoadTest;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    private readonly ATS _ats = new();
    private Signal _signal = new();
    private TID _tid = new();

    public MainWindow()
    {
        InitializeComponent();
    }

    private void IncrementAtsCount(object sender, RoutedEventArgs e)
    {
        _ats.IncrementAndExecute();
        Value1.Text = _ats.GetCurrentCount().ToString();
    }

    private void DecrementAtsCount(object sender, RoutedEventArgs e)
    {
        _ats.DecrementAndCancel();
        Value1.Text = _ats.GetCurrentCount().ToString();
    }

    private void IncrementSignalCount(object sender, RoutedEventArgs e)
    {
        _signal.IncrementAndExecute(); 
        Value2.Text = _signal.GetCurrentCount().ToString();
    }

    private void DecrementSignalCount(object sender, RoutedEventArgs e)
    {
        _signal.DecrementAndCancel(); 
        Value2.Text = _signal.GetCurrentCount().ToString();
    }

    private void IncrementTidCount(object sender, RoutedEventArgs e)
    {
        _tid.IncrementAndExecute(); 
        Value3.Text = _tid.GetCurrentCount().ToString();
    }

    private void DecrementTidCount(object sender, RoutedEventArgs e)
    {
        _tid.DecrementAndCancel(); 
        Value3.Text = _tid.GetCurrentCount().ToString();
    }
}