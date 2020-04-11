using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SQLite;
using SQLiteNetExtensions.Extensions;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using Firefly;
using HtmlAgilityPack;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;

namespace MYTGS
{
    public partial class MainWindow
    {
        private void InitializeTasksDB(SQLiteConnection sqldb)
        {

            sqldb.CreateTable<FullTask>();
        }

        public List<FullTask> DBTaskSearch(SQLiteConnection sqldb, string criteria = "", string teacher = "", string id = "", string classcriteria = "", int orderby = 0, bool deleted = false, bool hidden = false, bool HideMarked = false)
        {
            //Input validation
            criteria = criteria.ToLower().Trim();
            teacher = teacher.ToLower().Trim();
            id = id.ToLower().Trim();
            classcriteria = classcriteria.ToLower().Trim();

            //Return the table in array form
            IEnumerable<FullTask> result = null;
            if (HideMarked)
            {
                result = sqldb.GetAllWithChildren<FullTask>(pv => pv.title.ToLower().Contains(criteria) && pv.deleted == deleted && pv.hideFromRecipients == hidden && pv.mark == 0).AsEnumerable();
            }
            else
            {
                result = sqldb.GetAllWithChildren<FullTask>(pv => pv.title.ToLower().Contains(criteria) && pv.deleted == deleted && pv.hideFromRecipients == hidden).AsEnumerable();
            }
            switch (orderby)
            {
                case 1:
                    //Oldest activity
                    result = result.OrderBy(pv => pv.LatestestActivity);

                    break;
                case 2:
                    //Latest due
                    result = result.OrderByDescending(pv => pv.dueDate);
                    break;
                case 3:
                    //Oldest due
                    result = result.OrderBy(pv => pv.dueDate);

                    break;
                case 4:
                    //latest set
                    result = result.OrderByDescending(pv => pv.setDate);

                    break;
                case 5:
                    //oldest set
                    result = result.OrderBy(pv => pv.setDate);

                    break;
                default:
                    //latest activity
                    result = result.OrderByDescending(pv => pv.LatestestActivity);

                    break;
            }


            List<FullTask> fullTasks = new List<FullTask>();
            if (teacher == "" && classcriteria == "")
            {
                foreach (var item in result)
                {
                    if (item.id.ToString().Contains(id))
                    {
                        FullTask k = item;
                        try
                        {
                            if (k.BlobPrincipal != null)
                                k.setter = JsonConvert.DeserializeObject<Principal>(k.BlobPrincipal);
                            if (k.BlobAddressees != null)
                                k.addressees = JsonConvert.DeserializeObject<Address[]>(k.BlobAddressees);
                            if (k.BlobCoowner != null)
                                k.coowners = JsonConvert.DeserializeObject<Principal[]>(k.BlobCoowner);
                            if (k.BlobRecipientStatuses != null)
                                k.recipientStatuses = JsonConvert.DeserializeObject<RecipientResponse[]>(k.BlobRecipientStatuses);
                            if (k.BloballRecipients != null)
                                k.allRecipientsResponses = JsonConvert.DeserializeObject<RecipientResponse[]>(k.BloballRecipients);
                            if (k.BlobDescription != null)
                                k.descriptionDetails = JsonConvert.DeserializeObject<DescriptionDetails>(k.BlobDescription);
                            if (k.BlobFileAttachment != null)
                                k.fileAttachments = JsonConvert.DeserializeObject<FileAttachments[]>(k.BlobFileAttachment);
                            if (k.BlobPageAttachment != null)
                                k.pageAttachments = JsonConvert.DeserializeObject<PageAttachments[]>(k.BlobPageAttachment);
                            if (k.BlobRecipients != null)
                                k.recipientsResponses = JsonConvert.DeserializeObject<RecipientResponse[]>(k.BlobRecipients);
                            k.ClassKeys = new List<string>();
                            if (k.addressees != null)
                            {
                                foreach (Address classitem in k.addressees)
                                {
                                    if (classitem.isGroup && classitem.principal.name != null)
                                    {
                                        var reg = new Regex(@"^Class");
                                        k.ClassKeys.Add(reg.Replace(classitem.principal.name, string.Empty).Trim());
                                    }
                                }
                            }
                        }
                        catch
                        {
                            //errored out
                        }
                        fullTasks.Add(k);
                    }
                }
            }
            else if (teacher == "")
            {
                Console.WriteLine("running simple class check " + result.Count());
                foreach (var item in result)
                {
                    Address[] addresses = new Address[0];
                    try
                    {
                        if (item.BlobAddressees != null)
                            addresses = JsonConvert.DeserializeObject<Address[]>(item.BlobAddressees);
                    }
                    catch
                    {
                        //do nothing
                        addresses = new Address[0];
                    }
                    
                    List<string> classkeys = new List<string>();
                    foreach (Address classitem in addresses)
                    {
                        if (classitem.isGroup && classitem.principal.name != null)
                        {
                            var reg = new Regex(@"^Class");
                            classkeys.Add(reg.Replace(classitem.principal.name, string.Empty).Trim());
                        }
                    }

                    if (item.id.ToString().Contains(id))
                    {
                        bool HasClass = false;
                        foreach (string clss in classkeys)
                        {
                            if (clss.ToLower().Contains(classcriteria))
                            {
                                HasClass = true;
                                break;
                            }
                        }
                        if (HasClass == false)
                        {
                            //Omit result
                            continue;
                        }

                        FullTask k = item;
                        k.ClassKeys = classkeys;
                        k.addressees = addresses;
                        try
                        {
                            if (k.BlobPrincipal != null)
                                k.setter = JsonConvert.DeserializeObject<Principal>(k.BlobPrincipal);
                            if (k.BlobCoowner != null)
                                k.coowners = JsonConvert.DeserializeObject<Principal[]>(k.BlobCoowner);
                            if (k.BlobRecipientStatuses != null)
                                k.recipientStatuses = JsonConvert.DeserializeObject<RecipientResponse[]>(k.BlobRecipientStatuses);
                            if (k.BloballRecipients != null)
                                k.allRecipientsResponses = JsonConvert.DeserializeObject<RecipientResponse[]>(k.BloballRecipients);
                            if (k.BlobDescription != null)
                                k.descriptionDetails = JsonConvert.DeserializeObject<DescriptionDetails>(k.BlobDescription);
                            if (k.BlobFileAttachment != null)
                                k.fileAttachments = JsonConvert.DeserializeObject<FileAttachments[]>(k.BlobFileAttachment);
                            if (k.BlobPageAttachment != null)
                                k.pageAttachments = JsonConvert.DeserializeObject<PageAttachments[]>(k.BlobPageAttachment);
                            if (k.BlobRecipients != null)
                                k.recipientsResponses = JsonConvert.DeserializeObject<RecipientResponse[]>(k.BlobRecipients);
                        }
                        catch
                        {
                            //errored out
                        }
                        fullTasks.Add(k);
                    }
                }
            }
            else if (classcriteria == "")
            {
                foreach (var item in result)
                {

                    Principal tt = new Principal();
                    try
                    {
                        if (item.BlobPrincipal != null)
                            tt = JsonConvert.DeserializeObject<Principal>(item.BlobPrincipal);
                    }
                    catch
                    {
                        //do nothing
                    }

                    if (tt.name != null && tt.name.ToLower().Contains(teacher) && item.id.ToString().Contains(id))
                    {

                        FullTask k = item;
                        try
                        {
                            if (k.BlobPrincipal != null)
                                k.setter = JsonConvert.DeserializeObject<Principal>(k.BlobPrincipal);
                            if (k.BlobAddressees != null)
                                k.addressees = JsonConvert.DeserializeObject<Address[]>(k.BlobAddressees);
                            if (k.BlobCoowner != null)
                                k.coowners = JsonConvert.DeserializeObject<Principal[]>(k.BlobCoowner);
                            if (k.BlobRecipientStatuses != null)
                                k.recipientStatuses = JsonConvert.DeserializeObject<RecipientResponse[]>(k.BlobRecipientStatuses);
                            if (k.BloballRecipients != null)
                                k.allRecipientsResponses = JsonConvert.DeserializeObject<RecipientResponse[]>(k.BloballRecipients);
                            if (k.BlobDescription != null)
                                k.descriptionDetails = JsonConvert.DeserializeObject<DescriptionDetails>(k.BlobDescription);
                            if (k.BlobFileAttachment != null)
                                k.fileAttachments = JsonConvert.DeserializeObject<FileAttachments[]>(k.BlobFileAttachment);
                            if (k.BlobPageAttachment != null)
                                k.pageAttachments = JsonConvert.DeserializeObject<PageAttachments[]>(k.BlobPageAttachment);
                            if (k.BlobRecipients != null)
                                k.recipientsResponses = JsonConvert.DeserializeObject<RecipientResponse[]>(k.BlobRecipients);
                            k.ClassKeys = new List<string>();
                            if (k.addressees != null)
                            {
                                foreach (Address classitem in k.addressees)
                                {
                                    if (classitem.isGroup && classitem.principal.name != null)
                                    {
                                        var reg = new Regex(@"^Class");
                                        k.ClassKeys.Add(reg.Replace(classitem.principal.name, string.Empty).Trim());
                                    }
                                }
                            }
                        }
                        catch
                        {
                            //errored out
                        }
                        fullTasks.Add(k);
                    }
                }
            }
            else 
            {

                foreach (var item in result)
                {
                    Address[] addresses = new Address[0];
                    try
                    {
                        if (item.BlobAddressees != null)
                            addresses = JsonConvert.DeserializeObject<Address[]>(item.BlobAddressees);
                    }
                    catch
                    {
                        //do nothing
                        addresses = new Address[0];
                    }

                    Principal tt = new Principal();
                    try
                    {
                        if (item.BlobPrincipal != null)
                            tt = JsonConvert.DeserializeObject<Principal>(item.BlobPrincipal);
                    }
                    catch
                    {
                        //do nothing
                    }

                    List<string> ClassKeys = new List<string>();

                    foreach (Address classitem in addresses)
                    {
                        if (classitem.isGroup && classitem.principal.name != null)
                        {
                            var reg = new Regex(@"^Class");
                            ClassKeys.Add(reg.Replace(classitem.principal.name, string.Empty).Trim());
                        }
                    }

                    if (tt.name != null && tt.name.ToLower().Contains(teacher) && item.id.ToString().Contains(id))
                    {
                        bool HasClass = false;
                        foreach(string clss in ClassKeys)
                        {
                            if (clss.ToLower().Contains(classcriteria))
                            {
                                HasClass = true;
                                break;
                            }
                        }
                        if (HasClass == false)
                        {
                            //Omit result
                            continue;
                        }

                        FullTask k = item;
                        k.ClassKeys = ClassKeys;
                        k.addressees = addresses;
                        try
                        {
                            k.setter = tt;
                            if (k.BlobAddressees != null)
                                k.addressees = JsonConvert.DeserializeObject<Address[]>(k.BlobAddressees);
                            if (k.BlobCoowner != null)
                                k.coowners = JsonConvert.DeserializeObject<Principal[]>(k.BlobCoowner);
                            if (k.BlobRecipientStatuses != null)
                                k.recipientStatuses = JsonConvert.DeserializeObject<RecipientResponse[]>(k.BlobRecipientStatuses);
                            if (k.BloballRecipients != null)
                                k.allRecipientsResponses = JsonConvert.DeserializeObject<RecipientResponse[]>(k.BloballRecipients);
                            if (k.BlobDescription != null)
                                k.descriptionDetails = JsonConvert.DeserializeObject<DescriptionDetails>(k.BlobDescription);
                            if (k.BlobFileAttachment != null)
                                k.fileAttachments = JsonConvert.DeserializeObject<FileAttachments[]>(k.BlobFileAttachment);
                            if (k.BlobPageAttachment != null)
                                k.pageAttachments = JsonConvert.DeserializeObject<PageAttachments[]>(k.BlobPageAttachment);
                            if (k.BlobRecipients != null)
                                k.recipientsResponses = JsonConvert.DeserializeObject<RecipientResponse[]>(k.BlobRecipients);
                        }
                        catch
                        {
                            //errored out
                        }
                        fullTasks.Add(k);
                    }
                }
            }

            return fullTasks;

            //pv.title.ToLower().Contains(criteria) &&
            //pv.setter.name.ToLower().Contains(teacher) &&
            //pv.id.ToString().Contains(id)
        }

