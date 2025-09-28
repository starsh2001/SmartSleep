using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

namespace SmartSleep.App.Views;

public partial class TrayTooltipWindow : Window
{
    private bool _updatesFrozen = false;
    private bool _shouldRender = true;

    public TrayTooltipWindow()
    {
        InitializeComponent();
        // Don't set visibility here - let the service handle it
    }

    public void FreezeUpdates(bool freeze)
    {
        _updatesFrozen = freeze;
    }

    public void SetShouldRender(bool shouldRender)
    {
        _shouldRender = shouldRender;
        Visibility = shouldRender ? Visibility.Visible : Visibility.Hidden;
    }

    public bool ShouldRender => _shouldRender;

    public void UpdateLines(IReadOnlyList<(string Text, Brush Brush)> lines)
    {
        if (_updatesFrozen)
        {
            return;
        }

        ContentBlock.Inlines.Clear();
        if (lines.Count == 0)
        {
            ContentBlock.Inlines.Add(new Run(string.Empty));
        }
        else
        {
            for (var i = 0; i < lines.Count; i++)
            {
                var (text, brush) = lines[i];
                var run = new Run(text) { Foreground = brush };
                ContentBlock.Inlines.Add(run);
                if (i < lines.Count - 1)
                {
                    ContentBlock.Inlines.Add(new LineBreak());
                }
            }
        }

    }
}
