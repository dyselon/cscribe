using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace InterpertedTextBlock
{
    /// <summary>
    /// Interaction logic for InterpertedTextBlock.xaml
    /// </summary>
    public partial class InterpertedTextBlock : UserControl
    {
        public InterpertedTextBlock()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            "Text",
            typeof(string),
            typeof(InterpertedTextBlock),
            new UIPropertyMetadata("Default", onTextPropertyChanged)
        );

        public string Text
        {
            get { return (string)this.GetValue(TextProperty); }
            set { this.SetValue(TextProperty, value); }
        }

        private static void onTextPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            InterpertedTextBlock block = (InterpertedTextBlock)sender;
            var text = block.TextBlock;
            var newtext = (string)args.NewValue;
            text.Inlines.Clear();
            var newcontent = block.ParseString(newtext);
            foreach (var inline in newcontent)
            {
                text.Inlines.Add(inline);
            }
            text.Inlines.Add(new Run("Test!"));
        }

        private struct ParseState
        {
            public bool Bold;
            public bool Italic;
        }

        private struct EscapeCharInfo
        {
            public char EscapeChar;
            public int Pos;
        }

        // I'm not sure there's a single thing I like about this function.
        private IEnumerable<Inline> ParseString(string text)
        {
            var output = new List<Inline>();

            var startingpos = 0;
            var parsestate = new ParseState();
            while (startingpos < text.Length)
            {
                // Find next escape char
                var info = FindNextEscapeChar(text, startingpos);

                var found = info.Pos != -1;
                var length = found ? info.Pos - startingpos : text.Length - startingpos;

                // If there's actually some text between here and the next escapechar/endofstring, add the run
                if (length > 0)
                {
                    Inline run = new Run(text.Substring(startingpos, length));
                    if (parsestate.Bold) { run = new Bold(run); }
                    if (parsestate.Italic) { run = new Italic(run); }
                    output.Add(run);
                }

                if (!found) { break; } // If we didn't find an escape character, we're done! Go us!

                // Handle escape based on type
                if (info.EscapeChar == '<')
                {
                    var close = text.IndexOf('>', info.Pos);
                    if (close >= 0)
                    {
                        var tag = text.Substring(info.Pos + 1, close - info.Pos - 1).ToLowerInvariant();
                        switch (tag)
                        {
                            case "b":
                                parsestate.Bold = true;
                                break;
                            case "i":
                                parsestate.Italic = true;
                                break;
                            case "/b":
                                parsestate.Bold = false;
                                break;
                            case "/i":
                                parsestate.Italic = false;
                                break;
                        }
                    }
                    startingpos = close + 1;
                }

                if (info.EscapeChar == '&') // Pretty sure this is fucked at the moment. So, there's that.
                {
                    var close = text.IndexOf(';', info.Pos);
                    if (close >= 0)
                    {
                        var tag = text.Substring(info.Pos + 1, close - info.Pos - 1).ToLowerInvariant();
                        var escapetext = "";
                        switch (tag)
                        {
                            case "lt":
                                escapetext = "<";
                                break;
                            case "gt":
                                escapetext = ">";
                                break;
                            case "lsb":
                                escapetext = "[";
                                break;
                            case "rsb":
                                escapetext = "]";
                                break;
                            case "amp":
                                escapetext = "&";
                                break;
                        }
                        Inline run = new Run(escapetext);
                        if (parsestate.Bold) { run = new Bold(run); }
                        if (parsestate.Italic) { run = new Italic(run); }
                        output.Add(run);
                    }
                    startingpos = close + 1;
                }

                if (info.EscapeChar == '[')
                {
                    var close = text.IndexOf(']', info.Pos);
                    if (close >= 0)
                    {
                        var tag = text.Substring(info.Pos + 1, close - info.Pos - 1);
                        var container = new InlineUIContainer();
                        var resource = RecursiveResourceLookup<UIElement>(tag, this);
                        if (resource != null)
                        {
                            // Stack Overflow tells me this is how to clone a control, so... uh...
                            string resourcexaml = XamlWriter.Save(resource);
                            var clone = (UIElement)XamlReader.Parse(resourcexaml);
                            container.Child = clone;
                            output.Add(container);
                        }
                        else { output.Add(new Run(String.Format("[[Resource {0} not found...]]", tag))); }
                        /*var resource = (UIElement)Resources[tag];
                        output.Add(new Run("![!"));
                        container.Child = resource != null ? resource : new TextBlock(new Run("Resource not found"));
                        output.Add(container);
                        output.Add(new Run("!]!"));*/
                        
                    }
                    startingpos = close + 1;
                }
            }

            return output;
        }

        private EscapeCharInfo FindNextEscapeChar(string text, int startingpos)
        {
            var info = new EscapeCharInfo();
            info.Pos = -1;

            // First look for a '<'
            var pos = text.IndexOf('<', startingpos);
            if (pos > info.Pos)
            {
                info.EscapeChar = '<';
                info.Pos = pos;
            }

            // Next look for a '['
            pos = text.IndexOf('[', startingpos);
            if (pos > info.Pos)
            {
                info.EscapeChar = '[';
                info.Pos = pos;
            }

            // Now a '&'
            pos = text.IndexOf('&', startingpos);
            if (pos > info.Pos)
            {
                info.EscapeChar = '&';
                info.Pos = pos;
            }

            return info;
        }

        private T RecursiveResourceLookup<T>(string key, FrameworkElement startingelement)
        {
            //Console.WriteLine("Looking for {0} in {1}", key, startingelement);
            T resource = (T)startingelement.TryFindResource(key);
            if (resource == null && startingelement.Parent != null)
            {
                return RecursiveResourceLookup<T>(key, (FrameworkElement)startingelement.Parent);
            }
            else
            {
                return resource;
            }
            
        }
    }
}