        //Function to call to switch to the task page and update its elements
        private void GotoTaskpage()
        {
            if (TaskStack.SelectedItem is Firefly.FullTask)
            {
                TaskTitleText.Text = ((Firefly.FullTask)TaskStack.SelectedItem).title;
                TaskSetByText.Text = ((Firefly.FullTask)TaskStack.SelectedItem).setter.name;
                TaskSetLbl.Content = ((Firefly.FullTask)TaskStack.SelectedItem).setDate.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");
                TaskDueLbl.Content = ((Firefly.FullTask)TaskStack.SelectedItem).dueDate.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");
                TaskIdText.Text = ((Firefly.FullTask)TaskStack.SelectedItem).id.ToString();
                TaskClassKeysListBox.ItemsSource = ((Firefly.FullTask)TaskStack.SelectedItem).ClassKeys;
                TaskHyperLink.NavigateUri = new Uri(FF.SchoolUrl + "/set-tasks/" + ((Firefly.FullTask)TaskStack.SelectedItem).id);

                TaskStackPanel.Children.Clear();
                if (((Firefly.FullTask)TaskStack.SelectedItem).descriptionDetails.htmlContent != null)
                {
                    try
                    {
                        HtmlDocument html = new HtmlDocument();
                        html.LoadHtml(((Firefly.FullTask)TaskStack.SelectedItem).descriptionDetails.htmlContent);
                        foreach (HtmlNode item in html.DocumentNode.ChildNodes)
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
                                        TaskStackPanel.Children.Add(text);
                                        break;
                                    case "h2":
                                        //size 26px
                                        text.FontSize = 26;
                                        PopulateTextblockHtmlToXaml(item, ref text);
                                        TaskStackPanel.Children.Add(text);
                                        break;
                                    case "h3":
                                        //size 24px
                                        text.FontSize = 24;
                                        PopulateTextblockHtmlToXaml(item, ref text);
                                        TaskStackPanel.Children.Add(text);
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
                                            TaskStackPanel.Children.Add(border);
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
                                            TaskStackPanel.Children.Add(border);
                                        }
                                        else
                                        {
                                            PopulateTextblockHtmlToXaml(item, ref text);
                                            TaskStackPanel.Children.Add(text);
                                        }
                                        break;
                                }
                            }
                            catch
                            {
                                //Do Nothing
                            }
                        }
                    }
                    catch
                    {
                        //Do nothing
                    }
                }
                TaskStackPanel.Children.Add(new Separator());

                try
                {
                    if (((Firefly.FullTask)TaskStack.SelectedItem).recipientsResponses.Count() > 0)
                    {
                        foreach (Firefly.Response item in ((Firefly.FullTask)TaskStack.SelectedItem).recipientsResponses.First().responses.OrderByDescending(pv => pv.sentTimestamp))
                        {
                            try
                            {
                                ResponseControl resp = new ResponseControl();
                                resp.Datet = item.sentTimestamp.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");
                                switch (item.eventType)
                                {
                                    case "set-task":
                                        resp.Titlet = item.authorName + " set task";
                                        TaskStackPanel.Children.Add(resp);
                                        break;
                                    case "archive-task":
                                        resp.Titlet = item.authorName + " archived task";
                                        TaskStackPanel.Children.Add(resp);
                                        break;
                                    case "add-file":
                                        resp.Titlet = item.authorName + " added a file";
                                        TaskStackPanel.Children.Add(resp);
                                        break;
                                    case "mark-as-done":
                                        resp.Titlet = item.authorName + " marked as done";
                                        TaskStackPanel.Children.Add(resp);
                                        break;
                                    case "mark-as-undone":
                                        resp.Titlet = item.authorName + " marked as undone";
                                        TaskStackPanel.Children.Add(resp);
                                        break;
                                    case "mark-and-grade":
                                        resp.Titlet = item.authorName + " added a mark";
                                        Border bb = new Border();
                                        Label text = new Label();
                                        text.Foreground = Brushes.White;
                                        bb.CornerRadius = new CornerRadius(5);
                                        bb.Background = new SolidColorBrush(Color.FromRgb(89, 121, 146));
                                        bb.Child = text;
                                        bb.HorizontalAlignment = HorizontalAlignment.Left;
                                        bb.VerticalAlignment = VerticalAlignment.Top;
                                        bb.MaxHeight = 50;
                                        bb.MaxWidth = 200;
                                        if (item.message != null)
                                        {
                                            Grid grid2 = new Grid();
                                            grid2.ColumnDefinitions.Add(new ColumnDefinition());
                                            grid2.ColumnDefinitions.Add(new ColumnDefinition());
                                            TextBlock textbl2 = new TextBlock();
                                            textbl2.TextWrapping = TextWrapping.Wrap;
                                            textbl2.Text = item.message;
                                            textbl2.SetValue(Grid.ColumnProperty, 1);
                                            grid2.Children.Add(textbl2);
                                            grid2.Children.Add(bb);
                                            resp.Content = grid2;
                                        }
                                        else
                                        {
                                            resp.Content = bb;
                                        }

                                        if (((Firefly.FullTask)TaskStack.SelectedItem).totalMarkOutOf != 0)
                                        {
                                            text.Content = item.mark + "/" + ((Firefly.FullTask)TaskStack.SelectedItem).totalMarkOutOf + "  " + Math.Round(item.mark * 100 / ((Firefly.FullTask)TaskStack.SelectedItem).totalMarkOutOf) + "%";
                                            TaskStackPanel.Children.Add(resp);
                                        }
                                        else if (item.outOf != 0)
                                        {
                                            text.Content = item.mark + "/" + item.outOf + "  " + Math.Round(item.mark * 100 / item.outOf) + "%";
                                            TaskStackPanel.Children.Add(resp);
                                        }
                                        else if (item.taskAssessmentDetails.assessmentMarkMax != 0)
                                        {
                                            text.Content = item.mark + "/" + item.taskAssessmentDetails.assessmentMarkMax + "  " + Math.Round(item.mark * 100 / item.taskAssessmentDetails.assessmentMarkMax) + "%";
                                            TaskStackPanel.Children.Add(resp);
                                        }

                                        break;
                                    case "comment":
                                        resp.Titlet = item.authorName + " added a comment";
                                        TextBlock textbl = new TextBlock();
                                        textbl.TextWrapping = TextWrapping.Wrap;
                                        if (item.message != null)
                                            textbl.Text = item.message;
                                        resp.Content = textbl;
                                        TaskStackPanel.Children.Add(resp);
                                        break;
                                    default:
                                        break;
                                }
                            }
                            catch
                            {
                                //Do nothing
                            }
                        }
                    }
                }
                catch
                {
                    //Do nothing
                }

            }
            MainTabControl.SelectedIndex = 5;
        }

    }
}
