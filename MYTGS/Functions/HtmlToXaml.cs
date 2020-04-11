using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;
using SQLiteNetExtensions.Extensions;
using Firefly;
using System.Windows.Controls;
using HtmlAgilityPack;
using System.Web;
using System.Xaml;
using System.Text.RegularExpressions;
using System.Windows.Documents;
using System.Windows;

namespace MYTGS
{
    public partial class MainWindow
    {
        //Will convert the html of the dashboard message to a similar grid
        //<div[\S\s]*?data-ff-component-type="html"[\S\s]*?>([\S\s]*?)<\/div>
        private void DashboardMessageToXaml(HtmlNode html)
        {

            DashboardMessagePanel.Children.Clear();

            bool errors = false;
            try
            {
                foreach (HtmlNode item in html.ChildNodes)
                {
                    try
                    {
                        TextBlock text = new TextBlock();
                        text.FontSize = 16;
                        text.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0xFF, 102, 102, 102));
                        text.FontFamily = new System.Windows.Media.FontFamily("Source Sans Pro");
                        text.TextWrapping = TextWrapping.Wrap;
                        switch (item.Name)
                        {
                            case "h1":
                                //size 36px
                                text.FontSize = 36;
                                PopulateTextblockHtmlToXaml(item, ref text);
                                DashboardMessagePanel.Children.Add(text);
                                break;
                            case "h2":
                                //size 26px
                                text.FontSize = 26;
                                PopulateTextblockHtmlToXaml(item, ref text);
                                DashboardMessagePanel.Children.Add(text);
                                break;
                            case "h3":
                                //size 24px
                                text.FontSize = 24;
                                PopulateTextblockHtmlToXaml(item, ref text);
                                DashboardMessagePanel.Children.Add(text);
                                break;
                            case "p":
                                //size 16px
                                if (item.HasClass("ff-style-highlight"))
                                {
                                    Border border = new Border();
                                    border.CornerRadius = new CornerRadius(10);
                                    border.Padding = new Thickness(5);
                                    border.VerticalAlignment = VerticalAlignment.Top;
                                    border.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0xFF, 0xFD, 0xEB, 0x75));
                                    Grid grid = new Grid();
                                    border.Child = grid;
                                    FontAwesome5.SvgAwesome icon = new FontAwesome5.SvgAwesome();
                                    icon.Icon = FontAwesome5.EFontAwesomeIcon.Solid_Star;
                                    icon.Height = 30;
                                    icon.HorizontalAlignment = HorizontalAlignment.Left;
                                    icon.VerticalAlignment = VerticalAlignment.Top;
                                    icon.Foreground = System.Windows.Media.Brushes.White;
                                    grid.Children.Add(icon);
                                    PopulateTextblockHtmlToXaml(item, ref text);
                                    text.Margin = new Thickness(40, 0, 0, 0);
                                    text.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0xFF, 0x7e, 0x6d, 0x02));
                                    grid.Children.Add(text);
                                    DashboardMessagePanel.Children.Add(border);
                                }
                                else if (item.HasClass("ff-style-guidance"))
                                {
                                    Border border = new Border();
                                    border.CornerRadius = new CornerRadius(10);
                                    border.Padding = new Thickness(5);
                                    border.VerticalAlignment = VerticalAlignment.Top;
                                    border.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0xFF, 0xB3, 0xEC, 0xCE));
                                    Grid grid = new Grid();
                                    border.Child = grid;
                                    FontAwesome5.SvgAwesome icon = new FontAwesome5.SvgAwesome();
                                    icon.Icon = FontAwesome5.EFontAwesomeIcon.Solid_Shapes;
                                    icon.Height = 30;
                                    icon.HorizontalAlignment = HorizontalAlignment.Left;
                                    icon.VerticalAlignment = VerticalAlignment.Top;
                                    icon.Foreground = System.Windows.Media.Brushes.White;
                                    grid.Children.Add(icon);
                                    PopulateTextblockHtmlToXaml(item, ref text);
                                    text.Margin = new Thickness(40, 0, 0, 0);
                                    text.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0xFF, 0x18, 0x66, 0x3e));
                                    grid.Children.Add(text);
                                    DashboardMessagePanel.Children.Add(border);
                                }
                                else
                                {
                                    PopulateTextblockHtmlToXaml(item, ref text);
                                    DashboardMessagePanel.Children.Add(text);
                                }
                                break;
                        }
                    }
                    catch
                    {
                        errors = true;
                    }
                }
            }
            catch
            {
                Label lbl = new Label();
                lbl.Content = "**Unable to process Dashboard Message";
                lbl.Foreground = System.Windows.Media.Brushes.Red;
                lbl.FontSize = 16;
                DashboardMessagePanel.Children.Insert(0, lbl);
            }
            if (errors)
            {
                Label lbl = new Label();
                lbl.Content = "**Error Processing Dashboard - Parts may be missing";
                lbl.Foreground = System.Windows.Media.Brushes.Red;
                lbl.FontSize = 16;
                DashboardMessagePanel.Children.Insert(0, lbl);
            }

            return;
        }

        private void PopulateTextblockHtmlToXaml(HtmlNode item, ref TextBlock textbl)
        {
            HtmlNode previous = null;
            foreach (HtmlNode item2 in item.Descendants("#text"))
            {
                bool isBold = IsBold(item, item2);
                bool isUnderlined = IsUnderlined(item, item2);
                string isHyperlink = IsAnchor(item, item2, FF.SchoolUrl);
                string text = Regex.Replace(item2.InnerText, @"\t|\r\n|\n|\r", " ");
                text = HttpUtility.HtmlDecode(text);

                HtmlNode reverseitem = item2;
                while(reverseitem!= previous){
                    if (reverseitem.Name == "br")
                    {
                        textbl.Inlines.Add(Environment.NewLine);
                    }
                    reverseitem = reverseitem.PreviousSibling;
                    if (reverseitem == null)
                        break;
                }
                previous = item2;
                
                if (isHyperlink != null)
                {
                    Hyperlink link = new Hyperlink();
                    link.RequestNavigate += Hyperlink_RequestNavigate;
                    link.NavigateUri = new Uri(isHyperlink);
                    if (isBold && isUnderlined)
                    {
                        link.Inlines.Add(new Run(text) { FontWeight = FontWeights.Bold, TextDecorations = TextDecorations.Underline });
                    }
                    else if (isBold)
                    {
                        link.Inlines.Add(new Run(text) { FontWeight = FontWeights.Bold });
                    }
                    else if (isUnderlined)
                    {
                        link.Inlines.Add(new Run(text) { TextDecorations = TextDecorations.Underline });
                    }
                    else
                    {
                        link.Inlines.Add(text);
                    }
                    textbl.Inlines.Add(link);
                }
                else
                {
                    if (isBold && isUnderlined)
                    {
                        textbl.Inlines.Add(new Run(text) { FontWeight = FontWeights.Bold, TextDecorations = TextDecorations.Underline });
                    }
                    else if (isBold)
                    {
                        textbl.Inlines.Add(new Run(text) { FontWeight = FontWeights.Bold });
                    }
                    else if (isUnderlined)
                    {
                        textbl.Inlines.Add(new Run(text) { TextDecorations = TextDecorations.Underline });
                    }
                    else
                    {
                        textbl.Inlines.Add(text);
                    }
                }

                
            }
        }

        private bool IsBold(HtmlNode Parent, HtmlNode Child)
        {
            while (Parent != Child)
            {
                if(Child.Name == "b")
                {
                    return true;
                }
                else
                {
                    if (Child.ParentNode == null)
                    {
                        return false;
                    }
                    Child = Child.ParentNode;
                }
            }
            return false;
        }

        private string IsAnchor(HtmlNode Parent, HtmlNode Child, string schooluri)
        {
            while (Parent != Child)
            {
                if (Child.Name == "a" && Child.Attributes["href"] != null)
                {
                    string anchorurl = Child.Attributes["href"].Value;
                    if (anchorurl.StartsWith("../"))
                    {
                        anchorurl = schooluri + anchorurl.Substring(2);
                    }
                    else if (anchorurl.StartsWith("http://") || anchorurl.StartsWith("https://"))
                    {
                        return anchorurl;
                    }
                    else if (anchorurl.StartsWith("/"))
                    {
                        return schooluri + anchorurl;
                    }
                    else
                    {
                        return schooluri + "/" + anchorurl;
                    }
                }
                else
                {
                    if (Child.ParentNode == null)
                    {
                        return null;
                    }
                    Child = Child.ParentNode;
                }
            }
            return null;
        }

        private bool IsUnderlined(HtmlNode Parent, HtmlNode Child)
        {
            while (Parent != Child)
            {
                if (Child.Name == "u")
                {
                    return true;
                }
                else
                {
                    if (Child.Attributes["style"] != null)
                    {
                        if (Child.Attributes["style"].Value.Contains("text-decoration-line: underline;"))
                        {
                            return true;
                        }
                    }
                    if (Child.ParentNode == null)
                    {
                        return false;
                    }
                    Child = Child.ParentNode;
                }
            }
            return false;
        }

        private string cleanHtml(string input)
        {
            //NavigateUri="https://github.com/Torca2001/MYTGS" TargetName="Github" RequestNavigate="Hyperlink_RequestNavigate" for hyperlink
            string output = Regex.Replace(input, @"<h2[\s\S]*?>", @"<TextBlock FontFamily=""Source Sans Pro"" FontSize=""26"" HorizontalAlignment=""Left"" VerticalAlignment=""Top"">");
            output = Regex.Replace(output, @"<h1[\s\S]*?>", @"<TextBlock FontFamily=""Source Sans Pro"" FontSize=""36"" HorizontalAlignment=""Left"" VerticalAlignment=""Top"">");
            output = Regex.Replace(output, @"<h3[\s\S]*?>", @"<TextBlock FontFamily=""Source Sans Pro"" FontSize=""24"" HorizontalAlignment=""Left"" VerticalAlignment=""Top"">");
            output = Regex.Replace(output, @"<h[\s\S]*?>", @"<TextBlock FontFamily=""Source Sans Pro"" FontSize=""16"" HorizontalAlignment=""Left"" VerticalAlignment=""Top"">");
            output = Regex.Replace(output, @"<\/h[1-9]>", @"</TextBlock>");
            foreach (Match item in Regex.Matches(output, @"<p.*?class=""(.*?)"".*?>([\S\s]*?)<\/p>"))
            {
                string newline = "";
                switch (item.Groups[1].Value)
                {
                    case "ff-style-highlight":
                        newline = @"<Border Background=""#FFFDEB75"" CornerRadius=""10"" Padding=""5"" VerticalAlignment=""Top"">
                            <Grid>
                                <fa:SvgAwesome Icon=""Solid_Star"" Height=""30"" HorizontalAlignment=""Left"" VerticalAlignment=""Top"" Foreground=""White"" RenderTransformOrigin=""0.5,0.5"">
                                    <fa:SvgAwesome.RenderTransform>
                                        <TransformGroup>
                                            <ScaleTransform/>
                                            <SkewTransform/>
                                            <RotateTransform Angle=""-20""/>
                                            <TranslateTransform/>
                                        </TransformGroup>
                                    </fa:SvgAwesome.RenderTransform>
                                </fa:SvgAwesome>
                                <TextBlock Margin=""50,0,0,0"" FontFamily=""Source Sans Pro"" FontSize=""16"" TextWrapping=""Wrap"">
                                " + item.Groups[2].Value + @"
                                </TextBlock>
                            </Grid>
                        </Border>";
                        break;
                    case "ff-style-guidance":
                        newline = @"<Border Background=""#FFB3ECCE"" CornerRadius=""10"" Padding=""5"" VerticalAlignment=""Top"">
                            <Grid>
                                <fa:SvgAwesome Icon=""Solid_Shapes"" Height=""30"" HorizontalAlignment=""Left"" VerticalAlignment=""Top"" Foreground=""White"" />
                                <TextBlock Margin=""50,0,0,0"" FontFamily=""Source Sans Pro"" FontSize=""16"" TextWrapping=""Wrap"">
                                " + item.Groups[2].Value + @"
                                </TextBlock>
                            </Grid>
                        </Border>";
                        break;
                    default:
                        newline = @"<Border Background=""#FFECE4B3"" CornerRadius=""10"" Padding=""5"" VerticalAlignment=""Top"">
                            <Grid>
                                <fa:SvgAwesome Icon=""Solid_ExclamationCircle"" Height=""30"" HorizontalAlignment=""Left"" VerticalAlignment=""Top"" Foreground=""White"" />
                                <TextBlock Margin=""50,0,0,0"" FontFamily=""Source Sans Pro"" FontSize=""16"" TextWrapping=""Wrap"">
                                " + item.Groups[2].Value + @"
                                </TextBlock>
                            </Grid>
                        </Border>";
                        break;
                }
                output = output.Replace(item.Value, newline);
            }


            foreach (Match item in Regex.Matches(output, @"<b.*?text-decoration-line: underline.*?>([\S\s]*?)<\/b>"))
            {
                output = output.Replace(item.Value, @"<Bold><Underline>" + item.Groups[1] + @"</Underline></Bold>");
            }


            output = Regex.Replace(output, @"<b.*?>", @"<Bold>");
            output = Regex.Replace(output, @"<\/b>", @"</Bold>");
            output = Regex.Replace(output, @"<u.*?>", @"<Underline>");
            output = Regex.Replace(output, @"<\/u>", @"</Underline>");

            return output;
        }

    }
}
