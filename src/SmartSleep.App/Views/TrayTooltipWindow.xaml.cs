using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

namespace SmartSleep.App.Views;

public partial class TrayTooltipWindow : Window
{
    public TrayTooltipWindow()
    {
        InitializeComponent();
    }

    public void UpdateLines(IReadOnlyList<(string Text, Brush Brush)> lines)
    {
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

        ContentBlock.UpdateLayout();
        UpdateLayout();
    }
}
