using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;
using ThorFlasher.Core.Models;

namespace ThorFlasher.UI.Views;

public static class RichTextBoxLogBehavior
{
    public static readonly DependencyProperty EntriesProperty =
        DependencyProperty.RegisterAttached(
            "Entries",
            typeof(IEnumerable<LogEntry>),
            typeof(RichTextBoxLogBehavior),
            new PropertyMetadata(null, OnEntriesChanged));

    private static readonly DependencyProperty SubscriptionStateProperty =
        DependencyProperty.RegisterAttached(
            "SubscriptionState",
            typeof(SubscriptionState),
            typeof(RichTextBoxLogBehavior),
            new PropertyMetadata(null));

    public static void SetEntries(DependencyObject element, IEnumerable<LogEntry>? value)
    {
        element.SetValue(EntriesProperty, value);
    }

    public static IEnumerable<LogEntry>? GetEntries(DependencyObject element)
    {
        return (IEnumerable<LogEntry>?)element.GetValue(EntriesProperty);
    }

    private static void OnEntriesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not RichTextBox richTextBox)
        {
            return;
        }

        var state = GetOrCreateSubscriptionState(richTextBox);
        state.Detach();

        if (e.NewValue is IEnumerable<LogEntry> entries)
        {
            state.Attach(richTextBox, entries);
            RebuildDocument(richTextBox, entries);
            return;
        }

        richTextBox.Document = CreateDocument();
    }

    private static SubscriptionState GetOrCreateSubscriptionState(RichTextBox richTextBox)
    {
        if (richTextBox.GetValue(SubscriptionStateProperty) is SubscriptionState state)
        {
            return state;
        }

        state = new SubscriptionState();
        richTextBox.SetValue(SubscriptionStateProperty, state);
        return state;
    }

    private static FlowDocument CreateDocument()
    {
        return new FlowDocument
        {
            PagePadding = new Thickness(0),
            TextAlignment = TextAlignment.Left
        };
    }

    private static void RebuildDocument(RichTextBox richTextBox, IEnumerable<LogEntry> entries)
    {
        var document = CreateDocument();
        foreach (var entry in entries)
        {
            document.Blocks.Add(CreateParagraph(entry));
        }

        richTextBox.Document = document;
        TryAutoScroll(richTextBox);
    }

    private static void AppendEntries(RichTextBox richTextBox, IEnumerable<LogEntry> entries)
    {
        if (richTextBox.Document is null)
        {
            richTextBox.Document = CreateDocument();
        }

        foreach (var entry in entries)
        {
            richTextBox.Document.Blocks.Add(CreateParagraph(entry));
        }

        TryAutoScroll(richTextBox);
    }

    private static Paragraph CreateParagraph(LogEntry entry)
    {
        return new Paragraph(new Run(entry.DisplayText))
        {
            Margin = new Thickness(0),
            Foreground = LogEntryTextStyle.GetForegroundBrush(entry.Level)
        };
    }

    private static void TryAutoScroll(RichTextBox richTextBox)
    {
        richTextBox.Dispatcher.BeginInvoke(
            DispatcherPriority.Background,
            new Action(() =>
            {
                if (!richTextBox.IsKeyboardFocusWithin)
                {
                    richTextBox.ScrollToEnd();
                }
            }));
    }

    private sealed class SubscriptionState
    {
        private INotifyCollectionChanged? _collection;
        private NotifyCollectionChangedEventHandler? _handler;

        public void Attach(RichTextBox richTextBox, IEnumerable<LogEntry> entries)
        {
            if (entries is not INotifyCollectionChanged observable)
            {
                return;
            }

            _handler = (_, args) =>
            {
                richTextBox.Dispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(() => HandleCollectionChanged(richTextBox, args)));
            };

            _collection = observable;
            _collection.CollectionChanged += _handler;
        }

        public void Detach()
        {
            if (_collection is not null && _handler is not null)
            {
                _collection.CollectionChanged -= _handler;
            }

            _collection = null;
            _handler = null;
        }

        private static void HandleCollectionChanged(RichTextBox richTextBox, NotifyCollectionChangedEventArgs args)
        {
            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Add when args.NewItems is not null:
                    AppendEntries(richTextBox, args.NewItems.OfType<LogEntry>());
                    break;
                case NotifyCollectionChangedAction.Reset:
                    RebuildDocument(richTextBox, GetEntries(richTextBox) ?? Array.Empty<LogEntry>());
                    break;
                default:
                    RebuildDocument(richTextBox, GetEntries(richTextBox) ?? Array.Empty<LogEntry>());
                    break;
            }
        }
    }
}
